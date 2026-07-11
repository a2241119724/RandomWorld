namespace LAB2D
{
    public interface IEnemySpawnService
    {
        long CreateEnemy(GameGridPosition position);
        int CountAliveEnemies();
    }
}
