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
    public class BuildMap : BaseTileMap
    {
        public static BuildMap Instance { private set; get; }
        
        // Map�ر������±�
        private List<Vector3Int> targetMaps;


        protected override void Awake()
        {
            base.Awake();
            Instance = this;
            targetMaps = new List<Vector3Int>();
        }

        /// <summary>
        /// Color a 0.5f��������ײ�壬0.49f����û����ײ�壬
        /// </summary>
        /// <param name="targetMap"></param>
        /// <param name="tile"></param>
        public BuildMap addBuilding(Vector3Int targetMap, TileBase tile, bool isCollider=true) {
            tilemap.SetTile(targetMap, tile);
            tilemap.RemoveTileFlags(targetMap, TileFlags.LockColor);
            tilemap.SetColliderType(targetMap, Tile.ColliderType.None);
            tilemap.SetColor(targetMap, new Color(1,1,1, isCollider ? 0.5f : 0.49f));
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
        /// <param name="isPass">�Ƿ��ͨ��</param>
        /// <returns></returns>
        public BuildMap directBuild(Vector3Int targetMap, TileBase tile, bool isPass=true)
        {
            tilemap.SetTile(targetMap, tile);
            if (isPass)
            {
                tilemap.RemoveTileFlags(targetMap, TileFlags.LockColor);
                tilemap.SetColor(targetMap, new Color(1, 1, 1, 0.99f));
            }
            return this;
        }

        /// <summary>
        /// û����ײ������Color aΪ0.99f
        /// </summary>
        /// <param name="targetMap"></param>
        public void setComplete(Vector3Int targetMap)
        {
            if (tilemap.GetColor(targetMap).a == 0.5f)
            {
                tilemap.SetColliderType(targetMap, Tile.ColliderType.Sprite);
                tilemap.SetColor(targetMap, new Color(1, 1, 1, 1));
            }
            else
            {
                tilemap.SetColor(targetMap, new Color(1, 1, 1, 0.99f));
            }
            RoomManager.Instance.complete(targetMap);
        }

        public bool isBuilding(Vector3Int target)
        {
            return tilemap.GetColor(target).a < 1.0f;
        }

        public void cancelBuilding(Vector3Int targetMap)
        {
            tilemap.SetTile(targetMap, null);
            targetMaps.Remove(targetMap);
        }

        public void addTask()
        {
            Dictionary<int, ResourceInfo> resourceInfos = new Dictionary<int, ResourceInfo>();
            resourceInfos.Add(ItemDataManager.Instance.getByName("CustomWood").id,
                new ResourceInfo(ItemDataManager.Instance.getByName("CustomWood").id, 5));
            foreach (Vector3Int targetMap in targetMaps) { 
                // �������������õ�һ������㣬��Target����Ϊ��ʱInventory����û�в��ϣ�����default
                WorkerTaskManager.Instance.addTask(new WorkerBuildTask.BuildTaskBuilder().setBuildPos(targetMap)
                    .setNeedResource(new Dictionary<int, ResourceInfo>(resourceInfos)).build());
            }
            targetMaps.Clear();
        }

        /// <summary>
        /// �Ƿ����ͨ��,WorkerѰ·ʱʹ��
        /// </summary>
        /// <param name="posMap"></param>
        /// <returns></returns>
        public override bool isCanReach(Vector3Int posMap)
        {
            // �ſ���ͨ��
            return Mathf.Abs(tilemap.GetColor(posMap).a - 0.49f) < 1e-5
                || Mathf.Abs(tilemap.GetColor(posMap).a - 0.99f) < 1e-5 
                || base.isFreeTile(posMap);
        }
    }
}

