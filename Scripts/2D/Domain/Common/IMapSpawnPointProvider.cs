namespace LAB2D.Domain.Common
{
    public interface IMapSpawnPointProvider
    {
        GameGridPosition GetRandomReachablePosition();
        GameGridPosition WorldPosToGridPos(GameVector2 worldPosition);
        GameVector2 GridPosToWorldPos(GameGridPosition gridPosition);
    }
}
