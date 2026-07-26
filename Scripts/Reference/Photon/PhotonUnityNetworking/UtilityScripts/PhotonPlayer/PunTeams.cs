// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PunTeams.cs" company="Exit Games GmbH">
//   Part of: Photon Unity Utilities,
// </copyright>
// <summary>
// 借助玩家属性在房间/游戏中实现团队功能。通过Player.GetTeam扩展方法访问。
// </summary>
// <remarks>
// 团队由枚举Team定义。更改它以获取更多/不同的团队。
// 没有关于何时/是否可以加入团队的规则。你可以在JoinTeam或类似方法中添加此逻辑。
// </remarks>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

using Photon.Realtime;
using System;
using System.Collections.Generic;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Photon.Pun.UtilityScripts
{
    /// <summary>
    /// 借助玩家属性在房间/游戏中实现团队功能。通过Player.GetTeam扩展方法访问。
    /// </summary>
    /// <remarks>
    /// 团队由枚举Team定义。更改它以获取更多/不同的团队。
    /// 没有关于何时/是否可以加入团队的规则。你可以在JoinTeam或类似方法中添加此逻辑。
    /// </remarks>
    [Obsolete("do not use this or add it to the scene. use PhotonTeamsManager instead")]
    public class PunTeams : MonoBehaviourPunCallbacks
    {
        /// <summary>定义可用团队的枚举。第一个团队应该是中立的（这是此枚举任何字段的默认值）。</summary>
        [Obsolete("use custom PhotonTeam instead")]
        public enum Team : byte { none, red, blue };

        /// <summary>团队及其玩家列表的主列表。自动保持更新。</summary>
        /// <remarks>注意这是静态的。可以通过PunTeam.PlayersPerTeam访问。你不应该修改它。</remarks>
        [Obsolete("use PhotonTeamsManager.Instance.TryGetTeamMembers instead")]
        public static Dictionary<Team, List<Player>> PlayersPerTeam;

        /// <summary>定义用于"此"玩家团队归属的玩家自定义属性名称。</summary>
        [Obsolete("do not use this. PhotonTeamsManager.TeamPlayerProp is used internally instead.")]
        public const string TeamPlayerProp = "team";


        #region Events by Unity and Photon

        public void Start()
        {
            PlayersPerTeam = new Dictionary<Team, List<Player>>();
            Array enumVals = Enum.GetValues(typeof(Team));
            foreach (var enumVal in enumVals)
            {
                PlayersPerTeam[(Team)enumVal] = new List<Player>();
            }
        }

        public override void OnDisable()
        {
            base.OnDisable();
            this.Start();
        }

        /// <summary>加入房间时需要更新团队列表。</summary>
        /// <remarks>由PUN调用。参见枚举MonoBehaviourPunCallbacks了解说明。</remarks>
        public override void OnJoinedRoom()
        {

            this.UpdateTeams();
        }

        public override void OnLeftRoom()
        {
            Start();
        }

        /// <summary>刷新团队列表。这也可能是一个非团队相关的属性变更。</summary>
        /// <remarks>由PUN调用。参见枚举MonoBehaviourPunCallbacks了解说明。</remarks>
        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            this.UpdateTeams();
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            this.UpdateTeams();
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            this.UpdateTeams();
        }

        #endregion

        [Obsolete("do not call this.")]
        public void UpdateTeams()
        {
            Array enumVals = Enum.GetValues(typeof(Team));
            foreach (var enumVal in enumVals)
            {
                PlayersPerTeam[(Team)enumVal].Clear();
            }

            for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
            {
                Player player = PhotonNetwork.PlayerList[i];
                Team playerTeam = player.GetTeam();
                PlayersPerTeam[playerTeam].Add(player);
            }
        }
    }

    /// <summary>用于PunTeams和Player类的扩展方法。封装对玩家自定义属性的访问。</summary>
    public static class TeamExtensions
    {
        /// <summary>Player类的扩展方法，用于封装对玩家自定义属性的访问。</summary>
        /// <returns>如果（尚未）找到团队则返回PunTeam.Team.none。</returns>
        [Obsolete("Use player.GetPhotonTeam")]
        public static PunTeams.Team GetTeam(this Player player)
        {
            object teamId;
            if (player.CustomProperties.TryGetValue(PunTeams.TeamPlayerProp, out teamId))
            {
                return (PunTeams.Team)teamId;
            }

            return PunTeams.Team.none;
        }

        /// <summary>将该玩家的团队切换为你指定的团队。</summary>
        /// <remarks>内部检查此玩家是否已在该团队中。仅实际发送团队切换。</remarks>
        /// <param name="player"></param>
        /// <param name="team"></param>
        [Obsolete("Use player.JoinTeam")]
        public static void SetTeam(this Player player, PunTeams.Team team)
        {
            if (!PhotonNetwork.IsConnectedAndReady)
            {
                Debug.LogWarning("JoinTeam was called in state: " + PhotonNetwork.NetworkClientState + ". Not IsConnectedAndReady.");
                return;
            }

            PunTeams.Team currentTeam = player.GetTeam();
            if (currentTeam != team)
            {
                player.SetCustomProperties(new Hashtable() { { PunTeams.TeamPlayerProp, (byte)team } });
            }
        }
    }
}