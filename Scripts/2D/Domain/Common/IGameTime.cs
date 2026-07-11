namespace LAB2D
{
    /// <summary>
    /// Abstract time provider for domain services.
    /// Implementations wrap Unity Time, System.DateTime, or test fakes.
    /// </summary>
    public interface IGameTime
    {
        float DeltaTime { get; }
        float Time { get; }
        float RealtimeSinceStartup { get; }
    }
}
