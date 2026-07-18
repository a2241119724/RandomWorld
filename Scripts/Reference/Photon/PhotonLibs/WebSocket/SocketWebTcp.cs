#if UNITY_WEBGL || WEBSOCKET || ((UNITY_XBOXONE || UNITY_GAMECORE) && UNITY_EDITOR)

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SocketWebTcp.cs" company="Exit Games GmbH">
//   Copyright (c) Exit Games GmbH.  All rights reserved.
// </copyright>
// <summary>
//   内部类，封装实时库的网络 I/O 功能。
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------


namespace ExitGames.Client.Photon
{
    using System;
    using System.Collections;
    using UnityEngine;
    using SupportClassPun = ExitGames.Client.Photon.SupportClass;


#if !(UNITY_WEBGL || NETFX_CORE)
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
#endif

    /// <summary>
    /// 用于等待真实秒数的 Yield Instruction。如果 Time.TimeScale 被更改，保持连接正常工作非常重要，我们仍然需要准确的网络事件
    /// </summary>
    public sealed class WaitForRealSeconds : CustomYieldInstruction
    {
        private readonly float _endTime;

        public override bool keepWaiting
        {
            get { return this._endTime > Time.realtimeSinceStartup; }
        }

        public WaitForRealSeconds(float seconds)
        {
            this._endTime = Time.realtimeSinceStartup + seconds;
        }
    }


    /// <summary>
    /// 内部类，封装实时库的网络 I/O 功能。
    /// </summary>
    public class SocketWebTcp : IPhotonSocket, IDisposable
    {
        private WebSocket sock;

        private readonly object syncer = new object();

        public SocketWebTcp(PeerBase npeer) : base(npeer)
        {
            this.ServerAddress = npeer.ServerAddress;
            if (this.ReportDebugOfLevel(DebugLevel.INFO))
            {
                this.Listener.DebugReturn(DebugLevel.INFO, "new SocketWebTcp() for Unity. Server: " + this.ServerAddress);
            }

            //this.Protocol = ConnectionProtocol.WebSocket;
            this.PollReceive = false;
        }

        public void Dispose()
        {
            this.State = PhotonSocketState.Disconnecting;

            if (this.sock != null)
            {
                try
                {
                    if (this.sock.Connected) this.sock.Close();
                }
                catch (Exception ex)
                {
                    this.EnqueueDebugReturn(DebugLevel.INFO, "Exception in Dispose(): " + ex);
                }
            }

            this.sock = null;
            this.State = PhotonSocketState.Disconnected;
        }

        GameObject websocketConnectionObject;
        public override bool Connect()
        {
            //bool baseOk = base.Connect();
            //if (!baseOk)
            //{
            //    return false;
            //}


            this.State = PhotonSocketState.Connecting;

            if (this.websocketConnectionObject != null)
            {
                UnityEngine.Object.Destroy(this.websocketConnectionObject);
            }

            this.websocketConnectionObject = new GameObject("websocketConnectionObject");
            MonoBehaviour mb = this.websocketConnectionObject.AddComponent<MonoBehaviourExt>();
            this.websocketConnectionObject.hideFlags = HideFlags.HideInHierarchy;
            UnityEngine.Object.DontDestroyOnLoad(this.websocketConnectionObject);

#if UNITY_WEBGL || NETFX_CORE
            this.sock = new WebSocket(new Uri(this.ConnectAddress), this.SerializationProtocol);
            this.sock.Connect();

            mb.StartCoroutine(this.ReceiveLoop());
#else

            mb.StartCoroutine(this.DetectIpVersionAndConnect(mb));

#endif
            return true;
        }


#if !(UNITY_WEBGL || NETFX_CORE)
        private bool ipVersionDetectDone;
        private IEnumerator DetectIpVersionAndConnect(MonoBehaviour mb)
        {
            Uri uri = null;
            try
            {
                uri = new Uri(this.ConnectAddress);
            }
            catch (Exception ex)
            {
                if (this.ReportDebugOfLevel(DebugLevel.ERROR))
                {
                    this.Listener.DebugReturn(DebugLevel.ERROR, "Failed to create a URI from ConnectAddress (" + ConnectAddress + "). Exception: " + ex);
                }
            }

            if (uri != null && uri.HostNameType == UriHostNameType.Dns)
            {
                ipVersionDetectDone = false;

                ThreadPool.QueueUserWorkItem(this.DetectIpVersion, uri.Host);

                while (!this.ipVersionDetectDone)
                {
                    yield return new WaitForRealSeconds(0.1f);
                }
            }

            if (this.AddressResolvedAsIpv6)
            {
                this.ConnectAddress += "&IPv6";
            }

            if (this.ReportDebugOfLevel(DebugLevel.INFO))
            {
                this.Listener.DebugReturn(DebugLevel.INFO, "DetectIpVersionAndConnect() AddressResolvedAsIpv6: " + this.AddressResolvedAsIpv6 + " ConnectAddress: " + ConnectAddress);
            }


            this.sock = new WebSocket(new Uri(this.ConnectAddress), this.SerializationProtocol);
            this.sock.Connect();

            mb.StartCoroutine(this.ReceiveLoop());
        }

