namespace LAB2D.UI.Panel.PanelUI
{
    using LAB2D;
    using LAB2D.Core;
    using LAB2D.Item;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 本地房间列表 UI — 显示 RoomManager 中所有房间的名称、进度、温度、湿度。
    /// 挂载在 RoomListPanel 的 Panel GameObject 上。
    /// </summary>
    public class RoomListUI : MonoBehaviour
    {
        public static RoomListUI Instance { get; private set; }

        // ---- UI 引用 ----
        private Transform content;
        private Text emptyHintText;

        public void Awake()
        {
            Instance = this;
            ServiceLocator.Register(this);
            this.CacheUIReferences();
        }

        public void OnEnable()
        {
            this.RefreshRoomList();
        }

        public void OnDestroy()
        {
            ServiceLocator.Unregister<RoomListUI>();
        }

        /// <summary>
        /// 刷新房间列表。
        /// </summary>
        public void RefreshRoomList()
        {
            if (this.content == null)
            {
                this.CacheUIReferences();
            }

            if (this.content == null) return;

            // 清空已有项
            for (int i = this.content.childCount - 1; i >= 0; i--)
            {
                Destroy(this.content.GetChild(i).gameObject);
            }

            // 获取房间数据
            RoomManager roomManager = ServiceLocator.Get<RoomManager>();
            IReadOnlyDictionary<string, RoomInfo> rooms = roomManager.GetAllRooms();

            if (rooms.Count == 0)
            {
                if (this.emptyHintText != null)
                {
                    this.emptyHintText.gameObject.SetActive(true);
                    this.emptyHintText.text = "暂无房间\n建造墙体围成封闭空间即可创建房间";
                }
                return;
            }

            if (this.emptyHintText != null)
            {
                this.emptyHintText.gameObject.SetActive(false);
            }

            foreach (KeyValuePair<string, RoomInfo> kv in rooms)
            {
                this.CreateRoomItem(kv.Key, kv.Value);
            }
        }

        private void CacheUIReferences()
        {
            Transform scrollView = this.transform.Find("ScrollView");
            if (scrollView != null)
            {
                this.content = scrollView.Find("Viewport/Content");
            }

            Transform hintT = this.transform.Find("EmptyHint");
            if (hintT != null)
            {
                this.emptyHintText = hintT.GetComponent<Text>();
            }
        }

        private void CreateRoomItem(string name, RoomInfo info)
        {
            ResourceManager rm = ServiceLocator.Get<ResourceManager>();
            GameObject itemGo = rm.Instantiate(Constant.PrefabConstant.WORKER_ROOM_ITEM, this.content, false);

            if (itemGo == null)
            {
                AWorkerTask.LogProvider("WorkerRoomItem prefab not found", LogManager.LogLevelEnum.Error);
                return;
            }

            // 房间名称
            Transform nameT = itemGo.transform.Find("RoomName");
            if (nameT != null) nameT.GetComponent<Text>().text = name;

            // 所有者
            Transform ownerT = itemGo.transform.Find("Owner");
            if (ownerT != null)
            {
                ownerT.GetComponent<Text>().text = string.IsNullOrEmpty(info.OwnerName)
                    ? "所有者: 未知" : $"👤 {info.OwnerName}";
            }

            // 进度
            Transform progressT = itemGo.transform.Find("Progress");
            if (progressT != null)
            {
                progressT.GetComponent<Text>().text = info.Progress == 0
                    ? "✓ 已完成" : $"建造中 ({info.Progress} 剩余)";
            }

            // 温度
            Transform tempT = itemGo.transform.Find("Temperature");
            if (tempT != null) tempT.GetComponent<Text>().text = $"🌡 {info.Temperature:F1}°C";

            // 湿度
            Transform humT = itemGo.transform.Find("Humidity");
            if (humT != null) humT.GetComponent<Text>().text = $"💧 {info.Humidity:F1}%";
        }
    }
}
