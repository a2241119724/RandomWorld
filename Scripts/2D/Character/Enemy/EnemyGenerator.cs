using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D {
    public class EnemyGenerator : MonoBehaviour
    {
        [SerializeField] private float instanceInterval = 3.0f; // ʵ����ʱ����
        private float recordTime = 0.0f;

        void Update()
        {
            if (!PhotonNetwork.IsMasterClient) return;
            recordTime += Time.deltaTime;
            if (recordTime > instanceInterval)
            {
                EnemyManager.Instance.create();
                recordTime = 0.0f;
            }
        }

        //private int sendQuantity = 100; // һ�η�������
        //private int sendIndex = 0; // �������ݵ�����
        //private bool isFinish; // �Ƿ������е�ͼ����
        //private bool isOne; // ִ��һ��

        ///// <summary>
        ///// ʹ���¼�����û�����������Master��������
        ///// </summary>
        //public void initData()
        //{
        //    isFinish = false;
        //    sendIndex = 0;
        //}

        ///// <summary>
        ///// ���л�����λ��
        ///// Э��LAB_2
        ///// ÿ�η���2 + sendQuantity * 4
        ///// ǰ2���ֽڱ�ʶ�����ͼ����ʼ����ϵ��[start,(start + 1))
        ///// [start * sendQuantity,(start + 1) * sendQuantity)
        ///// </summary>
        //private void SerializeTiles(PhotonStream stream)
        //{
        //    // ÿ�η���1000����ͼ����
        //    byte[] position = new byte[sendQuantity * 4 + 2];
        //    // ǰ���ֽڷ����ݷ�Χ(С�˴洢)
        //    position[0] = (byte)(sendIndex % (1 << 8));
        //    position[1] = (byte)(sendIndex / (1 << 8));
        //    int temp = 2; // �Ӷ���ʼ������
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
        ///// �����л�����λ��
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
        //            // ��ֹ��ȡʱԽ��
        //            stream.SendNext(-1);
        //        }
        //    }
        //    else
        //    {
        //        GlobalData.maxEnemyCount = (int)stream.ReceiveNext();
        //        currentEnemyCount = (int)stream.ReceiveNext();
        //        //int len = (int)stream.ReceiveNext();
        //        //enemysPosition.Capacity = len;
        //        // ���ƽ��������ݽ���һ��,������ظ����ݾͲ�������
        //        if (!isOne && stream.PeekNext() is byte[])
        //        {
        //            DeserializeTiles(stream);
        //        }
        //    }
        //}
    }
}
