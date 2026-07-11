namespace LAB2D
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Centralized Tip/log fallback helper.
    /// Eliminates the duplicated try/catch GlobalInit.ShowTip + Debug.Log pattern
    /// that was repeated across 11 Manager classes.
    ///
    /// Usage:
    ///   TipHelper.Show("message", "[MyPrefix]");
    ///   TipHelper.Show("message", "[MyPrefix]", OnTipRequested);
    /// </summary>
    public static class TipHelper
    {
        /// <summary>
        /// Show a tip via GlobalInit.ShowTip, falling back to Debug.Log.
        /// Also fires an optional event for external tip handlers.
        /// </summary>
        /// <param name="message">Tip message.</param>
        /// <param name="logPrefix">Prefix for Debug.Log fallback.</param>
        /// <param name="onTipRequested">Optional event to fire before showing the tip.</param>
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
