namespace LAB2D.UI.Panel.PanelUI
{
    using System.Collections.Generic;
    using LAB2D.Domain.Gameplay.Cultivation;
    using LAB2D.Domain.TurnBattle;
    using LAB2D.Manager;
    using UnityEngine;

    /// <summary>
    /// 回合制特效选型与帧缓存 — 按 Damage/Heal/Status 条目挑素材名（BtlFx_ 前缀，
    /// Resources/Images/Effect/Battle/ 下按帧号后缀 _0..N），交给 TurnBattleEffectPlayer 播放。
    /// 缺帧仅 [PanelDiag] Warning 一次并整组跳过（不阻断战斗，后续同名不再尝试）。
    /// </summary>
    public static class TurnBattleEffectLibrary
    {
        private const int SlashFrameCount = 6;
        private const int ElementFrameCount = 6;
        private const int ImpactFrameCount = 4;
        private const int HealFrameCount = 6;
        private const int SealFrameCount = 6;

        private const float DefaultFps = 12f;
        private const float DefaultSize = 176f;

        /// <summary>特效挂点相对卡片中心的局部偏移（头像偏上位置，避开血条文字）。</summary>
        private static readonly Vector3 CardFxLocalPos = new Vector3(0f, 20f, 0f);

        private static readonly Dictionary<string, Sprite[]> FrameCache = new Dictionary<string, Sprite[]>();
        private static readonly HashSet<string> MissingWarned = new HashSet<string>();

        /// <summary>伤害特效：追击=冲击点；带五行=五行特效；否则=斩击。flipX 由攻击方阵营决定（敌打我镜像）。</summary>
        public static void PlayDamage(Transform cardRoot, TurnBattleEffectEntry entry, bool flipX)
        {
            if (entry.IsCombo)
            {
                Play(cardRoot, "BtlFx_Impact", ImpactFrameCount, false, null);
                return;
            }

            if (entry.Element != null)
            {
                Play(cardRoot, $"BtlFx_{entry.Element.Value}", ElementFrameCount, flipX, null);
                return;
            }

            Play(cardRoot, "BtlFx_Slash", SlashFrameCount, flipX, null);
        }

        /// <summary>治疗特效（绿色同心环，素材自带色）。</summary>
        public static void PlayHeal(Transform cardRoot)
        {
            Play(cardRoot, "BtlFx_Heal", HealFrameCount, false, null);
        }

        /// <summary>状态施加特效（白色符环素材按状态色染色）。</summary>
        public static void PlayStatus(Transform cardRoot, Color tint)
        {
            Play(cardRoot, "BtlFx_Seal", SealFrameCount, false, tint);
        }

        /// <summary>取一组序列帧 — 命中缓存直接返；任一帧缺失整组作废并警告一次。</summary>
        internal static Sprite[] GetFrames(string name, int frameCount)
        {
            if (FrameCache.TryGetValue(name, out Sprite[] cached))
            {
                return cached;
            }

            if (MissingWarned.Contains(name))
            {
                return null;
            }

            if (!ServiceLocator.TryGet<ResourceManager>(out ResourceManager resourceManager))
            {
                return null; // 服务未注册（异常场景）：静默跳过，特效非关键路径
            }

            Sprite[] frames = new Sprite[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                Sprite frame = resourceManager.GetImage($"{name}_{i}");
                if (frame == null)
                {
                    MissingWarned.Add(name);
                    AWorkerTask.LogProvider(
                        $"[PanelDiag] 战斗特效 {name} 缺帧 {name}_{i}，整组跳过（检查 Resources/Images/Effect/Battle/）",
                        LogManager.LogLevelEnum.Warning);
                    return null;
                }

                frames[i] = frame;
            }

            FrameCache[name] = frames;
            return frames;
        }

        private static void Play(Transform cardRoot, string name, int frameCount, bool flipX, Color? tint)
        {
            if (cardRoot == null)
            {
                return;
            }

            Sprite[] frames = GetFrames(name, frameCount);
            if (frames == null)
            {
                return;
            }

            TurnBattleEffectPlayer.Create(cardRoot, CardFxLocalPos, frames, DefaultFps, DefaultSize, flipX, tint);
        }
    }
}
