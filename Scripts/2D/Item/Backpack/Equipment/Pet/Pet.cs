namespace LAB2D
{
    using System;

    /// <summary>
    /// 宠物
    /// </summary>
    [Serializable]
    public abstract class Pet : Equipment
    {
        public Pet()
        {
            this.EquipTypeValue = EquipType.Pet;
        }
    }

    /// <summary>
    /// 宠物对象
    /// </summary>
    public abstract class PetObject : EquipmentObject
    {
    }
}
