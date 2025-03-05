using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    public class ItemInfoPanel : BasePanel<ItemInfoPanel>
    {
        private Text textUI;
        private Transform character;

        public ItemInfoPanel()
        {
            Name = "ItemInfo";
            setPanel();
            textUI = Tool.GetComponentInChildren<Text>(panel, "Info");
            character = Tool.GetComponentInChildren<Transform>(panel, "Character");
        }

        public override void OnEnter()
        {
            base.OnEnter();
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override void OnPause()
        {
            base.OnPause();
            Time.timeScale = 0; // ÔÝÍ£
        }

        public override void OnRun()
        {
            base.OnRun();
            Time.timeScale = ForegroundPanel.Instance.TimeScale;
        }

        public void setItemInfo(string text)
        {
            this.character.gameObject.SetActive(false);
            textUI.text = text;
        }

        public void setCharacter(Character character)
        {
            this.character.gameObject.SetActive(true);
            for (int i = 0; i < this.character.childCount; i++)
            {
                this.character.GetChild(i).gameObject.SetActive(false);
            }
            if (character is Worker)
            {
                Transform worker = this.character.Find("Worker");
                worker.gameObject.SetActive(true);
                Weapon weapon = ((Worker)character).WearData.weapon;
                if (weapon != null)
                {
                    worker.Find("Weapon/Image").GetComponent<Image>().sprite = ResourcesManager.Instance.getImage(
                        ItemDataManager.Instance.getById(weapon.id).imageName);
                }
                Dictionary<Equipment.EquipType, Equipment> equipments = ((Worker)character).WearData.equipments;
                foreach (var item in equipments)
                {
                    if (item.Value != null)
                    {
                        worker.Find(item.Key.ToString()+ "/Image").GetComponent<Image>().sprite = ResourcesManager.Instance.getImage(
                            ItemDataManager.Instance.getById(item.Value.id).imageName);
                    }
                }
            }
            else if (character is Enemy)
            {
                this.character.Find("Enemy").gameObject.SetActive(true);
            }
            else if (character is Player)
            {
                this.character.Find("Player").gameObject.SetActive(true);
            }
        }
    }
}
