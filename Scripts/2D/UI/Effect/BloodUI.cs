namespace LAB2D.UI.Effect
{
    using UnityEngine;

    /// <summary>
    /// 溅血 UI
    /// </summary>
    public class BloodUI : MonoBehaviour
    {
        public void Start()
        {
            // 特效结束后销毁
            Destroy(this.gameObject, this.GetComponent<ParticleSystem>().main.startLifetime.constant);
        }
    }
}
