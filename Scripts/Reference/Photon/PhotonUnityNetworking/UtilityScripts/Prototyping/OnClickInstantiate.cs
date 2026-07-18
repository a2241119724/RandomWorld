// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OnClickInstantiate.cs" company="Exit Games GmbH">
// Part of: Photon Unity Utilities
// </copyright>
// <summary>一个用于原型的紧凑脚本。</summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------


namespace Photon.Pun.UtilityScripts
{
    using UnityEngine;
    using UnityEngine.EventSystems;


    /// <summary>
    /// 在点击时实例化一个网络GameObject。
    /// </summary>
    /// <remarks>
    /// 通过Unity的IPointerClickHandler接收OnClick()调用。需要在摄像机上设置PhysicsRaycaster。
    /// 参见: https://docs.unity3d.com/ScriptReference/EventSystems.IPointerClickHandler.html
    /// </remarks>
    public class OnClickInstantiate : MonoBehaviour, IPointerClickHandler
    {
        public enum InstantiateOption { Mine, Scene }


        public PointerEventData.InputButton Button;
        public KeyCode ModifierKey;

        public GameObject Prefab;

        [SerializeField]
        private InstantiateOption InstantiateType = InstantiateOption.Mine;


        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            if (!PhotonNetwork.InRoom || (this.ModifierKey != KeyCode.None && !Input.GetKey(this.ModifierKey)) || eventData.button != this.Button)
            {
                return;
            }


            switch (this.InstantiateType)
            {
                case InstantiateOption.Mine:
                    PhotonNetwork.Instantiate(this.Prefab.name, eventData.pointerCurrentRaycast.worldPosition + new Vector3(0, 0.5f, 0), Quaternion.identity, 0);
                    break;
                case InstantiateOption.Scene:
                    PhotonNetwork.InstantiateRoomObject(this.Prefab.name, eventData.pointerCurrentRaycast.worldPosition + new Vector3(0, 0.5f, 0), Quaternion.identity, 0, null);
                    break;
            }
        }
    }
}