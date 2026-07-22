namespace LAB2D.UnityAdapter
{
    using LAB2D;
    using LAB2D.Domain.Common;
    using UnityEngine;

    /// <summary>
    /// 地图查询接口的 Unity 实现，封装 BuildMap 和 TileMap。
    /// </summary>
    public sealed class UnityMapAdapter : IMapWalkabilityQuery, IMapSpawnPointProvider
    {
        /// <inheritdoc/>
        public bool IsCanReach(GameGridPosition position)
        {
            Vector3Int unityPos = UnityVectorAdapter.ToUnityVector3Int(position);
            return Core.ServiceLocator.TryGet(out BuildMap bm) && bm.IsCanReach(unityPos);
        }

        /// <inheritdoc/>
        public GameGridPosition GetRandomReachablePosition()
        {
            if (!Core.ServiceLocator.TryGet(out TileMap tm))
            {
                return new GameGridPosition(0, 0, 0);
            }

            Vector3Int randomPos = tm.GenCanReachPos();
            return UnityVectorAdapter.ToGameGridPosition(randomPos);
        }

        /// <inheritdoc/>
        public GameGridPosition WorldPosToGridPos(GameVector2 worldPosition)
        {
            if (!Core.ServiceLocator.TryGet(out TileMap tm))
            {
                return new GameGridPosition(0, 0, 0);
            }

            Vector3 worldPos = UnityVectorAdapter.ToUnityVector3(worldPosition);
            Vector3 gridPos = tm.WorldPosToMapPos(worldPos);
            return UnityVectorAdapter.ToGameGridPosition(Vector3Int.RoundToInt(gridPos));
        }

        /// <inheritdoc/>
        public GameVector2 GridPosToWorldPos(GameGridPosition gridPosition)
        {
            if (!Core.ServiceLocator.TryGet(out TileMap tm))
            {
                return new GameVector2(0f, 0f);
            }

            Vector3Int gridPos = UnityVectorAdapter.ToUnityVector3Int(gridPosition);
            Vector3 worldPos = tm.MapPosToWorldPos(gridPos);
            return UnityVectorAdapter.ToGameVector2(worldPos);
        }
    }
}
