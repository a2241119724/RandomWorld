// Gameplay Manager 接口 — 解耦 Singleton 直接引用
// WorkerUpdateSystem 和 GlobalInit 的消费者通过 ServiceLocator.Get<T>() 获取
// 其他 Manager 目前仍通过 Instance 访问，后续逐步迁移

namespace LAB2D.Gameplay
{
    using LAB2D.Character.Worker;

    // Worker 子系统
    public interface IWorkerConditionManager { void UpdateWorkerCondition(AWorker worker); }
    public interface IWorkerSupplyIssueManager { void Tick(); }
    public interface IWorkerTaskCongestionAdvisor { void Tick(); }

    // 技能
    public interface ISkillManager { void Tick(); void Initialize(); }

    // 监控
    public interface IPlayerVitalAlertManager { void Tick(); }
}
