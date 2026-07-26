// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OnStartDelete.cs" company="Exit Games GmbH">
//   Part of: Photon Unity Utilities,
// </copyright>
// <summary>
//  此组件将在Start()中销毁它所附加的GameObject。
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

using UnityEngine;

namespace Photon.Pun.UtilityScripts
{
    /// <summary>此组件将在Start()中销毁它所附加的GameObject。</summary>
    public class OnStartDelete : MonoBehaviour
    {
        // Use this for initialization
        public void Start()
        {
            Destroy(this.gameObject);
        }
    }
}