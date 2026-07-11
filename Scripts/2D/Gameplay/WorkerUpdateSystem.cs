namespace LAB2D.Gameplay
{
    using LAB2D;
    using LAB2D.Character.Worker;
    using LAB2D.Constant;
    using LAB2D.Domain.Common;
    using System.Collections.Generic;
    using UnityEngine;

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
            List<AWorker> workers = WorkerManager.Instance.Characters;
            foreach (AWorker worker in workers)
            {
                AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
                if (workerData == null)
                {
                    continue;
                }

                // 饥饿值自然衰减
                if (workerData.CurHungry > 0)
                {
                    workerData.CurHungry = Mathf.Max(
                        0.0f,
                        workerData.CurHungry - (deltaTime * WorkerConditionConstant.HungryDecayPerSecond));
                }

                // 疲劳值自然衰减
                if (workerData.CurTired > 0)
                {
                    workerData.CurTired = Mathf.Max(
                        0.0f,
                        workerData.CurTired - (deltaTime * WorkerConditionConstant.TiredDecayPerSecond));
                }

                WorkerConditionManager.Instance.UpdateWorkerCondition(worker);
            }

            // 子系统定时刷新（内部有节流控制）
            WorkerSupplyIssueManager.Instance.Tick();
            WorkerTaskCongestionAdvisor.Instance.Tick();
            ColonyCommandCenterManager.Instance.Tick();
            SkillManager.Instance.Tick();
            NearbyItemPickupHUD.Instance?.Tick();
        }
    }
}
