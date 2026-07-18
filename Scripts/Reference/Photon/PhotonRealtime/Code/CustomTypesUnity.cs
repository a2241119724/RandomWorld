// ----------------------------------------------------------------------------
// <copyright file="CustomTypes.cs" company="Exit Games GmbH">
//   PhotonNetwork Framework for Unity - Copyright (C) 2018 Exit Games GmbH
// </copyright>
// <summary>
// 设置对 Unity 特定类型的支持。可以作为注册自己的自定义类型以进行发送的蓝图。
// </summary>
// <author>developer@exitgames.com</author>
// ----------------------------------------------------------------------------

#if UNITY_4_7 || UNITY_5 || UNITY_5_3_OR_NEWER
#define SUPPORTED_UNITY
#endif

#if SUPPORTED_UNITY
namespace Photon.Realtime
{
    using ExitGames.Client.Photon;
    using UnityEngine;


    /// <summary>
    /// 内部使用的类，包含各种 Unity 特定类的反/序列化方法。
    /// 将这些添加到 Photon 序列化协议中允许您在事件中发送它们等。
    /// </summary>
    internal static class CustomTypesUnity
    {
        private const int SizeV2 = 2 * 4;
        private const int SizeV3 = 3 * 4;
        private const int SizeQuat = 4 * 4;


        /// <summary>注册 Unity 特定类型的反/序列化器方法。使这些类型可在 RaiseEvent 和 PUN 中使用。</summary>
        internal static void Register()
        {
            PhotonPeer.RegisterType(typeof(Vector2), (byte)'W', SerializeVector2, DeserializeVector2);
            PhotonPeer.RegisterType(typeof(Vector3), (byte)'V', SerializeVector3, DeserializeVector3);
            PhotonPeer.RegisterType(typeof(Quaternion), (byte)'Q', SerializeQuaternion, DeserializeQuaternion);
        }


        #region Custom De/Serializer Methods

        public static readonly byte[] memVector3 = new byte[SizeV3];

        private static short SerializeVector3(StreamBuffer outStream, object customobject)
        {
            Vector3 vo = (Vector3)customobject;

            int index = 0;
            lock (memVector3)
            {
                byte[] bytes = memVector3;
                Protocol.Serialize(vo.x, bytes, ref index);
                Protocol.Serialize(vo.y, bytes, ref index);
                Protocol.Serialize(vo.z, bytes, ref index);
                outStream.Write(bytes, 0, SizeV3);
            }

            return SizeV3;
        }

        private static object DeserializeVector3(StreamBuffer inStream, short length)
        {
            Vector3 vo = new Vector3();
            if (length != SizeV3)
            {
                return vo;
            }

            lock (memVector3)
            {
                inStream.Read(memVector3, 0, SizeV3);
                int index = 0;
                Protocol.Deserialize(out vo.x, memVector3, ref index);
                Protocol.Deserialize(out vo.y, memVector3, ref index);
                Protocol.Deserialize(out vo.z, memVector3, ref index);
            }

            return vo;
        }


        public static readonly byte[] memVector2 = new byte[SizeV2];

        private static short SerializeVector2(StreamBuffer outStream, object customobject)
        {
            Vector2 vo = (Vector2)customobject;
            lock (memVector2)
            {
                byte[] bytes = memVector2;
                int index = 0;
                Protocol.Serialize(vo.x, bytes, ref index);
                Protocol.Serialize(vo.y, bytes, ref index);
                outStream.Write(bytes, 0, SizeV2);
            }

            return SizeV2;
        }

        private static object DeserializeVector2(StreamBuffer inStream, short length)
        {
            Vector2 vo = new Vector2();
            if (length != SizeV2)
            {
                return vo;
            }

            lock (memVector2)
            {
                inStream.Read(memVector2, 0, SizeV2);
                int index = 0;
                Protocol.Deserialize(out vo.x, memVector2, ref index);
                Protocol.Deserialize(out vo.y, memVector2, ref index);
            }

            return vo;
        }


        public static readonly byte[] memQuarternion = new byte[SizeQuat];

        private static short SerializeQuaternion(StreamBuffer outStream, object customobject)
        {
            Quaternion o = (Quaternion)customobject;

            lock (memQuarternion)
            {
                byte[] bytes = memQuarternion;
                int index = 0;
                Protocol.Serialize(o.w, bytes, ref index);
                Protocol.Serialize(o.x, bytes, ref index);
                Protocol.Serialize(o.y, bytes, ref index);
                Protocol.Serialize(o.z, bytes, ref index);
                outStream.Write(bytes, 0, SizeQuat);
            }

            return 4 * 4;
        }

        private static object DeserializeQuaternion(StreamBuffer inStream, short length)
        {
            Quaternion o = Quaternion.identity;
            if (length != SizeQuat)
            {
                return o;
            }

            lock (memQuarternion)
            {
                inStream.Read(memQuarternion, 0, SizeQuat);
                int index = 0;
                Protocol.Deserialize(out o.w, memQuarternion, ref index);
                Protocol.Deserialize(out o.x, memQuarternion, ref index);
                Protocol.Deserialize(out o.y, memQuarternion, ref index);
                Protocol.Deserialize(out o.z, memQuarternion, ref index);
            }

            return o;
        }

        #endregion
    }
}
#endif