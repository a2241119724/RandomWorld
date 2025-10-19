namespace LAB2D
{
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
    /// 带ASaveData的单例.
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
