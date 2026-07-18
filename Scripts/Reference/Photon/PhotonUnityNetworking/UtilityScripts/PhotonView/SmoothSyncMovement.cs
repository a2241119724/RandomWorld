// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SmoothSyncMovement.cs" company="Exit Games GmbH">
//   Part of: Photon Unity Utilities,
// </copyright>
// <summary>
//  网络游戏对象的平滑移动
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

using UnityEngine;

namespace Photon.Pun.UtilityScripts
{
    /// <summary>
    /// 网络游戏对象的平滑移动
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class SmoothSyncMovement : Photon.Pun.MonoBehaviourPun, IPunObservable
    {
        public float SmoothingDelay = 5;
        public void Awake()
        {
            bool observed = false;
            foreach (Component observedComponent in this.pv.ObservedComponents)
            {
                if (observedComponent == this)
                {
                    observed = true;
                    break;
                }
            }
            if (!observed)
            {
                Debug.LogWarning(this + " is not observed by this object's photonView! OnPhotonSerializeView() in this class won't be used.");
            }
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                //我们拥有此玩家：将我们的数据发送给其他人
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);
            }
            else
            {
                //网络玩家，接收数据
                correctPlayerPos = (Vector3)stream.ReceiveNext();
                correctPlayerRot = (Quaternion)stream.ReceiveNext();
            }
        }

        private Vector3 correctPlayerPos = Vector3.zero; //我们向此处做Lerp
        private Quaternion correctPlayerRot = Quaternion.identity; //我们向此处做Lerp

        public void Update()
        {
            if (!pv.IsMine)
            {
                //更新远程玩家（平滑处理，看起来不错，但会牺牲一些精度）
                transform.position = Vector3.Lerp(transform.position, correctPlayerPos, Time.deltaTime * this.SmoothingDelay);
                transform.rotation = Quaternion.Lerp(transform.rotation, correctPlayerRot, Time.deltaTime * this.SmoothingDelay);
            }
        }

    }
}