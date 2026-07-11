namespace LAB2D
{
    using System.Collections.Generic;

    /// <summary>
    /// Worker 任务时间配置，单位为秒。
    /// </summary>
    public static class WorkerTaskTimeConfig
    {
        public const float DefaultTaskSeconds = 1.0f;
        public const float WorkTiredCostPerSecond = 0.08f;

        public const float BuildFetchResourceSeconds = 0.8f;
        public const float BuildBaseSeconds = 1.5f;
        public const float BuildSecondsPerResource = 0.35f;
        public const float BuildMaxSeconds = 4.0f;

        public const float CarryTakeSeconds = 1.0f;
        public const float CarryPutDownSeconds = 0.8f;
        public const float GatherSeconds = 4.5f;
        public const float EatSeconds = 1.2f;
        public const float ExerciseSeconds = 5.0f;
        public const long ExerciseSeekThreshold = 10;
        public const float WearSeconds = 0.8f;
        public const float SleepSeconds = 8.0f;
        public const float PlantFetchSeedSeconds = 0.8f;
        public const float PlantOneSeedSeconds = 1.1f;

        public const float IdleRestSeconds = 1.5f;
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

            return Clamp(BuildBaseSeconds + resourceCount * BuildSecondsPerResource, BuildBaseSeconds, BuildMaxSeconds);
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

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }
    }
}
