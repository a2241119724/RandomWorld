namespace LAB2D.Gameplay
{
    using LAB2D.Character.Worker;

    /// <summary>
    /// 地形效果服务接口 — 解耦 Singleton 直接引用。
    /// 实现：TerrainEffectManager。
    /// 消费者通过 ServiceLocator.Get&lt;ITerrainEffectService&gt;() 获取。
    /// </summary>
    public interface ITerrainEffectService
    {
        /// <summary>
        /// 获取角色当前所在地形 ID。越界或数据不可用时返回 0。
        /// </summary>
        int GetTerrainAtCharacter(Character.Character character);

        /// <summary>
        /// 获取角色在当前地形上的移速倍率（已按角色类型区分）。
        /// </summary>
        float GetMoveSpeedMultiplier(Character.Character character);

        /// <summary>
        /// 获取角色在当前地形上的调整后移速。
        /// </summary>
        float GetAdjustedCharacterMoveSpeed(Character.Character character, float baseSpeed);

        /// <summary>
        /// 获取工人在当前地形上的疲劳自然衰减倍率。
        /// </summary>
        float GetWorkerTiredDecayMultiplier(AWorker worker);

        /// <summary>
        /// 获取工人在当前地形上的饥饿自然衰减倍率。
        /// </summary>
        float GetWorkerHungryDecayMultiplier(AWorker worker);

        /// <summary>
        /// 启用地形效果。
        /// </summary>
        void Enable();

        /// <summary>
        /// 禁用地形效果（所有倍率回到 1.0）。
        /// </summary>
        void Disable();
    }
}
