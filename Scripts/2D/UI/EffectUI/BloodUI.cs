using UnityEngine;

public class BloodUI : MonoBehaviour
{
    //private GameObject parent; // ��������

    void Start()
    {
        // ��Ч����������
        Destroy(gameObject, GetComponent<ParticleSystem>().main.startLifetime.constant);
    }

    //public void setParent(GameObject parent) {
    //}
}
