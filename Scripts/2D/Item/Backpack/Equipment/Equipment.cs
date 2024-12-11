using System;

namespace LAB2D
{
    [Serializable]
    public abstract class Equipment : BackpackItem
    {
        public enum EquipType
        {
            Head,   //ͷ��
            Body,   //����
            Leg,    //�Ȳ�
            Foot,   //�㲿
            Wrist,  //��
            Knee,   //ϥ��
            Hand,   //�ֲ�
            Shoulder//���
        }
    }

    public abstract class EquipmentObject : BackpackItemObject
    {
    }
}