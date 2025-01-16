using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class EnvironmentManager : Singleton<EnvironmentManager>
    {
        public float Temperature = -10.0f;
        public float Humidity = -10.0f;
        /// <summary>
        /// �������ֵ
        /// </summary>
        public float MaxEnergy = 100.0f;
        /// <summary>
        /// ��ǰ����ֵ
        /// </summary>
        public float CurEnergy = 100.0f;

        /// <summary>
        /// �����ָ�����
        /// </summary>
        public void updateEnergy()
        {
            if(CurEnergy <= MaxEnergy)
            {
                CurEnergy += Time.deltaTime;
            }
        }

        public override string ToString()
        {
            return $"�¶�:{Temperature}\n" +
                $"ʪ��:{Humidity}\n";
        }

        public string ToString(Vector3Int posMap) {
            RoomInfo roomInfo = RoomManager.Instance.getRoomByPos(posMap);
            if (roomInfo != null)
            {
                return roomInfo.ToString();
            }
            return ToString();
        }
    }
}
