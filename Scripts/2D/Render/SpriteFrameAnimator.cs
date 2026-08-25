namespace LAB2D.Render
{
    using LAB2D.Manager;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Sprite 序列帧动画：以图片前缀（如物品英文名 Name）自动收集 {prefix}_0、{prefix}_1、
    /// {prefix}_2… 序列 Sprite（收集至首个缺失，最多默认 128 帧兜底），按固定默认帧率循环
    /// 切换 SpriteRenderer 的 sprite。由 TileVisualSpawner 在创建非恒底层（LayerMode != Bottom）
    /// 且 ItemData.IsAnimation 开启的独立视觉时挂载；无任何序列帧时回退静态显示（组件自毁，
    /// 不影响原 tile 静态图）。
    /// </summary>
    public class SpriteFrameAnimator : MonoBehaviour
    {
        private const float DefaultFrameRate = 6f; // 固定默认帧率（帧/秒），需求未提供可配置项
        private const int MaxFrameGuard = 128; // 帧数上限兜底，防异常命名无限循环

        private SpriteRenderer spriteRenderer;
        private Sprite[] frames = System.Array.Empty<Sprite>();
        private float frameInterval = 1f / DefaultFrameRate;
        private float timer;
        private int index;

        /// <summary>
        /// 当前已加载的帧图前缀（Init 成功时记录，供调用方判断同格换物品时是否需要重载帧序列）。
        /// </summary>
        public string Prefix { get; private set; }

        /// <summary>
        /// 以前缀初始化动画帧序列。
        /// </summary>
        /// <param name="prefix">帧图前缀（如物品英文名），按 {prefix}_0/{prefix}_1/... 收集。</param>
        /// <returns>是否收集到至少一帧；false 时由调用方回退静态显示。</returns>
        public bool Init(string prefix)
        {
            this.spriteRenderer = this.GetComponent<SpriteRenderer>();
            if (this.spriteRenderer == null || string.IsNullOrEmpty(prefix))
            {
                return false;
            }

            ResourceManager resourceManager = Core.ServiceLocator.Get<ResourceManager>();
            if (resourceManager == null)
            {
                return false;
            }

            Sprite[] collected = CollectFrames(resourceManager.TryGetImage, prefix);
            if (collected.Length == 0)
            {
                return false;
            }

            this.frames = collected;
            this.frameInterval = 1f / DefaultFrameRate;
            this.index = 0;
            this.timer = 0f;
            this.Prefix = prefix;
            this.spriteRenderer.sprite = this.frames[0];
            return true;
        }

        private void Update()
        {
            if (this.frames.Length < 2)
            {
                return; // 单帧/无帧：静态显示，不切换
            }

            this.timer += Time.deltaTime;
            while (this.timer >= this.frameInterval)
            {
                this.timer -= this.frameInterval;
                this.index = (this.index + 1) % this.frames.Length;
            }

            if (this.spriteRenderer != null)
            {
                this.spriteRenderer.sprite = this.frames[this.index];
            }
        }

        /// <summary>
        /// 纯函数：按 {prefix}_0/{prefix}_1/... 收集帧序列，首个缺失即结束（上限 maxFrames 兜底）。
        /// 与 MonoBehaviour/ServiceLocator 解耦，注入加载器便于单测（测试可用 string 代替 Sprite）。
        /// </summary>
        /// <param name="loader">按图片名取帧的委托（如 ResourceManager.TryGetImage），不存在时返回 null。</param>
        /// <param name="prefix">帧图前缀（如物品英文名）。</param>
        /// <param name="maxFrames">帧数上限兜底，防异常命名无限循环。</param>
        /// <typeparam name="T">帧类型（生产为 Sprite，测试可为 string）。</typeparam>
        /// <returns>收集到的帧序列；无任何帧时为空数组。</returns>
        public static T[] CollectFrames<T>(System.Func<string, T> loader, string prefix, int maxFrames = MaxFrameGuard)
            where T : class
        {
            List<T> collected = new List<T>();
            for (int i = 0; i < maxFrames; i++)
            {
                T frame = loader(prefix + "_" + i);
                if (frame == null)
                {
                    break; // 首个缺失即视为序列结束
                }

                collected.Add(frame);
            }

            return collected.ToArray();
        }
    }
}
