namespace LAB2D
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 物品信心面板
    /// </summary>
    public class ItemInfoPanel : ABasePanel<ItemInfoPanel>
    {
        private readonly Text textUI;
        private readonly Transform character;

        public ItemInfoPanel()
        {
            this.Name = "ItemInfo";
            this.Init();
            this.textUI = Tool.GetComponentInChildren<Text>(this.Panel, "Info");
            this.character = Tool.GetComponentInChildren<Transform>(this.Panel, "Character");
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            base.OnExit();
        }

        /// <inheritdoc/>
        public override void OnPause()
        {
            base.OnPause();
            Time.timeScale = 0; // 暂停
        }

        /// <inheritdoc/>
        public override void OnRun()
        {
            base.OnRun();
            Time.timeScale = ForegroundPanel.Instance.TimeScale;
        }

        /// <summary>
        /// 设置道具信息
        /// </summary>
        /// <param name="text">信息</param>
        public void SetItemInfo(string text)
        {
            this.character.gameObject.SetActive(false);
            this.textUI.text = text;
        }

        /// <summary>
        /// 设置角色
        /// </summary>
        /// <param name="character">角色</param>
        public void SetCharacter(Character character)
        {
            this.character.gameObject.SetActive(true);
            for (int i = 0; i < this.character.childCount; i++)
            {
                this.character.GetChild(i).gameObject.SetActive(false);
            }

            if (character is Worker worker1)
            {
                Worker.WorkerData workerData = worker1.CharacterDataLAB as Worker.WorkerData;
                Transform worker = this.character.Find("Worker");
                worker.gameObject.SetActive(true);
                AWeapon weapon = workerData.Weapon;
                if (weapon != null)
                {
                    worker.Find("Weapon/Image").GetComponent<Image>().sprite = ResourceManager.Instance.GetImage(
                        ItemDataManager.Instance.GetById(weapon.Id).EnName);
                }

                Dictionary<AEquipment.EquipTypeEnum, AEquipment> equipments = workerData.Equipments;
                foreach (var item in equipments)
                {
                    if (item.Value != null)
                    {
                        worker.Find(item.Key.ToString() + "/Image").GetComponent<Image>().sprite = ResourceManager.Instance.GetImage(
                            ItemDataManager.Instance.GetById(item.Value.Id).EnName);
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
