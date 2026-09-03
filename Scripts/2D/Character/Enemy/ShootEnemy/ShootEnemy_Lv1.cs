namespace LAB2D.Character.Enemy.ShootEnemy
{
    using LAB2D;

    /// <summary>
    /// 远程妖狐 — 远射程高速灵弹，血量偏低。
    /// 状态机与子弹 Attack 全复用 CommonEnemy_Lv1，仅数值差异化。
    /// </summary>
    public class ShootEnemy_Lv1 : CommonEnemy.CommonEnemy_Lv1
    {
        /// <inheritdoc/>
        public override void Start()
        {
            // 先走基类全套（AttackRange=7 + 五状态机），再覆盖妖狐数值
            base.Start();

            EnemyData enemyData = this.CharacterDataLAB as EnemyData;
            enemyData.AttackRange = 9.0f;   // 射程比普通敌人远
            enemyData.BulletSpeed = 60.0f;  // 灵弹更快
            enemyData.SoundRange = 5.0f;
        }
    }
}
