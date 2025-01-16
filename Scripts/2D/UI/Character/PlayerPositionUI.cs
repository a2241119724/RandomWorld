using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    public class PlayerPositionUI : MonoBehaviour
    {
        public static PlayerPositionUI Instance;

        private Text positon;

        private void Awake()
        {
            Instance = this;
            positon = GetComponent<Text>();
            if (positon == null)
            {
                Debug.LogError("text Not Found!!!");
                return;
            }
        }

        public void setPosition(Vector3 worldPos)
        {
            if (worldPos == null)
            {
                Debug.LogError("v is null!!!");
                return;
            }
            Vector3Int posMap = TileMap.Instance.worldPosToMapPos(worldPos);
            positon.text = "(" + posMap.x + "," + posMap.y + ")";
        }
    }
}
