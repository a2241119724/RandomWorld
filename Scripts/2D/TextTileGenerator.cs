using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    /// <summary>
    /// 再世界上显示数字
    /// </summary>
    public class TextTileGenerator : MonoBehaviour
    {
        public static TextTileGenerator Instance { private set; get; }

        private Sprite[] sprites;

        private void Awake()
        {
            Instance = this;
            sprites = new Sprite[1000];
        }

        private void Start()
        {
            BuildMap.Instance.directBuild(new Vector3Int(1, 1, 0), getTileByNum(1));
        }

        public Tile getTileByNum(int num) { 
            if(num<0 || num >=1000)
            {
                LogManager.Instance.log("错误的输入", LogManager.LogLevel.Error);
                return null;
            }
            if (sprites[num] == null)
            {
                Sprite sprite = generatorSpriteByNum("" + num);
                sprites[num] = sprite;
            }
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprites[num];
            return tile;
        }

        private Sprite generatorSpriteByNum(string number)
        {
            return null;
        }
    }
}
