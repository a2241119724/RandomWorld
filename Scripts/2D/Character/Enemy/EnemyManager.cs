namespace LAB2D
{
    /// <summary>
    /// 敌人管理器.
    /// </summary>
    public class EnemyManager : CharacterManager<EnemyManager, Enemy, EnemyCreator>
    {
        /// <summary>
        /// 最大敌人数量.
        /// </summary>
        public int MaxEnemyCount { get; set; }
    }
}