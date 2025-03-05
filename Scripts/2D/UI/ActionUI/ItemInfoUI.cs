using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace LAB2D
{
    public class ItemInfoUI : MonoBehaviourInit
    {
        public static ItemInfoUI Instance { get; private set; }

        private string text;
        /// <summary>
        /// ʵʱ���ٽ�ɫ
        /// </summary>
        private Character character;
        private string select = "";
        private Vector3Int selectPos = default;

        private void Awake()
        {
            Instance = this;
        }

        void Update()
        {
            // ʵʱ����Character��Ϣ
            if (character != null)
            {
                if(PanelController.Instance.Panels.Peek() != ItemInfoPanel.Instance)
                {
                    PanelController.Instance.show(ItemInfoPanel.Instance);
                }
                ItemInfoPanel.Instance.setItemInfo(character.ToString());
                ItemInfoPanel.Instance.setCharacter(character);
            }
            if (Input.GetMouseButtonDown(1))
            {
                List<RaycastResult> results = Tool.getUIByMousePos();
                // ���˲��ǻ�������Ļ�Ķ���
                if (results.Count > 0 && results[0].gameObject.name != "Foreground") return;
                selectPos = TileMap.Instance.worldPosToMapPos(Camera.main.ScreenToWorldPoint(Input.mousePosition));
                SelectUI selectUI;
                if (Input.GetKey(KeyCode.LeftControl))
                {
                    selectUI = SelectManager.Instance.getFreeSelect(selectPos);
                }
                else
                {
                    selectUI = SelectManager.Instance.getFirstAndFreeAll();
                }
                do {
                    character = getCharacter(selectPos);
                    if (character != null)
                    {
                        text = character.ToString();
                        SelectUI _selectUI = SelectManager.Instance.getByCharacter(character);
                        if(_selectUI != null)
                        {
                            selectUI = _selectUI;
                        }
                        selectUI.setTarget(selectPos);
                        selectUI.Character = character;
                        ItemInfoPanel.Instance.setCharacter(character);
                        break;
                    }
                    selectUI.setTarget(selectPos);
                    // ��������ֿ߲�
                    select = "Resource";
                    text = getResource(selectPos);
                    if (!text.Equals(""))
                    {
                        break;
                    }
                    // û����Ʒ,��ʾ��ͼTile
                    select = "Tile";
                    TileBase tileBase = getTile(selectPos);
                    if (tileBase == null) return;
                    text += $"{tileBase.name}\n" +
                        EnvironmentManager.Instance.ToString(selectPos);
                    break;
                } while (true);
                if (PanelController.Instance.Panels.Peek() != ItemInfoPanel.Instance)
                {
                    PanelController.Instance.show(ItemInfoPanel.Instance);
                }
                ItemInfoPanel.Instance.setItemInfo(text);
            }
        }

        /// <summary>
        /// ��̬����Tile��Ϣ
        /// </summary>
        /// <param name="name">������ѡ�е��������ʾ</param>
        /// <param name="text"></param>
        public void updateInfo(string name, Vector3Int pos, string text)
        {
            if (select.Equals(name) && selectPos.x==pos.x && selectPos.y == pos.y)
            {
                ItemInfoPanel.Instance.setItemInfo(text);
            }
        }

        public Character getCharacter(Vector3Int posMap)
        {
            // Player
            Character _character = PlayerManager.Instance.getCharacterByPos(posMap);
            if (_character == null)
            {
                // Enemy
                _character = EnemyManager.Instance.getCharacterByPos(posMap);
            }
            if (_character == null)
            {
                //  Worker
                _character = WorkerManager.Instance.getCharacterByPos(posMap);
            }
            return _character;
        }

        private string getResource(Vector3Int posMap) {
            // Drop
            select = "DropResourceManager";
            string text = DropResourceManager.Instance.ToString(posMap);
            if (text.Equals(""))
            {
                // Inventory
                select = "InventoryManager";
                text = InventoryManager.Instance.ToString(posMap);
                if (!text.Equals(""))
                {
                    InventoryManager.Instance.showWearMenu(posMap);
                }
            }
            return text;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="posMap"></param>
        /// <param name="isTile"></param>
        /// <param name="isUI"></param>
        /// <returns></returns>
        public TileBase getTile(Vector3Int posMap,bool isTile=true,bool isUI=true) {
            TileBase tileBase = BuildMap.Instance.getTile(posMap);
            text = "Build:";
            // ���������Ǵ�����չʾ�����Worker
            if (tileBase != null && tileBase.name.Contains("Bed"))
            {
                WorkerBedUI.Instance.showWorkerBed(posMap);
            }
            if (tileBase == null)
            {
                text = "Resource:";
                tileBase = ResourceMap.Instance.getTile(posMap);
                // �ֶ��������
                if (tileBase != null && isUI) {
                    GatherUI.Instance.setPostion(posMap);
                }
            }
            if (tileBase == null && isTile)
            {
                text = "Tile:";
                tileBase = TileMap.Instance.getTile(posMap);
            }
            return tileBase;
        }

        public override void init()
        {
            base.init();
            character = null;
            select = "";
            selectPos = default;
        }
    }
}
