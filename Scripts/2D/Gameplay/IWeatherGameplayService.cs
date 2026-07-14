namespace LAB2D.Gameplay
{
    using System;

    /// <summary>
    /// 天气玩法服务接口 — 解耦 Singleton 直接引用。
    /// 实现：WeatherGameplayEffect。
    /// 消费者通过 ServiceLocator.Get&lt;IWeatherGameplayService&gt;() 获取。
    /// </summary>
    public interface IWeatherGameplayService
    {
        WeatherGameplayState CurrentState { get; }
        float EnergyRecoveryMultiplier { get; }
        event Action<WeatherGameplayState> OnWeatherEffectChanged;
        float GetAdjustedCharacterMoveSpeed(Character.Character character, float baseSpeed);
        float GetWorkerTaskProgressMultiplier(LAB2D.Enum.WorkerTaskType taskType);
    }
}
