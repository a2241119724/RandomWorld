namespace LAB2D
{
    using LAB2D.Character.Worker.Task;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 再世界上显示数字.
    /// </summary>
    public class TextTileGenerator : MonoBehaviour
    {
        private Sprite[] sprites;

        /// <summary>
        /// 单例.
        /// </summary>
        public static TextTileGenerator Instance { get; private set; }

        /// <summary>
        /// 根据数字获取对应的数字Tile.
        /// </summary>
        /// <param name="num">数字.</param>
        /// <returns>对应的数字Tile.</returns>
        public Tile GetTileByNum(int num)
        {
            if (num < 0 || num >= 1000)
            {
                AWorkerTask.LogProvider("错误的输入", LogManager.LogLevelEnum.Error);
                return null;
            }

            if (this.sprites[num] == null)
            {
                Sprite sprite = this.GeneratorSpriteByNum(string.Empty + num);
                this.sprites[num] = sprite;
            }

            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = this.sprites[num];
            return tile;
        }

        public void Awake()
        {
            Instance = this;
            this.sprites = new Sprite[1000];
        }

        public void Start()
        {
            BuildMap.Instance.DirectBuild(new Vector3Int(1, 1, 0), this.GetTileByNum(1));
        }

        private Sprite GeneratorSpriteByNum(string number)
        {
            return null;
        }
    }
}
