using Photon.Pun;
using UnityEngine;

namespace LAB2D
{
    public class PlayerBullet : Bullet
    {
        private PhotonView photonView;

        protected override void Awake()
        {
            base.Awake();
            layerMask = LayerMask.GetMask("Tile", "Enemy");
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

        public override void hitObject()
        {
            if (rayCastHit2D.transform.gameObject.CompareTag("Enemy")) // 击中敌人处理
            {
                Enemy e = rayCastHit2D.transform.GetComponent<Enemy>();
                e.Target = Origin;
                e.reduceHp(Damage);
            }
        }
    }
}
