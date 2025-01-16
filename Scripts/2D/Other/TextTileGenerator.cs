using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

namespace LAB2D
{
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
            BuildMap.Instance.addPassBuild(new Vector3Int(1, 1, 0), getTileByNum(1));
        }

        public Tile getTileByNum(int num) { 
            if(num<0 || num >=1000)
            {
                Debug.LogError("¥ÌŒÛµƒ ‰»Î");
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
