namespace LAB2D.Render
{
    using UnityEngine;

    /// <summary>
    /// Sprite 帧动画：按名称（如物品英文名 Name）从 AnimationManager 取 AnimatorController，
    /// 挂 Animator 组件播放其默认状态（循环由 .anim 的 Loop Time 驱动）。
    /// Init 时随机化播放相位，避免成片装饰（树木等）多实例同相位摆动。
    /// 由 TileVisualSpawner 在创建非恒底层（LayerMode != Bottom）
    /// 且 ItemData.IsAnimation 开启的独立视觉时挂载；取不到 controller 时回退静态显示
    /// （组件自毁，不影响原 tile 静态图）。
    /// </summary>
    public class SpriteFrameAnimator : MonoBehaviour
    {
        private Animator animator; // 挂载的 Animator（须随本组件销毁，避免残留覆写 sprite）

        /// <summary>
        /// 当前播放的 controller 名称（Init 成功时记录，供调用方判断同格换物品时是否需要重载动画）。
        /// </summary>
        public string Prefix { get; private set; }

        /// <summary>
        /// 以名称初始化动画：从 AnimationManager 取 AnimatorController 并经 Animator 组件播放。
        /// </summary>
        /// <param name="prefix">controller 名称（如物品英文名 Name），同时是状态机内状态名。</param>
        /// <returns>是否成功取到 controller；false 时由调用方回退静态显示。</returns>
        public bool Init(string prefix)
        {
            if (this.GetComponent<SpriteRenderer>() == null || string.IsNullOrEmpty(prefix))
            {
                return false;
            }

            RuntimeAnimatorController controller = ServiceLocator.Get<AnimationManager>()?.GetController(prefix);
            if (controller == null)
            {
                return false;
            }

            // 同格换物品等场景复用已有组件；Animator 默认按 Renderer 可见性剔除——sprite 初始为
            // null（无边界）会被判不可见，动画永不求值、sprite 永不被写 → 必须 AlwaysAnimate
            if (this.animator == null)
            {
                this.animator = this.gameObject.AddComponent<Animator>();
                this.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }

            // 换 controller 即播其默认状态；按状态名（= prefix）随机归一化相位，
            // 打散成片装饰（树木等）的同步摆动；状态名不匹配时静默落回默认状态第 0 帧
            this.animator.runtimeAnimatorController = controller;
            this.animator.Play(prefix, 0, Random.Range(0f, 1f));

            this.Prefix = prefix;
            return true;
        }

        private void OnDestroy()
        {
            // Animator 残留会每帧覆写 sprite，与静态 tile 图回退逻辑打架，须一并销毁
            if (this.animator != null)
            {
                Object.Destroy(this.animator);
            }
        }
    }
}
