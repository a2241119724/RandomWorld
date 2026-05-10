namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// 装备掉落光束特效组件 — 静态光柱 + 微呼吸动画。
    /// 由 EquipmentBeamManager 统一创建和销毁。
    /// </summary>
    public class EquipmentBeam : MonoBehaviour
    {
        private Vector3 baseScale;
        private float elapsed;
        private float pulsePhase;
        private float pulseAmplitude;

        public EquipmentRarityType Rarity { get; private set; }

        private void Awake()
        {
            this.pulsePhase = Random.Range(0f, Mathf.PI * 2f);
        }

        public void Initialize(EquipmentRarityType rarity)
        {
            this.Rarity = rarity;
            this.pulseAmplitude = EquipmentLootTool.HasGlowEffect(rarity)
                ? EquipmentBeamConstant.PulseAmplitudeGlow
                : EquipmentBeamConstant.PulseAmplitudeNormal;
            this.baseScale = this.transform.localScale;
        }

        private void Update()
        {
            this.elapsed += Time.deltaTime;
            float pulse = 1f + Mathf.Sin(this.elapsed * EquipmentBeamConstant.PulseSpeed + this.pulsePhase) * this.pulseAmplitude;
            this.transform.localScale = new Vector3(this.baseScale.x * pulse, this.baseScale.y, 1f);
        }
    }
}
