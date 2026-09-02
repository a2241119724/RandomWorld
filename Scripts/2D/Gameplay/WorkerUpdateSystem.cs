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
        /// <summary>环境倍率（地形/温度）缓存刷新间隔（秒）。分钟级衰减对此延迟无感知，且与 TemperatureEffect 内部缓存口径一致。</summary>
        private const float EnvRefreshInterval = 0.5f;

        /// <summary>失效条目清理间隔（秒）：移除已移除 Worker 的残留缓存，防字典泄漏。</summary>
        private const float StaleCleanInterval = 30f;

        /// <summary>单 Worker 环境倍率缓存：地形(tired/hungry) + 温度，到期才重查（WorldToCell + ServiceLocator 是热路径）。</summary>
        private struct EnvMultiplier
        {
            public float TiredTerrain;
            public float HungryTerrain;
            public float Temperature;
            public float NextRefreshTime;
        }

        private readonly Dictionary<int, EnvMultiplier> envCache = new Dictionary<int, EnvMultiplier>();
        private float elapsed;
        private int envCursor;
        private float staleCleanTimer;

        /// <inheritdoc/>
        public void Tick(float deltaTime)
        {
            // 服务引用与全局天气倍率循环外提：几百 Worker 时省 ~3 次/人/帧的 ServiceLocator 字典查找；
            // 天气是全局状态与具体 Worker 无关，原实现每 Worker 查一次属纯浪费。
            var terrainEffect = ServiceLocator.Get<ITerrainEffectService>();
            var conditionManager = ServiceLocator.Get<IWorkerConditionManager>();
            List<AWorker> workers = ServiceLocator.Get<WorkerManager>().Characters;

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

            ITemperatureEffectService tempEffect = null;
            try
            {
                tempEffect = ServiceLocator.Get<ITemperatureEffectService>();
            }
            catch { /* 温度系统未注册时保持默认倍率 */ }

            this.elapsed += deltaTime;
            this.RefreshEnvBatch(workers, terrainEffect, tempEffect, deltaTime);

            foreach (AWorker worker in workers)
            {
                AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
                if (workerData == null)
                {
                    continue;
                }

                // 环境倍率走 0.5s 分帧缓存；新 Worker 未被游标轮到前用默认 1.0（出生即健康，无感）
                if (!this.envCache.TryGetValue(worker.GetInstanceID(), out EnvMultiplier env))
                {
                    env.TiredTerrain = 1.0f;
                    env.HungryTerrain = 1.0f;
                    env.Temperature = 1.0f;
                }

                // 饥饿值自然衰减
                if (workerData.CurHungry > 0)
                {
                    workerData.CurHungry = System.Math.Max(
                        0.0f,
                        workerData.CurHungry - (deltaTime * WorkerConditionConstant.HungryDecayPerSecond * env.HungryTerrain));
                }

                // 疲劳值自然累积（受地形 + 天气 + 温度影响，雨雪天/严寒加速，CurTired 越大越疲）
                if (workerData.CurTired < workerData.MaxTired)
                {
                    workerData.CurTired = System.Math.Min(
                        workerData.MaxTired,
                        workerData.CurTired + (deltaTime * WorkerConditionConstant.TiredDecayPerSecond
                            * env.TiredTerrain * weatherMult * env.Temperature));
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

                // 压力自然衰减（无任务/轻任务时缓慢降压；工作期间 Execute 累积更快，净效果为上升）
                if (workerData.CurStress > 0)
                {
                    workerData.CurStress = System.Math.Max(
                        0.0f,
                        workerData.CurStress - (deltaTime * WorkerConditionConstant.StressDecayPerSecond));
                }

                // 士气动态：困苦（饥饿/疲劳/高压）→ 下降；安好 → 回升。
                // 困苦度 = 饥饿不足×0.4 + 疲劳×0.4 + 压力×0.2，安好时为 0。
                {
                    float hungryRatio = workerData.MaxHungry > 0 ? workerData.CurHungry / workerData.MaxHungry : 0f;
                    float tiredRatio = workerData.MaxTired > 0 ? workerData.CurTired / workerData.MaxTired : 0f;
                    float stressRatio = workerData.MaxStress > 0 ? workerData.CurStress / workerData.MaxStress : 0f;
                    float suffer = ((1f - hungryRatio) * 0.4f) + (tiredRatio * 0.4f) + (stressRatio * 0.2f);
                    float moraleDelta = (WorkerConditionConstant.MoraleRecoverPerSecond
                        - (suffer * WorkerConditionConstant.MoraleSufferDecayPerSecond)) * deltaTime;
                    workerData.CurMorale = System.Math.Max(
                        0f,
                        System.Math.Min(workerData.MaxMorale, workerData.CurMorale + moraleDelta));
                }

                conditionManager.UpdateWorkerCondition(worker);
            }

            // 子系统定时刷新（内部有节流控制）
            ServiceLocator.Get<IWorkerSupplyIssueManager>().Tick();
            ServiceLocator.Get<IWorkerTaskCongestionAdvisor>().Tick();
            ServiceLocator.Get<IColonyCommandCenterService>().Tick();
            ServiceLocator.Get<ISkillManager>().Tick();
            NearbyItemPickupHUD.Instance?.Tick();
        }

        /// <summary>
        /// 分帧轮换刷新环境倍率缓存：每帧只游标推进约 1/15 的 Worker（30fps 下 0.5s 内全员轮换一遍），
        /// 且仅对到期条目真正执行地形/温度查询——把几百次/帧的 WorldToCell 摊薄为 ~20 次/帧且无周期性尖峰。
        /// </summary>
        private void RefreshEnvBatch(
            List<AWorker> workers,
            ITerrainEffectService terrainEffect,
            ITemperatureEffectService tempEffect,
            float deltaTime)
        {
            int count = workers.Count;
            if (count == 0)
            {
                if (this.envCache.Count > 0)
                {
                    this.envCache.Clear();
                }

                return;
            }

            float now = this.elapsed;
            int batch = (count / 15) + 1;
            this.envCursor %= count;
            for (int i = 0; i < batch; i++)
            {
                AWorker worker = workers[this.envCursor];
                this.envCursor = (this.envCursor + 1) % count;
                if (worker == null)
                {
                    continue;
                }

                int instanceId = worker.GetInstanceID();
                if (this.envCache.TryGetValue(instanceId, out EnvMultiplier env)
                    && now < env.NextRefreshTime)
                {
                    continue;
                }

                env.TiredTerrain = terrainEffect != null ? terrainEffect.GetWorkerTiredDecayMultiplier(worker) : 1.0f;
                env.HungryTerrain = terrainEffect != null ? terrainEffect.GetWorkerHungryDecayMultiplier(worker) : 1.0f;
                env.Temperature = tempEffect != null ? tempEffect.GetWorkerFatigueDecayMultiplier(worker) : 1.0f;
                env.NextRefreshTime = now + EnvRefreshInterval;
                this.envCache[instanceId] = env;
            }

            // 定期清理已移除 Worker 的残留条目（Worker 死亡后不再被游标触达）
            this.staleCleanTimer += deltaTime;
            if (this.staleCleanTimer < StaleCleanInterval)
            {
                return;
            }

            this.staleCleanTimer = 0f;
            HashSet<int> liveIds = new HashSet<int>(count);
            foreach (AWorker worker in workers)
            {
                if (worker != null)
                {
                    liveIds.Add(worker.GetInstanceID());
                }
            }

            if (this.envCache.Count == liveIds.Count)
            {
                return;
            }

            List<int> staleIds = new List<int>();
            foreach (int id in this.envCache.Keys)
            {
                if (!liveIds.Contains(id))
                {
                    staleIds.Add(id);
                }
            }

            foreach (int id in staleIds)
            {
                this.envCache.Remove(id);
            }
        }
    }
}
