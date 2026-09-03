namespace LAB2D.Render
{
    using UnityEngine;

    /// <summary>
    /// Tile 级点光源参数载体 — lightResolver（如 BuildMap 按 BuildItemData 的
    /// LightRadius/LightIntensity/LightColor/LightFlicker 字段）产出，
    /// 由 TileVisualSpawner.SyncLight 调和到视觉 GO 下的 Light2D。
    /// null = 该格无光（SyncLight 负责销毁残留光 GO）。
    /// </summary>
    public class TileLightConfig
    {
        /// <summary>点光源外半径（世界单位/格）。</summary>
        public float Radius;

        /// <summary>点光源强度。</summary>
        public float Intensity;

        /// <summary>光颜色。</summary>
        public Color Color;

        /// <summary>是否平滑闪烁（火光摇曳）。</summary>
        public bool Flicker;
    }
}
