using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class SelectUI : MonoBehaviourInit
    {
        public Character Character { get; set; }
        public Vector3Int Target { get; private set; }

        private void Awake()
        {
            init();
        }

        private void Update()
        {
            if (Character != null)
            {
                transform.position = Character.transform.position;
            }
        }

        public void setTarget(Vector3Int posMap) {
            Target = posMap;
            Vector3 pos = TileMap.Instance.mapPosToWorldPos(posMap);
            transform.position = new Vector3(pos.x, pos.y, 0.0f);
        }

        public override void init()
        {
            base.init();
            transform.position = ResourceConstant.VECTOR3_DEFAULT;
            Target = ResourceConstant.VECTOR3INT_DEFAULT;
            Character = null;
        }
    }
}
