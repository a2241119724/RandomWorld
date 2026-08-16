namespace LAB2D.AI.Worker
{
    using LAB2D.Character.Worker;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Core;
    using LAB2D.Data;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Gameplay;
    using LAB2D.Domain.Worker;
    using LAB2D.Gameplay;
    using LAB2D.Item;
    using LAB2D.Manager;
    using LAB2D.Map;
    using LAB2D.Serializable;
    using UnityEngine;

    /// <summary>
    /// Worker 行为决策模型的 C# 特征提取层。
    ///
    /// 从 <see cref="AWorker"/>（含 WorkerData / CharacterData / Attribute / 人格）+ 全局上下文
    /// （天气温度、任务队列压力、游戏时刻）+ 局部视野（附近食物/资源/建筑/其他 Worker 数）
    /// 构造 <see cref="FeatureDim"/> 维特征向量，顺序与归一化公式严格对齐
    /// <c>model/config/feature_schema.yaml</c>（与 <c>model/src/features.py</c> 一致）：
    ///
    ///   ratio    → clamp01(value / max)                    （当前值 / 最大值）
    ///   minmax   → clamp01((value - lo) / (hi - lo))       （线性归一化）
    ///   log      → log1p(value) / log1p(max)               （长尾非负量，如金币）
    ///   onehot   → 按 categories 展开，未知类别全零
    ///   passthrough → 原样输出（要求已归一化到 [0,1]）
    ///
    /// 41 维顺序（严格对齐 schema 的 features 列表）：
    ///   0-2   hungry_ratio / tired_ratio / spirit_ratio
    ///   3-4   hp_ratio / mp_ratio
    ///   5-6   level / exp_ratio
    ///   7-14  8 维战斗属性 ATN/INT/DEF/RES/CRT/CSD/SPD/HIT
    ///   15-18 mood / ambition / diligence / sociality
    ///   19-20 greed / laziness（预留扩展位，固定 0.5）
    ///   21    gold（log）
    ///   22    bed_available
    ///   23-25 life_stage onehot
    ///   26-28 home_build_stage onehot
    ///   29-32 current_goal onehot
    ///   33    favorability
    ///   34    weather_temperature
    ///   35    task_pressure
    ///   36    time_of_day
    ///   37-40 nearby_food / nearby_resource / nearby_building / nearby_worker
    ///
    /// 零 MonoBehaviour 依赖，可在测试中独立调用。所有 Manager 访问均判空兜底，
    /// 保证任意初始化阶段都能返回合法向量（缺数据用训练分布的中性默认值）。
    /// </summary>
    public static class WorkerFeatureExtractor
    {
        /// <summary>特征向量维度（须与 mlp 权重输入维度一致 = 41）。</summary>
        public const int FeatureDim = 41;

        /// <summary>视野扫描默认半径（与 WorkerBrain.ScanRadius 一致）。</summary>
        public const int DefaultScanRadius = 20;

        // ---- 归一化辅助（公式与 features.py 完全一致）----

        private static float Ratio(float value, float maxv)
        {
            if (maxv <= 0f) return 0f;
            return Mathf.Clamp01(value / maxv);
        }

        private static float MinMax(float value, float lo, float hi)
        {
            if (hi <= lo) return 0f;
            return Mathf.Clamp01((value - lo) / (hi - lo));
        }

        private static float Log(float value, float maxv)
        {
            if (maxv <= 0f) return 0f;
            float v = Mathf.Clamp(value, 0f, maxv);
            return Mathf.Log(1f + v) / Mathf.Log(1f + maxv);
        }

        /// <summary>
        /// 提取 Worker 当前状态的 41 维特征向量。
        /// </summary>
        /// <param name="worker">目标 Worker（非 null）。</param>
        /// <param name="scanRadius">视野扫描半径（切比雪夫距离），默认 <see cref="DefaultScanRadius"/>。</param>
        /// <returns>长度固定为 <see cref="FeatureDim"/> 的 float 数组。</returns>
        public static float[] Extract(AWorker worker, int scanRadius = DefaultScanRadius)
        {
            float[] f = new float[FeatureDim];
            int i = 0;

            AWorker.WorkerData wd = worker?.CharacterDataLAB as AWorker.WorkerData;
            if (wd == null)
            {
                // 极端兜底：无数据时返回全零向量（调用方应回退硬规则）
                return f;
            }

            // ---- 生存（0-2）----
            f[i++] = Ratio(wd.CurHungry, wd.MaxHungry);
            f[i++] = Ratio(wd.CurTired, wd.MaxTired);
            f[i++] = Ratio(wd.CurSpirit, wd.MaxSpirit);

            // ---- 生命 / 成长（3-6）----
            f[i++] = Ratio(wd.Hp, wd.MaxHp);
            f[i++] = Ratio(wd.Mp, wd.MaxMp);
            f[i++] = MinMax(wd.Level, 1f, 20f);
            f[i++] = Ratio(wd.CurExperience, wd.MaxExperience);

            // ---- 8 维战斗属性（7-14）----
            f[i++] = MinMax(wd.ATN, 0f, 100f);
            f[i++] = MinMax(wd.INT, 0f, 100f);
            f[i++] = MinMax(wd.DEF, 0f, 100f);
            f[i++] = MinMax(wd.RES, 0f, 100f);
            f[i++] = MinMax(wd.CRT, 0f, 100f);
            f[i++] = MinMax(wd.CSD, 0f, 100f);
            f[i++] = MinMax(wd.SPD, 0f, 100f);
            f[i++] = MinMax(wd.HIT, 0f, 100f);

            // ---- 人格（15-18）----
            f[i++] = MinMax(wd.Personality.Mood, 0f, 100f);
            f[i++] = MinMax(wd.Personality.Ambition, 0f, 100f);
            f[i++] = MinMax(wd.Personality.Diligence, 0f, 100f);
            f[i++] = MinMax(wd.Personality.Sociality, 0f, 100f);

            // ---- 预留扩展位：贪婪 / 懒惰（19-20，固定中性 0.5）----
            f[i++] = 0.5f;
            f[i++] = 0.5f;

            // ---- 经济（21）----
            f[i++] = Log(wd.Wallet.Gold, 1000f);

            // ---- 阶段 / 目标（22-32）----
            f[i++] = worker.BedItem != null ? 1f : 0f;                 // bed_available（passthrough）
            EncodeOneHot(f, ref i, LifeStageIndex(wd.LifeStage), 3);   // life_stage
            EncodeOneHot(f, ref i, HomeBuildStageIndex(wd), 3);        // home_build_stage
            EncodeOneHot(f, ref i, GoalIndex(wd.CurrentGoal.Type), 4); // current_goal

            // ---- 社交（33）----
            FavorabilityManager favor = ServiceLocator.Get<FavorabilityManager>();
            float favorability = favor != null ? favor.GetFavorabilityWithPlayer(worker) : 50f;
            f[i++] = MinMax(favorability, 0f, 100f);

            // ---- 全局上下文（34-36）----
            f[i++] = MinMax(GetOutdoorTemperature(), -20f, 40f);
            f[i++] = MinMax(GetTaskPressure(), 0f, 100f);
            f[i++] = MinMax(GetTimeOfDay(), 0f, 24f);

            // ---- 局部视野（37-40）----
            ScanNearby(worker, scanRadius, out int food, out int resource, out int building, out int nearbyWorker);
            f[i++] = MinMax(food, 0f, 20f);
            f[i++] = MinMax(resource, 0f, 20f);
            f[i++] = MinMax(building, 0f, 20f);
            f[i++] = MinMax(nearbyWorker, 0f, 20f);

            return f;
        }

        // ---- onehot 编码 ----

        private static void EncodeOneHot(float[] f, ref int index, int activeCategory, int categoryCount)
        {
            for (int c = 0; c < categoryCount; c++)
            {
                f[index++] = (c == activeCategory) ? 1f : 0f;
            }
        }

        private static int LifeStageIndex(WorkerLifeStage stage)
        {
            switch (stage)
            {
                case WorkerLifeStage.Bootstrap: return 0;
                case WorkerLifeStage.Settled: return 1;
                case WorkerLifeStage.Established: return 2;
                default: return -1; // 未知 → 全零
            }
        }

        /// <summary>
        /// 建家阶段三值映射（对齐 Python 训练语义）：
        ///   2 = 已有家（HomePosition 非 null）
        ///   1 = 正在建（无家但 HomeBuildStage > 0）
        ///   0 = 尚未开始建
        /// </summary>
        private static int HomeBuildStageIndex(AWorker.WorkerData wd)
        {
            if (wd.HomePosition != null) return 2;
            if (wd.HomeBuildStage > 0) return 1;
            return 0;
        }

        private static int GoalIndex(WorkerGoalType type)
        {
            switch (type)
            {
                case WorkerGoalType.EarnMoney: return 0;
                case WorkerGoalType.BuildStructure: return 1;
                case WorkerGoalType.StockFood: return 2;
                case WorkerGoalType.CraftEquipment: return 3;
                default: return -1; // 未知 → 全零
            }
        }

        // ---- 全局上下文取值 ----

        /// <summary>室外温度 = 季节基础 + 天气偏移 + 昼夜波动（TemperatureRuleService）。</summary>
        private static float GetOutdoorTemperature()
        {
            WeatherManager.WeatherTypeEnum weather = WeatherManager.WeatherTypeEnum.Sunny;
            WeatherManager weatherManager = ServiceLocator.Get<WeatherManager>();
            if (weatherManager != null)
            {
                weather = weatherManager.CurrentWeather;
            }

            double curTime = GameTimeManager.Instance.CurGameTime;
            return new TemperatureRuleService().GetOutdoorTemperature(
                curTime, GlobalData.GameDayTime, MapWeather(weather));
        }

        private static WeatherType MapWeather(WeatherManager.WeatherTypeEnum weather)
        {
            switch (weather)
            {
                case WeatherManager.WeatherTypeEnum.Rain: return WeatherType.Rain;
                case WeatherManager.WeatherTypeEnum.Snow: return WeatherType.Snow;
                default: return WeatherType.Clear;
            }
        }

        /// <summary>任务队列压力：任务总数 × 5，clamp 到 [0,100]。</summary>
        private static float GetTaskPressure()
        {
            WorkerTaskManager taskManager = WorkerTaskManager.Instance;
            if (taskManager == null) return 0f;
            return Mathf.Clamp(taskManager.CreateTaskQueueSnapshot().TotalTaskCount * 5, 0f, 100f);
        }

        /// <summary>游戏时刻（0-24 小时）：当天已过秒数 / 每天秒数 × 24。</summary>
        private static float GetTimeOfDay()
        {
            double gameDaySeconds = GlobalData.GameDayTime;
            if (gameDaySeconds <= 0) return 0f;
            double dayElapsed = GameTimeManager.Instance.CurGameTime % gameDaySeconds;
            return (float)((dayElapsed / gameDaySeconds) * 24.0);
        }

        // ---- 局部视野扫描（切比雪夫距离 ≤ scanRadius）----

        private static void ScanNearby(
            AWorker worker,
            int scanRadius,
            out int food,
            out int resource,
            out int building,
            out int nearbyWorker)
        {
            food = 0;
            resource = 0;
            building = 0;
            nearbyWorker = 0;

            Vector3Int workerPos = AWorkerTask.TileMapWorldToMapProvider(worker.transform.position);

            // 可采集资源（ResourceMap.PosMap：key=格坐标, value=tile 名）
            ResourceMap resourceMap = ServiceLocator.Get<ResourceMap>();
            if (resourceMap?.ResourceMapDataLAB?.PosMap != null)
            {
                ItemDataManager itemDataManager = ServiceLocator.Get<ItemDataManager>();
                foreach (System.Collections.Generic.KeyValuePair<Vector3IntLAB, string> kv
                    in resourceMap.ResourceMapDataLAB.PosMap)
                {
                    Vector3Int pos = Vector3IntLAB.ToVector3Int(kv.Key);
                    if (!WithinChebyshev(workerPos, pos, scanRadius)) continue;

                    resource++;
                    string tileName = kv.Value;
                    if (!string.IsNullOrEmpty(tileName)
                        && itemDataManager != null
                        && itemDataManager.TryGetByName(tileName, out ItemData itemData)
                        && AWorkerTask.ItemTypeProvider(itemData.Id) == AItem.ItemTypeEnum.Food)
                    {
                        food++;
                    }
                }
            }

            // 已完成建筑（BuildMap.PosMap：key=格坐标, value=BuildTileData.IsComplete）
            BuildMap buildMap = ServiceLocator.Get<BuildMap>();
            if (buildMap?.BuildMapDataLAB?.PosMap != null)
            {
                foreach (System.Collections.Generic.KeyValuePair<Vector3IntLAB, BuildMap.BuildTileData> kv
                    in buildMap.BuildMapDataLAB.PosMap)
                {
                    if (!kv.Value.IsComplete) continue;
                    Vector3Int pos = Vector3IntLAB.ToVector3Int(kv.Key);
                    if (WithinChebyshev(workerPos, pos, scanRadius)) building++;
                }
            }

            // 附近其他 Worker（不含自身）
            WorkerManager workerManager = ServiceLocator.Get<WorkerManager>();
            if (workerManager?.Characters != null)
            {
                foreach (AWorker other in workerManager.Characters)
                {
                    if (other == null || ReferenceEquals(other, worker)) continue;
                    Vector3Int otherPos = AWorkerTask.TileMapWorldToMapProvider(other.transform.position);
                    if (WithinChebyshev(workerPos, otherPos, scanRadius)) nearbyWorker++;
                }
            }
        }

        private static bool WithinChebyshev(Vector3Int a, Vector3Int b, int radius)
        {
            return Mathf.Abs(a.x - b.x) <= radius && Mathf.Abs(a.y - b.y) <= radius;
        }
    }
}
