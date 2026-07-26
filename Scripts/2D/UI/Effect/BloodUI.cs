namespace LAB2D.UI.Effect
{
    using UnityEngine;

    /// <summary>
    /// 溅血 UI
    /// </summary>
    public class BloodUI : MonoBehaviour
    {
        // private GameObject parent; // 跟随物体
        public void Start()
        {
            // 特效结束后销毁
            Destroy(this.gameObject, this.GetComponent<ParticleSystem>().main.startLifetime.constant);
        }

        // public void setParent(GameObject parent) {
        // }
    }
}
