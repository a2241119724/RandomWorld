namespace LAB2D
{
    using System;

    /// <summary>
    /// 自定义剑对象
    /// </summary>
    public class CustomSwordObject : SwordObject
    {
        /// <inheritdoc/>
        protected override void Awake()
        {
            base.Awake();
            this.name = "CustomSword";
            this.attackInterval = 0.5f;
            this.attackEffect = AttackEffectManager.EffectTypeEnum.KnifeLight;
        }

        /// <inheritdoc/>
        protected override void DoAttack(AttackEffect attackEffect)
        {
        }

        // protected override void OnTriggerEnter2D(Collider2D collision)
        // {
        //     base.OnTriggerEnter2D(collision);
        //     if (!photonView.IsMine && PhotonNetwork.IsConnected) return;
        //     if (collision.transform.CompareTag("Enemy"))
        //     {
        //         // 防止多人联机时,同时调用
        //         if (PlayerManager.Instance.Select.weaponData == null) return;
        //         collision.GetComponent<Enemy>().setPlayer(player.GetComponent<Player>());
        //         //collision.GetComponent<PhotonView>().RPC("reduceHp", RpcTarget.All, ((WeaponData)PlayerManager.Instance.Select.weaponData).getDamage());
        //         collision.transform.GetComponent<Enemy>().reduceHp((PlayerManager.Instance.Select.weaponData).getDamage());
        //         GameObject go = Instantiate(blood, collision.transform.position, Quaternion.identity);
        //         if (go == null)
        //         {
        // LogManager.Instance.log("blood Instantiate Error!!!", LogManager.LogLevel.Error);
        //             return;
        //         }
        //         go.name = blood.name;
        //         go.transform.SetParent(collision.transform);
        //     }
        // }
    }

    /// <summary>
    /// 自定义剑
    /// </summary>
    [Serializable]
    public class CustomSword : Sword
    {
        public CustomSword()
        {
            this.Attribute.ATN = this.RankRandom(5.0f, 10.0f);
            this.Attribute.INT = this.RankRandom(5.0f, 10.0f);
            this.Attribute.CRT = this.RankRandom(0.05f, 0.1f);
            this.Attribute.CSD = this.RankRandom(0f, 1.0f);
            this.Attribute.HIT = this.RankRandom(5.0f, 10.0f);
            this.Attribute.RES = this.RankRandom(5.0f, 10.0f);
            this.Attribute.SPD = this.RankRandom(5.0f, 10.0f);
            this.Attribute.DEF = this.RankRandom(5.0f, 10.0f);
        }
    }
}