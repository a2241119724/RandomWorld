using UnityEngine;

namespace LAB2D
{
    public class SelectUI : MonoBehaviourInit
    {
        public Character Character { get; set; }
        public Vector3Int Target { get; private set; }

        private void Awake()
        {
            Init();
        }

        private void Update()
        {
            if (Character != null)
            {
                transform.position = Character.transform.position;
            }
        }

        public void setTarget(Vector3Int posMap)
        {
            Target = posMap;
            Vector3 pos = TileMap.Instance.MapPosToWorldPos(posMap);
            transform.position = new Vector3(pos.x, pos.y, 0.0f);
        }

        public override void Init()
        {
            base.Init();
            transform.position = ResourceConstant.VECTOR3_DEFAULT;
            Target = ResourceConstant.VECTOR3INT_DEFAULT;
            Character = null;
        }
    }
}
