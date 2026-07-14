namespace LAB2D.Core
{
    using LAB2D.Data;

    /// <summary>
    /// 单例基类（非 MonoBehaviour）。
    /// 通过泛型继承提供线程不安全的延迟初始化 Instance。
    ///
    /// 设计说明：
    ///   C# 单继承限制意味着 Singleton&lt;T&gt; 和 ASingletonSaveData&lt;T&gt;
    ///   是两条独立分支，无法组合。如果需要同时具备 Singleton + 其他基类
    ///   （如 MonoBehaviour、MonoBehaviourPun），应使用手动 Instance 属性
    ///   模式替代继承：
    ///   <code>
    ///   public class MyManager : MonoBehaviourPun
    ///   {
    ///       public static MyManager Instance { get; private set; }
    ///       void Awake() { Instance = this; }
    ///   }
    ///   </code>
    ///   参考：GlobalInit、PanelController 均采用此模式。
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
