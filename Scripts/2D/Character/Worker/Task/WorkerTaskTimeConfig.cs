namespace LAB2D.Character.Worker.Task
{
    using LAB2D;
    using LAB2D.Data;
    using LAB2D.Item;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Worker 任务时间配置，单位为秒。
    ///
    /// 设计基准：1 游戏天 = 1800 真实秒（30 分钟）。
    /// 工人一天有效工作时间约 765s（由疲劳衰减速率决定），
    /// 任务时长决定了一天内的任务产出次数。
    /// 数值经过 1.5-2.5x 上调，使各任务占游戏天的比例更合理，
    /// 同时保持"花钱买时间"（装备/技能加速进度条）的感知价值。
    /// </summary>
    public static class WorkerTaskTimeConfig
    {
        /// <summary>默认任务时长（2.0s, 占一天 0.11%）。</summary>
        public const float DefaultTaskSeconds = 2.0f;

        /// <summary>默认工作疲劳消耗速度（/秒），子类通过 TiredCostPerSecond 虚属性重写。</summary>
        public const float WorkTiredCostPerSecond = 0.04f;

        /// <summary>重体力工作疲劳消耗（砍树/挖矿），0.07/s。</summary>
        public const float HeavyWorkTiredCostPerSecond = 0.07f;

        /// <summary>中体力工作疲劳消耗（建造），0.05/s。</summary>
        public const float MediumWorkTiredCostPerSecond = 0.05f;

        /// <summary>轻体力工作疲劳消耗（种植/搬运），0.03/s。</summary>
        public const float LightWorkTiredCostPerSecond = 0.03f;

        // ---- 建造 (Build) ----
        /// <summary>建造取材料阶段（1.5s, 占一天 0.08%）。</summary>
        public const float BuildFetchResourceSeconds = 1.5f;
        /// <summary>建造基础时长（3.0s, 占一天 0.17%）。</summary>
        public const float BuildBaseSeconds = 3.0f;
        /// <summary>建造每资源额外时长（0.6s）。</summary>
        public const float BuildSecondsPerResource = 0.6f;
        /// <summary>建造最大时长（8.0s, 占一天 0.44%）。</summary>
        public const float BuildMaxSeconds = 8.0f;

        // ---- 搬运 (Carry) ----
        /// <summary>搬运取货阶段（2.0s, 占一天 0.11%）。</summary>
        public const float CarryTakeSeconds = 2.0f;
        /// <summary>搬运放货阶段（1.5s, 占一天 0.08%）。</summary>
        public const float CarryPutDownSeconds = 1.5f;

        // ---- 采集 (Gather) ----
        /// <summary>采集/砍树/挖矿（12.0s, 占一天 0.67%, 一天约64次）。</summary>
        public const float GatherSeconds = 12.0f;

        // ---- 进食 (Eat) ----
        /// <summary>进食（3.0s, 占一天 0.17%）。</summary>
        public const float EatSeconds = 3.0f;

        // ---- 锻炼 (Exercise) ----
        /// <summary>锻炼/空闲（8.0s, 占一天 0.44%）。</summary>
        public const float ExerciseSeconds = 8.0f;
        public const long ExerciseSeekThreshold = 10;

        // ---- 穿戴 (Wear) ----
        /// <summary>穿戴装备（1.5s, 占一天 0.08%）。</summary>
        public const float WearSeconds = 1.5f;

        // ---- 睡眠 (Sleep) ----
        /// <summary>床上睡眠（15.0s, 占一天 0.83%）。</summary>
        public const float SleepSeconds = 15.0f;
        /// <summary>地面睡眠（20.0s, 占一天 1.1%, 更久恢复更少）。</summary>
        public const float GroundSleepSeconds = 20.0f;

        // ---- 漫游 (Wander) ----
        /// <summary>漫游每路点（10.0s, 占一天 0.56%, 3-5路点约30-50s）。</summary>
        public const float WanderSeconds = 10.0f;

        // ---- 种植 (Plant) ----
        /// <summary>种植取种子阶段（1.5s）。</summary>
        public const float PlantFetchSeedSeconds = 1.5f;
        /// <summary>种植一颗种子（2.0s）。</summary>
        public const float PlantOneSeedSeconds = 2.0f;

        // ---- 其他 ----
        /// <summary>空闲休息（3.0s, 到达目标后的短暂停顿）。</summary>
        public const float IdleRestSeconds = 3.0f;
        /// <summary>逃跑（3.0s）。</summary>
        public const float EscapeSeconds = 3.0f;

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
