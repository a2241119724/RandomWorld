namespace LAB2D.Editor
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// 序列帧动画生成器：把已切割的 Sprite Sheet（Multiple 模式）一键生成帧动画 AnimationClip。
    ///
    /// 使用流程：
    /// 1. 在 Sprite Editor 中把序列帧图切割为多个切片（每切片 = 一帧）
    /// 2. 打开本窗口：工具/动画/序列帧动画生成器
    /// 3. 拖入贴图 → 自动列出所有切片并预览
    /// 4. 配置动画名/帧率/循环 → 点击"生成动画"
    ///
    /// 生成的 .anim 按切片顺序绑定 SpriteRenderer.m_Sprite，可直接拖进 Animator 播放。
    /// 重新生成会覆盖同名 .anim，便于切片调整后一键重跑。
    /// </summary>
    public class SpriteSheetAnimationGeneratorWindow : EditorWindow
    {
        private const string MenuPath = "工具/动画/序列帧动画生成器";
        private const float DefaultFrameRate = 6f; // 对齐 SpriteFrameAnimator 的默认节奏（帧/秒）
        private const int SampleRate = 60; // AnimationClip 采样率，仅影响编辑器显示；关键帧时间按配置帧率铺开

        private Texture2D sourceTexture;
        private List<Sprite> sprites = new List<Sprite>();
        private bool spritesLoaded;

        private string animationName = string.Empty;
        private float frameRate = DefaultFrameRate;
        private bool loop = true;
        private string outputDir = string.Empty; // 空 = 贴图同目录

        private Vector2 scrollPos;
        private bool showPreview = true;

        [MenuItem(MenuPath)]
        public static void ShowWindow()
        {
            SpriteSheetAnimationGeneratorWindow window = GetWindow<SpriteSheetAnimationGeneratorWindow>("序列帧动画生成器");
            window.minSize = new Vector2(420, 420);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("序列帧动画生成器", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "从已切割的 Sprite Sheet 一键生成帧动画 .anim。\n" +
                "步骤：拖入贴图 → 配置 → 生成。重新生成会覆盖同名 .anim。",
                MessageType.Info);
            EditorGUILayout.Space(5);

            this.DrawTextureSection();
            EditorGUILayout.Space(5);
            this.DrawConfigSection();
            EditorGUILayout.Space(10);
            this.DrawGenerateButton();
        }

        /// <summary>
        /// 贴图选择 + 切片预览区域
        /// </summary>
        private void DrawTextureSection()
        {
            EditorGUILayout.LabelField("1. 选择贴图（已切割 Multiple Sprite）", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            this.sourceTexture = (Texture2D)EditorGUILayout.ObjectField(
                "源贴图",
                this.sourceTexture,
                typeof(Texture2D),
                false);
            if (EditorGUI.EndChangeCheck())
            {
                this.LoadSpritesFromTexture();
            }

            if (this.spritesLoaded && this.sprites.Count > 0)
            {
                this.showPreview = EditorGUILayout.Foldout(this.showPreview,
                    $"已检测到 {this.sprites.Count} 个切片", true);

                if (this.showPreview)
                {
                    this.scrollPos = EditorGUILayout.BeginScrollView(this.scrollPos, GUILayout.Height(120));
                    EditorGUILayout.BeginHorizontal();

                    foreach (Sprite sprite in this.sprites)
                    {
                        if (sprite == null)
                        {
                            continue;
                        }

                        EditorGUILayout.BeginVertical(GUILayout.Width(56));
                        Rect rect = GUILayoutUtility.GetRect(48, 48, GUILayout.Width(48), GUILayout.Height(48));
                        GUI.DrawTextureWithTexCoords(rect, sprite.texture, new Rect(
                            sprite.rect.x / sprite.texture.width,
                            sprite.rect.y / sprite.texture.height,
                            sprite.rect.width / sprite.texture.width,
                            sprite.rect.height / sprite.texture.height));
                        GUILayout.Label(sprite.name, EditorStyles.miniLabel, GUILayout.Width(54));
                        EditorGUILayout.EndVertical();
                    }

                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndScrollView();
                }
            }
            else if (this.sourceTexture != null && !this.spritesLoaded)
            {
                EditorGUILayout.HelpBox(
                    "未检测到切片。请确保贴图 Texture Type 为 Sprite (2D and UI)、" +
                    "Sprite Mode 为 Multiple，且已在 Sprite Editor 中完成切割。",
                    MessageType.Warning);
            }
        }

        /// <summary>
        /// 动画配置区域
        /// </summary>
        private void DrawConfigSection()
        {
            EditorGUILayout.LabelField("2. 动画配置", EditorStyles.boldLabel);

            this.animationName = EditorGUILayout.TextField("动画名", this.animationName);
            this.frameRate = Mathf.Max(0.01f, EditorGUILayout.FloatField("帧率（帧/秒）", this.frameRate));
            this.loop = EditorGUILayout.Toggle("循环播放", this.loop);

            EditorGUILayout.BeginHorizontal();
            this.outputDir = EditorGUILayout.TextField("输出目录", this.outputDir);
            if (GUILayout.Button("选择", GUILayout.Width(50)))
            {
                string defaultDir = this.GetDefaultOutputDir();
                string picked = EditorUtility.OpenFolderPanel("选择输出目录", defaultDir, "");
                if (!string.IsNullOrEmpty(picked))
                {
                    // 转成项目内相对路径（Assets/...）
                    this.outputDir = picked.Replace('\\', '/').Replace(Application.dataPath, "Assets");
                }
            }

            EditorGUILayout.EndHorizontal();

            if (string.IsNullOrEmpty(this.outputDir))
            {
                EditorGUILayout.HelpBox($"留空则输出到贴图同目录：{this.GetDefaultOutputDir()}", MessageType.None);
            }
        }

        /// <summary>
        /// 生成按钮 + 缺失提示
        /// </summary>
        private void DrawGenerateButton()
        {
            bool canGenerate = this.spritesLoaded && this.sprites.Count > 0 && !string.IsNullOrEmpty(this.animationName);
            EditorGUI.BeginDisabledGroup(!canGenerate);
            if (GUILayout.Button("🚀 生成帧动画 .anim", GUILayout.Height(40)))
            {
                this.GenerateAnimation();
            }

            EditorGUI.EndDisabledGroup();

            if (!canGenerate)
            {
                List<string> missing = new List<string>();
                if (!this.spritesLoaded || this.sprites.Count == 0)
                {
                    missing.Add("选择已切割的贴图");
                }

                if (string.IsNullOrEmpty(this.animationName))
                {
                    missing.Add("填写动画名");
                }

                EditorGUILayout.HelpBox($"还需要：{string.Join("、", missing)}", MessageType.Warning);
            }
        }

        /// <summary>
        /// 从贴图加载所有切片 Sprite，并按尾数字自然排序（"LightningFrame_2" 排在 "_10" 之前）。
        /// </summary>
        private void LoadSpritesFromTexture()
        {
            this.sprites.Clear();
            this.spritesLoaded = false;

            if (this.sourceTexture == null)
            {
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(this.sourceTexture);
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (asset is Sprite sprite)
                {
                    this.sprites.Add(sprite);
                }
            }

            this.sprites = this.sprites
                .OrderBy(s => s.name, SpriteNameNaturalSorter.Instance)
                .ToList();
            this.spritesLoaded = this.sprites.Count > 0;

            if (this.spritesLoaded && string.IsNullOrEmpty(this.animationName))
            {
                // 从贴图名自动推断动画名（去掉尾部 _数字）
                this.animationName = this.InferBaseName(this.sourceTexture.name);
            }
        }

        /// <summary>
        /// 从贴图名推断基础名（去掉尾部 _数字），如 "LightningFrame_0" → "LightningFrame"。
        /// </summary>
        private string InferBaseName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            int lastUnderscore = name.LastIndexOf('_');
            if (lastUnderscore > 0)
            {
                string suffix = name.Substring(lastUnderscore + 1);
                if (int.TryParse(suffix, out _))
                {
                    return name.Substring(0, lastUnderscore);
                }
            }

            return name;
        }

        /// <summary>
        /// 默认输出目录：贴图同目录。
        /// </summary>
        private string GetDefaultOutputDir()
        {
            if (this.sourceTexture == null)
            {
                return "Assets";
            }

            string assetPath = AssetDatabase.GetAssetPath(this.sourceTexture);
            string dir = Path.GetDirectoryName(assetPath);
            return string.IsNullOrEmpty(dir) ? "Assets" : dir.Replace('\\', '/');
        }

        /// <summary>
        /// 生成帧动画：每个切片按顺序一帧，绑定 SpriteRenderer.m_Sprite。
        /// </summary>
        private void GenerateAnimation()
        {
            string dir = string.IsNullOrEmpty(this.outputDir)
                ? this.GetDefaultOutputDir()
                : this.outputDir.TrimEnd('/');

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            string assetPath = $"{dir}/{this.animationName}.anim";

            // 覆盖已存在的同名动画：先删除旧资产再创建，保证切片调整后一键重跑
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath) != null)
            {
                if (!AssetDatabase.DeleteAsset(assetPath))
                {
                    Debug.LogWarning($"[SpriteAnimGen] 删除旧动画失败，已中止：{assetPath}");
                    return;
                }
            }

            AnimationClip clip = new AnimationClip();
            clip.name = this.animationName;
            clip.frameRate = SampleRate;

            ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[this.sprites.Count];
            float fps = Mathf.Max(0.01f, this.frameRate);
            for (int i = 0; i < this.sprites.Count; i++)
            {
                keys[i] = new ObjectReferenceKeyframe
                {
                    time = i / fps, // 关键帧时间以秒计：每帧间隔 1/fps 秒
                    value = this.sprites[i],
                };
            }

            EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = this.loop;
            settings.stopTime = keys.Length > 0 ? keys[keys.Length - 1].time : 0f;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            AssetDatabase.CreateAsset(clip, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            AnimationClip created = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            Selection.activeObject = created;
            EditorGUIUtility.PingObject(created);

            Debug.Log($"[SpriteAnimGen] 生成完成：{assetPath}\n" +
                $"  {this.sprites.Count} 帧 / {fps} fps / {(this.loop ? "循环" : "不循环")}，已选中");
        }
    }

    /// <summary>
    /// 按尾部数字自然排序的字符串比较器："Frame_2" 排在 "Frame_10" 之前。
    /// </summary>
    internal class SpriteNameNaturalSorter : IComparer<string>
    {
        public static readonly SpriteNameNaturalSorter Instance = new SpriteNameNaturalSorter();

        public int Compare(string x, string y)
        {
            if (x == null || y == null)
            {
                return string.Compare(x, y, System.StringComparison.Ordinal);
            }

            int i = x.Length - 1;
            int j = y.Length - 1;
            while (i >= 0 && char.IsDigit(x[i]))
            {
                i--;
            }

            while (j >= 0 && char.IsDigit(y[j]))
            {
                j--;
            }

            string prefixX = x.Substring(0, i + 1);
            string prefixY = y.Substring(0, j + 1);
            int prefixCmp = string.Compare(prefixX, prefixY, System.StringComparison.Ordinal);
            if (prefixCmp != 0)
            {
                return prefixCmp;
            }

            int numX = 0, numY = 0;
            int.TryParse(x.Substring(i + 1), out numX);
            int.TryParse(y.Substring(j + 1), out numY);
            return numX.CompareTo(numY);
        }
    }
}
