using UnityEngine;

public class Blood : MonoBehaviour
{
    //private GameObject parent; // 跟随物体

    void Start()
    {
        // 特效结束后销毁
        Destroy(gameObject, GetComponent<ParticleSystem>().main.startLifetime.constant);
    }

    //public void setParent(GameObject parent) {
    //}
}
