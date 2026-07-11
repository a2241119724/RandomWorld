namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// Unity implementation of map query interfaces wrapping BuildMap and TileMap.
    /// </summary>
    public sealed class UnityMapAdapter : IMapWalkabilityQuery, IMapSpawnPointProvider
    {
        /// <inheritdoc/>
        public bool IsCanReach(GameGridPosition position)
        {
            if (BuildMap.Instance == null)
            {
                return false;
            }

            Vector3Int unityPos = UnityVectorAdapter.ToUnityVector3Int(position);
            return BuildMap.Instance.IsCanReach(unityPos);
        }

        /// <inheritdoc/>
        public GameGridPosition GetRandomReachablePosition()
        {
            if (TileMap.Instance == null)
            {
                return new GameGridPosition(0, 0, 0);
            }

            Vector3Int randomPos = TileMap.Instance.GenCanReachPos();
            return UnityVectorAdapter.ToGameGridPosition(randomPos);
        }

        /// <inheritdoc/>
        public GameGridPosition WorldPosToGridPos(GameVector2 worldPosition)
        {
            if (TileMap.Instance == null)
            {
                return new GameGridPosition(0, 0, 0);
            }

            Vector3 worldPos = UnityVectorAdapter.ToUnityVector3(worldPosition);
            Vector3 gridPos = TileMap.Instance.WorldPosToMapPos(worldPos);
            return UnityVectorAdapter.ToGameGridPosition(Vector3Int.RoundToInt(gridPos));
        }

        /// <inheritdoc/>
        public GameVector2 GridPosToWorldPos(GameGridPosition gridPosition)
        {
            if (TileMap.Instance == null)
            {
                return new GameVector2(0f, 0f);
            }

            Vector3Int gridPos = UnityVectorAdapter.ToUnityVector3Int(gridPosition);
            Vector3 worldPos = TileMap.Instance.MapPosToWorldPos(gridPos);
            return UnityVectorAdapter.ToGameVector2(worldPos);
        }
    }
}
