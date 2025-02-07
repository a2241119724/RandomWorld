using Photon.Pun;
using UnityEngine;

namespace LAB2D {
    public class EnemyBullet : Bullet
    {
        protected override void Awake() {
            base.Awake();
            layerMask = LayerMask.GetMask("Tile", "Player", "Worker");
        }

        public override void hitObject()
        {
            if (rayCastHit2D.transform.CompareTag("Player")) // 击中玩家处理
            {
                rayCastHit2D.transform.GetComponent<Character>().reduceHp(Damage);
            }
        }
    }
}
