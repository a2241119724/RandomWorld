namespace LAB2D.UnityAdapter
{
    using LAB2D;
    using System;
    using UnityEngine;

    /// <summary>
    /// 集中式 Tip/Log 兜底辅助类。
    /// 消除了在 11 个 Manager 类中重复出现的 try/catch GlobalInit.ShowTip + Debug.Log 模式。
    ///
    /// 用法：
    ///   TipHelper.Show("message", "[MyPrefix]");
    ///   TipHelper.Show("message", "[MyPrefix]", OnTipRequested);
    /// </summary>
    public static class TipHelper
    {
        /// <summary>
        /// 通过 GlobalInit.ShowTip 显示提示，失败时回退到 Debug.Log。
        /// 同时触发一个可选事件供外部提示处理器使用。
        /// </summary>
        /// <param name="message">提示消息。</param>
        /// <param name="logPrefix">Debug.Log 回退时的前缀。</param>
        /// <param name="onTipRequested">显示提示前触发的可选事件。</param>
        public static void Show(string message, string logPrefix = "", Action<string> onTipRequested = null)
        {
            onTipRequested?.Invoke(message);

            try
            {
                GlobalInit init = GlobalInit.Instance;
                if (init != null)
                {
                    init.ShowTip(message);
                    return;
                }
            }
            catch (Exception exception)
            {
                string prefix = string.IsNullOrEmpty(logPrefix) ? string.Empty : logPrefix + " ";
                Debug.LogWarning($"{prefix}Show tip failed: {exception.Message}");
            }

            string fullMessage = string.IsNullOrEmpty(logPrefix)
                ? message
                : $"{logPrefix} {message}";
            Debug.Log(fullMessage);
        }
    }
}
