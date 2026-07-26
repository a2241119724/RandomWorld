// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PhotonLagSimulationGui.cs" company="Exit Games GmbH">
//   Part of: Photon Unity Utilities,
// </copyright>
// <summary>
// 此MonoBehaviour是Photon客户端网络模拟功能的基本GUI。
// 它可以修改延迟（固定延迟）、抖动（随机延迟）和丢包。
// 属于[可选GUI](@ref optionalGui)。
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------


using ExitGames.Client.Photon;
using UnityEngine;

namespace Photon.Pun.UtilityScripts
{
    /// <summary>
    /// 此MonoBehaviour是Photon客户端网络模拟功能的基本GUI。
    /// 它可以修改延迟（固定延迟）、抖动（随机延迟）和丢包。
    /// </summary>
    /// \ingroup optionalGui
    public class PhotonLagSimulationGui : MonoBehaviour
    {
        /// <summary>窗口的定位矩形。</summary>
        public Rect WindowRect = new Rect(0, 100, 120, 100);

        /// <summary>Unity GUI窗口ID（必须唯一，否则会导致问题）。</summary>
        public int WindowId = 101;

        /// <summary>显示或隐藏GUI（不影响设置）。</summary>
        public bool Visible = true;

        /// <summary>当前使用的peer（用于设置网络模拟）。</summary>
        public PhotonPeer Peer { get; set; }

        public void Start()
        {
            this.Peer = PhotonNetwork.NetworkingClient.LoadBalancingPeer;
        }

        public void OnGUI()
        {
            if (!this.Visible)
            {
                return;
            }

            if (this.Peer == null)
            {
                this.WindowRect = GUILayout.Window(this.WindowId, this.WindowRect, this.NetSimHasNoPeerWindow, "Netw. Sim.");
            }
            else
            {
                this.WindowRect = GUILayout.Window(this.WindowId, this.WindowRect, this.NetSimWindow, "Netw. Sim.");
            }
        }

        private void NetSimHasNoPeerWindow(int windowId)
        {
            GUILayout.Label("No peer to communicate with. ");
        }

        private void NetSimWindow(int windowId)
        {
            GUILayout.Label(string.Format("Rtt:{0,4} +/-{1,3}", this.Peer.RoundTripTime, this.Peer.RoundTripTimeVariance));

            bool simEnabled = this.Peer.IsSimulationEnabled;
            bool newSimEnabled = GUILayout.Toggle(simEnabled, "Simulate");
            if (newSimEnabled != simEnabled)
            {
                this.Peer.IsSimulationEnabled = newSimEnabled;
            }

            float inOutLag = this.Peer.NetworkSimulationSettings.IncomingLag;
            GUILayout.Label("Lag " + inOutLag);
            inOutLag = GUILayout.HorizontalSlider(inOutLag, 0, 500);

            this.Peer.NetworkSimulationSettings.IncomingLag = (int)inOutLag;
            this.Peer.NetworkSimulationSettings.OutgoingLag = (int)inOutLag;

            float inOutJitter = this.Peer.NetworkSimulationSettings.IncomingJitter;
            GUILayout.Label("Jit " + inOutJitter);
            inOutJitter = GUILayout.HorizontalSlider(inOutJitter, 0, 100);

            this.Peer.NetworkSimulationSettings.IncomingJitter = (int)inOutJitter;
            this.Peer.NetworkSimulationSettings.OutgoingJitter = (int)inOutJitter;

            float loss = this.Peer.NetworkSimulationSettings.IncomingLossPercentage;
            GUILayout.Label("Loss " + loss);
            loss = GUILayout.HorizontalSlider(loss, 0, 10);

            this.Peer.NetworkSimulationSettings.IncomingLossPercentage = (int)loss;
            this.Peer.NetworkSimulationSettings.OutgoingLossPercentage = (int)loss;

            // 如果有任何点击，此窗口的高度可能已更改。减小它以在下一帧重新布局
            if (GUI.changed)
            {
                this.WindowRect.height = 100;
            }

            GUI.DragWindow();
        }
    }
}