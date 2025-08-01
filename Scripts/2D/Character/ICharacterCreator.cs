namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// 角色创建器基
    /// </summary>
    public interface ICharacterCreator
    {
        /// <summary>
        /// 角色创建
        /// </summary>
        /// <param name="worldPos">位置</param>
        /// <returns>对象</returns>
        GameObject Create(Vector3 worldPos = default);
    }
}