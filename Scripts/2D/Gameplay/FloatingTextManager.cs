namespace LAB2D.Gameplay
{
    using LAB2D;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 浮动战斗文字系统管理器（Singleton）
    /// 统一管理浮动文字的生成、对象池、世界坐标→屏幕坐标转换和生命周期。
    ///
    /// 使用方式：
    ///   FloatingTextManager.Instance.SpawnDamageText(worldPos, damage, isCritical, isCombo);
    ///   FloatingTextManager.Instance.SpawnHealText(worldPos, healAmount);
    ///   FloatingTextManager.Instance.SpawnComboText(worldPos, comboCount);
    ///
    /// 初始化：在 GlobalInit.Start() 中调用 EnsureInitialized()。
    /// 注意：浮动文字为纯本地表现，不参与 Photon 同步。
    /// </summary>
    public class FloatingTextManager : Singleton<FloatingTextManager>
    {
        /// <summary>
        /// 主摄像机引用（用于世界坐标→屏幕坐标转换）
        /// </summary>
        private Camera mainCamera;

        /// <summary>
        /// 浮动文字 Canvas
        /// </summary>
        private GameObject canvasObj;

        /// <summary>
        /// 对象池容器
        /// </summary>
        private GameObject poolContainer;

        /// <summary>
        /// 可用对象池（空闲文字）
        /// </summary>
        private readonly Queue<FloatingTextUI> availablePool = new Queue<FloatingTextUI>();

        /// <summary>
        /// 活跃文字列表（用于追踪和清场）
        /// </summary>
        private readonly List<FloatingTextUI> activeTexts = new List<FloatingTextUI>();

        /// <summary>
        /// 当前活跃文字总数
        /// </summary>
        private int totalCreatedCount = 0;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        private bool initialized = false;

        /// <summary>
        /// 确保浮动文字系统已初始化
        /// 多次调用安全：已初始化时直接返回
        /// </summary>
        public void EnsureInitialized()
        {
            if (this.initialized)
            {
                return;
            }

            this.initialized = true;

            // 获取主摄像机
            this.mainCamera = Camera.main;
            if (this.mainCamera == null)
            {
                this.mainCamera = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();
            }

            // 创建 Canvas
            this.canvasObj = FloatingTextTool.EnsureCanvas(
                FloatingTextConstant.CanvasName,
                100); // sortingOrder=100，在普通 UI 之上

            // 创建池容器
            this.poolContainer = new GameObject(FloatingTextConstant.PoolContainerName);
            this.poolContainer.transform.SetParent(this.canvasObj.transform, false);
            this.poolContainer.SetActive(false); // 池容器隐藏，不影响渲染

            // 预热对象池
            this.WarmPool(FloatingTextConstant.DefaultPoolSize);

            if (this.mainCamera == null)
            {
                LogManager.Instance.Log(
                    "FloatingTextManager: 未找到主摄像机，世界坐标转换将使用 Vector3.zero 作为默认值",
                    LogManager.LogLevelEnum.Warning);
            }
        }

        /// <summary>
        /// 预热对象池，预创建指定数量的浮动文字对象
        /// </summary>
        /// <param name="count">预创建数量</param>
        private void WarmPool(int count)
        {
            for (int i = 0; i < count; i++)
            {
                FloatingTextUI ft = this.CreateNewPoolItem(i);
                ft.gameObject.SetActive(false);
                this.availablePool.Enqueue(ft);
            }
        }

        /// <summary>
        /// 创建新的对象池元素
        /// </summary>
        /// <param name="index">序号</param>
        /// <returns>浮动文字组件</returns>
        private FloatingTextUI CreateNewPoolItem(int index)
        {
            GameObject obj = FloatingTextTool.CreateFloatingTextObject(
                FloatingTextConstant.FloatingTextPrefix + index,
                this.poolContainer.transform);
            FloatingTextUI ft = obj.AddComponent<FloatingTextUI>();
            return ft;
        }

        /// <summary>
        /// 从对象池获取一个浮动文字实例
        /// </summary>
        /// <returns>可用的 FloatingTextUI</returns>
        private FloatingTextUI GetFromPool()
        {
            if (this.availablePool.Count > 0)
            {
                return this.availablePool.Dequeue();
            }

            // 池耗尽时动态扩容（不超过最大容量）
            if (this.totalCreatedCount < FloatingTextConstant.MaxPoolSize)
            {
                FloatingTextUI ft = this.CreateNewPoolItem(this.totalCreatedCount);
                this.totalCreatedCount++;
                return ft;
            }

            // 达到最大容量时，回收最老的活跃文字
            if (this.activeTexts.Count > 0)
            {
                FloatingTextUI oldest = this.activeTexts[0];
                oldest.ForceRecycle();
                this.activeTexts.RemoveAt(0);
                return oldest;
            }

            // 极端情况：创建一个不纳入池的临时对象
            FloatingTextUI emergency = this.CreateNewPoolItem(this.totalCreatedCount);
            this.totalCreatedCount++;
            return emergency;
        }

        /// <summary>
        /// 回收浮动文字到对象池（由 FloatingTextUI 生命周期结束时自动调用）
        /// </summary>
        /// <param name="ft">要回收的浮动文字</param>
        public void ReturnToPool(FloatingTextUI ft)
        {
            if (ft == null)
            {
                return;
            }

            ft.transform.SetParent(this.poolContainer.transform, false);
            this.activeTexts.Remove(ft);
            this.availablePool.Enqueue(ft);
        }

        // =============================================================================================================
        // 公开生成接口
        // =============================================================================================================

        /// <summary>
        /// 在指定世界坐标生成伤害浮动文字
        /// </summary>
        /// <param name="worldPos">世界坐标</param>
        /// <param name="damage">伤害值</param>
        /// <param name="isCritical">是否暴击</param>
        /// <param name="isComboDamage">是否为连击增益伤害</param>
        public void SpawnDamageText(Vector3 worldPos, float damage, bool isCritical = false, bool isComboDamage = false)
        {
            if (!this.initialized)
            {
                return;
            }

            FloatingTextType type = isCritical ? FloatingTextType.Critical : FloatingTextType.Damage;

            // 连击伤害在数值上追加标记
            string text = FloatingTextTool.FormatDamageText(damage);

            this.SpawnText(type, text, worldPos);
        }

        /// <summary>
        /// 在指定世界坐标生成治疗浮动文字
        /// </summary>
        /// <param name="worldPos">世界坐标</param>
        /// <param name="healAmount">治疗量</param>
        public void SpawnHealText(Vector3 worldPos, float healAmount)
        {
            if (!this.initialized || healAmount <= 0)
            {
                return;
            }

            string text = FloatingTextTool.FormatHealText(healAmount);
            this.SpawnText(FloatingTextType.Heal, text, worldPos);
        }

        /// <summary>
        /// 在指定世界坐标生成连击浮动文字
        /// </summary>
        /// <param name="worldPos">世界坐标</param>
        /// <param name="comboCount">当前连击数</param>
        public void SpawnComboText(Vector3 worldPos, int comboCount)
        {
            if (!this.initialized || comboCount <= 1)
            {
                return;
            }

            string text = FloatingTextTool.FormatComboText(comboCount);
            this.SpawnText(FloatingTextType.Combo, text, worldPos);
        }

        /// <summary>
        /// 在指定世界坐标生成经验获取浮动文字
        /// </summary>
        /// <param name="worldPos">世界坐标</param>
        /// <param name="expAmount">经验值</param>
        public void SpawnExpText(Vector3 worldPos, int expAmount)
        {
            if (!this.initialized || expAmount <= 0)
            {
                return;
            }

            string text = FloatingTextTool.FormatExpText(expAmount);
            this.SpawnText(FloatingTextType.Experience, text, worldPos);
        }

        /// <summary>
        /// 在指定世界坐标生成闪避浮动文字
        /// </summary>
        /// <param name="worldPos">世界坐标</param>
        public void SpawnDodgeText(Vector3 worldPos)
        {
            if (!this.initialized)
            {
                return;
            }

            this.SpawnText(FloatingTextType.Dodge, FloatingTextConstant.DodgeText, worldPos);
        }

        /// <summary>
        /// 在指定世界坐标生成状态效果浮动文字
        /// </summary>
        /// <param name="worldPos">世界坐标</param>
        /// <param name="statusText">状态描述文字（如 "中毒"、"减速"）</param>
        public void SpawnStatusText(Vector3 worldPos, string statusText)
        {
            if (!this.initialized || string.IsNullOrEmpty(statusText))
            {
                return;
            }

            this.SpawnText(FloatingTextType.StatusEffect, statusText, worldPos);
        }

        /// <summary>
        /// 核心生成方法：从池获取、初始化、放置到屏幕位置
        /// </summary>
        /// <param name="type">文字类型</param>
        /// <param name="text">显示文本</param>
        /// <param name="worldPos">世界坐标</param>
        private void SpawnText(FloatingTextType type, string text, Vector3 worldPos)
        {
            FloatingTextUI ft = this.GetFromPool();
            if (ft == null)
            {
                return;
            }

            // 世界坐标 → 屏幕坐标
            Vector2 screenPos = this.WorldToScreenPosition(worldPos);

            // 添加随机水平偏移避免多个伤害文字完全重叠
            screenPos.x += FloatingTextTool.RandomHorizontalOffset();

            // 将对象移到 Canvas 下（如果不在的话）
            if (ft.transform.parent != this.canvasObj.transform)
            {
                ft.transform.SetParent(this.canvasObj.transform, false);
            }

            ft.Spawn(type, text, screenPos);
            this.activeTexts.Add(ft);
        }

        /// <summary>
        /// 世界坐标转换为屏幕坐标
        /// </summary>
        /// <param name="worldPos">世界坐标</param>
        /// <returns>屏幕坐标（anchoredPosition 空间）</returns>
        private Vector2 WorldToScreenPosition(Vector3 worldPos)
        {
            if (this.mainCamera == null)
            {
                return Vector2.zero;
            }

            Vector3 screenPoint = this.mainCamera.WorldToScreenPoint(worldPos);

            // 屏幕坐标转 Canvas 的 anchoredPosition
            Canvas canvas = this.canvasObj?.GetComponent<Canvas>();
            if (canvas == null)
            {
                return screenPoint;
            }

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            Vector2 anchoredPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPoint,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : this.mainCamera,
                out anchoredPos);
            return anchoredPos;
        }

        // =============================================================================================================
        // 系统管理
        // =============================================================================================================

        /// <summary>
        /// 清空所有活跃浮动文字（用于场景切换或重置）
        /// </summary>
        public void ClearAll()
        {
            foreach (FloatingTextUI ft in this.activeTexts)
            {
                if (ft != null)
                {
                    ft.ForceRecycle();
                }
            }

            this.activeTexts.Clear();
        }

        /// <summary>
        /// 当前活跃文字数量（调试用）
        /// </summary>
        public int ActiveCount
        {
            get { return this.activeTexts.Count; }
        }

        /// <summary>
        /// 对象池可用数量（调试用）
        /// </summary>
        public int AvailablePoolCount
        {
            get { return this.availablePool.Count; }
        }
    }
}
