namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// 敌人子弹
    /// </summary>
    public class EnemyBullet : Bullet
    {
        protected override void Awake()
        {
            base.Awake();
            this.layerMask = LayerMask.GetMask("Tile", "Player", "Worker");
        }

        /// <inheritdoc/>
        public override void HitObject()
        {
            // 击中玩家处理
            if (this.rayCastHit2D.transform.CompareTag("Player"))
            {
                this.rayCastHit2D.transform.GetComponent<Character>().ReduceHp(this.Damage);
            }
        }
    }
}
