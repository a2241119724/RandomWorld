using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace LAB2D
{
    public class GameTimeUI : MonoBehaviour
    {
        private double gameTime;
        private double _gameTime;
        private Light2D globalLight;
        private Text time;

        private void Awake()
        {
            time = transform.GetComponent<Text>();
            globalLight = GameObject.FindGameObjectWithTag(ResourceConstant.GLOBAL_LIGHT_TAG).GetComponent<Light2D>();
        }

        void Update()
        {
            gameTime += Time.deltaTime;
            globalLight.intensity = Mathf.Clamp(Mathf.Abs(Mathf.Cos((float)gameTime/1000)),0.2f,1.0f);
            if (gameTime - _gameTime >= 1.0)
            {
                _gameTime = gameTime;
                int hour = (int)gameTime / 3600;
                int minute = ((int)gameTime - hour * 3600) / 60;
                int second = (int)gameTime - hour * 3600 - minute * 60;
                time.text = string.Format("<color=#2ED573>游戏时间: </color>{0:D2}:{1:D2}:{2:D2}", hour, minute, second);
            }
        }
    }
}