// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PunPlayerScores.cs" company="Exit Games GmbH">
//   Part of: Photon Unity Utilities,
// </copyright>
// <summary>
//  PhotonPlayer的计分系统
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

using Photon.Realtime;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Photon.Pun.UtilityScripts
{
    /// <summary>
    /// PhotonPlayer的计分系统
    /// </summary>
    public class PunPlayerScores : MonoBehaviour
    {
        public const string PlayerScoreProp = "score";
    }

    public static class ScoreExtensions
    {
        public static void SetScore(this Player player, int newScore)
        {
            Hashtable score = new Hashtable();  // 使用PUN的Hashtable实现
            score[PunPlayerScores.PlayerScoreProp] = newScore;

            player.SetCustomProperties(score);  // 这会在本地设置分数并在游戏中尽快同步。
        }

        public static void AddScore(this Player player, int scoreToAddToCurrent)
        {
            int current = player.GetScore();
            current = current + scoreToAddToCurrent;

            Hashtable score = new Hashtable();  // 使用PUN的Hashtable实现
            score[PunPlayerScores.PlayerScoreProp] = current;

            player.SetCustomProperties(score);  // 这会在本地设置分数并在游戏中尽快同步。
        }

        public static int GetScore(this Player player)
        {
            object score;
            if (player.CustomProperties.TryGetValue(PunPlayerScores.PlayerScoreProp, out score))
            {
                return (int)score;
            }

            return 0;
        }
    }
}