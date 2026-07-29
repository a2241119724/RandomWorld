namespace LAB2D.Core
{
    using LAB2D;
    using LAB2D.Character;
    using LAB2D.Character.Worker;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Gameplay;
    using LAB2D.UI.Character;
    using LAB2D.UI.Panel.PanelUI;
    using System;
    using UnityEngine;

    /// <summary>
    /// 全局服务提供者 — 从 AWorkerTask 中提取的跨模块 Provider。
    /// 这些 Provider 原本定义在 AWorkerTask 中作为全局服务定位器使用，
    /// 但实际调用方全部是 Enemy/Manager/UI 等非 Task 模块。
    /// 迁移到此处的目的是解耦 AWorkerTask 与全局服务定位器职责。
    ///
    /// 迁移策略：AWorkerTask 中保留 [Obsolete] 代理属性桥接到此类的对应字段，
    /// 后续 PR 中逐步将外部调用方迁移到直接使用 GameServices。
    /// </summary>
    public static class GameServices
    {
        // --- Enemy ---
        public static System.Func<AttackEffectManager.EffectTypeEnum, float, ParticleSystem> AttackEffectProvider { get; set; }
            = (type, rad) => ServiceLocator.Get<AttackEffectManager>().GetEffect(type, rad);

        public static System.Action<AEnemy> EnemyRemoveProvider { get; set; }
            = (enemy) => ServiceLocator.Get<EnemyManager>().Remove(enemy);

        public static System.Func<bool> EnemyCanCreateProvider { get; set; }
            = () => ServiceLocator.Get<EnemyManager>().CanCreateEnemy();

        public static System.Action<AEnemy, Character, int> EnemyDefeatedProvider { get; set; }
            = (enemy, attacker, xp) => ServiceLocator.Get<GameplaySessionStats>().RecordEnemyDefeated(enemy, attacker, xp);

        public static System.Func<int> WaveIndexProvider { get; set; }
            = () => ServiceLocator.Get<WaveManager>() != null ? ServiceLocator.Get<WaveManager>().CurrentWaveIndex - 1 : 0;

        // --- Player ---
        public static System.Func<int> PlayerCountProvider { get; set; }
            = () => ServiceLocator.Get<PlayerManager>().Count();

        public static System.Func<int, Character> PlayerGetProvider { get; set; }
            = (i) => ServiceLocator.Get<PlayerManager>().Get(i);

        // --- Worker ---
        public static System.Func<int> WorkerCountProvider { get; set; }
            = () => ServiceLocator.Get<WorkerManager>().Count();

        public static System.Func<int, Character> WorkerGetProvider { get; set; }
            = (i) => ServiceLocator.Get<WorkerManager>().Get(i);

        public static System.Action<AWorker> FurnitureBedProvider { get; set; }
            = (worker) => ServiceLocator.Get<FurnitureManager>().RemoveWorkerFromBed(worker);

        // --- Network ---
        public static System.Func<bool> NetworkIsOnlineProvider { get; set; }
            = () => ServiceLocator.Get<NetworkConnect>() != null && ServiceLocator.Get<NetworkConnect>().IsOnline;

        public static System.Func<bool> NetworkIsMasterClientProvider { get; set; }
            = () => Photon.Pun.PhotonNetwork.IsMasterClient;

        public static System.Action<GameObject> NetworkDestroyProvider { get; set; }
            = (go) => Photon.Pun.PhotonNetwork.Destroy(go);

        // --- UI / Feedback ---
        public static System.Action<Vector3, float, bool, bool> FloatingTextProvider { get; set; }
            = (pos, dmg, crit, combo) => ServiceLocator.Get<FloatingTextManager>().SpawnDamageText(pos, dmg, crit, combo);

        public static System.Action<string> ShowTipProvider { get; set; }
            = (message) => { if (ServiceLocator.Get<GlobalInit>() != null) ServiceLocator.Get<GlobalInit>().ShowTip(message); };

        public static System.Action<AWorker> LocateWorkerUIAddProvider { get; set; }
            = (worker) => ServiceLocator.Get<LocateWorkerUI>().AddWorkerItem(worker);

        public static System.Action<AWorker> LocateWorkerUIRemoveProvider { get; set; }
            = (worker) => ServiceLocator.Get<LocateWorkerUI>().RemoveWorkerItem(worker);

        public static System.Func<string> NameGeneratorProvider { get; set; }
            = () => ServiceLocator.Get<NameGenerator>().GetRandomName();

        // --- Async Progress ---
        public static System.Action<string> AsyncProgressSetTipProvider { get; set; }
            = (tip) => ServiceLocator.Get<AsyncProgressUI>().SetTip(tip);

        public static System.Action<System.Action> AsyncProgressCompleteProvider { get; set; }
            = (callback) => ServiceLocator.Get<AsyncProgressUI>().Complete += new AsyncProgressUI.CompleteDelegate(callback);

        public static System.Action AsyncProgressAddOneProvider { get; set; }
            = () => ServiceLocator.Get<AsyncProgressUI>().AddOneProcess();

        public static System.Action<int> AsyncProgressAddTotalProvider { get; set; }
            = (total) => ServiceLocator.Get<AsyncProgressUI>().AddTotal(total);

        // --- Resource ---
        public static System.Func<string, bool, GameObject> ResourceInstantiateProvider { get; set; }
            = (name, active) => ServiceLocator.Get<ResourceManager>().Instantiate(name, active);
    }
}
