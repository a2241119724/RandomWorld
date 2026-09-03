namespace LAB2D.Data
{
    using LAB2D;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Gameplay.LingQi;
    using UnityEngine;

    /// <summary>
    /// 环境管理 — 灵气浓度展示（M4 起重定义：原 CurEnergy/MaxEnergy 恢复模型删除，
    /// 浓度由 LingQiManager 按玩家位置实时合成：地形×灵脉×聚灵阵×天气，100=草地基准）。
    /// 温度由 TemperatureEffect 计算，不在此维护。
    /// </summary>
    public class EnvironmentManager : Singleton<EnvironmentManager>, ITickable
    {
        /// <summary>
        /// 湿度（占位默认值；房间内湿度由 RoomManager 维护，野外湿度暂不做天气联动）
        /// </summary>
        public float Humidity = 25.0f;

        /// <summary>
        /// 当前灵气浓度（百分比，100=草地基准；玩家缺失时保持 100）
        /// </summary>
        public float CurDensity { get; private set; } = 100.0f;

        /// <summary>
        /// 采样玩家位置的灵气浓度（Tick 驱动；打坐面板/环境 HUD 读此值）
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (ServiceLocator.TryGet(out PlayerManager pm) && pm.Mine != null)
            {
                this.CurDensity = LingQiManager.Instance.GetDensityAtWorld(pm.Mine.transform.position) * 100f;
            }
        }

        /// <summary>
        /// 获取当前环境信息（温度 = 实时室外温度，浓度 = 玩家位置）。
        /// </summary>
        /// <returns>信息</returns>
        public override string ToString()
        {
            float temp = ServiceLocator.TryGet<ITemperatureEffectService>(out var tempEffect)
                ? tempEffect.GetOutdoorTemperature()
                : 0f;
            return $"温度:{temp:F1}\n" +
                $"湿度:{this.Humidity}\n" +
                $"灵气浓度:{this.CurDensity:F0}%\n";
        }

        /// <summary>
        /// 获取某位置的当前环境信息。
        /// 房间内显示房间实时温度（室外+保温+供暖），野外显示室外温度。
        /// 尾部拼被点格灵气浓度分项（点地看浓度 = 选址工具），分项「地形×t 灵脉×v 阵×a 天气×w」（=1 省略段）。
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <returns>信息</returns>
        public string ToString(Vector3Int posMap)
        {
            RoomInfo roomInfo = ServiceLocator.Get<RoomManager>().GetRoomInterior(posMap);
            string envText = roomInfo != null
                ? roomInfo.ToString()
                : this.ToString();

            return envText + FormatDensityAt(posMap);
        }

        /// <summary>被点格浓度分项（拼在环境信息尾部，选址工具）。</summary>
        private static string FormatDensityAt(Vector3Int posMap)
        {
            LingQiManager.LocalFactors f = LingQiManager.Instance.GetLocalFactors(posMap);
            var parts = new System.Text.StringBuilder();
            if (!Mathf.Approximately(f.Terrain, 1f))
            {
                parts.Append($"地形×{f.Terrain:F1} ");
            }

            if (f.VeinBoosted)
            {
                parts.Append($"灵脉×{LingQiRuleService.VeinBoostMultiplier:F1} ");
            }

            if (f.Arrays > 0)
            {
                float stack = LingQiRuleService.ApplySpiritArrayBoost(1f, f.Arrays);
                parts.Append($"阵×{stack:F2} ");
            }

            if (!Mathf.Approximately(f.Weather, 1f))
            {
                parts.Append($"天气×{f.Weather:F2}");
            }

            string breakdown = parts.Length > 0 ? $"（{parts.ToString().TrimEnd()}）" : string.Empty;
            return $"此处灵气浓度:{f.Composed * 100f:F0}%{breakdown}\n";
        }
    }
}
