namespace LAB2D
{
    using System.Collections.Generic;
    using Photon.Pun;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 敌人死亡状态.
    /// </summary>
    public class EnemyDeadState : EnemyState
    {
        private const float DeadTime = 0.5f; // 死亡时间
        private readonly Dictionary<int, TileBase> probToDropItem; // key:获取对应item的概率值
        private float recordTime = 0.0f;
        private int dropTotal; // 生成Item的总概率值

        public EnemyDeadState(Enemy character)
            : base(character)
        {
            this.probToDropItem = new Dictionary<int, TileBase>();
            List<AItem> items = ItemInstanceFactory.Instance.GenBackpackItems();
            this.AddDropItem(10, null);
            foreach (ABackpackItem item in items)
            {
                this.AddDropItem(10, item.Tile);
            }
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();

            // LogManager.Instance.log("DeadState", LogManager.LogLevel.Warning);
            // 如果敌人初次进入死亡状态,那么禁用敌人的一些组件(碰撞体组件)
            this.Character.transform.GetComponent<Collider2D>().enabled = false;
            PlayerManager.Instance.Mine.GetComponent<Player>().AddExperienceValue(5); // 增加经验值

            // 播放死亡动画
            // animator.applyRootMotion = true;
            // animator.SetTrigger("toDie");
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            base.OnExit();
            this.Character.GetComponent<Enemy>().enabled = false;
        }

        /// <inheritdoc/>
        public override void OnUpdate()
        {
            this.recordTime += Time.deltaTime;
            if (this.recordTime > DeadTime)
            {
                this.DropItem();

                // if (item != null)
                // {
                //     GameObject g = Object.Instantiate(item);
                //     if (g != null)
                //     {
                //         g.name = item.name;
                //         g.transform.position = Character.transform.position;
                //         // 设置层级
                //         g.layer = LayerMask.NameToLayer("Item");
                //     }
                //     else
                //     {
                // LogManager.Instance.log("item Instantiate Error!!!", LogManager.LogLevel.Error);
                //     }
                // }
                // Object.Destroy(character.gameObject); // Destroy不会立即销毁,下一帧销毁
                PhotonNetwork.Destroy(this.Character.gameObject); // Destroy不会立即销毁,下一帧销毁

                // 执行OnExit并关闭脚本
                this.Character.Manager.ChangeState(EnemyStateTypeEnum.Wander);
            }
        }

        /// <summary>
        /// 概率获取掉落道具.
        /// </summary>
        public void DropItem()
        {
            int rand = Random.Range(0, this.dropTotal);

            // 转为数组下标
            Vector3Int pos = IsAvailableMap.Instance.GenAvailablePosMap(
                TileMap.Instance.WorldPosToMapPos(this.Character.transform.position), 3, true);
            if (pos == default)
            {
                return;
            }

            foreach (KeyValuePair<int, TileBase> dropItem in this.probToDropItem)
            {
                if (rand <= dropItem.Key)
                {
                    if (dropItem.Value == null)
                    {
                        break;
                    }

                    ItemData itemData = ItemDataManager.Instance.GetByName(dropItem.Value.name);
                    ResourceInfo resourceInfo = new (itemData.Id, 1);
                    ItemMap.Instance.PutDownToDrop(pos, dropItem.Value, resourceInfo);
                    break;
                }
            }
        }

        /// <summary>
        /// 添加可掉落物品到字典中.
        /// </summary>
        private void AddDropItem(int value, TileBase tileBase)
        {
            this.dropTotal += value;
            this.probToDropItem.Add(this.dropTotal, tileBase);
        }
    }
}