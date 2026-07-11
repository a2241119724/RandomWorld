namespace LAB2D.Domain.Common
{
    public interface IMapWalkabilityQuery
    {
        bool IsCanReach(GameGridPosition position);
    }
}
