namespace LAB2D.Editor
{
    using LAB2D.Data;
    using LAB2D.SO;
    using System.Collections.Generic;
    using System.IO;
    using UnityEditor;
    using UnityEditor.Animations;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 一次性资产接入工具：为 Torch/Campfire 光源建筑生成 tile/动画/SO 条目。
    /// 菜单「工具/光源建筑资产生成」一键完成（幂等：资产存在即重建、SO 条目按 Name 去重）。
    ///
    /// 前置：tmp/gen_flame_sprites.py 已输出 8 帧素材到
    /// Resources/Images/Item/Build/{Torch,Campfire}/{Name}_{0..3}.png（96x96 透明底）。
    /// 生成内容（约定：三同 = 类名 == tile 名 == SO 条目 Name）：
    /// - Tile：Resources/Tilemap/Item/Build/{Name}.asset（引用帧 0 sprite，Sprite collider）
    /// - 动画：Resources/Animation/Build/{Name}.anim + {Name}.controller
    ///   （单状态名 = Name，AnimationManager 按资产名加载、SpriteFrameAnimator.Play(Name)）
    /// - SO 条目：Resources/SO/Build/BuildOtherItemData.asset 追加（含 LightRadius 等光照字段；
    ///   Id 由 BuildItemDataSO.OnEnable 按列表顺序自动分配，磁盘写 0 占位）
    /// </summary>
    public static class BuildLightAssetGenerator
    {
        private const string PngRoot = "Assets/Resources/Images/Item/Build";
        private const string TileDir = "Assets/Resources/Tilemap/Item/Build";
        private const string AnimDir = "Assets/Resources/Animation/Build";
        private const string SoPath = "Assets/Resources/SO/Build/BuildOtherItemData.asset";
        private const int FrameCount = 4;

        private class LightBuildSpec
        {
            public string Name;
            public string CnName;
            public string Info;
            public float LightRadius;
            public float LightIntensity;
            public (string item, int count)[] Costs;

            public override string ToString() => $"{this.Name}({this.CnName})";
        }

        private static readonly LightBuildSpec[] Specs =
        {
            new LightBuildSpec
            {
                Name = "Torch",
                CnName = "火把",
                Info = "火把：建成后发光，照亮周边（木头x2）",
                LightRadius = 2.5f,
                LightIntensity = 0.9f,
                Costs = new[] { ("CustomWood", 2) },
            },
            new LightBuildSpec
            {
                Name = "Campfire",
                CnName = "篝火",
                Info = "篝火：大范围暖光源，夜间聚集点（木头x5 石头x2）",
                LightRadius = 4.0f,
                LightIntensity = 1.2f,
                Costs = new[] { ("CustomWood", 5), ("CustomStone", 2) },
            },
        };

