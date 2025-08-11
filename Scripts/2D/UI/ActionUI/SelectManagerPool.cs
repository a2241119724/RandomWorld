namespace LAB2D
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    /// <summary>
    /// 对所有的Select的池子
    /// </summary>
    public class SelectManagerPool : Singleton<SelectManagerPool>
    {
        /// <summary>
        /// 选择的所有Tile
        /// </summary>
        public Dictionary<SelectUI, bool> Selects;

        public SelectManagerPool()
        {
            this.Selects = new Dictionary<SelectUI, bool>();
        }

        /// <summary>
        /// 释放所有的Select,并获取一个Select
        /// </summary>
        /// <returns>Select</returns>
        public SelectUI ReleaseAllAndGetOne()
        {
            this.ReleaseAll();
            if (this.Selects.Count == 0)
            {
                return this.CreateFreeSelect(default);
            }

            Dictionary<SelectUI, bool>.Enumerator enumerator = this.Selects.GetEnumerator();
            enumerator.MoveNext();
            this.Selects[enumerator.Current.Key] = true;
            return enumerator.Current.Key;
        }

        /// <summary>
        /// 创建一个空闲的Select
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <returns>Select</returns>
        public SelectUI CreateFreeSelect(Vector3Int posMap)
        {
            // 判断该位置是否有SelectUI
            SelectUI select = null;
            foreach (KeyValuePair<SelectUI, bool> pair in this.Selects)
            {
                if (pair.Key.Target.x == posMap.x && pair.Key.Target.y == posMap.y)
                {
                    select = pair.Key;
                    break;
                }

                if (!pair.Value)
                {
                    select = pair.Key;
                }
            }

            if (select != null)
            {
                this.Selects[select] = true;
                return select;
            }

            // 没有更多的SelectUI,创建新的
            select = GameObject.Instantiate(ResourceManager.Instance.GetPrefab("Select")).GetComponent<SelectUI>();
            select.transform.SetParent(GameObject.FindGameObjectWithTag(TagConstant.ACTION_UI_TAG).transform);
            this.Selects.Add(select, true);
            return select;
        }

        /// <summary>
        /// 将所有SelectUI置为空闲
        /// </summary>
        public void ReleaseAll()
        {
            foreach (SelectUI selectUI in this.Selects.Keys.ToList())
            {
                this.Selects[selectUI] = false;
                selectUI.Init();
            }
        }

        /// <summary>
        /// 得到已经选中角色的SelectUI
        /// </summary>
        /// <param name="character">角色</param>
        /// <returns>Select</returns>
        public SelectUI GetForCharacter(Character character)
        {
            foreach (KeyValuePair<SelectUI, bool> pair in this.Selects)
            {
                if (pair.Key.Character == character)
                {
                    return pair.Key;
                }
            }

            return null;
        }
    }
}
