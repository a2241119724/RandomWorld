namespace LAB2D.Editor
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// 序列帧动画生成器：一键生成帧动画 AnimationClip，支持两种来源模式。
    ///
    /// 单贴图切片模式：
    /// 1. 在 Sprite Editor 中把序列帧图切割为多个切片（每切片 = 一帧）
    /// 2. 打开本窗口：工具/动画/序列帧动画生成器
    /// 3. 拖入贴图 → 自动列出所有切片并预览，点击缩略图选中/取消参与动画的切片
    /// 4. 配置动画名/帧率/循环 → 点击"生成动画"
    ///
    /// 多文件序列模式：Project 窗口多选（也可右键 → 生成序列帧动画），支持两类来源——
    /// 已切割贴图（取其全部切片，如 Run_0…Run_7）或独立帧图 PNG（&lt;动画名&gt;_m-&lt;帧号&gt;_n），
    /// 同前缀自动归组，按"总时间"均分关键帧，各组一键生成 .anim。
    ///
    /// 生成的 .anim 按帧序绑定 SpriteRenderer.m_Sprite，可直接拖进 Animator 播放。
    /// 重新生成会覆盖同名 .anim，便于切片调整后一键重跑。
    /// </summary>
    public class SpriteSheetAnimationGeneratorWindow : EditorWindow
    {
        private const string MenuPath = "工具/动画/序列帧动画生成器";
        private const float DefaultFrameRate = 6f; // 对齐 SpriteFrameAnimator 的默认节奏（帧/秒）
        private const int SampleRate = 60; // AnimationClip 采样率，仅影响编辑器显示；关键帧时间按配置帧率铺开
        private const float DefaultTotalDuration = 1f; // 多文件模式默认总时间（秒）
        private const string TotalDurationPrefKey = "SpriteAnimGen.TotalDuration"; // 跨窗口/右键菜单记住的总时间
        private static readonly string[] ModeLabels = { "单贴图切片", "批量（切片/单图）" };
        private static readonly Regex MultiFilePattern = new Regex(@"^(?<name>.+)_m-(?<idx>\d+)_n$", RegexOptions.Compiled);

        private SourceMode mode = SourceMode.SingleSheet;

        private Texture2D sourceTexture;
        private List<Sprite> sprites = new List<Sprite>();
        private bool spritesLoaded;
        private HashSet<int> selectedSlices = new HashSet<int>(); // 参与生成的切片索引（预览区点击切换，换贴图重置为全选）

        private string animationName = string.Empty;
        private float frameRate = DefaultFrameRate;
        private bool loop = true;
        private string outputDir = string.Empty; // 空 = 源图同目录

        private Vector2 scrollPos;
        private bool showPreview = true;

        // 多文件序列模式状态
        private List<Texture2D> multiTextures = new List<Texture2D>();
        private List<FrameGroup> multiGroups = new List<FrameGroup>();
        private List<string> unmatchedNames = new List<string>();
        private float totalDuration = DefaultTotalDuration;
        private Vector2 multiScrollPos;
        private bool showMultiPreview = true;

        /// <summary>
        /// 帧来源模式。
        /// </summary>
        private enum SourceMode
        {
            SingleSheet, // 单张贴图 Multiple 切片，按帧率生成
            MultiFiles, // 批量：多张贴图（切片或独立帧图）按前缀归组，按总时间生成
        }

        [MenuItem(MenuPath)]
        public static void ShowWindow()
        {
            SpriteSheetAnimationGeneratorWindow window = GetWindow<SpriteSheetAnimationGeneratorWindow>("序列帧动画生成器");
            window.minSize = new Vector2(420, 420);
            window.Show();
        }

        private void OnEnable()
        {
            this.totalDuration = EditorPrefs.GetFloat(TotalDurationPrefKey, DefaultTotalDuration);
        }

        private void OnSelectionChange()
        {
            if (this.mode != SourceMode.MultiFiles)
            {
                return;
            }

            this.CaptureMultiSelection();
            this.Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("序列帧动画生成器", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUI.BeginChangeCheck();
            this.mode = (SourceMode)GUILayout.Toolbar((int)this.mode, ModeLabels);
            if (EditorGUI.EndChangeCheck() && this.mode == SourceMode.MultiFiles)
            {
                this.CaptureMultiSelection(); // 切入多文件模式时捕获当前 Project 选中
            }

            EditorGUILayout.Space(5);

            if (this.mode == SourceMode.SingleSheet)
            {
                EditorGUILayout.HelpBox(
                    "从已切割的 Sprite Sheet 一键生成帧动画 .anim。\n" +
                    "步骤：拖入贴图 → 点击切片选中/取消 → 配置 → 生成。重新生成会覆盖同名 .anim。",
                    MessageType.Info);
                EditorGUILayout.Space(5);

                this.DrawTextureSection();
                EditorGUILayout.Space(5);
                this.DrawConfigSection();
                EditorGUILayout.Space(10);
                this.DrawGenerateButton();
            }
            else
            {
                this.DrawMultiFileSection();
            }
        }

        /// <summary>
        /// 贴图选择 + 切片选择预览区域（点击缩略图切换选中，仅选中切片参与生成）。
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
                    $"已检测到 {this.sprites.Count} 个切片（已选 {this.selectedSlices.Count}）", true);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("全选"))
                {
                    this.selectedSlices = new HashSet<int>(Enumerable.Range(0, this.sprites.Count));
                }

                if (GUILayout.Button("全不选"))
                {
                    this.selectedSlices.Clear();
                }

                if (GUILayout.Button("反选"))
                {
                    this.selectedSlices = new HashSet<int>(Enumerable
                        .Range(0, this.sprites.Count)
                        .Where(i => !this.selectedSlices.Contains(i)));
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.HelpBox("点击切片缩略图选中/取消，仅选中的切片参与生成。", MessageType.None);

                if (this.showPreview)
                {
                    this.scrollPos = EditorGUILayout.BeginScrollView(this.scrollPos, GUILayout.Height(120));
                    EditorGUILayout.BeginHorizontal();

                    for (int i = 0; i < this.sprites.Count; i++)
                    {
                        Sprite sprite = this.sprites[i];
                        if (sprite == null)
                        {
                            continue;
                        }

                        bool selected = this.selectedSlices.Contains(i);
                        EditorGUILayout.BeginVertical(GUILayout.Width(56));
                        Rect rect = GUILayoutUtility.GetRect(48, 48, GUILayout.Width(48), GUILayout.Height(48));

                        Event evt = Event.current;
                        if (evt.type == EventType.MouseDown && evt.button == 0 && rect.Contains(evt.mousePosition))
                        {
                            if (!this.selectedSlices.Remove(i))
                            {
                                this.selectedSlices.Add(i);
                            }

                            evt.Use();
                        }

                        Color cachedColor = GUI.color;
                        GUI.color = new Color(1f, 1f, 1f, selected ? 1f : 0.25f);
                        GUI.DrawTextureWithTexCoords(rect, sprite.texture, new Rect(
                            sprite.rect.x / sprite.texture.width,
                            sprite.rect.y / sprite.texture.height,
                            sprite.rect.width / sprite.texture.width,
                            sprite.rect.height / sprite.texture.height));
                        GUI.color = cachedColor;

                        if (selected)
                        {
                            DrawSelectionBorder(rect);
                        }

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

            // 总时间与帧率双向联动（同一节奏的两种表达）：改总时间即反算帧率（按选中切片数计）
            int frameCount = this.selectedSlices.Count;
            float duration = frameCount / this.frameRate;
            float newDuration = Mathf.Max(0.01f, EditorGUILayout.FloatField("总时间（秒）", duration));
            if (!Mathf.Approximately(newDuration, duration))
            {
                this.frameRate = frameCount / newDuration;
            }

            if (frameCount > 0)
            {
                EditorGUILayout.LabelField(
                    $"{frameCount} 帧 × {1f / this.frameRate:F3} 秒 = {duration:F2} 秒", EditorStyles.miniLabel);
            }

            this.loop = EditorGUILayout.Toggle("循环播放", this.loop);
            this.DrawOutputDirField();
        }

        /// <summary>
        /// 输出目录选择区域（两种模式共用）。
        /// </summary>
        private void DrawOutputDirField()
        {
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
                EditorGUILayout.HelpBox($"留空则输出到源图同目录：{this.GetDefaultOutputDir()}", MessageType.None);
            }
        }

        /// <summary>
        /// 生成按钮 + 缺失提示（单贴图切片模式）。
        /// </summary>
        private void DrawGenerateButton()
        {
            bool canGenerate = this.spritesLoaded && this.selectedSlices.Count > 0 && !string.IsNullOrEmpty(this.animationName);
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
                else if (this.selectedSlices.Count == 0)
                {
                    missing.Add("至少选中一个切片");
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
            this.selectedSlices.Clear();

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
            this.selectedSlices = new HashSet<int>(Enumerable.Range(0, this.sprites.Count));

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
        /// 默认输出目录：源图同目录（单贴图取源贴图，多文件取第一张帧图）。
        /// </summary>
        private string GetDefaultOutputDir()
        {
            Object source = this.mode == SourceMode.MultiFiles && this.multiTextures.Count > 0
                ? (Object)this.multiTextures[0]
                : (Object)this.sourceTexture;
            if (source == null)
            {
                return "Assets";
            }

            string assetPath = AssetDatabase.GetAssetPath(source);
            string dir = Path.GetDirectoryName(assetPath);
            return string.IsNullOrEmpty(dir) ? "Assets" : dir.Replace('\\', '/');
        }

        /// <summary>
        /// 多文件模式：选择帧图（Project 多选自动捕获）+ 分组预览。
        /// </summary>
        private void DrawMultiFileSection()
        {
            EditorGUILayout.LabelField("1. 选择贴图（Project 窗口可多选）", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "选中已切割的贴图（取其全部切片，如 Run_0…Run_7）或独立帧图 PNG（<动画名>_m-<帧号>_n）。\n" +
                "选中变化时自动捕获；按帧前缀归组、按帧号排序，各组各生成一个 .anim。",
                MessageType.Info);

            if (GUILayout.Button("捕获 Project 当前选中贴图", GUILayout.Height(24)))
            {
                this.CaptureMultiSelection();
            }

            if (this.multiGroups.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    this.multiTextures.Count > 0
                        ? $"捕获到 {this.multiTextures.Count} 张贴图，但切片命名均不匹配：<动画名>_m-<帧号>_n 或 <动画名>_<帧号>（如 Run_0）"
                        : "尚未捕获贴图：在 Project 窗口选中一张或多张贴图（已切割或独立帧图）。",
                    MessageType.Warning);
            }
            else
            {
                int totalFrames = this.multiGroups.Sum(g => g.Frames.Count);
                this.showMultiPreview = EditorGUILayout.Foldout(this.showMultiPreview,
                    $"已捕获 {this.multiGroups.Count} 组 / 共 {totalFrames} 帧", true);
                if (this.showMultiPreview)
                {
                    this.multiScrollPos = EditorGUILayout.BeginScrollView(this.multiScrollPos, GUILayout.Height(150));
                    foreach (FrameGroup group in this.multiGroups)
                    {
                        EditorGUILayout.LabelField($"{group.Name}  ({group.Frames.Count} 帧)", EditorStyles.boldLabel);
                        EditorGUILayout.BeginHorizontal();
                        foreach (FrameItem item in group.Frames)
                        {
                            EditorGUILayout.BeginVertical(GUILayout.Width(56));
                            Rect rect = GUILayoutUtility.GetRect(48, 48, GUILayout.Width(48), GUILayout.Height(48));
                            if (item.Sprite != null)
                            {
                                GUI.DrawTextureWithTexCoords(rect, item.Sprite.texture, new Rect(
                                    item.Sprite.rect.x / item.Sprite.texture.width,
                                    item.Sprite.rect.y / item.Sprite.texture.height,
                                    item.Sprite.rect.width / item.Sprite.texture.width,
                                    item.Sprite.rect.height / item.Sprite.texture.height));
                            }

                            GUILayout.Label(item.Name, EditorStyles.miniLabel, GUILayout.Width(54));
                            EditorGUILayout.EndVertical();
                        }

                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.EndScrollView();
                }
            }

            if (this.unmatchedNames.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    $"忽略不匹配命名的文件：{string.Join("、", this.unmatchedNames)}", MessageType.Warning);
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("2. 动画配置", EditorStyles.boldLabel);

            this.totalDuration = Mathf.Max(0.01f, EditorGUILayout.FloatField("总时间（秒）", this.totalDuration));
            if (this.multiGroups.Count > 0)
            {
                int totalFrames = this.multiGroups.Sum(g => g.Frames.Count);
                EditorGUILayout.LabelField($"等效帧率：{totalFrames / this.totalDuration:F2} 帧/秒", EditorStyles.miniLabel);
            }

            this.loop = EditorGUILayout.Toggle("循环播放", this.loop);
            this.DrawOutputDirField();

            EditorGUILayout.Space(10);
            bool canGenerate = this.multiGroups.Count > 0;
            EditorGUI.BeginDisabledGroup(!canGenerate);
            if (GUILayout.Button($"🚀 生成帧动画 .anim（{this.multiGroups.Count} 组）", GUILayout.Height(40)))
            {
                this.GenerateMultiFileAnimations();
            }

            EditorGUI.EndDisabledGroup();
        }

        /// <summary>
        /// 从 Project 当前选中捕获贴图（多文件模式）。选择不含贴图时不打断已捕获内容。
        /// </summary>
        private void CaptureMultiSelection()
        {
            List<Object> selected = Selection.objects.ToList();
            List<Texture2D> picked = selected
                .OfType<Texture2D>()
                .Where(t => !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(t)))
                .ToList();
            Debug.Log($"[SpriteAnimGen] 捕获：当前选中 {selected.Count} 个对象，其中项目内贴图 {picked.Count} 个" +
                (picked.Count == 0 && selected.Count > 0
                    ? $"（类型：{string.Join("、", selected.Select(o => o == null ? "null" : o.GetType().Name).Distinct())}）"
                    : string.Empty));
            if (picked.Count == 0)
            {
                return;
            }

            this.multiTextures = picked;
            this.multiGroups.Clear();
            this.unmatchedNames.Clear();
            BuildGroups(picked, this.multiGroups, this.unmatchedNames);
            Debug.Log($"[SpriteAnimGen] 解析：{this.multiGroups.Count} 组（" +
                string.Join("、", this.multiGroups.Select(g => $"{g.Name}×{g.Frames.Count}帧")) +
                (this.unmatchedNames.Count > 0
                    ? $"），忽略：{string.Join("、", this.unmatchedNames)}"
                    : "）"));
        }

        /// <summary>
        /// 拆分帧图资产名：优先匹配 &lt;动画名&gt;_m-&lt;帧号&gt;_n，回退 &lt;动画名&gt;_&lt;帧号&gt;（与切片命名一致）。
        /// </summary>
        /// <returns>是否为可识别的帧图命名。</returns>
        private static bool TrySplitFrameName(string assetName, out string groupName, out int frameIndex)
        {
            Match match = MultiFilePattern.Match(assetName);
            if (match.Success)
            {
                groupName = match.Groups["name"].Value;
                return int.TryParse(match.Groups["idx"].Value, out frameIndex);
            }

            int lastUnderscore = assetName.LastIndexOf('_');
            if (lastUnderscore > 0 && int.TryParse(assetName.Substring(lastUnderscore + 1), out frameIndex))
            {
                groupName = assetName.Substring(0, lastUnderscore);
                return true;
            }

            groupName = null;
            frameIndex = 0;
            return false;
        }

        /// <summary>
        /// 加载贴图上的全部切片 Sprite，按自然排序（Multiple 多切片 / Single 单切片均适用）。
        /// </summary>
        private static List<Sprite> LoadSprites(string assetPath)
        {
            return AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<Sprite>()
                .OrderBy(s => s.name, SpriteNameNaturalSorter.Instance)
                .ToList();
        }

        /// <summary>
        /// 多文件模式生成入口：配置写入 EditorPrefs（供右键菜单复用），批量生成各组动画。
        /// </summary>
        private void GenerateMultiFileAnimations()
        {
            string dir = string.IsNullOrEmpty(this.outputDir)
                ? this.GetDefaultOutputDir()
                : this.outputDir.TrimEnd('/');
            float duration = Mathf.Max(0.01f, this.totalDuration);
            EditorPrefs.SetFloat(TotalDurationPrefKey, duration);

            List<AnimationClip> created = GenerateGroups(dir, this.multiGroups, duration, this.loop);
            if (created.Count == 0)
            {
                return;
            }

            AnimationClip last = created[created.Count - 1];
            Selection.activeObject = last;
            EditorGUIUtility.PingObject(last);
            Debug.Log($"[SpriteAnimGen] 生成完成：{created.Count} 个动画 → {dir}（总时间 {duration:F2}s / " +
                $"{(this.loop ? "循环" : "不循环")}），已选中：{last.name}");
        }

        /// <summary>
        /// 批量生成：每组一个 .anim，帧在总时间内均匀铺开（帧 i 位于 i × 总时间 / 帧数）。
        /// 循环时动画周期 = 总时间（末帧停留一帧间隔后无缝回绕）。
        /// </summary>
        private static List<AnimationClip> GenerateGroups(string dir, List<FrameGroup> groups, float totalDuration, bool loop)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            List<AnimationClip> created = new List<AnimationClip>();
            foreach (FrameGroup group in groups)
            {
                int frameCount = group.Frames.Count;
                float step = totalDuration / Mathf.Max(1, frameCount); // 每帧间隔 = 总时间 / 帧数
                ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frameCount];
                for (int i = 0; i < frameCount; i++)
                {
                    keys[i] = new ObjectReferenceKeyframe
                    {
                        time = i * step,
                        value = group.Frames[i].Sprite,
                    };
                }

                AnimationClip clip = CreateClipAsset(dir, group.Name, keys, loop, loop ? totalDuration : (float?)null);
                if (clip != null)
                {
                    created.Add(clip);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return created;
        }

        /// <summary>
        /// 写盘生成 .anim 资产（已存在同名则覆盖删除），返回创建后的资产；失败返回 null。
        /// </summary>
        private static AnimationClip CreateClipAsset(string dir, string clipName, ObjectReferenceKeyframe[] keys, bool loop, float? stopTime = null)
        {
            string assetPath = $"{dir}/{clipName}.anim";

            // 覆盖已存在的同名动画：先删除旧资产再创建，保证帧调整后一键重跑
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath) != null)
            {
                if (!AssetDatabase.DeleteAsset(assetPath))
                {
                    Debug.LogWarning($"[SpriteAnimGen] 删除旧动画失败，已中止：{assetPath}");
                    return null;
                }
            }

            AnimationClip clip = new AnimationClip();
            clip.name = clipName;
            clip.frameRate = SampleRate;

            EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            settings.stopTime = stopTime ?? (keys.Length > 0 ? keys[keys.Length - 1].time : 0f);
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            AssetDatabase.CreateAsset(clip, assetPath);
            return clip;
        }

        /// <summary>
        /// 一组同前缀的帧序列。
        /// </summary>
        private class FrameGroup
        {
            public string Name;
            public List<FrameItem> Frames = new List<FrameItem>();
        }

        /// <summary>
        /// 单帧：切片 Sprite + 帧号。
        /// </summary>
        private class FrameItem
        {
            public Sprite Sprite;
            public string Name;
            public int Index;
        }

        /// <summary>
        /// Project 右键一键生成：选中帧图按命名分组，各组生成 .anim（总时间取窗口上次设置）。
        /// </summary>
        [MenuItem("Assets/生成序列帧动画")]
        private static void GenerateFromSelectionMenu()
        {
            List<Texture2D> textures = Selection.objects
                .OfType<Texture2D>()
                .Where(t => !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(t)))
                .ToList();
            List<FrameGroup> groups = new List<FrameGroup>();
            List<string> unmatched = new List<string>();
            BuildGroups(textures, groups, unmatched);
            if (groups.Count == 0)
            {
                Debug.LogWarning("[SpriteAnimGen] 选中内容中没有可识别的帧图命名（<动画名>_m-<帧号>_n 或 <动画名>_<帧号>）：" +
                    string.Join("、", unmatched));
                return;
            }

            string dir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(textures[0]))?.Replace('\\', '/');
            float duration = Mathf.Max(0.01f, EditorPrefs.GetFloat(TotalDurationPrefKey, DefaultTotalDuration));
            List<AnimationClip> created = GenerateGroups(dir, groups, duration, loop: true);
            if (created.Count > 0)
            {
                EditorGUIUtility.PingObject(created[created.Count - 1]);
                Debug.Log($"[SpriteAnimGen] 生成完成：{created.Count} 个动画 → {dir}（总时间 {duration:F2}s / 循环），已 ping");
            }
        }

        /// <summary>
        /// 右键菜单校验：选中内容含项目内贴图时可用。
        /// </summary>
        [MenuItem("Assets/生成序列帧动画", true)]
        private static bool ValidateGenerateFromSelectionMenu()
        {
            return Selection.objects.OfType<Texture2D>()
                .Any(t => !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(t)));
        }

        /// <summary>
        /// 按命名解析并分组（静态版，供窗口与右键菜单共用）：遍历每张贴图的全部切片，
        /// 按 &lt;动画名&gt;_m-&lt;帧号&gt;_n 或 &lt;动画名&gt;_&lt;帧号&gt;（如 Run_0…Run_7）拆前缀归组，组内按帧号升序。
        /// </summary>
        private static void BuildGroups(List<Texture2D> textures, List<FrameGroup> groups, List<string> unmatched)
        {
            Dictionary<string, FrameGroup> dict = new Dictionary<string, FrameGroup>();
            foreach (Texture2D texture in textures)
            {
                string assetPath = AssetDatabase.GetAssetPath(texture);
                if (string.IsNullOrEmpty(assetPath))
                {
                    continue;
                }

                foreach (Sprite sprite in LoadSprites(assetPath))
                {
                    if (!TrySplitFrameName(sprite.name, out string groupName, out int frameIndex))
                    {
                        unmatched.Add(sprite.name);
                        continue;
                    }

                    if (!dict.TryGetValue(groupName, out FrameGroup group))
                    {
                        group = new FrameGroup { Name = groupName };
                        dict[groupName] = group;
                    }

                    group.Frames.Add(new FrameItem { Sprite = sprite, Name = sprite.name, Index = frameIndex });
                }
            }

            groups.AddRange(dict.Values.OrderBy(g => g.Name, System.StringComparer.Ordinal));
            foreach (FrameGroup group in groups)
            {
                group.Frames.Sort((a, b) => a.Index.CompareTo(b.Index));
            }
        }

        /// <summary>
        /// 生成帧动画：选中切片按原有顺序各一帧，绑定 SpriteRenderer.m_Sprite。
        /// </summary>
        private void GenerateAnimation()
        {
            List<Sprite> frames = new List<Sprite>();
            for (int i = 0; i < this.sprites.Count; i++)
            {
                if (this.sprites[i] != null && this.selectedSlices.Contains(i))
                {
                    frames.Add(this.sprites[i]);
                }
            }

            string dir = string.IsNullOrEmpty(this.outputDir)
                ? this.GetDefaultOutputDir()
                : this.outputDir.TrimEnd('/');

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frames.Count];
            float fps = Mathf.Max(0.01f, this.frameRate);
            for (int i = 0; i < frames.Count; i++)
            {
                keys[i] = new ObjectReferenceKeyframe
                {
                    time = i / fps, // 关键帧时间以秒计：每帧间隔 1/fps 秒
                    value = frames[i],
                };
            }

            AnimationClip created = CreateClipAsset(dir, this.animationName, keys, this.loop);
            if (created == null)
            {
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = created;
            EditorGUIUtility.PingObject(created);

            Debug.Log($"[SpriteAnimGen] 生成完成：{dir}/{this.animationName}.anim\n" +
                $"  {frames.Count} 帧（切片共 {this.sprites.Count} 个）/ {fps} fps / {(this.loop ? "循环" : "不循环")}，已选中");
        }

        /// <summary>
        /// 给选中的切片缩略图描高亮边框。
        /// </summary>
        private static void DrawSelectionBorder(Rect rect)
        {
            Color color = new Color(0.24f, 0.49f, 0.9f);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 2f, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - 2f, rect.y, 2f, rect.height), color);
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
