// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CullingHandler.cs" company="Exit Games GmbH">
//   Part of: Photon Unity Utilities,
// </copyright>
// <summary>
//  处理网络剔除。
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;

using UnityEngine;

namespace Photon.Pun.UtilityScripts
{
    /// <summary>
    ///     处理网络剔除。
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class CullingHandler : MonoBehaviour, IPunObservable
    {
        #region VARIABLES

        private int orderIndex;

        private CullArea cullArea;

        private List<byte> previousActiveCells, activeCells;

        private PhotonView pView;

        private Vector3 lastPosition, currentPosition;


        // 用于限制每秒UpdateInterestGroups调用的次数（即使剔除算法看起来需要更频繁地更改组，也没有必要每秒更改超过几次）
        private float timeSinceUpdate;
        // 参见 timeSinceUpdate
        private float timeBetweenUpdatesMin = 0.33f;


        #endregion

        #region UNITY_FUNCTIONS

        /// <summary>
        ///     获取对PhotonView组件和剔除区域游戏对象的引用。
        /// </summary>
        private void OnEnable()
        {
            if (this.pView == null)
            {
                this.pView = GetComponent<PhotonView>();

                if (!this.pView.IsMine)
                {
                    return;
                }
            }

            if (this.cullArea == null)
            {
                this.cullArea = FindObjectOfType<CullArea>();
            }

            this.previousActiveCells = new List<byte>(0);
            this.activeCells = new List<byte>(0);

            this.currentPosition = this.lastPosition = transform.position;
        }

        /// <summary>
        ///     初始化正确的兴趣组或准备PhotonView组件兴趣组的永久更改。
        /// </summary>
        public void Start()
        {
            if (!this.pView.IsMine)
            {
                return;
            }

            if (PhotonNetwork.InRoom)
            {
                if (this.cullArea.NumberOfSubdivisions == 0)
                {
                    this.pView.Group = this.cullArea.FIRST_GROUP_ID;

                    PhotonNetwork.SetInterestGroups(this.cullArea.FIRST_GROUP_ID, true);
                }
                else
                {
                    // 这用于持续更新活动组。
                    this.pView.ObservedComponents.Add(this);
                }
            }
        }



        /// <summary>
        ///     检查玩家之前是否移动过，并在必要时更新兴趣组。
        /// </summary>
        public void Update()
        {
            if (!this.pView.IsMine)
            {
                return;
            }

            // 我们将限制此更新运行的频率（以避免过于频繁的更改和用SetInterestGroups调用淹没服务器）
            this.timeSinceUpdate += Time.deltaTime;
            if (this.timeSinceUpdate < this.timeBetweenUpdatesMin)
            {
                return;
            }

            this.lastPosition = this.currentPosition;
            this.currentPosition = transform.position;

            // 这是当前位置和先前位置的简单比较。
            // 在较大项目中使用网络剔除时，请记住可能
            // 还有更多与transform相关的选项，例如旋转或其他需要检查的选项。
            if (this.currentPosition != this.lastPosition)
            {
                if (this.HaveActiveCellsChanged())
                {
                    this.UpdateInterestGroups();
                    this.timeSinceUpdate = 0;
                }
            }
        }

        /// <summary>
        ///     绘制信息。
        /// </summary>
        private void OnGUI()
        {
            if (!this.pView.IsMine)
            {
                return;
            }

            string subscribedAndActiveCells = "Inside cells:\n";
            string subscribedCells = "Subscribed cells:\n";

            for (int index = 0; index < this.activeCells.Count; ++index)
            {
                if (index <= this.cullArea.NumberOfSubdivisions)
                {
                    subscribedAndActiveCells += this.activeCells[index] + " | ";
                }

                subscribedCells += this.activeCells[index] + " | ";
            }
            GUI.Label(new Rect(20.0f, Screen.height - 120.0f, 200.0f, 40.0f), "<color=white>PhotonView Group: " + this.pView.Group + "</color>", new GUIStyle() { alignment = TextAnchor.UpperLeft, fontSize = 16 });
            GUI.Label(new Rect(20.0f, Screen.height - 100.0f, 200.0f, 40.0f), "<color=white>" + subscribedAndActiveCells + "</color>", new GUIStyle() { alignment = TextAnchor.UpperLeft, fontSize = 16 });
            GUI.Label(new Rect(20.0f, Screen.height - 60.0f, 200.0f, 40.0f), "<color=white>" + subscribedCells + "</color>", new GUIStyle() { alignment = TextAnchor.UpperLeft, fontSize = 16 });
        }

        #endregion

        /// <summary>
        ///     检查先前活动的单元格是否已更改。
        /// </summary>
        /// <returns>如果先前活动的单元格已更改则返回True，否则返回false。</returns>
        private bool HaveActiveCellsChanged()
        {
            if (this.cullArea.NumberOfSubdivisions == 0)
            {
                return false;
            }

            this.previousActiveCells = new List<byte>(this.activeCells);
            this.activeCells = this.cullArea.GetActiveCells(transform.position);

            // 如果玩家离开区域，我们将整个区域本身插入为活动单元格。
            // 如果确定玩家无法离开该区域，则可以移除此操作。
            while (this.activeCells.Count <= this.cullArea.NumberOfSubdivisions)
            {
                this.activeCells.Add(this.cullArea.FIRST_GROUP_ID);
            }

            if (this.activeCells.Count != this.previousActiveCells.Count)
            {
                return true;
            }

            if (this.activeCells[this.cullArea.NumberOfSubdivisions] != this.previousActiveCells[this.cullArea.NumberOfSubdivisions])
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     取消订阅旧兴趣组并订阅新兴趣组。
        /// </summary>
        private void UpdateInterestGroups()
        {
            List<byte> disable = new List<byte>(0);

            foreach (byte groupId in this.previousActiveCells)
            {
                if (!this.activeCells.Contains(groupId))
                {
                    disable.Add(groupId);
                }
            }

            PhotonNetwork.SetInterestGroups(disable.ToArray(), this.activeCells.ToArray());
        }

        #region IPunObservable implementation

        /// <summary>
        ///     这次OnPhotonSerializeView不用于发送或接收任何类型的数据。
        ///     它用于更改PhotonView组件当前活动的组，使其更直接地与PUN协同工作。
        ///     请记住，仅当房间中至少还有一名其他玩家时，此函数才会执行。
        /// </summary>
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            // 如果玩家离开区域，我们将整个区域本身插入为活动单元格。
            // 如果确定玩家无法离开该区域，则可以移除此操作。
            while (this.activeCells.Count <= this.cullArea.NumberOfSubdivisions)
            {
                this.activeCells.Add(this.cullArea.FIRST_GROUP_ID);
            }

            if (this.cullArea.NumberOfSubdivisions == 1)
            {
                this.orderIndex = (++this.orderIndex % this.cullArea.SUBDIVISION_FIRST_LEVEL_ORDER.Length);
                this.pView.Group = this.activeCells[this.cullArea.SUBDIVISION_FIRST_LEVEL_ORDER[this.orderIndex]];
            }
            else if (this.cullArea.NumberOfSubdivisions == 2)
            {
                this.orderIndex = (++this.orderIndex % this.cullArea.SUBDIVISION_SECOND_LEVEL_ORDER.Length);
                this.pView.Group = this.activeCells[this.cullArea.SUBDIVISION_SECOND_LEVEL_ORDER[this.orderIndex]];
            }
            else if (this.cullArea.NumberOfSubdivisions == 3)
            {
                this.orderIndex = (++this.orderIndex % this.cullArea.SUBDIVISION_THIRD_LEVEL_ORDER.Length);
                this.pView.Group = this.activeCells[this.cullArea.SUBDIVISION_THIRD_LEVEL_ORDER[this.orderIndex]];
            }
        }

        #endregion
    }
}