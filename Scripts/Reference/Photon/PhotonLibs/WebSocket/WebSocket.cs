#if UNITY_WEBGL || WEBSOCKET || ((UNITY_XBOXONE || UNITY_GAMECORE) && UNITY_EDITOR)

// --------------------------------------------------------------------------------------------------------------------
// <summary>
//   最初由 Unity 提供，用于覆盖 WebGL 和编辑器中的 WebSocket 支持。由 Exit Games GmbH 修改。
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------


namespace ExitGames.Client.Photon
{
    using System;
    using System.Text;
#if UNITY_WEBGL && !UNITY_EDITOR
    using System.Runtime.InteropServices;
#else
    using WebSocketSharp;
    using System.Collections.Generic;
    using System.Security.Authentication;
#endif


    public class WebSocket
    {
        private Uri mUrl;
        /// <summary>Photon 使用此值来协商序列化协议。可选值：GpBinaryV16 或 GpBinaryV18。基于 enum SerializationProtocol。</summary>
        private string protocols = "GpBinaryV16";

        public WebSocket(Uri url, string serialization = null)
        {
            this.mUrl = url;
            if (serialization != null)
            {
                this.protocols = serialization;
            }

            string protocol = mUrl.Scheme;
            if (!protocol.Equals("ws") && !protocol.Equals("wss"))
                throw new ArgumentException("Unsupported protocol: " + protocol);
        }

        public void SendString(string str)
        {
            Send(Encoding.UTF8.GetBytes (str));
        }

        public string RecvString()
        {
            byte[] retval = Recv();
            if (retval == null)
                return null;
            return Encoding.UTF8.GetString (retval);
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int SocketCreate (string url, string protocols);

        [DllImport("__Internal")]
        private static extern int SocketState (int socketInstance);

        [DllImport("__Internal")]
        private static extern void SocketSend (int socketInstance, byte[] ptr, int length);

        [DllImport("__Internal")]
        private static extern void SocketRecv (int socketInstance, byte[] ptr, int length);

        [DllImport("__Internal")]
        private static extern int SocketRecvLength (int socketInstance);

        [DllImport("__Internal")]
        private static extern void SocketClose (int socketInstance);

        [DllImport("__Internal")]
        private static extern int SocketError (int socketInstance, byte[] ptr, int length);

        int m_NativeRef = 0;

        public void Send(byte[] buffer)
        {
            SocketSend (m_NativeRef, buffer, buffer.Length);
        }

        public byte[] Recv()
        {
            int length = SocketRecvLength (m_NativeRef);
            if (length == 0)
                return null;
            byte[] buffer = new byte[length];
            SocketRecv (m_NativeRef, buffer, length);
            return buffer;
        }

        public void Connect()
        {
            m_NativeRef = SocketCreate (mUrl.ToString(), this.protocols);

            //while (SocketState(m_NativeRef) == 0)
            //    yield return 0;
        }

        public void Close()
        {
            SocketClose(m_NativeRef);
        }

        public bool Connected
        {
            get { return SocketState(m_NativeRef) != 0; }
        }

        public string Error
        {
            get {
                const int bufsize = 1024;
                byte[] buffer = new byte[bufsize];
                int result = SocketError (m_NativeRef, buffer, bufsize);

                if (result == 0)
                    return null;

                return Encoding.UTF8.GetString (buffer);
            }
        }
#else
        WebSocketSharp.WebSocket m_Socket;
        Queue<byte[]> m_Messages = new Queue<byte[]>();
        bool m_IsConnected = false;
        string m_Error = null;

        public void Connect()
        {
            m_Socket = new WebSocketSharp.WebSocket(mUrl.ToString(), new string[] { this.protocols });
            m_Socket.SslConfiguration.EnabledSslProtocols = m_Socket.SslConfiguration.EnabledSslProtocols | (SslProtocols)(3072| 768);
            m_Socket.OnMessage += (sender, e) => m_Messages.Enqueue(e.RawData);
            m_Socket.OnOpen += (sender, e) => m_IsConnected = true;
            //this.m_Socket.Log.Level = LogLevel.Debug;
            //this.m_Socket.Log.Output += Output;
            this.m_Socket.OnClose += SocketOnClose;
            m_Socket.OnError += (sender, e) => m_Error = e.Message + (e.Exception == null ? "" : " / " + e.Exception);
            m_Socket.ConnectAsync();
        }

        private void SocketOnClose(object sender, CloseEventArgs e)
        {
            //UnityEngine.Debug.Log(e.Code.ToString());

            // this code is used for cases when the socket failed to get created (specifically used to detect "blocked by Windows firewall")
            // for some reason this situation is not calling OnError
            if (e.Code == 1006)
            {
                this.m_Error = e.Reason;
                this.m_IsConnected = false;
            }
        }

        public bool Connected { get { return m_IsConnected; } }// 由 TS 添加


        public void Send(byte[] buffer)
        {
            m_Socket.Send(buffer);
        }

        public byte[] Recv()
        {
            if (m_Messages.Count == 0)
                return null;
            return m_Messages.Dequeue();
        }

        public void Close()
        {
            m_Socket.Close();
        }

        public string Error
        {
            get
            {
                return m_Error;
            }
        }
#endif
    }
}
#endif