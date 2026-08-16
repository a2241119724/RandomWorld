namespace LAB2D.Editor
{
    using LAB2D.AI.Worker;
    using LAB2D.Core;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Worker 行为决策模型可视化调试面板（Editor 专用，不进运行时构建）。
    ///
    /// 在 Play Mode 下选择任意 Worker，实时观察：
    ///   1. 状态注入 —— 拖动滑条直接改当前 Worker 的饥饿/疲劳/精气神/人格，测试模型对输入的响应；
    ///   2. 41 维特征向量 —— <see cref="WorkerFeatureExtractor"/> 的实际输出（严格对齐 feature_schema.yaml）；
    ///   3. 14 类 logits —— <see cref="WorkerModelInference"/> 的原始输出 + argmax 高亮（含 softmax 概率）；
    ///   4. A/B 对比 —— 模型 argmax vs 硬规则 <see cref="WorkerBrain.Decide"/>（UseModelDecision=false）。
    ///
    /// 注意：状态注入与硬规则对比会改动运行时真实状态（与真实决策副作用一致），仅用于调试。
    /// </summary>
    public class WorkerModelDebugWindow : EditorWindow
    {
        private const string MenuRoot = "工具/Worker 模型调试/";

        /// <summary>特征/logits 重算间隔（秒）。视野扫描较重，不做每帧重算。</summary>
        private const double FeatureRefreshInterval = 0.5;

        /// <summary>硬规则决策重算间隔（秒）。Decide 含 Random 门控与副作用，低频重算避免闪烁。</summary>
        private const double HardRuleRefreshInterval = 2.0;

        /// <summary>Worker 列表刷新间隔（秒）。</summary>
        private const double WorkerListRefreshInterval = 1.0;

        /// <summary>视野扫描半径（与 WorkerBrain.ScanRadius 一致）。</summary>
        private const int ScanRadius = 20;

        // ---- 41 维特征名（严格对齐 WorkerFeatureExtractor.Extract 的顺序）----
        private static readonly string[] FeatureNames =
        {
            "hungry_ratio", "tired_ratio", "spirit_ratio",
            "hp_ratio", "mp_ratio", "level", "exp_ratio",
            "ATN", "INT", "DEF", "RES", "CRT", "CSD", "SPD", "HIT",
            "mood", "ambition", "diligence", "sociality",
            "greed(预留)", "laziness(预留)",
            "gold",
            "bed_available",
            "life_stage:Bootstrap", "life_stage:Settled", "life_stage:Established",
            "home_build_stage:0", "home_build_stage:1", "home_build_stage:2",
            "goal:EarnMoney", "goal:BuildStructure", "goal:StockFood", "goal:CraftEquipment",
            "favorability",
            "weather_temperature", "task_pressure", "time_of_day",
            "nearby_food", "nearby_resource", "nearby_building", "nearby_worker",
        };

        /// <summary>14 类行为名（与 WorkerDecisionType 枚举顺序一致，即模型输出索引）。</summary>
        private static readonly string[] ActionNames = System.Enum.GetNames(typeof(WorkerDecisionType));

        // ---- Worker 列表 ----
        private readonly List<AWorker> workers = new List<AWorker>();
        private readonly List<string> workerNames = new List<string>();
        private int selectedIndex;

        // ---- 刷新控制 ----
        private bool autoRefresh = true;
        private double lastFeatureRefresh = double.MinValue;
        private double lastHardRuleRefresh = double.MinValue;
        private double lastWorkerListRefresh = double.MinValue;

        // ---- 缓存 ----
        private float[] features;
        private float[] logits;
        private float[] softmax;
        private int modelActionIndex = -1;
        private WorkerBrain.Decision hardDecision;
        private bool hasHardDecision;
        private Vector2 featureScroll;
        private Vector2 logitScroll;

        /// <summary>硬规则脑（UseModelDecision=false，模拟纯硬规则决策用于 A/B 对比）。</summary>
        private WorkerBrain hardBrain;

        [MenuItem(MenuRoot + "打开模型调试面板", false, 450)]
        public static void Open()
        {
            WorkerModelDebugWindow window = GetWindow<WorkerModelDebugWindow>("Worker 模型调试");
            window.minSize = new Vector2(360f, 480f);
        }

        private void OnEnable()
        {
            this.hardBrain = new WorkerBrain { UseModelDecision = false };
        }

        private void OnInspectorUpdate()
        {
            // Play Mode 下持续重绘（约 10fps），实际重算由节流控制
            if (Application.isPlaying && this.autoRefresh)
            {
                this.Repaint();
            }
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "请在 Play Mode 中使用本面板：模型推理依赖运行时服务（WorkerManager / ServiceLocator）与 Worker 数据。",
                    MessageType.Info);
                return;
            }

            this.RefreshWorkerList();

            AWorker worker = this.SelectedWorker();
            if (worker == null)
            {
                EditorGUILayout.HelpBox("场景中暂无 Worker，或 Worker 尚未生成。", MessageType.Warning);
                return;
            }

            this.DrawHeader();
            this.DrawInjection(worker);
            this.DrawDecision(worker);
            this.DrawLogits();
            this.DrawFeatures();
        }

        // ---- 头部：选择 + 刷新 + 模型状态 ----

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Worker", GUILayout.Width(48f));
            this.selectedIndex = EditorGUILayout.Popup(this.selectedIndex, this.workerNames.ToArray());

            this.autoRefresh = EditorGUILayout.Toggle("自动刷新", this.autoRefresh, GUILayout.Width(84f));
            if (GUILayout.Button("手动刷新", GUILayout.Width(72f)))
            {
                this.ForceRefresh();
            }
            EditorGUILayout.EndHorizontal();

            WorkerModelInference model = WorkerModelInference.Instance;
            string modelState = model.IsLoaded
                ? $"已加载（输入 {model.InputDim} 维 → 输出 {model.NumActions} 类）"
                : "未加载（回退硬规则）";
            EditorGUILayout.LabelField("模型", modelState);
        }

        // ---- 状态注入 ----

        private void DrawInjection(AWorker worker)
        {
            AWorker.WorkerData wd = worker.CharacterDataLAB as AWorker.WorkerData;
            if (wd == null)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("状态注入（实时改当前 Worker 状态）", EditorStyles.boldLabel);

            wd.CurHungry = EditorGUILayout.Slider("饥饿", wd.CurHungry, 0f, 100f);
            wd.CurTired = EditorGUILayout.Slider("疲劳", wd.CurTired, 0f, 100f);
            wd.CurSpirit = EditorGUILayout.Slider("精气神", wd.CurSpirit, 0f, 100f);

            // WorkerPersonality 是 struct，需整体读写（直接改字段只作用于临时副本）
            var p = wd.Personality;
            p.Mood = EditorGUILayout.Slider("心情", p.Mood, 0f, 100f);
            p.Ambition = EditorGUILayout.Slider("事业心", p.Ambition, 0f, 100f);
            p.Diligence = EditorGUILayout.Slider("勤奋", p.Diligence, 0f, 100f);
            p.Sociality = EditorGUILayout.Slider("社交", p.Sociality, 0f, 100f);
            wd.Personality = p;
        }

        // ---- 决策对比 ----

        private void DrawDecision(AWorker worker)
        {
            this.RefreshFeatures(worker);
            this.RefreshHardRule(worker);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("决策对比", EditorStyles.boldLabel);

            string modelText = this.modelActionIndex >= 0 && this.modelActionIndex < ActionNames.Length
                ? ActionNames[this.modelActionIndex]
                : "—";

            string hardText = this.hasHardDecision
                ? $"{this.hardDecision.Type}（{this.hardDecision.Description}）"
                : "—";

            EditorGUILayout.LabelField("模型 argmax", modelText);
            EditorGUILayout.LabelField("硬规则", hardText);

            if (!this.hasHardDecision)
            {
                return;
            }

            bool same = this.modelActionIndex >= 0
                && (WorkerDecisionType)this.modelActionIndex == this.hardDecision.Type;
            bool modelWouldFallback = this.modelActionIndex >= 0
                && IsSurvivalType((WorkerDecisionType)this.modelActionIndex);

            if (same)
            {
                GUI.color = Color.green;
                EditorGUILayout.LabelField("✅ 一致");
            }
            else
            {
                GUI.color = Color.yellow;
                string suffix = modelWouldFallback ? "（模型输出生存类，实际运行回退硬规则）" : string.Empty;
                EditorGUILayout.LabelField("⚠️ 分歧" + suffix);
            }
            GUI.color = Color.white;
        }

        // ---- 模型输出 logits ----

        private void DrawLogits()
        {
            if (this.logits == null || this.logits.Length == 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("模型未加载，无法显示 logits。", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("模型输出 logits（14 类，argmax 高亮）", EditorStyles.boldLabel);

            this.logitScroll = EditorGUILayout.BeginScrollView(this.logitScroll, GUILayout.Height(200f));
            for (int i = 0; i < this.logits.Length; i++)
            {
                bool isArgmax = i == this.modelActionIndex;
                string label = isArgmax ? $"▶ {ActionNames[i]}" : $"    {ActionNames[i]}";
                string value = this.logits[i].ToString("F3");
                if (this.softmax != null && i < this.softmax.Length)
                {
                    value += $"  ({this.softmax[i] * 100f:F1}%)";
                }

                GUI.color = isArgmax ? Color.green : Color.white;
                EditorGUILayout.LabelField(label, value);
            }
            GUI.color = Color.white;
            EditorGUILayout.EndScrollView();
        }

        // ---- 特征向量 ----

        private void DrawFeatures()
        {
            if (this.features == null)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"特征向量（{WorkerFeatureExtractor.FeatureDim} 维）", EditorStyles.boldLabel);

            this.featureScroll = EditorGUILayout.BeginScrollView(this.featureScroll, GUILayout.Height(220f));
            for (int i = 0; i < this.features.Length; i++)
            {
                EditorGUILayout.LabelField($"[{i:D2}] {FeatureNames[i]}", this.features[i].ToString("F3"));
            }
            EditorGUILayout.EndScrollView();
        }

        // ---- 刷新逻辑 ----

        private void RefreshWorkerList()
        {
            double now = EditorApplication.timeSinceStartup;
            if (now - this.lastWorkerListRefresh < WorkerListRefreshInterval && this.workers.Count > 0)
            {
                return;
            }
            this.lastWorkerListRefresh = now;

            // ServiceLocator.Get 未注册会抛异常，改用 TryGet 安全获取（Play Mode 早期可能未就绪）
            if (!ServiceLocator.TryGet<WorkerManager>(out WorkerManager manager) || manager.Characters == null)
            {
                this.workers.Clear();
                this.workerNames.Clear();
                return;
            }

            this.workers.Clear();
            this.workerNames.Clear();

            foreach (AWorker w in manager.Characters)
            {
                if (w == null)
                {
                    continue;
                }
                this.workers.Add(w);
                this.workerNames.Add(w.name);
            }

            if (this.selectedIndex >= this.workers.Count)
            {
                this.selectedIndex = this.workers.Count > 0 ? 0 : -1;
            }
        }

        private void RefreshFeatures(AWorker worker)
        {
            double now = EditorApplication.timeSinceStartup;
            if (now - this.lastFeatureRefresh < FeatureRefreshInterval)
            {
                return;
            }
            this.lastFeatureRefresh = now;

            this.features = WorkerFeatureExtractor.Extract(worker, ScanRadius);

            WorkerModelInference model = WorkerModelInference.Instance;
            if (model.IsLoaded)
            {
                this.logits = model.PredictLogits(this.features);
                this.modelActionIndex = model.PredictActionIndex(this.features);
                this.softmax = Softmax(this.logits);
            }
            else
            {
                this.logits = null;
                this.softmax = null;
                this.modelActionIndex = -1;
            }
        }

        private void RefreshHardRule(AWorker worker)
        {
            double now = EditorApplication.timeSinceStartup;
            if (now - this.lastHardRuleRefresh < HardRuleRefreshInterval)
            {
                return;
            }
            this.lastHardRuleRefresh = now;

            // 副作用与真实决策一致：RefreshGoal / 阶段升级 / Random 概率门控
            this.hardDecision = this.hardBrain.Decide(worker);
            this.hasHardDecision = true;
        }

        private void ForceRefresh()
        {
            this.lastFeatureRefresh = double.MinValue;
            this.lastHardRuleRefresh = double.MinValue;
            this.lastWorkerListRefresh = double.MinValue;
            this.Repaint();
        }

        private AWorker SelectedWorker()
        {
            if (this.selectedIndex < 0 || this.selectedIndex >= this.workers.Count)
            {
                return null;
            }
            return this.workers[this.selectedIndex];
        }

        private static bool IsSurvivalType(WorkerDecisionType type)
        {
            return type == WorkerDecisionType.Eat
                || type == WorkerDecisionType.Sleep
                || type == WorkerDecisionType.GroundSleep
                || type == WorkerDecisionType.Withdraw;
        }

        /// <summary>softmax（数值稳定的概率归一化），用于直观显示模型信心程度。</summary>
        private static float[] Softmax(float[] logits)
        {
            if (logits == null || logits.Length == 0)
            {
                return null;
            }

            float max = float.NegativeInfinity;
            foreach (float v in logits)
            {
                if (v > max)
                {
                    max = v;
                }
            }

            float[] probs = new float[logits.Length];
            float sum = 0f;
            for (int i = 0; i < logits.Length; i++)
            {
                probs[i] = Mathf.Exp(logits[i] - max);
                sum += probs[i];
            }
            for (int i = 0; i < probs.Length; i++)
            {
                probs[i] /= sum;
            }
            return probs;
        }
    }
}
