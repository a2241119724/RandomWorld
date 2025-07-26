namespace LAB2D
{
    using System.Collections.Generic;
    using Photon.Pun;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.UI;

    /// <summary>
    /// 房间UI
    /// </summary>
    public class RoomUI : MonoBehaviourPunCallbacks
    {
        private GameObject prefabRoomBox;

        /// <summary>
        /// 单例
        /// </summary>
        public static RoomUI Instance { get; private set; }

        /// <summary>
        /// 点击并展示
        /// </summary>
        public UnityAction<string> ClickAndShow { get; set; }

        /// <inheritdoc/>
        public override void OnRoomListUpdate(List<Photon.Realtime.RoomInfo> roomList)
        {
            base.OnRoomListUpdate(roomList);
            foreach (Photon.Realtime.RoomInfo room in roomList)
            {
                GameObject g = Instantiate(this.prefabRoomBox);
                g.GetComponent<Button>().onClick.AddListener(
                    () =>
                    {
                        this.OnClick_RoomBox(room.Name);
                    });
                g.name = this.prefabRoomBox.name;
                g.transform.Find("RoomName").GetComponent<Text>().text = string.Format("{0}   {1}/{2}", room.Name, room.PlayerCount, 20);
                g.transform.SetParent(this.transform, false);
            }
        }

        private void OnClick_RoomBox(string str)
        {
            this.ClickAndShow?.Invoke(str);
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            this.prefabRoomBox = ResourcesManager.Instance.GetPrefab("RoomItem");
        }
    }
}