namespace LAB2D.Domain.Common
{
    public interface IEnemySpawnService
    {
        long CreateEnemy(GameGridPosition position);
        int CountAliveEnemies();
    }
}
