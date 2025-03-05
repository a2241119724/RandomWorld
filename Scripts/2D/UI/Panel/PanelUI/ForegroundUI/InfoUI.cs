using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace LAB2D
{
    public class InfoUI : MonoBehaviour
    {
        public static InfoUI Instance { get; private set; }

        private Text fps;
        private Text position;
        private Text time;
        // fps
        private float accum;
        private int frames;
        // time
        private double gameTime;
        private double _gameTime;
        /// <summary>
        /// 白天黑天显示
        /// </summary>
        private Light2D globalLight;

        private void Awake()
        {
            Instance = this;
            fps = Tool.GetComponentInChildren<Text>(gameObject, "FPS");
            position = Tool.GetComponentInChildren<Text>(gameObject, "PlayerPosition");
            time = Tool.GetComponentInChildren<Text>(gameObject, "Time");
            globalLight = GameObject.FindGameObjectWithTag(ResourceConstant.GLOBAL_LIGHT_TAG).GetComponent<Light2D>();
        }

        void Start()
        {
            StartCoroutine(FPS());
        }

        void Update()
        {
            // fps
            // 添加本次可能会执行的帧数
            accum += Time.timeScale / Time.deltaTime;
            // 一秒总共的次数
            ++frames;
            // time
            gameTime += Time.deltaTime;
            globalLight.intensity = Mathf.Clamp(Mathf.Abs(Mathf.Cos((float)gameTime / GlobalData.dayTime)), 0.2f, 1.0f);
            if (gameTime - _gameTime >= 1.0)
            {
                _gameTime = gameTime;
                int hour = (int)gameTime / 3600;
                int minute = ((int)gameTime - hour * 3600) / 60;
                int second = (int)gameTime - hour * 3600 - minute * 60;
                time.text = string.Format("<color=blue>游戏时间: </color>{0:D2}:{1:D2}:{2:D2}", hour, minute, second);
            }
        }

        private IEnumerator FPS()
        {
            while (true)
            {
                // 每秒平均帧数
                accum = accum / frames;
                //if (!double.IsNaN(accum))
                //{
                fps.text = "FPS:" + accum.ToString("F1");
                //}
                accum = 0.0f;
                frames = 0;
                yield return new WaitForSeconds(1.0f);
            }
        }

        public void setPosition(Vector3 worldPos)
        {
            if (worldPos == null)
            {
                LogManager.Instance.log("v is null!!!", LogManager.LogLevel.Error);
                return;
            }
            Vector3Int posMap = TileMap.Instance.worldPosToMapPos(worldPos);
            position.text = "(" + posMap.x + "," + posMap.y + ")";
        }
    }
}
