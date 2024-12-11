using UnityEngine;
using UnityEngine.UI;

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

    public void setPosition(Vector3 v) {
        if (v == null) {
            Debug.LogError("v is null!!!");
            return;
        }
        positon.text ="("+ (int)Mathf.Round(v.x) + "," + (int)Mathf.Round(v.y) + ")";
    }
}
