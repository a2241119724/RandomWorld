namespace LAB2D.Gameplay
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Domain.Common;
    using LAB2D.Enum;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 装备光束特效管理器（Singleton）。
    /// 在掉落装备位置生成静态半透明光柱，按稀有度着色。
    /// 渐变/呼吸/流光全部由 Custom/BeamGradient shader 程序化生成（无贴图、无逐实例组件），
    /// 材质按稀有度缓存共享。
    /// </summary>
    public class EquipmentBeamManager : Singleton<EquipmentBeamManager>, IInitializable
    {
        private Dictionary<Vector3Int, BeamEntry> activeBeams = new Dictionary<Vector3Int, BeamEntry>();
        private Transform beamContainer;

        /// <summary>光柱 shader（Resources/Shader/BeamGradient，程序化无贴图）</summary>
        private const string BeamShaderName = "Custom/BeamGradient";

        private Shader beamShader;

        /// <summary>按稀有度缓存的共享材质（同稀有度光柱共用 → SRP Batcher 合批）</summary>
        private readonly Dictionary<EquipmentRarityType, Material> rarityMaterials = new Dictionary<EquipmentRarityType, Material>();

        /// <summary>共享 Quad Mesh</summary>
        private Mesh quadMesh;

        public bool IsInitialized { get; private set; }

        private class BeamEntry
        {
            public GameObject Go;
            public EquipmentRarityType Rarity;
        }

        public void Initialize()
        {
            if (this.IsInitialized) return;
            this.activeBeams = new Dictionary<Vector3Int, BeamEntry>();
            this.beamContainer = GameObject.Find("All")?.transform.Find(EquipmentBeamConstant.BeamContainerName);
            this.quadMesh = this.BuildQuadMesh();
            this.beamShader = Shader.Find(BeamShaderName);
            if (this.beamShader == null)
            {
                AWorkerTask.LogProvider($"未找到 {BeamShaderName}，光柱无法显示（shader 应位于 Resources/Shader 下）", LogManager.LogLevelEnum.Warning);
            }

            this.IsInitialized = true;
            AWorkerTask.LogProvider("EquipmentBeamManager 初始化完成", LogManager.LogLevelEnum.Trace);
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

            this.activeBeams[mapPos] = new BeamEntry { Go = beamObj, Rarity = rarity };
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
                EquipmentRarityType rarity = entry.Rarity;
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
            if (!Core.ServiceLocator.TryGet(out ItemMap im)) return;
            List<Vector3Int> stale = new List<Vector3Int>();
            foreach (KeyValuePair<Vector3Int, BeamEntry> kv in this.activeBeams)
                if (kv.Value.Go == null || im.GetTile(kv.Key) == null)
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
            if (this.quadMesh == null || this.beamShader == null) return null;

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

            // 按稀有度共享材质（shader 程序化生成渐变/流光，同稀有度光柱合批）
            mr.sharedMaterial = this.GetOrCreateBeamMaterial(rarity);
            mr.sortingLayerName = "Highest";
            mr.sortingOrder = 0;

            return beamObj;
        }

        /// <summary>
        /// 获取或创建稀有度对应的光柱材质（懒建缓存，同稀有度光柱共享一份）。
        /// 参数逐项对应原 CPU 贴图生成与 EquipmentBeam 呼吸动画：
        /// _Falloff = 6/width²（原 GenerateBeamTexture 同公式）、_PulseAmp = Glow/Normal 两档。
        /// </summary>
        private Material GetOrCreateBeamMaterial(EquipmentRarityType rarity)
        {
            if (this.rarityMaterials.TryGetValue(rarity, out Material cached))
            {
                return cached;
            }

            Material mat = new Material(this.beamShader);
            Color rarityColor = EquipmentLootTool.GetRarityColor(rarity);
            float beamWidth = EquipmentBeamConstant.GetBeamWidth(rarity);
            mat.SetColor("_BeamColor", new Color(rarityColor.r, rarityColor.g, rarityColor.b, EquipmentBeamConstant.GetBeamAlpha(rarity)));
            mat.SetFloat("_Falloff", 6f / (beamWidth * beamWidth));
            mat.SetFloat("_PulseSpeed", EquipmentBeamConstant.PulseSpeed);
            mat.SetFloat("_PulseAmp", EquipmentLootTool.HasGlowEffect(rarity)
                ? EquipmentBeamConstant.PulseAmplitudeGlow
                : EquipmentBeamConstant.PulseAmplitudeNormal);

            this.rarityMaterials[rarity] = mat;
            return mat;
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
            if (entry?.Go != null)
            {
                Object.Destroy(entry.Go);
            }
        }
    }
}
