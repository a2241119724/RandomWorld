// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OnJoinedInstantiate.cs" company="Exit Games GmbH">
//   Part of: Photon Unity Utilities,
// </copyright>
// <summary>
//  此组件将在按下Escape键时退出应用程序
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using UnityEngine;

namespace Photon.Pun.UtilityScripts
{
    /// <summary>
    /// 此组件将在按下Escape键时退出应用程序
    /// </summary>
    public class OnEscapeQuit : MonoBehaviour
    {
        [Conditional("UNITY_ANDROID"), Conditional("UNITY_IOS")]
        public void Update()
        {
            // 手机的"返回"按钮等同于"Escape"。如果按下则退出应用
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }
        }
    }
}