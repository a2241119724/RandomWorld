// 批量接口定义 — WorkerUpdateSystem 消费的 Gameplay Manager 抽象

namespace LAB2D.Gameplay
{
    using System;
    using LAB2D.Character.Worker;

    public interface IWorkerConditionManager { void UpdateWorkerCondition(AWorker worker); }
    public interface IWorkerSupplyIssueManager { void Tick(); }
    public interface IWorkerTaskCongestionAdvisor { void Tick(); }
    public interface ISkillManager { void Tick(); void Initialize(); }
    public interface IPlayerVitalAlertManager { void Tick(); }
}
