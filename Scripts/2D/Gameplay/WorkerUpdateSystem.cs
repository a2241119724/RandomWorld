namespace LAB2D.Gameplay
{
    using LAB2D;
    using LAB2D.Character.Worker;
    using LAB2D.Constant;
    using LAB2D.Domain.Common;
    using LAB2D.Tool;
    using System.Collections.Generic;

    /// <summary>
    /// 工人更新系统 — 从 GlobalInit 提取的独立 Tick 系统。
    /// 负责工人饥饿/疲劳衰减及所有子系统的定时刷新。
    /// 实现 ITickable 接口，由 GlobalInit 在 Update 中驱动。
    /// </summary>
    public sealed class WorkerUpdateSystem : ITickable
    {
        /// <inheritdoc/>
        public void Tick(float deltaTime)
        {
            var terrainEffect = ServiceLocator.Get<ITerrainEffectService>();
            List<AWorker> workers = ServiceLocator.Get<WorkerManager>().Characters;
            foreach (AWorker worker in workers)
            {
                AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
                if (workerData == null)
                {
                    continue;
                }

                // 地形效果对衰减速率的影响
                float tiredTerrainMult = terrainEffect.GetWorkerTiredDecayMultiplier(worker);
                float hungryTerrainMult = terrainEffect.GetWorkerHungryDecayMultiplier(worker);

                // 饥饿值自然衰减
                if (workerData.CurHungry > 0)
                {
                    workerData.CurHungry = System.Math.Max(
                        0.0f,
                        workerData.CurHungry - (deltaTime * WorkerConditionConstant.HungryDecayPerSecond * hungryTerrainMult));
                }

                // 疲劳值自然衰减（受地形 + 天气影响，雨雪天加速）
                if (workerData.CurTired > 0)
                {
                    float weatherMult = 1.0f;
                    try
                    {
                        var weatherMgr = ServiceLocator.Get<WeatherManager>();
                        if (weatherMgr != null)
                        {
                            weatherMult = WeatherGameplayTool.GetFatigueDecayMultiplier(weatherMgr.CurrentWeather);
                        }
                    }
                    catch { /* 天气系统未注册时保持默认倍率 */ }

                    workerData.CurTired = System.Math.Max(
                        0.0f,
                        workerData.CurTired - (deltaTime * WorkerConditionConstant.TiredDecayPerSecond
                            * tiredTerrainMult * weatherMult));
                }

                // 精气神自然衰减
                if (workerData.CurSpirit > 0)
                {
                    float spiritDecay = deltaTime * WorkerConditionConstant.SpiritDecayPerSecond;
                    // 有任务且不是吃饭/睡觉/漫游/锻炼时，额外消耗精气神
                    if (workerData.Task != null)
                    {
                        var taskType = workerData.Task.TaskType;
                        if (taskType != Enum.WorkerTaskType.Eat
                            && taskType != Enum.WorkerTaskType.Sleep
                            && taskType != Enum.WorkerTaskType.GroundSleep
                            && taskType != Enum.WorkerTaskType.Wander
                            && taskType != Enum.WorkerTaskType.Exercise)
                        {
                            spiritDecay += deltaTime * WorkerConditionConstant.SpiritWorkDecayPerSecond;
                        }
                    }

                    workerData.CurSpirit = System.Math.Max(
                        0.0f,
                        workerData.CurSpirit - spiritDecay);
                }

                ServiceLocator.Get<IWorkerConditionManager>().UpdateWorkerCondition(worker);
            }

            // 子系统定时刷新（内部有节流控制）
            ServiceLocator.Get<IWorkerSupplyIssueManager>().Tick();
            ServiceLocator.Get<IWorkerTaskCongestionAdvisor>().Tick();
            ServiceLocator.Get<IColonyCommandCenterService>().Tick();
            ServiceLocator.Get<ISkillManager>().Tick();
            NearbyItemPickupHUD.Instance?.Tick();
        }
    }
}
