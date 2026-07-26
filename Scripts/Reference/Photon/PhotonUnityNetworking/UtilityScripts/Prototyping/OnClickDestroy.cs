// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OnClickDestroy.cs" company="Exit Games GmbH">
// Part of: Photon Unity Utilities
// </copyright>
// <summary>一个用于原型的紧凑脚本。</summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------


namespace Photon.Pun.UtilityScripts
{
    using System.Collections;
    using UnityEngine;
    using UnityEngine.EventSystems;

    /// <summary>
    /// 通过PhotonNetwork.Destroy或通过发送调用Object.Destroy()的RPC来销毁网络GameObject。
    /// </summary>
    /// <remarks>
    /// 使用RPC来Destroy一个GameObject通常是一个坏主意。
    /// 它允许任何玩家销毁一个GameObject，并可能导致错误。
    ///
    /// 客户端必须清理服务器的事件缓存，其中包含与GO相关的Instantiate和
    /// 缓冲RPC事件。
    ///
    /// 缓冲的RPC会在发送玩家离开房间时被清理，因此后续加入的玩家
    /// 不会收到这些缓冲的RPC。这反过来意味着他们可能因为加入较晚而不会销毁该GO。
    ///
    /// 反之，当创建玩家离开房间时，GameObject的Instantiate可能会被清理。
    /// 这样，RPC目标的GameObject可能会丢失。
    ///
    /// 测试这些情况是有意义的。许多不是破坏性错误，你只需要意识到它们。
    ///
    ///
    /// 通过Unity的IPointerClickHandler接收OnClick()调用。需要在摄像机上设置PhysicsRaycaster。
    /// 参见: https://docs.unity3d.com/ScriptReference/EventSystems.IPointerClickHandler.html
    /// </remarks>
    public class OnClickDestroy : MonoBehaviourPun, IPointerClickHandler
    {
        public PointerEventData.InputButton Button;
        public KeyCode ModifierKey;

        public bool DestroyByRpc;


        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            if (!PhotonNetwork.InRoom || (this.ModifierKey != KeyCode.None && !Input.GetKey(this.ModifierKey)) || eventData.button != this.Button)
            {
                return;
            }


            if (this.DestroyByRpc)
            {
                this.pv.RPC("DestroyRpc", RpcTarget.AllBuffered);
            }
            else
            {
                PhotonNetwork.Destroy(this.gameObject);
            }
        }


        [PunRPC]
        public IEnumerator DestroyRpc()
        {
            Destroy(this.gameObject);
            yield return 0; // 如果你允许1帧的通过，对象的OnDestroy()方法会被调用并清理引用。
        }
    }
}