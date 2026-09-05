namespace LAB2D.Tool
{
    using LAB2D;
    using LAB2D.Enum;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 浮动战斗文字工具类
    /// 提供对象池管理、文字 GameObject 创建、颜色/字号查询、数值格式化等公共方法。
    /// 纯运行时工具，不依赖 UnityEditor，可用于所有子模块。
    ///
    /// 使用方式：FloatingTextManager 调用本类方法获取/回收浮动文字对象。
    /// </summary>
    public static class FloatingTextTool
    {
        /// <summary>
        /// 获取指定类型的显示颜色
        /// </summary>
        /// <param name="type">浮动文字类型</param>
        /// <returns>对应颜色</returns>
        public static Color GetColor(FloatingTextType type)
        {
            switch (type)
            {
                case FloatingTextType.Damage:
                    return FloatingTextConstant.DamageColor;
                case FloatingTextType.Critical:
                    return FloatingTextConstant.CriticalColor;
                case FloatingTextType.Heal:
                    return FloatingTextConstant.HealColor;
                case FloatingTextType.Combo:
                    return FloatingTextConstant.ComboColor;
                case FloatingTextType.Experience:
                    return FloatingTextConstant.ExperienceColor;
                case FloatingTextType.Dodge:
                    return FloatingTextConstant.DodgeColor;
                case FloatingTextType.StatusEffect:
                    return FloatingTextConstant.StatusEffectColor;
                default:
                    return FloatingTextConstant.DamageColor;
            }
        }

        /// <summary>
        /// 获取指定类型的字号
        /// </summary>
        /// <param name="type">浮动文字类型</param>
        /// <returns>对应字号</returns>
        public static int GetFontSize(FloatingTextType type)
        {
            switch (type)
            {
                case FloatingTextType.Damage:
                    return FloatingTextConstant.DamageFontSize;
                case FloatingTextType.Critical:
                    return FloatingTextConstant.CriticalFontSize;
                case FloatingTextType.Heal:
                    return FloatingTextConstant.HealFontSize;
                case FloatingTextType.Combo:
                    return FloatingTextConstant.ComboFontSize;
                case FloatingTextType.Experience:
                    return FloatingTextConstant.ExperienceFontSize;
                case FloatingTextType.Dodge:
                    return FloatingTextConstant.DodgeFontSize;
                case FloatingTextType.StatusEffect:
                    return FloatingTextConstant.StatusEffectFontSize;
                default:
                    return FloatingTextConstant.DamageFontSize;
            }
        }

        /// <summary>
        /// 获取指定类型的上浮速度
        /// </summary>
        /// <param name="type">浮动文字类型</param>
        /// <returns>上浮速度（世界单位/秒）</returns>
        public static float GetFloatSpeed(FloatingTextType type)
        {
            switch (type)
            {
                case FloatingTextType.Critical:
                    return FloatingTextConstant.CriticalFloatSpeed;
                case FloatingTextType.Combo:
                    return FloatingTextConstant.ComboFloatSpeed;
                case FloatingTextType.Experience:
                case FloatingTextType.StatusEffect:
                    return FloatingTextConstant.SlowFloatSpeed;
                default:
                    return FloatingTextConstant.DefaultFloatSpeed;
            }
        }

        /// <summary>
        /// 获取指定类型的存活时间
        /// </summary>
        /// <param name="type">浮动文字类型</param>
        /// <returns>存活时间（秒）</returns>
        public static float GetLifetime(FloatingTextType type)
        {
            switch (type)
            {
                case FloatingTextType.Critical:
                    return FloatingTextConstant.CriticalLifetime;
                case FloatingTextType.Combo:
                    return FloatingTextConstant.ComboLifetime;
                default:
                    return FloatingTextConstant.DefaultLifetime;
            }
        }

        /// <summary>
        /// 获取指定类型的弹出缩放倍数（1.0 表示无弹出动画）
        /// </summary>
        /// <param name="type">浮动文字类型</param>
        /// <returns>缩放倍数</returns>
        public static float GetPopScale(FloatingTextType type)
        {
            switch (type)
            {
                case FloatingTextType.Critical:
                    return FloatingTextConstant.CriticalPopScale;
                case FloatingTextType.Combo:
                    return FloatingTextConstant.ComboPopScale;
                default:
                    return 1.0f;
            }
        }

        /// <summary>
        /// 格式化伤害数值文本（保留一位小数）
        /// </summary>
        /// <param name="value">伤害值</param>
        /// <returns>格式化后的文本</returns>
        public static string FormatDamageText(float value)
        {
            return value.ToString("F1");
        }

        /// <summary>
        /// 格式化治疗数值文本
        /// </summary>
        /// <param name="value">治疗量</param>
        /// <returns>带 "+" 前缀的文本</returns>
        public static string FormatHealText(float value)
        {
            return FloatingTextConstant.HealPrefix + value.ToString("F1");
        }

        /// <summary>
        /// 格式化经验数值文本
        /// </summary>
        /// <param name="value">经验值</param>
        /// <returns>带前缀和后缀的文本</returns>
        public static string FormatExpText(int value)
        {
            return FloatingTextConstant.ExpPrefix + value + FloatingTextConstant.ExpSuffix;
        }

        /// <summary>
        /// 格式化连击文本
        /// </summary>
        /// <param name="comboCount">连击数</param>
        /// <returns>带 "x" 前缀的文本</returns>
        public static string FormatComboText(int comboCount)
        {
            return FloatingTextConstant.ComboPrefix + comboCount;
        }

        /// <summary>
        /// 生成随机水平偏移量，避免多个文字完全重叠
        /// </summary>
        /// <returns>[-RandomOffsetX, +RandomOffsetX] 范围内的随机偏移</returns>
        public static float RandomHorizontalOffset()
        {
            return Random.Range(
                -FloatingTextConstant.RandomOffsetX,
                FloatingTextConstant.RandomOffsetX);
        }

        /// <summary>
        /// 安全创建浮动文字 GameObject（含 Text 组件）
        /// 不纳入对象池管理，由调用方负责生命周期或回收到池。
        /// </summary>
        /// <param name="name">对象名称</param>
        /// <param name="parent">父节点 Transform</param>
        /// <returns>创建好的 GameObject（含 Text 组件）</returns>
        public static GameObject CreateFloatingTextObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name);
            if (parent != null)
            {
                obj.transform.SetParent(parent, false);
            }

            Text text = obj.AddComponent<Text>();
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false; // 不拦截射线，避免影响点击

            // 像素字体单一来源（铁律③：一律 ark-pixel，禁 Unity 内置字体）
            text.font = PixelUITheme.PixelFont;

            // 添加描边效果增强可读性
            Outline outline = obj.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.5f);
            outline.effectDistance = new Vector2(1, -1);

            return obj;
        }

        /// <summary>
        /// 查找场景中已有的 Canvas（不再动态创建，请在场景中手动放置）。
        /// </summary>
        /// <param name="canvasName">Canvas 名称</param>
        /// <param name="sortingOrder">渲染排序层级（保留参数兼容性，不再使用）</param>
        /// <returns>Canvas GameObject，未找到时返回 null</returns>
        public static GameObject EnsureCanvas(string canvasName, int sortingOrder)
        {
            return GameObject.Find(canvasName);
        }
    }
}
