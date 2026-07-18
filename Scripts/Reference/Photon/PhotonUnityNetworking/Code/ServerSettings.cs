// ----------------------------------------------------------------------------
// <copyright file="ServerSettings.cs" company="Exit Games GmbH">
//   PhotonNetwork Framework for Unity - Copyright (C) 2018 Exit Games GmbH
// </copyright>
// <summary>
// 定义服务器设置的ScriptableObject。实例创建为<b>PhotonServerSettings</b>。
// </summary>
// <author>developer@exitgames.com</author>
// ----------------------------------------------------------------------------


namespace Photon.Pun
{
    using Photon.Realtime;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 连接相关设置的集合，由PhotonNetwork.ConnectUsingSettings内部使用。
    /// </summary>
    /// <remarks>
    /// 包含来自Realtime API的AppSettings类以及一些其他与PUN相关的设置。</remarks>
    [Serializable]
    [HelpURL("https://doc.photonengine.com/en-us/pun/v2/getting-started/initial-setup")]
    public class ServerSettings : ScriptableObject
    {
        [Tooltip("Core Photon Server/Cloud settings.")]
        public AppSettings AppSettings;

        /// <summary>编辑器和开发版本将使用的区域。这确保所有用户在同一区域进行测试。</summary>
        [Tooltip("Developer build override for Best Region.")]
        public string DevRegion;

        [Tooltip("Log output by PUN.")]
        public PunLogLevel PunLogging = PunLogLevel.ErrorsOnly;

        [Tooltip("Logs additional info for debugging.")]
        public bool EnableSupportLogger;

        [Tooltip("Enables apps to keep the connection without focus.")]
        public bool RunInBackground = true;

        [Tooltip("Simulates an online connection.\nPUN can be used as usual.")]
        public bool StartInOfflineMode;

        [Tooltip("RPC name list.\nUsed as shortcut when sending calls.")]
        public List<string> RpcList = new List<string>();   // 由脚本和/或通过Inspector设置

#if UNITY_EDITOR
        public bool DisableAutoOpenWizard;
        public bool ShowSettings;
        public bool DevRegionSetOnce;
#endif

        /// <summary>在AppSettings中设置appid和区域代码。在编辑器中使用。</summary>
        public void UseCloud(string cloudAppid, string code = "")
        {
            this.AppSettings.AppIdRealtime = cloudAppid;
            this.AppSettings.Server = null;
            this.AppSettings.FixedRegion = string.IsNullOrEmpty(code) ? null : code;
        }

        /// <summary>通过尝试创建一个Guid来检查字符串是否为有效的Guid。</summary>
        /// <param name="val">要检查的潜在guid。</param>
        /// <returns>如果new Guid(val)未失败则返回True。</returns>
        public static bool IsAppId(string val)
        {
            try
            {
                new Guid(val);
            }
            catch
            {
                return false;
            }
            return true;
        }

        /// <summary>从偏好设置中获取"最佳区域摘要"。</summary>
        /// <value>偏好设置中的最佳区域代码。</value>
        public static string BestRegionSummaryInPreferences
        {
            get { return PhotonNetwork.BestRegionSummaryInPreferences; }
        }

        /// <summary>将偏好设置中的"最佳区域摘要"设置为null。下次启动时，客户端将ping所有可用区域。</summary>
        public static void ResetBestRegionCodeInPreferences()
        {
            PhotonNetwork.BestRegionSummaryInPreferences = null;
        }

        /// <summary>AppSettings的字符串摘要。</summary>
        public override string ToString()
        {
            return "ServerSettings: " + this.AppSettings.ToStringFull();
        }
    }
}