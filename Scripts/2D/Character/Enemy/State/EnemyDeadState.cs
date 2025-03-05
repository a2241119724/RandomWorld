using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    public class EnemyDeadState : CharacterState<Enemy>
    {
        private float _recordTime = 0.0f;
        private const float deadTime = 0.5f; // 死亡时间
        /// <summary>
        /// 生成Item的总概率值
        /// </summary>
        private int dropTotal;
        /// <summary>
        /// key:获取对应item的概率值
        /// </summary>
        private Dictionary<int, TileBase> pToDropItem;

        public EnemyDeadState(Enemy character) : base(character)
        {
            pToDropItem = new Dictionary<int, TileBase>();
            List<Item> items = ItemFactory.Instance.genBackpackItems();
            addDropItem(10, null);
            foreach (BackpackItem item in items)
            {
                addDropItem(10, item.tile);
            }
        }

        public override void OnEnter()
        {
            base.OnEnter();
            //LogManager.Instance.log("DeadState", LogManager.LogLevel.Warning);
            // 如果敌人初次进入死亡状态,那么禁用敌人的一些组件(碰撞体组件)
            Character.transform.GetComponent<Collider2D>().enabled = false;
            PlayerManager.Instance.Mine.GetComponent<Player>().addExperienceValue(5); // 增加经验值
            //播放死亡动画
            //animator.applyRootMotion = true;
            //animator.SetTrigger("toDie");
        }

        public override void OnExit()
        {
            base.OnExit();
            Character.GetComponent<Enemy>().enabled = false;
        }

        public override void OnUpdate()
        {
            _recordTime += Time.deltaTime;
            if (_recordTime > deadTime) {
                dropItem();
                //if (item != null)
                //{
                //    GameObject g = Object.Instantiate(item);
                //    if (g != null)
                //    {
                //        g.name = item.name;
                //        g.transform.position = Character.transform.position;
                //        // 设置层级
                //        g.layer = LayerMask.NameToLayer("Item");
                //    }
                //    else
                //    {
                        //LogManager.Instance.log("item Instantiate Error!!!", LogManager.LogLevel.Error);
                //    }
                //}
                //Object.Destroy(character.gameObject); // Destroy不会立即销毁,下一帧销毁
                PhotonNetwork.Destroy(Character.gameObject); // Destroy不会立即销毁,下一帧销毁
                // 执行OnExit并关闭脚本
                Character.Manager.changeState(EnemyStateType.Wander);
            }
        }

        /// <summary>
        /// 概率获取掉落道具
        /// </summary>
        /// <returns>道具</returns>
        public void dropItem()
        {
            int rand = Random.Range(0, dropTotal);
            // 转为数组下标
            Vector3Int pos = IsAvailableMap.Instance.genAvailablePosMap(
                TileMap.Instance.worldPosToMapPos(Character.transform.position), 3, true);
            if(pos == default) return;
            foreach (KeyValuePair<int,TileBase> dropItem in pToDropItem)
            {
                if (rand <= dropItem.Key)
                {
                    if (dropItem.Value == null) break;
                    ItemData itemData = ItemDataManager.Instance.getByName(dropItem.Value.name);
                    ResourceInfo resourceInfo = new ResourceInfo(itemData.id, 1);
                    ItemMap.Instance.putDownToDrop(pos, dropItem.Value, resourceInfo);
                    break;
                }
            }
        }

        /// <summary>
        /// 添加可掉落物品到字典中
        /// </summary>
        /// <param name="value"></param>
        /// <param name="tileBase"></param>
        private void addDropItem(int value, TileBase tileBase) {
            dropTotal += value;
            pToDropItem.Add(dropTotal, tileBase);
        }
    }
}