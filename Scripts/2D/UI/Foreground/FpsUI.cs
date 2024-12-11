using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    public class FpsUI : MonoBehaviour
    {
        private float accum;
        private int frames;

        void Start()
        {
            StartCoroutine(FPS());
        }

        void Update()
        {
            // 添加本次可能会执行的帧数
            accum += Time.timeScale / Time.deltaTime;
            // 一秒总共的次数
            ++frames;
        }

        private IEnumerator FPS()
        {
            while (true)
            {
                // 每秒平均帧数
                accum = accum / frames;
                if (!double.IsNaN(accum))
                {
                    gameObject.GetComponent<Text>().text = "FPS:" + accum.ToString("F1");
                }
                accum = 0.0f;
                frames = 0;
                yield return new WaitForSecondsRealtime(1.0f);
            }
        }
    }
}