using System;

namespace LAB2D
{
    [Serializable]
    public abstract class Equipment : BackpackItem
    {
        public EquipType equipType; //װ������

        public enum EquipType
        {
            Head,   //ͷ��
            Body,   //����
            Trouser, // ����
            Shoes,  //Ь��
            Weapon, //����
            Shield, //����
            Ring,   //��ָ
            Necklace, //����
            Bracelet, //����
            Belt,   //����
            Earring, //����
            Wing,   //���
            Mount,  //����
            Pet,    //����
            Null
        }
    }

    public abstract class EquipmentObject : BackpackItemObject
    {
    }
}