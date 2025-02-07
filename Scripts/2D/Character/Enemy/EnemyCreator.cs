using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D {
    public class EnemyCreator : CharacterCreator<EnemyCreator>
    {
        private const float instanceInterval = 3.0f; // 实例化时间间隔

        /// <summary>
        /// 实例化在玩家附近的敌人
        /// </summary>
        /// <param name="index">敌人编号</param>
        protected override GameObject _create(Vector3 worldPos, string name, string layer)
        {
            if (EnemyManager.Instance.Characters.Count >= EnemyManager.Instance.MaxEnemyCount) return null;
            return base._create(worldPos, "Enemy_Lv1", "Enemy");
        }

        /// <summary>
        /// 每隔一段时间生成敌人
        /// </summary>
        /// <returns></returns>
        public IEnumerator genEnemy()
        {
            while (true)
            {
                EnemyManager.Instance.create();
                yield return new WaitForSeconds(instanceInterval);
            }
        }

        //private int sendQuantity = 100; // 一次发送数量
        //private int sendIndex = 0; // 发送数据的索引
        //private bool isFinish; // 是否发送所有地图数据
        //private bool isOne; // 执行一次

        ///// <summary>
        ///// 使得新加入的用户调用来控制Master发送数据
        ///// </summary>
        //public void initData()
        //{
        //    isFinish = false;
        //    sendIndex = 0;
        //}

        ///// <summary>
        ///// 序列化敌人位置
        ///// 协议LAB_2
        ///// 每次发送2 + sendQuantity * 4
        ///// 前2个字节标识传输地图的起始索引系数[start,(start + 1))
        ///// [start * sendQuantity,(start + 1) * sendQuantity)
        ///// </summary>
        //private void SerializeTiles(PhotonStream stream)
        //{
        //    // 每次发送1000个地图数据
        //    byte[] position = new byte[sendQuantity * 4 + 2];
        //    // 前两字节放数据范围(小端存储)
        //    position[0] = (byte)(sendIndex % (1 << 8));
        //    position[1] = (byte)(sendIndex / (1 << 8));
        //    int temp = 2; // 从而开始存数据
        //    int len = (sendIndex + 1) * sendQuantity;
        //    int total = enemysPosition.Count;
        //    for (int i = sendIndex * sendQuantity; i < len; i++)
        //    {
        //        if (i >= total)
        //        {
        //            stream.SendNext(position);
        //            //sendIndex = 0;
        //            isFinish = true;
        //            return;
        //        }
        //        position[temp++] = (byte)(enemysPosition[i].x % (1 << 8));
        //        position[temp++] = (byte)(enemysPosition[i].x / (1 << 8));
        //        position[temp++] = (byte)(enemysPosition[i].y % (1 << 8));
        //        position[temp++] = (byte)(enemysPosition[i].y / (1 << 8));
        //    }
        //    stream.SendNext(position);
        //    ++sendIndex;
        //}

        ///// <summary>
        ///// 反序列化敌人位置
        ///// </summary>
        //private void DeserializeTiles(PhotonStream stream)
        //{
        //    byte[] tiles = (byte[])stream.ReceiveNext();
        //    int temp = 2;
        //    int start = tiles[1] * 256 + tiles[0];
        //    int len = (start + 1) * sendQuantity;
        //    int total = enemysPosition.Count;
        //    for (int i = start * sendQuantity; i < len; i++)
        //    {
        //        byte a = tiles[temp++];
        //        byte b = tiles[temp++];
        //        byte c = tiles[temp++];
        //        byte d = tiles[temp++];
        //        int x = b * (1 << 8) + a;
        //        int y = d * (1 << 8) + c;
        //        enemysPosition.Add(new Vector2(x, y));
        //        if (i == (total - 1))
        //        {
        //            isOne = true;
        //            return;
        //        }
        //    }
        //}

        //public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        //{
        //    if (stream.IsWriting)
        //    {
        //        stream.SendNext(GlobalData.maxEnemyCount);
        //        stream.SendNext(currentEnemyCount);
        //        //stream.SendNext(enemysPosition.Count);
        //        if (!isFinish)
        //        {
        //            SerializeTiles(stream);
        //        }
        //        else
        //        {
        //            // 防止读取时越界
        //            stream.SendNext(-1);
        //        }
        //    }
        //    else
        //    {
        //        GlobalData.maxEnemyCount = (int)stream.ReceiveNext();
        //        currentEnemyCount = (int)stream.ReceiveNext();
        //        //int len = (int)stream.ReceiveNext();
        //        //enemysPosition.Capacity = len;
        //        // 控制将所有数据接收一遍,后面的重复数据就不接收了
        //        if (!isOne && stream.PeekNext() is byte[])
        //        {
        //            DeserializeTiles(stream);
        //        }
        //    }
        //}
    }
}
