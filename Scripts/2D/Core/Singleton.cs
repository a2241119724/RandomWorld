namespace LAB2D.Core
{
    using LAB2D.Data;

    /// <summary>
    /// 单例.
    /// </summary>
    /// <typeparam name="T">需要单例的类.</typeparam>
    public abstract class Singleton<T>
        where T : new()
    {
        private static T instance;

        /// <summary>
        /// 单例.
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
        /// 初始化.
        /// </summary>
        public virtual void Init()
        {
            // 初始化Instance
        }
    }

    /// <summary>
    /// 带ASaveData的单例。
    /// 与 Singleton&lt;T&gt; 的单例逻辑重复是 C# 单继承的限制：
    /// 必须同时继承 ASaveData（供 ArchiveManager 反射发现）并拥有 Singleton 能力。
    /// </summary>
    /// <typeparam name="T">需要单例的类.</typeparam>
    public abstract class ASingletonSaveData<T> : ASaveData
        where T : new()
    {
        private static T instance;

        /// <summary>
        /// 单例.
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
        /// 初始化.
        /// </summary>
        public void Init()
        {
            // 初始化Instance
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
