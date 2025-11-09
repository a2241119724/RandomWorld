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
            this.attackInterval = 0.2f;
            this.attackEffect = AttackEffectManager.EffectType.KnifeLight;
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
    }
}