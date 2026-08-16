namespace LAB2D.Gameplay
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using Character = LAB2D.Character.Character;

    /// <summary>
    /// 温度玩法影响服务接口。
    /// </summary>
    public interface ITemperatureEffectService
    {
        /// <summary>当前平滑后的室外温度（℃）。</summary>
        float GetOutdoorTemperature();

        /// <summary>获取某地图格的温度：房间内→房间实时温度，野外→室外温度。</summary>
        float GetTemperatureAt(Vector3Int posMap);

        /// <summary>获取角色移动速度倍率（按角色位置温度）。</summary>
        float GetCharacterMoveSpeedMultiplier(Character character);

        /// <summary>获取 Worker 疲劳消耗倍率（按角色位置温度）。</summary>
        float GetWorkerFatigueDecayMultiplier(AWorker worker);

        /// <summary>套用温度倍率后的移动速度。</summary>
        float GetAdjustedCharacterMoveSpeed(Character character, float baseSpeed);
    }

    /// <summary>
    /// 温度玩法管理器。
    /// 温度来源 = 季节基础 + 天气偏移 + 昼夜波动；房间温度 = 室外 + 保温 + 供暖功率。
    /// 每帧平滑室外温度，定时扫描房间热源刷新房间温度，缓存角色位置温度。
    /// 本类不修改存档结构，不写入资源，不参与 Photon 同步。
    /// </summary>
    public class TemperatureEffect : Singleton<TemperatureEffect>, ITemperatureEffectService, ITickable
    {
        /// <summary>游戏时间 Provider（累计真实秒），测试可覆盖。</summary>
        internal static Func<double> GameTimeProvider { get; set; }
            = () => Core.ServiceLocator.Get<GameTimeManager>().CurGameTime;

        /// <summary>天气 Provider，测试可覆盖。</summary>
        internal static Func<WeatherManager.WeatherTypeEnum> WeatherProvider { get; set; }
            = () => Core.ServiceLocator.Get<WeatherManager>().CurrentWeather;

        private static readonly TemperatureRuleService RuleService = new TemperatureRuleService();

        private const float OutdoorSmoothPerSecond = 0.5f; // 室外温度平滑速度 ℃/s
        private const float RoomScanInterval = 1.5f;       // 房间温度/供暖扫描间隔（秒）
        private const float CharacterCacheInterval = 0.5f; // 角色位置温度缓存刷新间隔（秒）

        private float currentOutdoorTemp;
        private bool initialized;
        private bool enabled = true;
        private float lastRoomScanTime;
        private float lastCacheTime;
        private readonly Dictionary<object, float> characterTempCache = new Dictionary<object, float>();

        public TemperatureEffect()
        {
            // 不在构造函数访问 ServiceLocator（RegisterSafeServices 于 BeforeSceneLoad 创建，
            // 此时 WeatherManager 尚未注册）。首次 EnsureInitialized/Tick 时才计算目标温度。
            this.currentOutdoorTemp = 0f;
        }

        /// <inheritdoc/>
        public float GetOutdoorTemperature()
        {
            this.EnsureInitialized();
            return this.currentOutdoorTemp;
        }

        /// <inheritdoc/>
        public float GetTemperatureAt(Vector3Int posMap)
        {
            this.EnsureInitialized();
            RoomManager roomManager = Core.ServiceLocator.Get<RoomManager>();
            if (roomManager != null)
            {
                RoomInfo room = roomManager.GetRoomInterior(posMap);
                if (room != null)
                {
                    return room.Temperature;
                }
            }

            return this.currentOutdoorTemp;
        }

        /// <inheritdoc/>
        public float GetCharacterMoveSpeedMultiplier(Character character)
        {
            if (!this.enabled || character == null)
            {
                return 1.0f;
            }

            return RuleService.GetMoveSpeedMultiplier(this.GetCharacterTemperature(character));
        }

        /// <inheritdoc/>
        public float GetWorkerFatigueDecayMultiplier(AWorker worker)
        {
            if (!this.enabled || worker == null)
            {
                return 1.0f;
            }

            return RuleService.GetFatigueDecayMultiplier(this.GetCharacterTemperature(worker));
        }

        /// <inheritdoc/>
        public float GetAdjustedCharacterMoveSpeed(Character character, float baseSpeed)
        {
            return WeatherGameplayTool.ApplyMultiplier(
                baseSpeed,
                this.GetCharacterMoveSpeedMultiplier(character),
                0.0f);
        }

        /// <inheritdoc/>
        public void Tick(float deltaTime)
        {
            this.EnsureInitialized();

            // 1. 平滑室外温度向目标值靠近（天气/时间变化时渐变，避免突变）
            float target = this.ComputeTargetOutdoor();
            this.currentOutdoorTemp = UnityEngine.Mathf.MoveTowards(
                this.currentOutdoorTemp, target, OutdoorSmoothPerSecond * deltaTime);

            // 2. 定时扫描房间热源并刷新房间温度
            this.RefreshRoomTemperatures();

            // 3. 定时刷新角色位置温度缓存
            this.RefreshCharacterCache();
        }

        /// <summary>
        /// 计算当前目标室外温度（季节 + 天气 + 昼夜）。
        /// Provider 未就绪（如测试环境）时返回当前值，不抛异常。
        /// </summary>
        private float ComputeTargetOutdoor()
        {
            try
            {
                WeatherManager.WeatherTypeEnum weather = WeatherProvider();
                double gameTime = GameTimeProvider();
                return RuleService.GetOutdoorTemperature(
                    gameTime, GlobalData.GameDayTime, this.MapWeather(weather));
            }
            catch (Exception exception)
            {
                AWorkerTask.LogProvider(
                    "计算目标室外温度失败: " + exception.Message,
                    LogManager.LogLevelEnum.Warning);
                return this.currentOutdoorTemp;
            }
        }

        private void EnsureInitialized()
        {
            if (this.initialized)
            {
                return;
            }

            this.initialized = true;
            this.currentOutdoorTemp = this.ComputeTargetOutdoor();
        }

        /// <summary>
        /// 定时刷新所有已完成房间的温度：室外 + 保温 + 房间内供暖建筑功率之和。
        /// RoomInfo.Temperature 为公共字段（引用类型），直接写值，RoomListUI 等展示自动变实时。
        /// </summary>
        private void RefreshRoomTemperatures()
        {
            if (UnityEngine.Time.time - this.lastRoomScanTime < RoomScanInterval)
            {
                return;
            }

            this.lastRoomScanTime = UnityEngine.Time.time;
            RoomManager roomManager = Core.ServiceLocator.Get<RoomManager>();
            if (roomManager == null)
            {
                return;
            }

            foreach (KeyValuePair<string, RoomInfo> kv in roomManager.GetAllRooms())
            {
                RoomInfo room = kv.Value;
                if (room.Progress != 0)
                {
                    continue; // 建造中的房间不计算温度
                }

                room.Temperature = RuleService.GetRoomTemperature(
                    this.currentOutdoorTemp, this.ScanRoomHeatPower(room));
            }
        }

        /// <summary>
        /// 扫描房间内部包围盒的所有建造格，累加供暖建筑（BuildItemData.HeatPower &gt; 0）功率。
        /// 数据驱动：本期不在 SO 配数值，后续给某建筑配置 HeatPower &gt; 0 即自动生效。
        /// </summary>
        private float ScanRoomHeatPower(RoomInfo room)
        {
            float sum = 0.0f;
            BuildMap buildMap = Core.ServiceLocator.Get<BuildMap>();
            ItemDataManager itemDataManager = Core.ServiceLocator.Get<ItemDataManager>();
            if (buildMap == null || itemDataManager == null)
            {
                return sum;
            }

            for (int x = room.MinX; x <= room.MaxX; x++)
            {
                for (int y = room.MinY; y <= room.MaxY; y++)
                {
                    var tile = buildMap.GetTile(new Vector3Int(x, y, 0));
                    if (tile == null)
                    {
                        continue;
                    }

                    try
                    {
                        BuildItemData buildData = itemDataManager.GetBuildItemDataByName(tile.name);
                        if (buildData != null && buildData.HeatPower > 0f)
                        {
                            sum += buildData.HeatPower;
                        }
                    }
                    catch (System.InvalidCastException) { /* 非建造 tile（如 Bounty 标记），忽略 */ }
                }
            }

            return sum;
        }

        /// <summary>
        /// 定时刷新角色位置温度缓存（Player + 全部 Worker）。
        /// 节流避免每帧对大量角色做全房间遍历；清空重填避免残留死亡角色的引用。
        /// </summary>
        private void RefreshCharacterCache()
        {
            if (UnityEngine.Time.time - this.lastCacheTime < CharacterCacheInterval)
            {
                return;
            }

            this.lastCacheTime = UnityEngine.Time.time;
            this.characterTempCache.Clear();
            try
            {
                TileMap tileMap = Core.ServiceLocator.Get<TileMap>();
                if (tileMap == null)
                {
                    return;
                }

                PlayerManager playerManager = Core.ServiceLocator.Get<PlayerManager>();
                if (playerManager != null && playerManager.Mine != null)
                {
                    Player player = playerManager.Mine;
                    Vector3Int posMap = tileMap.WorldPosToMapPos(player.transform.position);
                    this.characterTempCache[player] = this.GetTemperatureAt(posMap);
                }

                WorkerManager workerManager = Core.ServiceLocator.Get<WorkerManager>();
                if (workerManager != null && workerManager.Characters != null)
                {
                    foreach (AWorker worker in workerManager.Characters)
                    {
                        if (worker == null) continue;
                        Vector3Int posMap = tileMap.WorldPosToMapPos(worker.transform.position);
                        this.characterTempCache[worker] = this.GetTemperatureAt(posMap);
                    }
                }
            }
            catch (Exception exception)
            {
                AWorkerTask.LogProvider(
                    "刷新角色温度缓存失败: " + exception.Message,
                    LogManager.LogLevelEnum.Warning);
            }
        }

        /// <summary>
        /// 查询角色位置温度：优先缓存（Tick 每 0.5s 刷新），未命中时直接计算。
        /// </summary>
        private float GetCharacterTemperature(Character character)
        {
            if (this.characterTempCache.TryGetValue(character, out float cached))
            {
                return cached;
            }

            try
            {
                TileMap tileMap = Core.ServiceLocator.Get<TileMap>();
                Vector3Int posMap = tileMap.WorldPosToMapPos(character.transform.position);
                return this.GetTemperatureAt(posMap);
            }
            catch (Exception)
            {
                return this.currentOutdoorTemp;
            }
        }

        /// <summary>Manager 层天气类型 → 领域层天气类型。</summary>
        private WeatherType MapWeather(WeatherManager.WeatherTypeEnum weather)
        {
            switch (weather)
            {
                case WeatherManager.WeatherTypeEnum.Rain:
                    return WeatherType.Rain;
                case WeatherManager.WeatherTypeEnum.Snow:
                    return WeatherType.Snow;
                default:
                    return WeatherType.Clear;
            }
        }
    }
}
