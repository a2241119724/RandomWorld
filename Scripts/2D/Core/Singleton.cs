namespace LAB2D.Core
{
    using LAB2D.Data;

    /// <summary>
    /// 单例基类（非 MonoBehaviour）。
    /// 通过泛型继承提供线程不安全的延迟初始化 Instance。
    ///
    /// 访问约定：
    ///   - Domain 层（纯逻辑、RuleService、Calculator）:
    ///     必须使用 ServiceLocator.Get&lt;ISomeService&gt;()，禁止直接 .Instance。
    ///     这保证 Domain 代码可脱离 Unity 单元测试。
    ///   - Manager/Gameplay 层（同层间便捷访问）:
    ///     允许使用 .Instance，但跨层访问（如 Manager→Domain）
    ///     仍应通过 ServiceLocator.Get&lt;接口&gt;() 获取。
    ///   - UI/Presentation 层:
    ///     允许 .Instance 访问 Manager，但应逐步迁移至 MVC 的 ServiceLocator 路径。
    ///
    /// C# 单继承限制说明与 Singleton&lt;T&gt; 一致。
    /// </summary>
    /// <typeparam name="T">需要单例的类。</typeparam>
    public abstract class Singleton<T>
        where T : new()
    {
        private static T instance;

        /// <summary>
        /// 单例。
        /// </summary>
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new T();
                }

                return instance;
            }
        }

        /// <summary>
        /// 初始化。
        /// </summary>
        public virtual void Init()
        {
        }
    }

    /// <summary>
    /// 带 ASaveData 的单例（非 MonoBehaviour）。
    ///
    /// 与 Singleton&lt;T&gt; 的 Instance 逻辑重复是 C# 单继承的必然代价：
    /// 需要同时继承 ASaveData（供 ArchiveManager 反射发现存档类）
    /// 并拥有 Singleton 能力，但无法同时继承两个基类。
    ///
    /// 当需要 MonoBehaviour 生命周期时，使用 AMonoSaveData + 手动 Instance。
    /// </summary>
    /// <typeparam name="T">需要单例的类。</typeparam>
    public abstract class ASingletonSaveData<T> : ASaveData
        where T : new()
    {
        private static T instance;

        /// <summary>
        /// 单例。
        /// </summary>
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new T();
                }

                return instance;
            }
        }

        /// <summary>
        /// 初始化。
        /// </summary>
        public void Init()
        {
        }

        /// <inheritdoc/>
        public override void LoadData()
        {
        }

        /// <inheritdoc/>
        public override void SaveData()
        {
        }
    }
}
