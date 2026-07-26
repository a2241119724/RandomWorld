namespace LAB2D.UI.Panel.PanelUI
{
    using LAB2D;
    using LAB2D.Core;
    using System.Collections.Generic;
    using Photon.Pun;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.UI;

    /// <summary>
    /// 房间UI
    /// </summary>
    public class JoinMenuUI : MonoBehaviourPunCallbacks
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static JoinMenuUI Instance { get; private set; }

        /// <summary>
        /// 点击并展示
        /// </summary>
        public UnityAction<string> ClickAndShow { get; set; }

        public void Awake()
        {
            Instance = this;
            ServiceLocator.Register(this);
        }

        /// <inheritdoc/>
        public override void OnRoomListUpdate(List<Photon.Realtime.RoomInfo> roomList)
        {
            base.OnRoomListUpdate(roomList);
            for (int i = 0; i < this.transform.childCount; i++)
            {
                DestroyImmediate(this.transform.GetChild(i).gameObject);
            }

            foreach (Photon.Realtime.RoomInfo room in roomList)
            {
                GameObject g = ServiceLocator.Get<ResourceManager>().Instantiate(PrefabConstant.ROOM_ITEM, true);
                g.GetComponent<Button>().onClick.AddListener(
                    () =>
                    {
                        this.OnClick_RoomBox(room.Name);
                    });
                g.transform.Find("RoomName").GetComponent<Text>().text = string.Format("{0}   {1}/{2}", room.Name, room.PlayerCount, 20);
                g.transform.SetParent(this.transform, false);
            }
        }

        private void OnClick_RoomBox(string str)
        {
            this.ClickAndShow?.Invoke(str);
        }
    }
}