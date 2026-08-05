namespace LAB2D.Core
{
    using System;

    /// <summary>
    /// 地图初始化协调器 — 从 Lock.IsCompleteTileMap 提取的独立服务。
    /// 协程通过 WaitUntil 等待地图就绪，地图生成完成后设为 true。
    /// 订阅 OnMapReady 事件可在地图完成后立即收到回调。
    /// </summary>
    public sealed class MapInitCoordinator
    {
        private bool isComplete;

        /// <summary>
        /// 地图初始化完成事件。IsComplete 首次变为 true 时触发。
        /// 订阅方可在回调中安全访问 TileMapDataLAB。
        /// </summary>
        public event Action OnMapReady;

        /// <summary>
        /// 地图瓦片是否加载完成。TileMap/AchieveManager 设为 true，
        /// WaveManager/ResourceMap/EnemyManager 等协程等待。
        /// </summary>
        public bool IsComplete
        {
            get => this.isComplete;
            set
            {
                if (value && !this.isComplete)
                {
                    this.isComplete = true;
                    this.OnMapReady?.Invoke();
                }
                else
                {
                    this.isComplete = value;
                }
            }
        }
    }
}
