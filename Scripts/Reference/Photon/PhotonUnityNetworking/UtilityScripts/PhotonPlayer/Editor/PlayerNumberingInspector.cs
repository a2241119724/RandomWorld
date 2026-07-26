// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlayerNumberingInspector.cs" company="Exit Games GmbH">
//   Part of: Photon Unity Utilities,
// </copyright>
// <summary>
//  PlayerNumbering的自定义Inspector
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

using Photon.Realtime;
using UnityEditor;
using UnityEngine;

namespace Photon.Pun.UtilityScripts
{
    [CustomEditor(typeof(PlayerNumbering))]
    public class PlayerNumberingInspector : Editor
    {

        int localPlayerIndex;

        void OnEnable()
        {
            PlayerNumbering.OnPlayerNumberingChanged += RefreshData;
        }

        void OnDisable()
        {
            PlayerNumbering.OnPlayerNumberingChanged -= RefreshData;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            PlayerNumbering.OnPlayerNumberingChanged += RefreshData;

            if (PhotonNetwork.InRoom)
            {
                EditorGUILayout.LabelField("Player Index", "Player ID");
                if (PlayerNumbering.SortedPlayers != null)
                {
                    foreach (Player punPlayer in PlayerNumbering.SortedPlayers)
                    {
                        GUI.enabled = punPlayer.ActorNumber > 0;
                        EditorGUILayout.LabelField("Player " + punPlayer.GetPlayerNumber() + (punPlayer.IsLocal ? " - You -" : ""), punPlayer.ActorNumber == 0 ? "n/a" : punPlayer.ToStringFull());
                        GUI.enabled = true;
                    }
                }
            }
            else
            {
                GUILayout.Label("PlayerNumbering only works when localPlayer is inside a room");
            }
        }

        /// <summary>
        /// 强制刷新Inspector，否则我们不会在Inspector中看到新数据。
        /// 这比在OnInspectorGUI中每帧不必要地多次执行更好。
        /// </summary>
        void RefreshData()
        {
            Repaint();
        }

    }
}