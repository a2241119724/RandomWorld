namespace LAB2D
{
    using Photon.Pun;
    using UnityEngine;

    /// <summary>
    /// 玩家子弹
    /// </summary>
    public class PlayerBullet : Bullet
    {
        private PhotonView photonView;

        /// <inheritdoc/>
        public override void HitObject()
        {
            // 击中敌人处理
            if (this.rayCastHit2D.transform.gameObject.CompareTag("Enemy"))
            {
                Enemy e = this.rayCastHit2D.transform.GetComponent<Enemy>();
                e.Target = this.Origin;
                e.ReduceHp(this.Damage);
            }
        }

        /// <inheritdoc/>
        protected override void Awake()
        {
            base.Awake();
            this.layerMask = LayerMask.GetMask("Tile", "Enemy");
        }

        /// <inheritdoc/>
        protected override void Start()
        {
            base.Start();
            this.photonView = this.GetComponent<PhotonView>();
            if (this.photonView == null)
            {
                LogManager.Instance.Log("photonView Not Found!!!", LogManager.LogLevel.Error);
                return;
            }
        }

        /// <inheritdoc/>
        protected override void Update()
        {
            if (NetworkConnect.Instance.IsOnline && !this.photonView.IsMine && PhotonNetwork.IsConnected) return;
            base.Update();
        }
    }
}
