using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LAB2D {
    public class SelectManager : Singleton<SelectManager>
    {
        public Dictionary<SelectUI, bool> selects;

        public SelectManager()
        {
            selects = new Dictionary<SelectUI, bool>();
        }

        public SelectUI getFirstAndFreeAll()
        {
            freeAll();
            if (selects.Count == 0)
            {
                return getFreeSelect(default);
            }
            Dictionary<SelectUI, bool>.Enumerator enumerator = selects.GetEnumerator();
            enumerator.MoveNext();
            selects[enumerator.Current.Key] = true;
            return enumerator.Current.Key;
        }

        public SelectUI getFreeSelect(Vector3Int posMap)
        {
            // �жϸ�λ���Ƿ���SelectUI
            SelectUI select = null;
            foreach (KeyValuePair<SelectUI, bool> pair in selects)
            {
                if (pair.Key.Target.x == posMap.x && pair.Key.Target.y == posMap.y) {
                    select = pair.Key;
                    break;
                }
                if (!pair.Value)
                {
                    select = pair.Key;
                }
            }
            if(select != null)
            {
                selects[select] = true;
                return select;
            }
            // û�и����SelectUI,�����µ�
            select = GameObject.Instantiate(ResourcesManager.Instance.getPrefab("Select")).GetComponent<SelectUI>();
            select.transform.SetParent(GameObject.FindGameObjectWithTag(ResourceConstant.ACTION_UI_TAG).transform);
            selects.Add(select, true);
            return select;
        }

        /// <summary>
        /// ������SelectUI��Ϊ����
        /// </summary>
        public void freeAll()
        {
            foreach (SelectUI selectUI in selects.Keys.ToList())
            {
                selects[selectUI] = false;
                selectUI.init();
            }
        }

        /// <summary>
        /// �õ��Ѿ�ѡ�н�ɫ��SelectUI
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        public SelectUI getByCharacter(Character character)
        {
            foreach (KeyValuePair<SelectUI, bool> pair in selects)
            { 
                if(pair.Key.Character == character)
                {
                    return pair.Key;
                }
            }
            return null;
        }
    }
}
