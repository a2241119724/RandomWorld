using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D {
    public class EnemyCreator : CharacterCreator<EnemyCreator>
    {
        /// <summary>
        /// ʵ��������Ҹ����ĵ���
        /// </summary>
        /// <param name="index">���˱��</param>
        protected override GameObject _create()
        {
            List<Vector3> positions = EnemyManager.Instance.EnemyPositions;
            // ��������ɵ����������ɵ���֮��С�ڵ�������
            if (EnemyManager.Instance.totalCount() < EnemyManager.Instance.MaxEnemyCount)
            {
                Vector3 pos = TileMap.Instance.genAvailablePosMap();
                pos = new Vector3(pos.y, pos.x,0);
                for (int i = 0; i < PlayerManager.Instance.count(); i++)
                {
                    if ((PlayerManager.Instance.get(i)).isArround(pos))
                    {
                        return createEnemy(pos);
                    }
                }
                positions.Add(pos);
            }
            for (int i = 0; i < PlayerManager.Instance.count(); i++)
            {
                for (int j = 0; j < positions.Count; j++)
                {
                    if ((PlayerManager.Instance.get(i)).isArround(positions[j]))
                    {
                        GameObject g = createEnemy(positions[j]);
                        positions.RemoveAt(j);
                        return g;
                    }
                }
            }
            return null;
        }

        private GameObject createEnemy(Vector3 pos) {
            GameObject g = PhotonNetwork.Instantiate(ResourcesManager.Instance.getPath("Enemy_Lv1.prefab"),
                new Vector3(pos.x + TileMap.Instance.transform.position.x,
                pos.y + TileMap.Instance.transform.position.y,
                TileMap.Instance.transform.position.z), Quaternion.identity);
            if (g == null)
            {
                Debug.LogError("enemy Instantiate Error!!!");
                return null;
            }
            // ���ò㼶
            g.layer = LayerMask.NameToLayer("Enemy");
            EnemyManager.Instance.add(g.GetComponent<Enemy>());
            return g;
        }
    }
}
