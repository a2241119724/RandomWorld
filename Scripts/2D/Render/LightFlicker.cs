namespace LAB2D.Render
{
    using UnityEngine;
    using UnityEngine.Rendering.Universal;

    /// <summary>
    /// 火光平滑闪烁 — 双频 sin 叠加近似噪声（幅度 ~10%），实例随机相位防止
    /// 同屏多个火把同步闪。挂载于光源 GO（TileVisualSpawner.SyncLight 创建），
    /// 目标光销毁时随宿主 GameObject 一起销毁。
    /// </summary>
    public class LightFlicker : MonoBehaviour
    {
        private const float Amplitude = 0.10f; // 闪烁幅度（柔和）
        private const float SpeedA = 2.8f; // 低频主摆
        private const float SpeedB = 7.3f; // 高频细摆

        private Light2D target;
        private float baseIntensity;
        private float phaseA;
        private float phaseB;

        /// <summary>
        /// 绑定闪烁目标与基准强度（SyncLight 每次参数变化时重绑）。
        /// </summary>
        public void Init(Light2D target, float baseIntensity)
        {
            this.target = target;
            this.baseIntensity = baseIntensity;
            this.phaseA = Random.value * 10f;
            this.phaseB = Random.value * 10f;
        }

        private void Update()
        {
            if (this.target == null)
            {
                return; // 目标已销毁（Unity 假 null），等宿主一并回收
            }

            float t = Time.time;
            float n = Mathf.Sin((t + this.phaseA) * SpeedA) * 0.7f
                    + Mathf.Sin((t + this.phaseB) * SpeedB) * 0.3f;
            this.target.intensity = this.baseIntensity * (1f + Amplitude * n);
        }
    }
}
