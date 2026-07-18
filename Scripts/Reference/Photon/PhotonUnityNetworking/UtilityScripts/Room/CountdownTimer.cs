// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CountdownTimer.cs" company="Exit Games GmbH">
//   Part of: Photon Unity Utilities,
// </copyright>
// <summary>
// 这是一个基本的CountdownTimer。要启动计时器，MasterClient可以向自定义房间属性中添加一个特定的条目，
// 其中包含属性名称'StartTime'和描述计时器启动时刻的实际开始时间。
// 要拥有同步的计时器，最佳实践是使用PhotonNetwork.Time。
// 要订阅CountdownTimerHasExpired事件，你可以调用CountdownTimer.OnCountdownTimerHasExpired += OnCountdownTimerIsExpired;
// 例如在Unity的OnEnable函数中。取消订阅只需调用CountdownTimer.OnCountdownTimerHasExpired -= OnCountdownTimerIsExpired;。
// 你可以在例如Unity的OnDisable函数中执行此操作。
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

using ExitGames.Client.Photon;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

namespace Photon.Pun.UtilityScripts
{
    /// <summary>这是一个基于属性的基本网络同步CountdownTimer。</summary>
    /// <remarks>
    /// 要启动计时器，MasterClient可以调用SetStartTime()来设置开始时间戳。
    /// 属性'StartTime'包含计时器启动时的服务器时间戳。
    ///
    /// 要订阅CountdownTimerHasExpired事件，你可以调用CountdownTimer.OnCountdownTimerHasExpired
    /// += OnCountdownTimerIsExpired;
    /// 例如在Unity的OnEnable函数中。取消订阅只需调用CountdownTimer.OnCountdownTimerHasExpired
    /// -= OnCountdownTimerIsExpired;。
    ///
    /// 你可以在Unity的OnEnable和OnDisable函数中执行此操作。
    /// </remarks>
    public class CountdownTimer : MonoBehaviourPunCallbacks
    {
        /// <summary>
        ///     OnCountdownTimerHasExpired委托。
        /// </summary>
        public delegate void CountdownTimerHasExpired();

        public const string CountdownStartTime = "StartTime";

        [Header("Countdown time in seconds")]
        public float Countdown = 5.0f;

        private bool isTimerRunning;

        private int startTime;

        [Header("Reference to a Text component for visualizing the countdown")]
        public Text Text;


        /// <summary>
        ///     当计时器到期时调用。
        /// </summary>
        public static event CountdownTimerHasExpired OnCountdownTimerHasExpired;


        public void Start()
        {
            if (this.Text == null) Debug.LogError("Reference to 'Text' is not set. Please set a valid reference.", this);
        }

        public override void OnEnable()
        {
            Debug.Log("OnEnable CountdownTimer");
            base.OnEnable();

            // 开始时间可能已在属性中。查找它。
            Initialize();
        }

        public override void OnDisable()
        {
            base.OnDisable();
            Debug.Log("OnDisable CountdownTimer");
        }


        public void Update()
        {
            if (!this.isTimerRunning) return;

            float countdown = TimeRemaining();
            this.Text.text = string.Format("Game starts in {0} seconds", countdown.ToString("n0"));

            if (countdown > 0.0f) return;

            OnTimerEnds();
        }


        private void OnTimerRuns()
        {
            this.isTimerRunning = true;
            this.enabled = true;
        }

        private void OnTimerEnds()
        {
            this.isTimerRunning = false;
            this.enabled = false;

            Debug.Log("Emptying info text.", this.Text);
            this.Text.text = string.Empty;

            if (OnCountdownTimerHasExpired != null) OnCountdownTimerHasExpired();
        }


        public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            Debug.Log("CountdownTimer.OnRoomPropertiesUpdate " + propertiesThatChanged.ToStringFull());
            Initialize();
        }


        private void Initialize()
        {
            int propStartTime;
            if (TryGetStartTime(out propStartTime))
            {
                this.startTime = propStartTime;
                Debug.Log("Initialize sets StartTime " + this.startTime + " server time now: " + PhotonNetwork.ServerTimestamp + " remain: " + TimeRemaining());


                this.isTimerRunning = TimeRemaining() > 0;

                if (this.isTimerRunning)
                    OnTimerRuns();
                else
                    OnTimerEnds();
            }
        }


        private float TimeRemaining()
        {
            int timer = PhotonNetwork.ServerTimestamp - this.startTime;
            return this.Countdown - timer / 1000f;
        }


        public static bool TryGetStartTime(out int startTimestamp)
        {
            startTimestamp = PhotonNetwork.ServerTimestamp;

            object startTimeFromProps;
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(CountdownStartTime, out startTimeFromProps))
            {
                startTimestamp = (int)startTimeFromProps;
                return true;
            }

            return false;
        }


        public static void SetStartTime()
        {
            int startTime = 0;
            bool wasSet = TryGetStartTime(out startTime);

            Hashtable props = new Hashtable
            {
                {CountdownTimer.CountdownStartTime, (int)PhotonNetwork.ServerTimestamp}
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);


            Debug.Log("Set Custom Props for Time: " + props.ToStringFull() + " wasSet: " + wasSet);
        }
    }
}