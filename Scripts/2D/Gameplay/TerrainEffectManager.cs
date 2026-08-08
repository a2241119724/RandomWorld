namespace LAB2D.Gameplay
{
    using LAB2D;
    using LAB2D.Character.Worker;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Core;
    using LAB2D.Map;
    using Character = LAB2D.Character.Character;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 地形效果管理器。
    /// 根据角色当前所在地形，提供移速倍率和工人疲劳/饥饿消耗倍率。
    /// 本类不修改存档结构，不写入资源，不参与 Photon 同步。
    ///
    /// 注意：地形数据（TileMap）在场景加载后才可用，因此
    /// GetTerrainAtCharacter() 会安全处理 TileMap 为空的情况。
    /// </summary>
    public class TerrainEffectManager : Singleton<TerrainEffectManager>, ITerrainEffectService
    {
        private bool enabled = true;
        private TerrainConfigDatabase terrainDb;
        private IGameLogger gameLogger;

        private IGameLogger GameLogger
        {
            get
            {
                if (this.gameLogger == null)
                {
                    this.gameLogger = ServiceLocator.Get<IGameLogger>();
                }

                return this.gameLogger;
            }
        }

        /// <summary>
        /// 确保 TerrainConfigDatabase 已加载。
        /// </summary>
        private TerrainConfigDatabase TerrainDb
        {
            get
            {
                if (this.terrainDb == null)
                {
                    this.terrainDb = ServiceLocator.Get<TerrainConfigDatabase>();
                }

                return this.terrainDb;
            }
        }

        /// <inheritdoc/>
        public int GetTerrainAtCharacter(Character character)
        {
            if (character == null) return 0;

            // 使用项目约定：AWorkerTask.TileMapWorldToMapProvider 将世界坐标转为地图坐标
            Vector3Int mapPos = AWorkerTask.TileMapWorldToMapProvider(character.transform.position);

            TileMap tileMap = ServiceLocator.Get<TileMap>();
            var tileMapData = tileMap?.TileMapDataLAB;
            if (tileMapData == null ||
                mapPos.x < 0 || mapPos.x >= tileMapData.Height ||
                mapPos.y < 0 || mapPos.y >= tileMapData.Width)
            {
                return 0;
            }

            return tileMapData.MapTiles[mapPos.x, mapPos.y];
        }

        /// <inheritdoc/>
        public float GetMoveSpeedMultiplier(Character character)
        {
            if (!this.enabled || character == null) return 1.0f;

            int terrainId = this.GetTerrainAtCharacter(character);
            var db = this.TerrainDb;
            if (db == null) return 1.0f;

            if (character is Player)
            {
                return db.GetPlayerMoveSpeedMultiplier(terrainId);
            }

            if (character is AWorker)
            {
                return db.GetWorkerMoveSpeedMultiplier(terrainId);
            }

            if (character is AEnemy)
            {
                return db.GetEnemyMoveSpeedMultiplier(terrainId);
            }

            return 1.0f;
        }

        /// <inheritdoc/>
        public float GetAdjustedCharacterMoveSpeed(Character character, float baseSpeed)
        {
            float multiplier = this.GetMoveSpeedMultiplier(character);
            float safeMultiplier = multiplier < 0.0f ? 0.0f : multiplier;
            return baseSpeed * safeMultiplier;
        }

        /// <inheritdoc/>
        public float GetWorkerTiredDecayMultiplier(AWorker worker)
        {
            if (!this.enabled || worker == null) return 1.0f;

            int terrainId = this.GetTerrainAtCharacter(worker);
            var db = this.TerrainDb;
            if (db == null) return 1.0f;
            return db.GetWorkerTiredDecayMultiplier(terrainId);
        }

        /// <inheritdoc/>
        public float GetWorkerHungryDecayMultiplier(AWorker worker)
        {
            if (!this.enabled || worker == null) return 1.0f;

            int terrainId = this.GetTerrainAtCharacter(worker);
            var db = this.TerrainDb;
            if (db == null) return 1.0f;
            return db.GetWorkerHungryDecayMultiplier(terrainId);
        }

        /// <inheritdoc/>
        public void Enable()
        {
            this.enabled = true;
        }

        /// <inheritdoc/>
        public void Disable()
        {
            this.enabled = false;
        }
    }
}
