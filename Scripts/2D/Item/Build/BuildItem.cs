using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public abstract class BuildItem : Item
    {
        public int width = 1;
        public int height = 1;
        public bool isBottomLeft = false;
        [NonSerialized]
        public TileBase tile;

        public virtual void addBuildTask(Vector3Int centerMap)
        {
            BuildMap.Instance.addBuilding(centerMap, tile).addTask();
        }
    }

    public abstract class BuildItemObject : ItemObject
    {
    }

    /// <summary>
    /// ����ʹ�÷����ҵ�����
    /// �����뵽BuildMenu��
    /// </summary>
    public interface DontShow { }
}

