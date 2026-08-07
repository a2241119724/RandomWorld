namespace LAB2D.UI.Action
{
    using LAB2D;
    using LAB2D.Constant;
    using LAB2D.Core;
    using LAB2D.Item;
    using LAB2D.Map;
    using LAB2D.MVC.Backpack.Controller;
    using LAB2D.UnityAdapter;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 种植种子选择 UI — 右键点击空地农田时显示。
    /// 列出背包中所有种子，玩家选择后直接种植。
    /// </summary>
    public class PlantUI : MonoBehaviour
    {
        private Vector3Int posMap;
        private List<AItem> availableSeeds;
        private readonly List<GameObject> seedButtons = new List<GameObject>();

        private int showFrame = -1; // Show() 被调用的帧
        private int hideFrame = -1; // Hide() 被调用的帧

        /// <summary>
        /// 单例
        /// </summary>
        public static PlantUI Instance { get; private set; }

        public void Awake()
        {
            Instance = this;
            ServiceLocator.Register(this);
        }

        public void Start()
        {
            LAB2D.Tool.Tool.GetComponentInChildren<Button>(this.gameObject, "Cancel").onClick.AddListener(this.OnClick_Cancel);
        }

        public void Update()
        {
            // 跳过 Show 调用所在帧 — 防止同一帧内 Show 后又被 Update 立即 Hide
            if (Time.frameCount == this.showFrame)
            {
                return;
            }

            // 若不在默认位置，右键点击时：
            // - 点在空地农田上 → 不隐藏（ItemInfoUI 会刷新列表到新位置）
            // - 点在非农田位置 → 隐藏
            if (UnityGlobalInputAdapter.GetSecondaryMouseDown()
                && this.transform.position.x != ResourceConstant.VECTOR3_DEFAULT.x)
            {
                Vector3Int clickPos = ServiceLocator.Get<TileMap>().GetMapPosByMouse();
                if (!ServiceLocator.Get<FarmlandManager>().IsEmptySoil(clickPos))
                {
                    this.Hide();
                }
            }
        }

        /// <summary>
        /// 显示种植 UI 并填充种子列表。
        /// </summary>
        /// <param name="posMap">地图坐标</param>
        public void Show(Vector3Int posMap)
        {
            // 同一帧内刚被 Hide，忽略 ItemInfoUI 的重复 Show 请求
            if (Time.frameCount == this.hideFrame)
            {
                return;
            }

            this.posMap = posMap;
            this.showFrame = Time.frameCount;
            this.transform.position = ServiceLocator.Get<TileMap>().MapPosToWorldPos(posMap);

            // 获取背包中的种子
            BackpackController backpackCtrl = BackpackController.Instance;
            if (backpackCtrl == null)
            {
                Debug.LogWarning("PlantUI.Show: BackpackController.Instance 为 null");
                this.Hide();
                return;
            }

            this.availableSeeds = backpackCtrl.GetSeeds();
            if (this.availableSeeds.Count == 0)
            {
                this.Hide();
                return;
            }

            // 清除旧按钮
            this.ClearSeedButtons();

            // 在 Right/SeedList 容器下为每个种子创建按钮
            Transform seedList = this.transform.Find("Right/SeedList");
            if (seedList == null)
            {
                Debug.LogError("PlantUI.Show: 找不到 Right/SeedList 子节点！");
                this.Hide();
                return;
            }

            // 为每个种子创建按钮
            for (int i = 0; i < this.availableSeeds.Count; i++)
            {
                AItem seed = this.availableSeeds[i];
                int seedIndex = i; // 捕获循环变量

                GameObject buttonObj = ServiceLocator.Get<ResourceManager>().Instantiate(
                    PrefabConstant.BUTTON_ITEM);
                buttonObj.transform.SetParent(seedList, false);
                buttonObj.transform.localScale = Vector3.one;

                Button button = buttonObj.GetComponent<Button>();
                button.onClick.AddListener(() => this.OnClick_Seed(seedIndex));

                Text buttonText = buttonObj.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    ItemData itemData = ServiceLocator.Get<ItemDataManager>().GetById(seed.Id);
                    string seedName = itemData != null ? itemData.CnName : $"Seed({seed.Id})";
                    buttonText.text = $"{seedName} x{seed.Quantity}";
                }

                this.seedButtons.Add(buttonObj);
            }
        }

        /// <summary>
        /// 隐藏种植 UI。
        /// </summary>
        public void Hide()
        {
            this.hideFrame = Time.frameCount;
            this.transform.position = ResourceConstant.VECTOR3_DEFAULT;
            this.ClearSeedButtons();
        }

        /// <summary>
        /// 选择种子并执行种植。
        /// </summary>
        /// <param name="seedIndex">种子在列表中的索引</param>
        public void OnClick_Seed(int seedIndex)
        {
            if (seedIndex < 0 || seedIndex >= this.availableSeeds.Count)
            {
                this.Hide();
                return;
            }

            AItem selectedSeed = this.availableSeeds[seedIndex];

            // 从背包移除种子
            bool removed = BackpackController.Instance.RemoveSeedByUid(selectedSeed.Uid);
            if (!removed)
            {
                Debug.LogWarning("PlantUI: 从背包移除种子失败");
                this.Hide();
                return;
            }

            // 直接在农田上种植
            FarmlandManager.Instance.Plant(this.posMap, selectedSeed.Id, 1);

            this.Hide();
        }

        /// <summary>
        /// 取消种植。
        /// </summary>
        public void OnClick_Cancel()
        {
            this.Hide();
        }

        /// <summary>
        /// 清除所有动态生成的种子按钮。
        /// </summary>
        private void ClearSeedButtons()
        {
            foreach (GameObject btn in this.seedButtons)
            {
                if (btn != null)
                {
                    Destroy(btn);
                }
            }

            this.seedButtons.Clear();
        }
    }
}
