namespace LAB2D.Domain.Common
{
    /// <summary>
    /// 领域服务的抽象时间提供者。
    /// 实现类封装Unity Time、System.DateTime或测试假数据。
    /// </summary>
    public interface IGameTime
    {
        float DeltaTime { get; }
        float Time { get; }
        float RealtimeSinceStartup { get; }
    }
}
