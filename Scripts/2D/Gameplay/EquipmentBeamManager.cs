namespace LAB2D.Gameplay
{
    using LAB2D;
    using LAB2D.Domain.Common;
    using LAB2D.Enum;
    using LAB2D.UI.Effect;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 装备光束特效管理器（Singleton）。
    /// 在掉落装备位置生成静态半透明光柱，按稀有度着色。
    /// 使用程序化渐变 Quad Mesh，无需外部资源。
    /// </summary>
    public class EquipmentBeamManager : Singleton<EquipmentBeamManager>, IInitializable
    {
        private Dictionary<Vector3Int, BeamEntry> activeBeams = new Dictionary<Vector3Int, BeamEntry>();
        private Transform beamContainer;

        /// <summary>缓存的程序化光束贴图（按稀有度），底部亮 → 顶部透明</summary>
        private Dictionary<EquipmentRarityType, Texture2D> beamTextures = new Dictionary<EquipmentRarityType, Texture2D>();

        /// <summary>共享 Quad Mesh</summary>
        private Mesh quadMesh;

        /// <summary>共享材质模板</summary>
        private Material sharedMaterial;

        public bool IsInitialized { get; private set; }

        private class BeamEntry
        {
            public EquipmentBeam Beam;
        }

        public void Initialize()
        {
            if (this.IsInitialized) return;
            this.beamContainer = new GameObject(EquipmentBeamConstant.BeamContainerName).transform;
            this.activeBeams = new Dictionary<Vector3Int, BeamEntry>();
            this.quadMesh = this.BuildQuadMesh();
            this.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            this.sharedMaterial.SetInt("_ZWrite", 0);
            this.sharedMaterial.renderQueue = 3000;
            this.IsInitialized = true;
            LogManager.Instance.Log("EquipmentBeamManager 初始化完成", LogManager.LogLevelEnum.Trace);
        }

        public void SpawnBeam(Vector3Int mapPos, Vector3 worldPos, EquipmentRarityType rarity)
        {
            if (!this.IsInitialized) this.Initialize();
            if (this.activeBeams.TryGetValue(mapPos, out BeamEntry existing))
            {
                this.SafeDestroy(existing);
                this.activeBeams.Remove(mapPos);
            }

            GameObject beamObj = this.CreateBeamObject(worldPos, rarity);
            if (beamObj == null) return;

            EquipmentBeam beam = beamObj.AddComponent<EquipmentBeam>();
            beam.Initialize(rarity);
            this.activeBeams[mapPos] = new BeamEntry { Beam = beam };
        }

        public void RemoveBeamAt(Vector3Int mapPos)
        {
            if (!this.IsInitialized) return;
            if (this.activeBeams.TryGetValue(mapPos, out BeamEntry entry))
            {
                this.SafeDestroy(entry);
                this.activeBeams.Remove(mapPos);
            }
        }

        /// <summary>
        /// 移除指定位置的光束并返回其稀有度。
        /// 用于搬运等场景：拾取时移除光束并记录稀有度，放下时重新生成。
        /// </summary>
        /// <param name="mapPos">地图坐标</param>
        /// <returns>被移除光束的稀有度，如果该位置没有光束则返回 null</returns>
        public EquipmentRarityType? TryRemoveBeamAt(Vector3Int mapPos)
        {
            if (!this.IsInitialized) return null;
            if (this.activeBeams.TryGetValue(mapPos, out BeamEntry entry))
            {
                EquipmentRarityType rarity = entry.Beam != null
                    ? entry.Beam.Rarity
                    : EquipmentRarityType.Common;
                this.SafeDestroy(entry);
                this.activeBeams.Remove(mapPos);
                return rarity;
            }

            return null;
        }

        public void RemoveAllBeams()
        {
            if (!this.IsInitialized) return;
            foreach (BeamEntry entry in this.activeBeams.Values)
                this.SafeDestroy(entry);
            this.activeBeams.Clear();
        }

        public void CleanupStaleBeams()
        {
            if (!this.IsInitialized || this.activeBeams.Count == 0) return;
            if (ItemMap.Instance == null) return;
            List<Vector3Int> stale = new List<Vector3Int>();
            foreach (KeyValuePair<Vector3Int, BeamEntry> kv in this.activeBeams)
                if (kv.Value.Beam == null || ItemMap.Instance.GetTile(kv.Key) == null)
                    stale.Add(kv.Key);
            foreach (Vector3Int p in stale)
            {
                if (this.activeBeams.TryGetValue(p, out BeamEntry e))
                {
                    this.SafeDestroy(e);
                }

                this.activeBeams.Remove(p);
            }
        }

        // =============================================================================================================
        // 光束构建
        // =============================================================================================================

        private GameObject CreateBeamObject(Vector3 worldPos, EquipmentRarityType rarity)
        {
            if (this.quadMesh == null || this.sharedMaterial == null) return null;

            string objName = EquipmentBeamConstant.BeamObjectPrefix + EquipmentLootTool.GetRarityName(rarity);
            GameObject beamObj = new GameObject(objName);

            // 位置：道具正上方偏移一点，光束从道具"头顶"向上延伸
            float beamHeight = EquipmentBeamConstant.GetBeamHeight(rarity);
            // 光柱底部对齐道具位置，pivot 在底部
            beamObj.transform.position = new Vector3(worldPos.x, worldPos.y, -0.5f);
            beamObj.transform.SetParent(this.beamContainer, true);

            float beamWidth = EquipmentBeamConstant.GetBeamWidth(rarity);
            beamObj.transform.localScale = new Vector3(beamWidth, beamHeight, 1f);

            // MeshFilter + MeshRenderer
            MeshFilter mf = beamObj.AddComponent<MeshFilter>();
            mf.mesh = this.quadMesh;

            MeshRenderer mr = beamObj.AddComponent<MeshRenderer>();

            // 每个光束用独立材质实例（不同颜色）
            Color rarityColor = EquipmentLootTool.GetRarityColor(rarity);
            float alpha = EquipmentBeamConstant.GetBeamAlpha(rarity);
            Material mat = new Material(this.sharedMaterial);
            mat.SetColor("_Color", new Color(rarityColor.r, rarityColor.g, rarityColor.b, alpha));
            mat.mainTexture = this.GetBeamTexture(rarity);
            mr.material = mat;
            mr.sortingLayerName = "Highest";
            mr.sortingOrder = 0;

            return beamObj;
        }

        // =============================================================================================================
        // 程序化纹理
        // =============================================================================================================

        /// <summary>
        /// 获取或生成稀有度对应的渐变贴图（256x256，底部亮→顶部透明）。
        /// </summary>
        private Texture2D GetBeamTexture(EquipmentRarityType rarity)
        {
            if (!this.beamTextures.TryGetValue(rarity, out Texture2D tex))
            {
                tex = this.GenerateBeamTexture(rarity);
                this.beamTextures[rarity] = tex;
            }

            return tex;
        }

        /// <summary>
        /// 生成光束渐变贴图：底部实心→顶部完全透明，水平方向窄高斯衰减形成光柱形状。
        /// </summary>
        private Texture2D GenerateBeamTexture(EquipmentRarityType rarity)
        {
            int w = EquipmentBeamConstant.TextureWidth;
            int h = EquipmentBeamConstant.TextureHeight;
            float beamWidth = EquipmentBeamConstant.GetBeamWidth(rarity);
            // 宽度因子：光束越宽，高斯衰减越缓（让宽光束边缘更柔和）
            float falloff = 6f / (beamWidth * beamWidth);

            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];
            float halfW = w * 0.5f;

            for (int y = 0; y < h; y++)
            {
                float vFade = 1f - ((float)y / h); // 底部=1，顶部=0
                vFade = (float)System.Math.Pow(vFade, 0.6f);    // 加速顶部淡出

                for (int x = 0; x < w; x++)
                {
                    float dx = (x - halfW) / halfW;
                    float hFade = (float)System.Math.Exp(-falloff * dx * dx);
                    float alpha = hFade * vFade;
                    pixels[y * w + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }

        // =============================================================================================================
        // Quad Mesh
        // =============================================================================================================

        /// <summary>
        /// 构建宽1高1的 Quad（pivot 底部中心），UV 从左下(0,0)到右上(1,1)。
        /// </summary>
        private Mesh BuildQuadMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "BeamQuad";

            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(-0.5f, 0f, 0f),  // 左下
                new Vector3( 0.5f, 0f, 0f),  // 右下
                new Vector3(-0.5f, 1f, 0f),  // 左上
                new Vector3( 0.5f, 1f, 0f),  // 右上
            };

            Vector2[] uv = new Vector2[4]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
            };

            int[] triangles = new int[6] { 0, 2, 1, 2, 3, 1 };

            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private void SafeDestroy(BeamEntry entry)
        {
            if (entry?.Beam != null && entry.Beam.gameObject != null)
            {
                Object.Destroy(entry.Beam.gameObject);
            }
        }
    }
}