        [MenuItem("工具/光源建筑资产生成(Torch/Campfire)")]
        public static void GenerateAll()
        {
            foreach (LightBuildSpec spec in Specs)
            {
                GenerateOne(spec);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[BuildLightAssetGenerator] 全部完成：{Specs.Length} 个光源建筑接入就绪");
        }

        private static void GenerateOne(LightBuildSpec spec)
        {
            // 1. PNG → Sprite（单图模式、100PPU、点过滤、居中 pivot——对齐 CustomDoor 单格惯例）
            Sprite[] frames = new Sprite[FrameCount];
            for (int i = 0; i < FrameCount; i++)
            {
                string png = $"{PngRoot}/{spec.Name}/{spec.Name}_{i}.png";
                if (AssetDatabase.LoadAssetAtPath<Texture2D>(png) == null)
                {
                    Debug.LogError($"[BuildLightAssetGenerator] 缺少素材 {png}，先运行 tmp/gen_flame_sprites.py");
                    return;
                }

                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(png);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                // pivot 须经 TextureImporterSettings（TextureImporter 无直接 spritePivot/spriteAlignment 属性）
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                settings.spritePivot = new Vector2(0.5f, 0.5f);
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();

                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(png);
                if (frames[i] == null)
                {
                    Debug.LogError($"[BuildLightAssetGenerator] sprite 加载失败: {png}");
                    return;
                }
            }

            // 2. Tile（引用帧 0；Sprite collider 同 Bounty/BuildItemCreatorWindow 惯例）
            EnsureDirectory(TileDir);
            string tilePath = $"{TileDir}/{spec.Name}.asset";
            DeleteIfExists(tilePath);
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = frames[0];
            tile.colliderType = Tile.ColliderType.Sprite;
            tile.name = spec.Name;
            AssetDatabase.CreateAsset(tile, tilePath);

            // 3. AnimationClip（4 帧 PPtr 循环，6fps → 周期 2/3s）
            EnsureDirectory(AnimDir);
            string animPath = $"{AnimDir}/{spec.Name}.anim";
            DeleteIfExists(animPath);
            AnimationClip clip = new AnimationClip { frameRate = 6f };
            clip.name = spec.Name;
            var binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
            var keys = new ObjectReferenceKeyframe[FrameCount];
            for (int i = 0; i < FrameCount; i++)
            {
                keys[i] = new ObjectReferenceKeyframe { time = i / clip.frameRate, value = frames[i] };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            AnimationUtility.SetAnimationClipSettings(clip, new AnimationClipSettings { loopTime = true });
            AssetDatabase.CreateAsset(clip, animPath);

            // 4. AnimatorController（单状态名 = Name，默认状态——AnimationManager/SpriteFrameAnimator 约定）
            string controllerPath = $"{AnimDir}/{spec.Name}.controller";
            DeleteIfExists(controllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            AnimatorState state = controller.layers[0].stateMachine.AddState(spec.Name);
            state.motion = clip;
            controller.layers[0].stateMachine.defaultState = state;

            // 5. SO 条目（幂等：同名条目跳过）
            AddToDataSO(spec);

            Debug.Log($"[BuildLightAssetGenerator] {spec.Name} 完成：tile + anim/controller + SO 条目（LightRadius={spec.LightRadius}）");
        }

        private static void AddToDataSO(LightBuildSpec spec)
        {
            BuildItemDataSO targetSO = AssetDatabase.LoadAssetAtPath<BuildItemDataSO>(SoPath);
            if (targetSO == null)
            {
                Debug.LogError($"[BuildLightAssetGenerator] 找不到 {SoPath}");
                return;
            }

            SerializedObject so = new SerializedObject(targetSO);
            SerializedProperty listProp = so.FindProperty("BuildItemDatas");

            // 同名去重
            for (int i = 0; i < listProp.arraySize; i++)
            {
                if (listProp.GetArrayElementAtIndex(i).FindPropertyRelative("Name").stringValue == spec.Name)
                {
                    Debug.Log($"[BuildLightAssetGenerator] {spec.Name} 条目已存在，跳过 SO 追加");
                    return;
                }
            }

            // 跨 SO 撞名检测：Name 是 nameToId 的全局键（ItemDataManager.Awake 直接 Add，
            // 撞名即启动崩溃——如与消耗品 Torch 的撞名事故），写入前先扫全部 ItemData SO
            if (IsNameTakenByOtherSO(spec.Name, targetSO))
            {
                Debug.LogError($"[BuildLightAssetGenerator] Name={spec.Name} 已被其他 ItemData SO 占用，跳过（Name 全局唯一）");
                return;
            }

            int newIndex = listProp.arraySize;
            listProp.InsertArrayElementAtIndex(newIndex);
            SerializedProperty e = listProp.GetArrayElementAtIndex(newIndex);

            void SetStr(string field, string v) => e.FindPropertyRelative(field).stringValue = v;
            void SetBool(string field, bool v) => e.FindPropertyRelative(field).boolValue = v;
            void SetFloat(string field, float v) => e.FindPropertyRelative(field).floatValue = v;

            SetStr("CnName", spec.CnName);
            SetStr("Name", spec.Name);
            SetStr("Info", spec.Info);
            SetBool("IsStackable", false);
            SetBool("IsPass", false);          // 发光建筑不可通行（建成后阻挡）
            SetBool("IsNeedBuild", true);      // 放置后创建建造任务，Worker 参与建造
            SetBool("AutoGenerateDirections", false);
            SetBool("IsAnimation", true);      // 帧动画（Torch_0..3）
            e.FindPropertyRelative("LayerMode").intValue = (int)ItemLayerMode.Normal; // 参与 y 排序不淡化
            e.FindPropertyRelative("VisualMode").intValue = 0; // sprite 视觉（走 SyncLight 挂光分支）
            e.FindPropertyRelative("Id").intValue = (int)targetSO.ItemType * 100000 + newIndex; // 与 OnEnable 顺序分配一致（磁盘不再留 0 占位）
            e.FindPropertyRelative("Type").intValue = (int)targetSO.ItemType;

            // 建造消耗
            SerializedProperty costsProp = e.FindPropertyRelative("BuildCosts");
            costsProp.ClearArray();
            for (int i = 0; i < spec.Costs.Length; i++)
            {
                costsProp.InsertArrayElementAtIndex(i);
                SerializedProperty c = costsProp.GetArrayElementAtIndex(i);
                c.FindPropertyRelative("ItemName").stringValue = spec.Costs[i].item;
                c.FindPropertyRelative("Count").intValue = spec.Costs[i].count;
            }

            // 光照字段
            SetFloat("LightRadius", spec.LightRadius);
            SetFloat("LightIntensity", spec.LightIntensity);
            e.FindPropertyRelative("LightColor").colorValue = new Color(1f, 0.80f, 0.55f, 1f);
            SetBool("LightFlicker", true);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(targetSO);
        }

        /// <summary>检查 Name 是否被其他 ItemData SO 条目占用（nameToId 全局键唯一契约）。</summary>
        private static bool IsNameTakenByOtherSO(string name, BuildItemDataSO selfSO)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:BuildItemDataSO"))
            {
                BuildItemDataSO so = AssetDatabase.LoadAssetAtPath<BuildItemDataSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (so == null || so == selfSO || so.BuildItemDatas == null)
                {
                    continue;
                }

                foreach (BuildItemData d in so.BuildItemDatas)
                {
                    if (d.Name == name)
                    {
                        return true;
                    }
                }
            }

            foreach (string guid in AssetDatabase.FindAssets("t:ItemDataSO"))
            {
                ItemDataSO so = AssetDatabase.LoadAssetAtPath<ItemDataSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (so == null || so.ItemDatas == null)
                {
                    continue;
                }

                foreach (ItemData d in so.ItemDatas)
                {
                    if (d.Name == name)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void EnsureDirectory(string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                AssetDatabase.DeleteAsset(path);
            }
        }
    }
}