        // state 必须是 hostname 字符串
        private void DetectIpVersion(object state)
        {
            string host = state as string;
            IPAddress[] ipAddresses;
            try
            {
                ipAddresses = Dns.GetHostAddresses(host);
                foreach (IPAddress ipAddress in ipAddresses)
                {
                    if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        this.AddressResolvedAsIpv6 = true;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                this.Listener.DebugReturn(DebugLevel.INFO, "DetectIpVersionAndConnect (uri: " + host + "= thread failed: " + ex);
            }

            this.ipVersionDetectDone = true;
        }
#endif


        public override bool Disconnect()
        {
            if (this.ReportDebugOfLevel(DebugLevel.INFO))
            {
                this.Listener.DebugReturn(DebugLevel.INFO, "SocketWebTcp.Disconnect()");
            }

            this.State = PhotonSocketState.Disconnecting;

            lock (this.syncer)
            {
                if (this.sock != null)
                {
                    try
                    {
                        this.sock.Close();
                    }
                    catch (Exception ex)
                    {
                        this.Listener.DebugReturn(DebugLevel.ERROR, "Exception in Disconnect(): " + ex);
                    }
                    this.sock = null;
                }
            }

            if (this.websocketConnectionObject != null)
            {
                UnityEngine.Object.Destroy(this.websocketConnectionObject);
            }

            this.State = PhotonSocketState.Disconnected;
            return true;
        }

        /// <summary>
        /// 由 TPeer* 使用
        /// </summary>
        public override PhotonSocketError Send(byte[] data, int length)
        {
            if (this.State != PhotonSocketState.Connected)
            {
                return PhotonSocketError.Skipped;
            }

            try
            {
                if (data.Length > length)
                {
                    byte[] trimmedData = new byte[length];
                    Buffer.BlockCopy(data, 0, trimmedData, 0, length);
                    data = trimmedData;
                }

                if (this.ReportDebugOfLevel(DebugLevel.ALL))
                {
                    this.Listener.DebugReturn(DebugLevel.ALL, "Sending: " + SupportClassPun.ByteArrayToString(data));
                }

                if (this.sock != null)
                {
                    this.sock.Send(data);
                }
            }
            catch (Exception e)
            {
                this.Listener.DebugReturn(DebugLevel.ERROR, "Cannot send to: " + this.ServerAddress + ". " + e.Message);

                this.HandleException(StatusCode.Exception);
                return PhotonSocketError.Exception;
            }

            return PhotonSocketError.Success;
        }

        public override PhotonSocketError Receive(out byte[] data)
        {
            data = null;
            return PhotonSocketError.NoData;
        }


        internal const int ALL_HEADER_BYTES = 9;
        internal const int TCP_HEADER_BYTES = 7;
        internal const int MSG_HEADER_BYTES = 2;

        public IEnumerator ReceiveLoop()
        {
            //this.Listener.DebugReturn(DebugLevel.INFO, "ReceiveLoop()");
            if (this.sock != null)
            {
                while (this.sock != null && !this.sock.Connected && this.sock.Error == null)
                {
                    yield return new WaitForRealSeconds(0.1f);
                }


                if (this.sock != null)
                {
                    if (this.sock.Error != null)
                    {
                        this.Listener.DebugReturn(DebugLevel.ERROR, "Exiting receive thread. Server: " + this.ServerAddress + ":" + this.ServerPort + " Error: " + this.sock.Error);
                        this.HandleException(StatusCode.ExceptionOnConnect);
                    }
                    else
                    {
                        // 已连接
                        if (this.ReportDebugOfLevel(DebugLevel.ALL))
                        {
                            this.Listener.DebugReturn(DebugLevel.ALL, "Receiving by websocket. this.State: " + this.State);
                        }

                        this.State = PhotonSocketState.Connected;
                        this.peerBase.OnConnect();

                        while (this.State == PhotonSocketState.Connected)
                        {
                            if (this.sock != null)
                            {
                                if (this.sock.Error != null)
                                {
                                    this.Listener.DebugReturn(DebugLevel.ERROR, "Exiting receive thread (inside loop). Server: " + this.ServerAddress + ":" + this.ServerPort + " Error: " + this.sock.Error);
                                    this.HandleException(StatusCode.ExceptionOnReceive);
                                    break;
                                }
                                else
                                {
                                    byte[] inBuff = this.sock.Recv();
                                    if (inBuff == null || inBuff.Length == 0)
                                    {
                                        // 未收到任何内容。稍等片刻，再试一次
                                        yield return new WaitForRealSeconds(0.02f);
                                        continue;
                                    }

                                    if (this.ReportDebugOfLevel(DebugLevel.ALL))
                                    {
                                        this.Listener.DebugReturn(DebugLevel.ALL, "TCP << " + inBuff.Length + " = " + SupportClassPun.ByteArrayToString(inBuff));
                                    }

                                    if (inBuff.Length > 0)
                                    {
                                        try
                                        {
                                            this.HandleReceivedDatagram(inBuff, inBuff.Length, false);
                                        }
                                        catch (Exception e)
                                        {
                                            if (this.State != PhotonSocketState.Disconnecting && this.State != PhotonSocketState.Disconnected)
                                            {
                                                if (this.ReportDebugOfLevel(DebugLevel.ERROR))
                                                {
                                                    this.EnqueueDebugReturn(DebugLevel.ERROR, "Receive issue. State: " + this.State + ". Server: '" + this.ServerAddress + "' Exception: " + e);
                                                }

                                                this.HandleException(StatusCode.ExceptionOnReceive);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            this.Disconnect();
        }

        private class MonoBehaviourExt : MonoBehaviour { }
    }
}

#endif