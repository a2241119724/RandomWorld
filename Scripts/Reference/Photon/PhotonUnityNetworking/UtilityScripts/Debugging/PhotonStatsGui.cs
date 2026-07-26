// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PhotonStatsGui.cs" company="Exit Games GmbH">
//   Part of: Photon Unity Utilities,
// </copyright>
// <summary>
// 用于显示与Photon连接的网络流量和健康统计信息的基本GUI，
// 通过shift+tab切换。
// 属于[可选GUI](@ref optionalGui)。
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

using ExitGames.Client.Photon;
using UnityEngine;

namespace Photon.Pun.UtilityScripts
{
    /// <summary>
    /// 用于显示与Photon连接的网络流量和健康统计信息的基本GUI，
    /// 通过shift+tab切换。
    /// </summary>
    /// <remarks>
    /// 显示的健康值可以帮助识别连接丢失或性能问题。
    /// 示例：
    /// 如果连续两次SendOutgoingCommands调用之间的时间差为一秒或更多，
    /// 由此导致断开连接的可能性就会增加（因为需要及时向服务器发送确认）。
    /// </remarks>
    /// \ingroup optionalGui
    public class PhotonStatsGui : MonoBehaviour
    {
        /// <summary>显示或隐藏GUI（不影响是否收集统计数据）。</summary>
        public bool statsWindowOn = true;

        /// <summary>开启或关闭统计数据收集的选项（在Update()中使用）。</summary>
        public bool statsOn = true;

        /// <summary>显示连接的额外"健康"值。</summary>
        public bool healthStatsVisible;

        /// <summary>显示额外的"较低级别"流量统计数据。</summary>
        public bool trafficStatsOn;

        /// <summary>显示控制统计数据和重置它们的按钮。</summary>
        public bool buttonsOn;

        /// <summary>窗口的定位矩形。</summary>
        public Rect statsRect = new Rect(0, 100, 200, 50);

        /// <summary>Unity GUI窗口ID（必须唯一，否则会导致问题）。</summary>
        public int WindowId = 100;


        public void Start()
        {
            if (this.statsRect.x <= 0)
            {
                this.statsRect.x = Screen.width - this.statsRect.width;
            }
        }

        /// <summary>检查shift+tab组合键输入（用于切换statsOn）。</summary>
        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab) && Input.GetKey(KeyCode.LeftShift))
            {
                this.statsWindowOn = !this.statsWindowOn;
                this.statsOn = true;    // 显示窗口时启用统计
            }
        }

        public void OnGUI()
        {
            if (PhotonNetwork.NetworkingClient.LoadBalancingPeer.TrafficStatsEnabled != statsOn)
            {
                PhotonNetwork.NetworkingClient.LoadBalancingPeer.TrafficStatsEnabled = this.statsOn;
            }

            if (!this.statsWindowOn)
            {
                return;
            }

            this.statsRect = GUILayout.Window(this.WindowId, this.statsRect, this.TrafficStatsWindow, "Messages (shift+tab)");
        }

        public void TrafficStatsWindow(int windowID)
        {
            bool statsToLog = false;
            TrafficStatsGameLevel gls = PhotonNetwork.NetworkingClient.LoadBalancingPeer.TrafficStatsGameLevel;
            long elapsedMs = PhotonNetwork.NetworkingClient.LoadBalancingPeer.TrafficStatsElapsedMs / 1000;
            if (elapsedMs == 0)
            {
                elapsedMs = 1;
            }

            GUILayout.BeginHorizontal();
            this.buttonsOn = GUILayout.Toggle(this.buttonsOn, "buttons");
            this.healthStatsVisible = GUILayout.Toggle(this.healthStatsVisible, "health");
            this.trafficStatsOn = GUILayout.Toggle(this.trafficStatsOn, "traffic");
            GUILayout.EndHorizontal();

            string total = string.Format("Out {0,4} | In {1,4} | Sum {2,4}", gls.TotalOutgoingMessageCount, gls.TotalIncomingMessageCount, gls.TotalMessageCount);
            string elapsedTime = string.Format("{0}sec average:", elapsedMs);
            string average = string.Format("Out {0,4} | In {1,4} | Sum {2,4}", gls.TotalOutgoingMessageCount / elapsedMs, gls.TotalIncomingMessageCount / elapsedMs, gls.TotalMessageCount / elapsedMs);
            GUILayout.Label(total);
            GUILayout.Label(elapsedTime);
            GUILayout.Label(average);

            if (this.buttonsOn)
            {
                GUILayout.BeginHorizontal();
                this.statsOn = GUILayout.Toggle(this.statsOn, "stats on");
                if (GUILayout.Button("Reset"))
                {
                    PhotonNetwork.NetworkingClient.LoadBalancingPeer.TrafficStatsReset();
                    PhotonNetwork.NetworkingClient.LoadBalancingPeer.TrafficStatsEnabled = true;
                }
                statsToLog = GUILayout.Button("To Log");
                GUILayout.EndHorizontal();
            }

            string trafficStatsIn = string.Empty;
            string trafficStatsOut = string.Empty;
            if (this.trafficStatsOn)
            {
                GUILayout.Box("Traffic Stats");
                trafficStatsIn = "Incoming: \n" + PhotonNetwork.NetworkingClient.LoadBalancingPeer.TrafficStatsIncoming.ToString();
                trafficStatsOut = "Outgoing: \n" + PhotonNetwork.NetworkingClient.LoadBalancingPeer.TrafficStatsOutgoing.ToString();
                GUILayout.Label(trafficStatsIn);
                GUILayout.Label(trafficStatsOut);
            }

            string healthStats = string.Empty;
            if (this.healthStatsVisible)
            {
                GUILayout.Box("Health Stats");
                healthStats = string.Format(
                    "ping: {6}[+/-{7}]ms resent:{8} \n\nmax ms between\nsend: {0,4} \ndispatch: {1,4} \n\nlongest dispatch for: \nev({3}):{2,3}ms \nop({5}):{4,3}ms",
                    gls.LongestDeltaBetweenSending,
                    gls.LongestDeltaBetweenDispatching,
                    gls.LongestEventCallback,
                    gls.LongestEventCallbackCode,
                    gls.LongestOpResponseCallback,
                    gls.LongestOpResponseCallbackOpCode,
                    PhotonNetwork.NetworkingClient.LoadBalancingPeer.RoundTripTime,
                    PhotonNetwork.NetworkingClient.LoadBalancingPeer.RoundTripTimeVariance,
                    PhotonNetwork.NetworkingClient.LoadBalancingPeer.ResentReliableCommands);
                GUILayout.Label(healthStats);
            }

            if (statsToLog)
            {
                string complete = string.Format("{0}\n{1}\n{2}\n{3}\n{4}\n{5}", total, elapsedTime, average, trafficStatsIn, trafficStatsOut, healthStats);
                Debug.Log(complete);
            }

            // 如果有任何点击，此窗口的高度可能已更改。减小它以在下一帧重新布局
            if (GUI.changed)
            {
                this.statsRect.height = 100;
            }

            GUI.DragWindow();
        }
    }
}