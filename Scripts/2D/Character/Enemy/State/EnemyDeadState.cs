using Photon.Pun;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    public class EnemyDeadState : CharacterState<Enemy>
    {
        private float recordTime = 0.0f;
        private float deadTime = 0.5f; // 死亡时间

        public EnemyDeadState(Enemy character) : base(character)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            //Debug.Log("DeadState");
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
            recordTime += Time.deltaTime;
            if (recordTime > deadTime) {
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
                //        Debug.LogError("item Instantiate Error!!!");
                //    }
                //}
                //Object.Destroy(character.gameObject); // Destroy不会立即销毁,下一帧销毁
                PhotonNetwork.Destroy(Character.gameObject); // Destroy不会立即销毁,下一帧销毁
                // 执行OnExit并关闭脚本
                Character.Manager.changeState(EnemyStateType.Wander);
                Character.target = null;
            }
        }

        /// <summary>
        /// 概率获取掉落道具
        /// </summary>
        /// <returns>道具</returns>
        private void dropItem()
        {
            int rand = Random.Range(0, 100);
            // 转为数组下标
            Vector3Int posMap = new Vector3Int(Mathf.RoundToInt(Character.transform.position.y),Mathf.RoundToInt(Character.transform.position.x), 0);
            if (rand < 10)
            {
            }
            else if (rand < 50)
            {
                ItemMap.Instance.ItemTileMap.SetTile(posMap, (TileBase)ResourcesManager.Instance.getAsset("AddHp"));
            }
            else if (rand < 80)
            {
                ItemMap.Instance.ItemTileMap.SetTile(posMap, (TileBase)ResourcesManager.Instance.getAsset("CustomSword"));
            }
            else
            {
                ItemMap.Instance.ItemTileMap.SetTile(posMap, (TileBase)ResourcesManager.Instance.getAsset("SingleGun"));
            }
        }
    }
}