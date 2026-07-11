namespace LAB2D
{
    public interface IMapSpawnPointProvider
    {
        GameGridPosition GetRandomReachablePosition();
        GameGridPosition WorldPosToGridPos(GameVector2 worldPosition);
        GameVector2 GridPosToWorldPos(GameGridPosition gridPosition);
    }
}
