namespace LAB2D.UI.Action
{
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 植物生长进度条 — 种植后显示在世界空间，追踪生长进度。
    /// 生长完成后自动切换到可采集状态。
    /// </summary>
    public class PlantGrowthBar : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        [SerializeField] private Image fillImage;

        private Vector3Int mapPos;
        private float growthDuration = 30f;
        private float elapsed;
        private bool isComplete;

        private static readonly Color GrowingColor = new Color(0.3f, 0.7f, 0.3f);
        private static readonly Color ReadyColor = new Color(0.9f, 0.8f, 0.2f);

        /// <summary>
        /// 设置地图坐标。
        /// </summary>
        public void SetMapPos(Vector3Int posMap)
        {
            this.mapPos = posMap;
            this.elapsed = 0f;
            this.isComplete = false;

            if (this.fillImage != null)
            {
                this.fillImage.color = GrowingColor;
                this.fillImage.fillAmount = 0f;
            }

            if (this.slider != null)
            {
                this.slider.value = 0f;
            }
        }

        /// <summary>
        /// 设置生长总时长（秒）。
        /// </summary>
        public void SetDuration(float duration)
        {
            this.growthDuration = duration;
        }

        public void Update()
        {
            if (this.isComplete) return;

            this.elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(this.elapsed / this.growthDuration);

            if (this.slider != null)
            {
                this.slider.value = progress;
            }

            if (this.fillImage != null)
            {
                this.fillImage.fillAmount = progress;
            }

            if (progress >= 1f)
            {
                this.OnGrowthComplete();
            }
        }

        private void OnGrowthComplete()
        {
            this.isComplete = true;

            if (this.fillImage != null)
            {
                this.fillImage.color = ReadyColor;
            }

            if (this.slider != null)
            {
                this.slider.value = 1f;
            }
        }

        /// <summary>
        /// 是否生长完成（可采集）。
        /// </summary>
        public bool IsComplete => this.isComplete;

        /// <summary>
        /// 已生长时间（秒），用于存档。
        /// </summary>
        public float Elapsed => this.elapsed;

        /// <summary>
        /// 获取地图坐标。
        /// </summary>
        public Vector3Int MapPos => this.mapPos;

        /// <summary>
        /// 设置已生长时间（用于读档恢复）。
        /// </summary>
        public void SetElapsed(float elapsed)
        {
            this.elapsed = elapsed;
            float progress = Mathf.Clamp01(this.elapsed / this.growthDuration);
            if (this.slider != null) this.slider.value = progress;
            if (this.fillImage != null) this.fillImage.fillAmount = progress;
            if (progress >= 1f)
            {
                this.OnGrowthComplete();
            }
        }
    }
}
