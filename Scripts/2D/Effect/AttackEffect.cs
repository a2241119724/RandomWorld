namespace LAB2D
{
    using UnityEngine;

    public class AttackEffect : MonoBehaviour
    {
        private void OnParticleCollision(GameObject other)
        {
            if (other.GetComponent<Enemy>() != null)
            {
                other.GetComponent<Enemy>().ReduceHp(10);
            }
        }
    }
}
