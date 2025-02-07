using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LAB2D
{
    public class RoomUI : MonoBehaviourPunCallbacks
    {
        public static RoomUI Instance { private set; get; }
        public UnityAction<string> ClickAndShow { get; set; }

        private GameObject prefabRoomBox;

        private void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            prefabRoomBox = ResourcesManager.Instance.getPrefab("RoomItem");
        }

        public override void OnRoomListUpdate(List<Photon.Realtime.RoomInfo> roomList)
        {
            base.OnRoomListUpdate(roomList);
            foreach (Photon.Realtime.RoomInfo room in roomList)
            {
                GameObject g = Instantiate(prefabRoomBox);
                g.GetComponent<Button>().onClick.AddListener(
                    delegate {
                        OnClick_RoomBox(room.Name);
                    });
                g.name = prefabRoomBox.name;
                g.transform.Find("RoomName").GetComponent<Text>().text = string.Format("{0}   {1}/{2}", room.Name, room.PlayerCount,20);
                g.transform.SetParent(transform, false);
            }
        }

        private void OnClick_RoomBox(string str)
        {
            ClickAndShow?.Invoke(str);
        }
    }
}