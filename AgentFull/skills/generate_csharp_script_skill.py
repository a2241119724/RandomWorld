from __future__ import annotations

import re
from collections import Counter
from pathlib import Path
from typing import Any

from core.file_utils import (
    csharp_output_dir,
    mono_script_meta_content,
    read_text,
    safe_slug,
    unique_unity_asset_path,
    unity_generation_policy,
    unity_meta_path,
    write_text,
)
from core.llm_utils import compact_json, extract_csharp_code, record_model_call
from core.project_context import build_llm_project_context
from core.skill import Skill


class GenerateCSharpScriptSkill(Skill):
    name = "generate_csharp_script"
    description = "Generate a safe Unity C# script into the configured Unity script folder."
    input_schema = {"task_card": "task card", "script_kind": "MonoBehaviour|ScriptableObject|PlainClass"}
    output_schema = {"generated_files": "written C# files"}

    def run(self, params: dict[str, Any], context: Any) -> dict[str, Any]:
        task_card = params.get("task_card") or {}
        selected = params.get("selected_candidate") or {}
        report_dir = Path(params.get("report_dir") or context.get("report_dir") or ".")
        base_dir = Path(context.get_service("base_dir") or ".")
        configs = context.get("configs", {}).get("unity", {})

        script_kind = params.get("script_kind") or self._infer_kind(selected)
        implementation_type = selected.get("implementation_type")
        namespace = params.get("namespace") or self._default_namespace(context)
        class_name = params.get("class_name") or self._class_name(selected, script_kind)
        generated_dir = csharp_output_dir(
            base_dir,
            configs,
            report_dir,
            script_kind=script_kind,
            implementation_type=implementation_type,
        )
        target = unique_unity_asset_path(generated_dir / f"{class_name}.cs")
        class_name = target.stem
        content = self._llm_build_script(
            class_name,
            namespace,
            script_kind,
            task_card,
            selected,
            context,
        ) or self._build_script(class_name, namespace, script_kind, task_card, selected)

        target = write_text(target, content, overwrite=False)
        meta_path = None
        if unity_generation_policy(configs).get("default_output_mode", "project") != "report_only":
            meta_path = write_text(unity_meta_path(target), mono_script_meta_content(), overwrite=False)
        result = {
            "generated_files": [str(target)],
            "generated_meta_files": [str(meta_path)] if meta_path else [],
            "script_kind": script_kind,
            "namespace": namespace,
            "class_name": class_name,
            "policy": "configured_unity_path_no_overwrite",
        }
        return result

    def _llm_build_script(
        self,
        class_name: str,
        namespace: str,
        script_kind: str,
        task_card: dict[str, Any],
        selected: dict[str, Any],
        context: Any,
    ) -> str | None:
        router = context.get_service("model_router")
        if not router:
            return None

        base_dir = Path(context.get_service("base_dir") or ".")
        system_prompt = read_text(base_dir / "prompts" / "code_generator_prompt.md")
        project_context = build_llm_project_context(
            context,
            "generate_csharp_script",
            selected=selected,
            task_card=task_card,
        )
        messages = [
            {
                "role": "system",
                "content": (
                    system_prompt
                    + "\n只返回一个完整 C# 文件，并放在带 csharp 语言标记的 Markdown 代码块中。"
                    + "\n生成 C# 代码中的注释必须使用中文，包括 XML summary、普通注释和说明性注释。"
                ),
            },
            {
                "role": "user",
                "content": (
                    "请生成一个小而易审查的 Unity C# 文件，用来实现这个新功能。"
                    "必须使用给定的 namespace 和 class_name。优先生成独立运行时组件，"
                    "让用户在审查后手动挂载或引用。不要修改场景、Prefab、ScriptableObject、"
                    "StreamingAssets、Addressables、存档、网络或构建设置。不要包含破坏性文件操作。\n\n"
                    "完整上下文包（包含项目结构、关键 C# 片段、会话上下文、用户输入和最近模型调用）：\n"
                    f"{project_context}\n\n"
                    f"class_name: {class_name}\n"
                    f"namespace: {namespace}\n"
                    f"script_kind: {script_kind}\n"
                    f"selected_candidate:\n{compact_json(selected, 5000)}\n\n"
                    f"task_card:\n{compact_json(task_card, 7000)}"
                ),
            },
        ]
        response = router.chat_for_task("generate_csharp_script", messages)
        code = extract_csharp_code(response.get("content", ""))
        used = bool(code and self._is_safe_generated_code(code, class_name, script_kind))
        record_model_call(
            context,
            "generate_csharp_script",
            response,
            used=used,
            note=f"class_name={class_name}",
        )
        return code if used else None

    def _is_safe_generated_code(self, code: str, class_name: str, script_kind: str) -> bool:
        if class_name not in code or "```" in code:
            return False
        forbidden = [
            "AssetDatabase.DeleteAsset",
            "AssetDatabase.MoveAsset",
            "AssetDatabase.CreateAsset",
            "AssetDatabase.SaveAssets",
            "PrefabUtility.",
            "EditorSceneManager.",
            "File.Delete",
            "Directory.Delete",
            "Process.Start",
        ]
        if any(pattern in code for pattern in forbidden):
            return False
        if script_kind == "MonoBehaviour":
            return (
                "MonoBehaviour" in code
                and "using UnityEngine" in code
                and self._comments_are_chinese(code)
            )
        if script_kind == "ScriptableObject":
            return (
                "ScriptableObject" in code
                and "using UnityEngine" in code
                and self._comments_are_chinese(code)
            )
        return "class " in code and self._comments_are_chinese(code)

    def _comments_are_chinese(self, code: str) -> bool:
        line_comments = re.findall(r"^\s*//+.*$", code, flags=re.MULTILINE)
        block_comments = re.findall(r"/\*.*?\*/", code, flags=re.DOTALL)
        for comment in line_comments + block_comments:
            text = re.sub(r"</?\w+[^>]*>", "", comment)
            text = re.sub(r"[/\*]+", " ", text).strip()
            if not text:
                continue
            if re.search(r"[A-Za-z]{4,}", text) and not re.search(r"[\u4e00-\u9fff]", text):
                return False
        return True

    def _infer_kind(self, selected: dict[str, Any]) -> str:
        if selected.get("implementation_type") == "runtime_feature":
            return "MonoBehaviour"
        return "PlainClass"

    def _class_name(self, selected: dict[str, Any], script_kind: str) -> str:
        if selected.get("suggested_class_name") or selected.get("feature_name"):
            return safe_slug(
                "".join(
                    word[:1].upper() + word[1:]
                    for word in safe_slug(
                        selected.get("suggested_class_name") or selected.get("feature_name"),
                        64,
                    ).split("_")
                    if word
                ),
                64,
            ) or "GeneratedUnityFeature"
        if selected.get("candidate_id"):
            words = safe_slug(selected["candidate_id"], 48).split("_")
            return "".join(word.capitalize() for word in words if word) or "GeneratedUnityFeature"
        return f"Generated{script_kind}"

    def _default_namespace(self, context: Any) -> str:
        namespaces: list[str] = []
        for script in context.get("script_analysis", {}).get("scripts", []):
            namespaces.extend(str(item) for item in script.get("namespaces", []) if item)
        if namespaces:
            return f"{Counter(namespaces).most_common(1)[0][0]}.AgentGenerated"
        return "RandomWorld.AgentGenerated"

    def _build_script(
        self,
        class_name: str,
        namespace: str,
        script_kind: str,
        task_card: dict[str, Any],
        selected: dict[str, Any],
    ) -> str:
        goal = task_card.get("task_goal", "Generated Unity helper")
        candidate_text = " ".join(
            str(value)
            for value in [
                selected.get("candidate_id", ""),
                selected.get("feature_name", ""),
                selected.get("description", ""),
                goal,
            ]
        ).lower()
        if script_kind == "MonoBehaviour":
            if any(term in candidate_text for term in ["status effect", "status_effect", "buff", "debuff"]):
                return self._status_effect_template(class_name, namespace, goal)
            if any(term in candidate_text for term in ["weather", "season", "climate"]):
                return self._weather_cycle_template(class_name, namespace, goal)
            if any(term in candidate_text for term in ["morale", "happiness", "mood"]):
                return self._morale_template(class_name, namespace, goal)
            if any(term in candidate_text for term in ["resource threshold", "threshold alert", "stockpile"]):
                return self._resource_alert_template(class_name, namespace, goal)
            if any(term in candidate_text for term in ["combat event", "combat feed", "battlelog", "damagefeed"]):
                return self._combat_feed_template(class_name, namespace, goal)
            return self._runtime_feature_template(class_name, namespace, goal)
        if script_kind == "ScriptableObject":
            return self._scriptable_object_template(class_name, namespace, goal)
        return self._plain_class_template(class_name, namespace, goal)

    def _runtime_feature_template(self, class_name: str, namespace: str, goal: str) -> str:
        return f"""using System;
using UnityEngine;
using UnityEngine.Events;

namespace {namespace}
{{
    /// <summary>
    /// 为以下目标生成的运行时功能：{goal}
    /// 审查实现后，可手动把此组件挂载到合适的 GameObject 上。
    /// </summary>
    public sealed class {class_name} : MonoBehaviour
    {{
        [SerializeField] private bool startActive = true;
        [SerializeField] private string featureLabel = "{class_name}";

        public UnityEvent<string> FeatureActivated = new UnityEvent<string>();
        public UnityEvent<string> FeatureDeactivated = new UnityEvent<string>();

        public bool IsActive {{ get; private set; }}
        public DateTime ActivatedAtUtc {{ get; private set; }}

        private void OnEnable()
        {{
            if (startActive)
            {{
                Activate();
            }}
        }}

        public void Activate()
        {{
            if (IsActive)
            {{
                return;
            }}

            IsActive = true;
            ActivatedAtUtc = DateTime.UtcNow;
            FeatureActivated.Invoke(featureLabel);
        }}

        public void Deactivate()
        {{
            if (!IsActive)
            {{
                return;
            }}

            IsActive = false;
            FeatureDeactivated.Invoke(featureLabel);
        }}
    }}
}}
"""

    def _status_effect_template(self, class_name: str, namespace: str, goal: str) -> str:
        return f"""using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace {namespace}
{{
    /// <summary>
    /// 为以下目标生成的运行时功能：{goal}
    /// 管理限时状态效果，并且不需要修改已有角色脚本。
    /// </summary>
    public sealed class {class_name} : MonoBehaviour
    {{
        [Serializable]
        private sealed class ActiveEffect
        {{
            public string Id = "effect";
            public float DurationSeconds = 5f;
            public float Potency = 1f;
            public float TickIntervalSeconds = 1f;
            [NonSerialized] public float RemainingSeconds;
            [NonSerialized] public float TickTimerSeconds;
        }}

        [SerializeField] private bool applyStartingEffectsOnEnable;
        [SerializeField] private bool clearEffectsOnDisable = true;
        [SerializeField] private List<ActiveEffect> startingEffects = new List<ActiveEffect>();

        private readonly List<ActiveEffect> activeEffects = new List<ActiveEffect>();

        public UnityEvent<string> EffectApplied = new UnityEvent<string>();
        public UnityEvent<string> EffectExpired = new UnityEvent<string>();
        public UnityEvent<string> EffectTicked = new UnityEvent<string>();

        public float SpeedMultiplier {{ get; private set; }} = 1f;
        public float IncomingDamageMultiplier {{ get; private set; }} = 1f;

        private void OnEnable()
        {{
            if (!applyStartingEffectsOnEnable)
            {{
                return;
            }}

            foreach (ActiveEffect effect in startingEffects)
            {{
                ApplyEffect(effect.Id, effect.DurationSeconds, effect.Potency, effect.TickIntervalSeconds);
            }}
        }}

        private void OnDisable()
        {{
            if (clearEffectsOnDisable)
            {{
                activeEffects.Clear();
                RecalculateModifiers();
            }}
        }}

        private void Update()
        {{
            float deltaTime = Time.deltaTime;
            for (int index = activeEffects.Count - 1; index >= 0; index--)
            {{
                ActiveEffect effect = activeEffects[index];
                effect.RemainingSeconds -= deltaTime;
                effect.TickTimerSeconds -= deltaTime;

                if (effect.TickTimerSeconds <= 0f)
                {{
                    effect.TickTimerSeconds = Mathf.Max(0.1f, effect.TickIntervalSeconds);
                    EffectTicked.Invoke(effect.Id);
                }}

                if (effect.RemainingSeconds <= 0f)
                {{
                    string expiredId = effect.Id;
                    activeEffects.RemoveAt(index);
                    EffectExpired.Invoke(expiredId);
                }}
            }}

            RecalculateModifiers();
        }}

        public void ApplyEffect(string effectId, float durationSeconds, float potency = 1f, float tickIntervalSeconds = 1f)
        {{
            if (string.IsNullOrWhiteSpace(effectId) || durationSeconds <= 0f)
            {{
                return;
            }}

            ActiveEffect existing = activeEffects.Find(item => string.Equals(item.Id, effectId, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {{
                existing.RemainingSeconds = Mathf.Max(existing.RemainingSeconds, durationSeconds);
                existing.Potency = Mathf.Max(existing.Potency, potency);
                EffectApplied.Invoke(existing.Id);
                RecalculateModifiers();
                return;
            }}

            ActiveEffect effect = new ActiveEffect
            {{
                Id = effectId,
                DurationSeconds = durationSeconds,
                RemainingSeconds = durationSeconds,
                Potency = Mathf.Max(0f, potency),
                TickIntervalSeconds = Mathf.Max(0.1f, tickIntervalSeconds),
                TickTimerSeconds = Mathf.Max(0.1f, tickIntervalSeconds)
            }};
            activeEffects.Add(effect);
            EffectApplied.Invoke(effect.Id);
            RecalculateModifiers();
        }}

        public bool HasEffect(string effectId)
        {{
            return activeEffects.Exists(item => string.Equals(item.Id, effectId, StringComparison.OrdinalIgnoreCase));
        }}

        private void RecalculateModifiers()
        {{
            float speed = 1f;
            float incomingDamage = 1f;
            foreach (ActiveEffect effect in activeEffects)
            {{
                string id = effect.Id.ToLowerInvariant();
                if (id.Contains("slow"))
                {{
                    speed *= Mathf.Clamp01(1f - (0.15f * effect.Potency));
                }}
                else if (id.Contains("haste"))
                {{
                    speed *= 1f + (0.15f * effect.Potency);
                }}
                else if (id.Contains("vulnerable"))
                {{
                    incomingDamage *= 1f + (0.2f * effect.Potency);
                }}
                else if (id.Contains("guard"))
                {{
                    incomingDamage *= Mathf.Clamp01(1f - (0.2f * effect.Potency));
                }}
            }}

            SpeedMultiplier = Mathf.Max(0.05f, speed);
            IncomingDamageMultiplier = Mathf.Max(0.05f, incomingDamage);
        }}
    }}
}}
"""

    def _weather_cycle_template(self, class_name: str, namespace: str, goal: str) -> str:
        return f"""using System;
using UnityEngine;
using UnityEngine.Events;

namespace {namespace}
{{
    /// <summary>
    /// 为以下目标生成的运行时功能：{goal}
    /// 提供一个轻量天气循环，让其他系统可以订阅天气变化。
    /// </summary>
    public sealed class {class_name} : MonoBehaviour
    {{
        public enum WeatherState
        {{
            Clear,
            Rain,
            Storm,
            Fog,
            HeatWave,
            ColdSnap
        }}

        [Serializable]
        private sealed class WeatherRule
        {{
            public WeatherState State = WeatherState.Clear;
            [Min(0f)] public float Weight = 1f;
            [Min(1f)] public float MinDurationSeconds = 30f;
            [Min(1f)] public float MaxDurationSeconds = 90f;
            [Range(0f, 1f)] public float Intensity = 0.5f;
        }}

        [SerializeField] private WeatherRule[] weatherRules =
        {{
            new WeatherRule {{ State = WeatherState.Clear, Weight = 4f, Intensity = 0f }},
            new WeatherRule {{ State = WeatherState.Rain, Weight = 2f, Intensity = 0.45f }},
            new WeatherRule {{ State = WeatherState.Fog, Weight = 1f, Intensity = 0.35f }}
        }};
        [SerializeField] private bool advanceOnStart = true;

        private float remainingSeconds;

        public UnityEvent<string> WeatherChanged = new UnityEvent<string>();
        public WeatherState CurrentWeather {{ get; private set; }} = WeatherState.Clear;
        public float CurrentIntensity {{ get; private set; }}

        private void Start()
        {{
            if (advanceOnStart)
            {{
                PickNextWeather();
            }}
        }}

        private void Update()
        {{
            if (weatherRules == null || weatherRules.Length == 0)
            {{
                return;
            }}

            remainingSeconds -= Time.deltaTime;
            if (remainingSeconds <= 0f)
            {{
                PickNextWeather();
            }}
        }}

        public void PickNextWeather()
        {{
            WeatherRule rule = ChooseRule();
            CurrentWeather = rule.State;
            CurrentIntensity = Mathf.Clamp01(rule.Intensity);
            remainingSeconds = UnityEngine.Random.Range(
                Mathf.Min(rule.MinDurationSeconds, rule.MaxDurationSeconds),
                Mathf.Max(rule.MinDurationSeconds, rule.MaxDurationSeconds));
            WeatherChanged.Invoke(CurrentWeather.ToString());
        }}

        public float GetMovementMultiplier()
        {{
            if (CurrentWeather == WeatherState.Storm)
            {{
                return Mathf.Lerp(1f, 0.65f, CurrentIntensity);
            }}

            if (CurrentWeather == WeatherState.Rain || CurrentWeather == WeatherState.Fog)
            {{
                return Mathf.Lerp(1f, 0.85f, CurrentIntensity);
            }}

            return 1f;
        }}

        private WeatherRule ChooseRule()
        {{
            float totalWeight = 0f;
            foreach (WeatherRule rule in weatherRules)
            {{
                totalWeight += Mathf.Max(0f, rule.Weight);
            }}

            if (totalWeight <= 0f)
            {{
                return weatherRules[0];
            }}

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            foreach (WeatherRule rule in weatherRules)
            {{
                roll -= Mathf.Max(0f, rule.Weight);
                if (roll <= 0f)
                {{
                    return rule;
                }}
            }}

            return weatherRules[weatherRules.Length - 1];
        }}
    }}
}}
"""

    def _morale_template(self, class_name: str, namespace: str, goal: str) -> str:
        return f"""using UnityEngine;
using UnityEngine.Events;

namespace {namespace}
{{
    /// <summary>
    /// 为以下目标生成的运行时功能：{goal}
    /// 跟踪工人或基地士气值，并在达到阈值时触发事件。
    /// </summary>
    public sealed class {class_name} : MonoBehaviour
    {{
        [SerializeField, Range(0f, 100f)] private float morale = 75f;
        [SerializeField] private float passiveDecayPerMinute = 1.5f;
        [SerializeField] private float lowMoraleThreshold = 30f;
        [SerializeField] private float highMoraleThreshold = 80f;

        private bool lowMoraleRaised;
        private bool highMoraleRaised;

        public UnityEvent<float> MoraleChanged = new UnityEvent<float>();
        public UnityEvent LowMoraleReached = new UnityEvent();
        public UnityEvent HighMoraleReached = new UnityEvent();

        public float Morale => morale;
        public bool IsLowMorale => morale <= lowMoraleThreshold;
        public bool IsHighMorale => morale >= highMoraleThreshold;

        private void Update()
        {{
            if (passiveDecayPerMinute > 0f)
            {{
                AddMorale(-(passiveDecayPerMinute / 60f) * Time.deltaTime);
            }}
        }}

        public void AddMorale(float amount)
        {{
            float previous = morale;
            morale = Mathf.Clamp(morale + amount, 0f, 100f);
            if (Mathf.Approximately(previous, morale))
            {{
                return;
            }}

            MoraleChanged.Invoke(morale);
            EvaluateThresholds();
        }}

        public bool CanStartDemandingTask(float minimumMorale)
        {{
            return morale >= minimumMorale;
        }}

        private void EvaluateThresholds()
        {{
            if (IsLowMorale)
            {{
                if (!lowMoraleRaised)
                {{
                    LowMoraleReached.Invoke();
                }}

                lowMoraleRaised = true;
            }}
            else
            {{
                lowMoraleRaised = false;
            }}

            if (IsHighMorale)
            {{
                if (!highMoraleRaised)
                {{
                    HighMoraleReached.Invoke();
                }}

                highMoraleRaised = true;
            }}
            else
            {{
                highMoraleRaised = false;
            }}
        }}
    }}
}}
"""

    def _resource_alert_template(self, class_name: str, namespace: str, goal: str) -> str:
        return f"""using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace {namespace}
{{
    /// <summary>
    /// 为以下目标生成的运行时功能：{goal}
    /// 跟踪具名资源数量，并在资源跨过阈值时触发提醒。
    /// </summary>
    public sealed class {class_name} : MonoBehaviour
    {{
        [Serializable]
        private sealed class ResourceThreshold
        {{
            public string ResourceId = "wood";
            public int StartingAmount;
            public int MinimumAmount = 10;
        }}

        [Serializable]
        public sealed class ResourceAlertEvent : UnityEvent<string, int>
        {{
        }}

        [SerializeField] private List<ResourceThreshold> startingThresholds = new List<ResourceThreshold>();

        private readonly Dictionary<string, int> amounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> minimums = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> activeAlerts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public ResourceAlertEvent ResourceBelowThreshold = new ResourceAlertEvent();
        public ResourceAlertEvent ResourceRecovered = new ResourceAlertEvent();

        private void Awake()
        {{
            foreach (ResourceThreshold threshold in startingThresholds)
            {{
                SetThreshold(threshold.ResourceId, threshold.MinimumAmount);
                SetAmount(threshold.ResourceId, threshold.StartingAmount);
            }}
        }}

        public void SetThreshold(string resourceId, int minimumAmount)
        {{
            if (string.IsNullOrWhiteSpace(resourceId))
            {{
                return;
            }}

            minimums[resourceId] = Mathf.Max(0, minimumAmount);
            Evaluate(resourceId);
        }}

        public void SetAmount(string resourceId, int amount)
        {{
            if (string.IsNullOrWhiteSpace(resourceId))
            {{
                return;
            }}

            amounts[resourceId] = Mathf.Max(0, amount);
            Evaluate(resourceId);
        }}

        public void AddAmount(string resourceId, int delta)
        {{
            int current = GetAmount(resourceId);
            SetAmount(resourceId, current + delta);
        }}

        public int GetAmount(string resourceId)
        {{
            return amounts.TryGetValue(resourceId, out int amount) ? amount : 0;
        }}

        private void Evaluate(string resourceId)
        {{
            if (string.IsNullOrWhiteSpace(resourceId) || !minimums.ContainsKey(resourceId))
            {{
                return;
            }}

            int amount = GetAmount(resourceId);
            bool below = amount < minimums[resourceId];
            bool alreadyAlerting = activeAlerts.Contains(resourceId);
            if (below && !alreadyAlerting)
            {{
                activeAlerts.Add(resourceId);
                ResourceBelowThreshold.Invoke(resourceId, amount);
            }}
            else if (!below && alreadyAlerting)
            {{
                activeAlerts.Remove(resourceId);
                ResourceRecovered.Invoke(resourceId, amount);
            }}
        }}
    }}
}}
"""

    def _combat_feed_template(self, class_name: str, namespace: str, goal: str) -> str:
        return f"""using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace {namespace}
{{
    /// <summary>
    /// 为以下目标生成的运行时功能：{goal}
    /// 保存最近的战斗事件，并暴露给 UI 或调试系统使用。
    /// </summary>
    public sealed class {class_name} : MonoBehaviour
    {{
        [Serializable]
        public sealed class CombatEvent
        {{
            public string Source;
            public string Target;
            public string EventType;
            public float Amount;
            public float TimeSeconds;
        }}

        [Serializable]
        public sealed class CombatEventUnityEvent : UnityEvent<string>
        {{
        }}

        [SerializeField] private int maxEvents = 40;

        private readonly Queue<CombatEvent> events = new Queue<CombatEvent>();

        public CombatEventUnityEvent CombatEventRecorded = new CombatEventUnityEvent();

        public IReadOnlyCollection<CombatEvent> Events => events;

        public void RecordDamage(string source, string target, float amount)
        {{
            Record(source, target, "Damage", amount);
        }}

        public void RecordHealing(string source, string target, float amount)
        {{
            Record(source, target, "Healing", amount);
        }}

        public void RecordDeath(string source, string target)
        {{
            Record(source, target, "Death", 0f);
        }}

        public void Clear()
        {{
            events.Clear();
        }}

        private void Record(string source, string target, string eventType, float amount)
        {{
            CombatEvent entry = new CombatEvent
            {{
                Source = source,
                Target = target,
                EventType = eventType,
                Amount = amount,
                TimeSeconds = Time.time
            }};
            events.Enqueue(entry);
            while (events.Count > Mathf.Max(1, maxEvents))
            {{
                events.Dequeue();
            }}

            CombatEventRecorded.Invoke($"{{eventType}}: {{source}} -> {{target}} ({{amount:0.##}})");
        }}
    }}
}}
"""

    def _scriptable_object_template(self, class_name: str, namespace: str, goal: str) -> str:
        return f"""using UnityEngine;

namespace {namespace}
{{
    /// <summary>
    /// 为以下目标生成的 ScriptableObject：{goal}
    /// 使用此类型创建资源前，请先审查生成代码。
    /// </summary>
    [CreateAssetMenu(fileName = "{class_name}", menuName = "AgentFull/{class_name}")]
    public sealed class {class_name} : ScriptableObject
    {{
        [SerializeField] private string notes = "Generated by AgentFull.";

        public string Notes => notes;
    }}
}}
"""

    def _plain_class_template(self, class_name: str, namespace: str, goal: str) -> str:
        return f"""using System;

namespace {namespace}
{{
    /// <summary>
    /// 为以下目标生成的辅助类：{goal}
    /// </summary>
    public sealed class {class_name}
    {{
        public DateTime CreatedAtUtc {{ get; }} = DateTime.UtcNow;

        public string Describe()
        {{
            return "{class_name} generated by AgentFull.";
        }}
    }}
}}
"""
