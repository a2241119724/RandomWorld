namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// Select UI
    /// </summary>
    public class SelectUI : MonoBehaviourInit
    {
        /// <summary>
        /// 选中的角色
        /// </summary>
        public Character Character { get; set; }

        /// <summary>
        /// 选中的目标
        /// </summary>
        public Vector3Int Target { get; private set; }

        /// <summary>
        /// 设置Select的位置
        /// </summary>
        /// <param name="posMap">位置</param>
        public void SetTarget(Vector3Int posMap)
        {
            this.Target = posMap;
            Vector3 worldPos = TileMap.Instance.MapPosToWorldPos(posMap);
            worldPos.z = 0;
            this.transform.position = worldPos;
        }

        /// <inheritdoc/>
        public override void Init()
        {
            base.Init();
            this.transform.position = ResourceConstant.VECTOR3_DEFAULT;
            this.Target = ResourceConstant.VECTOR3INT_DEFAULT;
            this.Character = null;
        }

        public void Awake()
        {
            this.Init();
        }

        public void Update()
        {
            if (this.Character != null)
            {
                this.transform.position = this.Character.transform.position;
            }
        }
    }
}
