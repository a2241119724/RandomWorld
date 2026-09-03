namespace LAB2D.Domain.Wave
{
    /// <summary>
    /// 波次敌人种类 — 引擎无关的扩种协议标识。
    /// Unity 侧 EnemyCreator 按 Id 映射 prefab；数值与顺序稳定（存档字段缺省 0=Common，旧档兼容）。
    /// </summary>
    public enum WaveEnemyKind
    {
        /// <summary>远程弹幕妖兽（旧 CommonEnemy）。</summary>
        Common = 0,

        /// <summary>近战追击妖兽（旧 SeekEnemy）。</summary>
        Seek = 1,

        /// <summary>冲锋妖兽（ChargeEnemy，冲撞逼近）。</summary>
        Charge = 2,

        /// <summary>远程妖狐（ShootEnemy，灵弹射击）。</summary>
        Shoot = 3,
    }
}
