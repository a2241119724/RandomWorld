namespace LAB2D
{
    /// <summary>
    /// Abstract time provider for domain services.
    /// Implementations wrap Unity Time, System.DateTime, or test fakes.
    /// </summary>
    public interface IGameTime
    {
        /// <summary>Delta time since last frame (like Time.deltaTime).</summary>
        float DeltaTime { get; }

        /// <summary>Time since game start (like Time.time).</summary>
        float Time { get; }

        /// <summary>Real time since game start (like Time.realtimeSinceStartup).</summary>
        float RealtimeSinceStartup { get; }
    }

    /// <summary>
    /// Abstract map walkability query.
    /// Implementations wrap BuildMap/TileMap or test stubs.
    /// </summary>
    public interface IMapWalkabilityQuery
    {
        /// <summary>
        /// Check if a grid position can be reached (walked to).
        /// </summary>
        bool IsCanReach(GameGridPosition position);
    }

    /// <summary>
    /// Abstract item definition provider.
    /// Implementations wrap ItemDataManager or test stubs.
    /// </summary>
    public interface IItemDefinitionProvider
    {
        /// <summary>
        /// Get item type by item ID.
        /// </summary>
        int GetItemTypeById(int itemId);
    }

    /// <summary>
    /// Abstract spawn point provider for map positions.
    /// Implementations wrap TileMap or test stubs.
    /// </summary>
    public interface IMapSpawnPointProvider
    {
        /// <summary>
        /// Get a random reachable spawn position on the map.
        /// </summary>
        GameGridPosition GetRandomReachablePosition();

        /// <summary>
        /// Convert a world position to a grid position.
        /// </summary>
        GameGridPosition WorldPosToGridPos(GameVector2 worldPosition);

        /// <summary>
        /// Convert a grid position to a world position.
        /// </summary>
        GameVector2 GridPosToWorldPos(GameGridPosition gridPosition);
    }

    /// <summary>
    /// Abstract enemy spawn service.
    /// Implementations wrap EnemyManager or test stubs.
    /// </summary>
    public interface IEnemySpawnService
    {
        /// <summary>
        /// Create an enemy at the given position.
        /// Returns the entity ID of the spawned enemy, or 0 on failure.
        /// </summary>
        long CreateEnemy(GameGridPosition position);

        /// <summary>
        /// Count currently alive enemies.
        /// </summary>
        int CountAliveEnemies();
    }

    /// <summary>
    /// Abstract logger for game systems.
    /// Implementations wrap Unity Debug, LogManager, or test stubs.
    /// </summary>
    public interface IGameLogger
    {
        void Log(string message);
        void LogWarning(string message);
        void LogError(string message);
    }
}
