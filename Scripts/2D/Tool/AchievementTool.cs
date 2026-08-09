namespace LAB2D.Tool
{
    using LAB2D;
    using LAB2D.Domain.Gameplay;
    using LAB2D.Enum;
    using LAB2D.Gameplay;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 成就系统工具类
    /// 用途：提供成就进度格式化、条件检查、UI 创建辅助等可复用静态方法。
    /// 被 AchievementManager、AchievementPopup、AchievementPanel 和 AchievementMenu 调用。
    /// 不依赖 UnityEngine.UI 之外的具体业务脚本，无 UnityEditor 引用，安全用于运行时和构建。
    /// </summary>
    public static class AchievementTool
    {
        private static readonly AchievementRuleService RuleService = new AchievementRuleService();

        /// <summary>
        /// 格式化成就进度文本
        /// </summary>
        /// <param name="current">当前进度值</param>
        /// <param name="target">目标值</param>
        /// <returns>如 "50 / 100" 格式文本</returns>
        public static string FormatProgress(int current, int target)
        {
            if (target <= 0)
            {
                return current.ToString();
            }

            return $"{current} / {target}";
        }

        /// <summary>
        /// 计算成就进度百分比（0.0 ~ 1.0）
        /// </summary>
        /// <param name="current">当前进度值</param>
        /// <param name="target">目标值</param>
        /// <returns>0.0 到 1.0 之间的进度比例</returns>
        public static float GetProgressRatio(int current, int target)
        {
            return RuleService.GetProgressRatio(current, target);
        }

        /// <summary>
        /// 获取成就点数展示文本
        /// </summary>
        /// <param name="points">成就点数</param>
        /// <returns>如 "+10 点" 格式文本</returns>
        public static string FormatPoints(int points)
        {
            return $"+{points}{AchievementConstant.PointsSuffix}";
        }

        /// <summary>
        /// 获取成就类别中文显示名
        /// </summary>
        /// <param name="category">成就类别枚举</param>
        /// <returns>类别中文名</returns>
        public static string GetCategoryDisplayName(AchievementCategory category)
        {
            switch (category)
            {
                case AchievementCategory.Combat:
                    return "战斗";
                case AchievementCategory.Collection:
                    return "收集";
                case AchievementCategory.Survival:
                    return "生存";
                case AchievementCategory.Wave:
                    return "波次";
                case AchievementCategory.Worker:
                    return "工人";
                default:
                    return "其他";
            }
        }

        /// <summary>
        /// 获取成就状态中文显示名
        /// </summary>
        /// <param name="state">成就状态</param>
        /// <returns>状态中文名</returns>
        public static string GetStateDisplayName(AchievementState state)
        {
            switch (state)
            {
                case AchievementState.Locked:
                    return "未解锁";
                case AchievementState.Unlocked:
                    return "已解锁";
                case AchievementState.Claimed:
                    return "已领取";
                default:
                    return "未知";
            }
        }

        /// <summary>
        /// 查找 UI 根节点，用于挂载成就 UI，复用 UI 的 Canvas
        /// </summary>
        /// <returns>UI 根 Transform，找不到时返回 null</returns>
        public static Transform FindUIRoot()
        {
            return GameObject.FindGameObjectWithTag(TagConstant.UI_TAG)?.transform;
        }

        /// <summary>
        /// 安全创建带 Text 组件的 UI 文本节点
        /// </summary>
        /// <param name="parent">父 Transform</param>
        /// <param name="name">节点名</param>
        /// <param name="text">默认文本</param>
        /// <param name="fontSize">字体大小</param>
        /// <returns>创建的 Text 组件</returns>
        public static Text CreateText(Transform parent, string name, string text, int fontSize = 24)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            Text txt = obj.AddComponent<Text>();
            txt.text = text;
            txt.fontSize = fontSize;
            txt.color = Color.white;
            txt.font = LAB2D.AI.Dialogue.LLM.UIFontConfig.GetFont();
            txt.raycastTarget = false;
            return txt;
        }

        /// <summary>
        /// 安全创建带 Image 组件的 UI 图像节点
        /// </summary>
        /// <param name="parent">父 Transform</param>
        /// <param name="name">节点名</param>
        /// <param name="color">背景颜色</param>
        /// <returns>创建的 Image 组件</returns>
        public static Image CreateImage(Transform parent, string name, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            Image img = obj.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = true;
            return img;
        }

        /// <summary>
        /// 构建成就摘要文本（供 HUD / Tip 使用）
        /// </summary>
        /// <param name="unlockedList">已解锁成就列表</param>
        /// <param name="totalCount">成就总数</param>
        /// <param name="totalPoints">总成就点数</param>
        /// <returns>如 "成就: 3/20 已解锁 (30点)" 格式文本</returns>
        public static string BuildSummaryText(List<AchievementData> unlockedList, int totalCount, int totalPoints)
        {
            int unlocked = unlockedList?.Count ?? 0;
            int points = 0;
            if (unlockedList != null)
            {
                foreach (AchievementData a in unlockedList)
                {
                    points += a.Points;
                }
            }

            return $"成就: {unlocked}/{totalCount} 已解锁 ({points}点)";
        }

        /// <summary>
        /// 从条件描述模板构建具体条件文本
        /// </summary>
        /// <param name="conditionTemplate">条件模板，如 "击杀 {0} 个敌人"</param>
        /// <param name="target">目标值</param>
        /// <returns>具体条件文本，如 "击杀 100 个敌人"</returns>
        public static string BuildConditionText(string conditionTemplate, int target)
        {
            return string.Format(conditionTemplate, target);
        }
    }
}
