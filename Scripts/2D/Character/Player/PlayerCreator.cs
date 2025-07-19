namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// 玩家创建器
    /// </summary>
    public class PlayerCreator : CharacterCreator<PlayerCreator>
    {
        /// <summary>
        /// 实例化玩家
        /// </summary>
        /// <param name="worldPos">玩家世界坐标</param>
        /// <param name="name">玩家名字</param>
        /// <param name="layer">玩家层级</param>
        /// <returns>游戏对象</returns>
        protected override GameObject _create(Vector3 worldPos, string name, string layer)
        {
            return base._create(worldPos, "Player", "Player");
        }
    }
}