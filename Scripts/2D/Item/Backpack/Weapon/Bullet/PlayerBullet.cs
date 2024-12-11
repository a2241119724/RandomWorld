using Photon.Pun;
using UnityEngine;

namespace LAB2D
{
    public class PlayerBullet : Bullet
    {
        private PhotonView photonView;
        private Player player = null; // 发出子弹的玩家

        protected override void Awake()
        {
            base.Awake();
            layerMask = LayerMask.GetMask("Tile", "Enemy");
            name = "PlayerBullet";
        }

        protected override void Start()
        {
            base.Start();
            photonView = GetComponent<PhotonView>();
            if (photonView == null) {
                Debug.LogError("photonView Not Found!!!");
                return;
            }
        }

        protected override void Update()
        {
            if (!photonView.IsMine && PhotonNetwork.IsConnected) return;
            base.Update();
        }


        public void setPlayer(Player player)
        {
            this.player = player;
        }

        public override void hitObject()
        {
            if (rayCastHit2D.transform.gameObject.CompareTag("Enemy")) // 击中敌人处理
            {
                Enemy e = rayCastHit2D.transform.GetComponent<Enemy>();
                e.setPlayer(player);
                e.reduceHp(Damage);
            }
        }
    }
}
