namespace LAB2D
{
    using UnityEngine;

    public class RainUI : MonoBehaviour
    {
        public void Test()
        {
            // 创建粒子系统
            GameObject rain = new ("Rain");
            ParticleSystem ps = rain.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startSpeed = 10f; // 雨滴下落速度
            main.startSize = 0.1f; // 雨滴大小
            main.startLifetime = 2f; // 雨滴存活时间
            var emission = ps.emission;
            emission.rateOverTime = 1000f; // 雨滴密度
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.ConeVolume; // 圆锥形状模拟雨区
            shape.angle = 0f;
            shape.radius = 5f;

            // 添加材质
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        }
    }
}
