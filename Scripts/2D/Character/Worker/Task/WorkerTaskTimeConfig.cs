namespace LAB2D.Character.Worker.Task
{
    using LAB2D;
    using LAB2D.Data;
    using LAB2D.Item;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Worker 任务时间配置，所有时长均按游戏天(GameDayTime)的百分比计算。
    /// 修改 GameDayTime 时所有任务时长自动等比缩放。
    /// </summary>
    public static class WorkerTaskTimeConfig
    {
        // 基准时间
        private static float Day => GlobalData.GameDayTime;

        /// <summary>默认任务时长（1/450 天 ≈ 0.22%）。</summary>
        public static float DefaultTaskSeconds => Day / 450f;
        // ---- 疲劳消耗速率（/秒，绝对值，不随天数缩放）----
        /// <summary>默认工作疲劳消耗速度（/秒），子类通过 TiredCostPerSecond 虚属性重写。</summary>
        public const float WorkTiredCostPerSecond = 0.04f;
        /// <summary>重体力工作疲劳消耗（砍树/挖矿），0.07/s。</summary>
        public const float HeavyWorkTiredCostPerSecond = 0.07f;
        /// <summary>中体力工作疲劳消耗（建造），0.05/s。</summary>
        public const float MediumWorkTiredCostPerSecond = 0.05f;
        /// <summary>轻体力工作疲劳消耗（种植/搬运），0.03/s。</summary>
        public const float LightWorkTiredCostPerSecond = 0.03f;

        // ---- 建造 (Build) ----
        public static float BuildFetchResourceSeconds => Day / 600f;    // 0.167%
        public static float BuildBaseSeconds            => Day / 300f;    // 0.333%
        public static float BuildSecondsPerResource     => Day / 1500f;   // 0.067%
        public static float BuildMaxSeconds             => Day / 112.5f;  // 0.889%

        // ---- 搬运 (Carry) ----
        public static float CarryTakeSeconds    => Day / 450f;   // 0.222%
        public static float CarryPutDownSeconds => Day / 600f;   // 0.167%

        // ---- 采集 (Gather) ----
        public static float GatherSeconds => Day / 75f;  // 1.333%, 一天约32次

        // ---- 进食 (Eat) ----
        public static float EatSeconds => Day / 300f;  // 0.333%

        // ---- 锻炼 (Exercise) ----
        public static float ExerciseSeconds => Day / 112.5f;  // 0.889%
        public const long ExerciseSeekThreshold = 5;
        /// <summary>锻炼经验值获取速率（/秒），每次完成锻炼任务时根据时长结算。</summary>
        public const float ExerciseExperiencePerSecond = 0.5f;

        // ---- 穿戴 (Wear) ----
        public static float WearSeconds => Day / 600f;  // 0.167%

        // ---- 睡眠 (Sleep) ----
        public static float SleepSeconds       => Day * 0.10f;   // 10%
        public static float GroundSleepSeconds => Day * 0.15f;   // 15%

        // ---- 漫游 (Wander) ----
        public static float WanderSeconds => Day / 90f;  // 1.111%

        // ---- 种植 (Plant) ----
        public static float PlantFetchSeedSeconds => Day / 600f;   // 0.167%
        public static float PlantOneSeedSeconds   => Day / 450f;   // 0.222%

        // ---- 拆除 (Demolish) ----
        public static float DemolishSeconds => Day / 112.5f;  // 0.889%

        // ---- 其他 ----
        public static float IdleRestSeconds => Day / 300f;   // 0.333%
        public static float EscapeSeconds   => Day / 300f;   // 0.333%

        public static float GetBuildConstructionSeconds(Dictionary<int, ResourceInfo> needs)
        {
            int resourceCount = 0;
            if (needs != null)
            {
                foreach (KeyValuePair<int, ResourceInfo> pair in needs)
                {
                    if (pair.Value != null && pair.Value.Count > 0)
                    {
                        resourceCount += pair.Value.Count;
                    }
                }
            }

            return Mathf.Clamp(BuildBaseSeconds + resourceCount * BuildSecondsPerResource, BuildBaseSeconds, BuildMaxSeconds);
        }

        public static float ResolveCarryTakeSeconds(ItemData itemData)
        {
            if (itemData == null || itemData.RelatedTaskTime == null)
            {
                return CarryTakeSeconds;
            }

            return ResolveItemTime(itemData.RelatedTaskTime.TaskBaseTime, CarryTakeSeconds);
        }

        public static float ResolveCarryPutDownSeconds(ItemData itemData)
        {
            if (itemData == null || itemData.RelatedTaskTime == null)
            {
                return CarryPutDownSeconds;
            }

            return ResolveItemTime(itemData.RelatedTaskTime.CarryTaskPutDownTime, CarryPutDownSeconds);
        }

        public static float ResolveItemTime(float itemSeconds, float fallbackSeconds)
        {
            return itemSeconds > 0.0f ? itemSeconds : fallbackSeconds;
        }

    }
}
