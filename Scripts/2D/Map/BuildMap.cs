using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    /// <summary>
    /// ʹ��alpha�ж�Worker�Ƿ����ͨ��
    /// ʹ��Collider�����,WorkerѰ·���,��·���ϵ�Tile��������ɣ����²���ͨ��
    /// �Ӱ봴��ֱ�ӾͲ���ͨ��
    /// </summary>
    public class BuildMap : MonoBehaviour
    {
        public static BuildMap Instance { private set; get; }
        public Tilemap BuildTileMap { get; set; }
        
        // Map�ر������±�
        private List<Vector3Int> targetMaps;

        private void Awake()
        {
            Instance = this;
            BuildTileMap = GetComponent<Tilemap>();
            targetMaps = new List<Vector3Int>();
        }

        /// <summary>
        /// Color a 0.5f��������ײ�壬0.49f����û����ײ�壬
        /// </summary>
        /// <param name="targetMap"></param>
        /// <param name="tile"></param>
        public BuildMap addBuilding(Vector3Int targetMap, TileBase tile, bool isCollider=true) {
            BuildTileMap.SetTile(targetMap, tile);
            BuildTileMap.RemoveTileFlags(targetMap, TileFlags.LockColor);
            BuildTileMap.SetColliderType(targetMap, Tile.ColliderType.None);
            BuildTileMap.SetColor(targetMap, new Color(1,1,1, isCollider ? 0.5f : 0.49f));
            if (!targetMaps.Contains(targetMap))
            {
                targetMaps.Add(targetMap);
            }
            return this;
        }

        /// <summary>
        /// ֱ�ӽ������,Worker
        /// </summary>
        /// <param name="targetMap"></param>
        /// <param name="tile"></param>
        /// <returns></returns>
        public BuildMap addPassBuild(Vector3Int targetMap, TileBase tile)
        {
            BuildTileMap.SetTile(targetMap, tile);
            BuildTileMap.RemoveTileFlags(targetMap, TileFlags.LockColor);
            BuildTileMap.SetColor(targetMap, new Color(1, 1, 1, 0.99f));
            return this;
        }

        /// <summary>
        /// ֱ�ӽ������
        /// </summary>
        /// <param name="targetMap"></param>
        /// <param name="tile"></param>
        /// <returns></returns>
        public BuildMap addNoPassBuild(Vector3Int targetMap, TileBase tile)
        {
            BuildTileMap.SetTile(targetMap, tile);
            return this;
        }

        /// <summary>
        /// û����ײ������Color aΪ0.99f
        /// </summary>
        /// <param name="targetMap"></param>
        public void setComplete(Vector3Int targetMap)
        {
            if (BuildTileMap.GetColor(targetMap).a == 0.5f)
            {
                BuildTileMap.SetColliderType(targetMap, Tile.ColliderType.Sprite);
                BuildTileMap.SetColor(targetMap, new Color(1, 1, 1, 1));
            }
            else
            {
                BuildTileMap.SetColor(targetMap, new Color(1, 1, 1, 0.99f));
            }
            RoomManager.Instance.complete(targetMap);
        }

        public bool isBuilding(Vector3Int target)
        {
            return BuildTileMap.GetColor(target).a < 1.0f;
        }

        public void cancelBuilding(Vector3Int targetMap)
        {
            BuildTileMap.SetTile(targetMap, null);
            targetMaps.Remove(targetMap);
        }

        public void addTask()
        {
            Dictionary<int, ResourceInfo> resourceInfos = new Dictionary<int, ResourceInfo>();
            resourceInfos.Add(ItemDataManager.Instance.getByName("Wood").id,new ResourceInfo(ItemDataManager.Instance.getByName("Wood").id, 5));
            foreach (Vector3Int targetMap in targetMaps) { 
                // �������������õ�һ������㣬��Target����Ϊ��ʱInventory����û�в��ϣ�����default
                WorkerTaskManager.Instance.addTask(new BuildTask.BuildTaskBuilder().setBuildPos(targetMap)
                    .setNeedResource(new NeedResource(Tool.DeepCopyByBinary(resourceInfos))).build());
            }
            targetMaps.Clear();
        }

        /// <summary>
        /// ����ͼ������,��Ϊnull�򲻿���
        /// </summary>
        /// <param name="posMap"></param>
        /// <returns></returns>
        public bool isAvailableTile(Vector3Int posMap) {
            return BuildTileMap.GetTile(posMap) == null;
        }

        /// <summary>
        /// �Ƿ����ͨ��,WorkerѰ·ʱʹ��
        /// </summary>
        /// <param name="posMap"></param>
        /// <returns></returns>
        public bool isPassTile(Vector3Int posMap)
        {
            return Mathf.Abs(BuildTileMap.GetColor(posMap).a - 0.49f) < 1e-5
                || Mathf.Abs(BuildTileMap.GetColor(posMap).a - 0.99f) < 1e-5;
        }

        public TileBase getTile(Vector3Int pos)
        {
            return BuildTileMap.GetTile(pos);
        }
    }
}

