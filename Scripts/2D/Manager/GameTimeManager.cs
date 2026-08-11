namespace LAB2D
{
    using LAB2D.Core;
    using LAB2D.Data;
    using System;

    /// <summary>
    /// 游戏时间管理器 — 持久化游戏内时间（天/时/分）。
    /// GameTimeUI 从此 Manager 读取时间并驱动昼夜/天气逻辑。
    /// </summary>
    public class GameTimeManager : ASingletonSaveData<GameTimeManager>
    {
        /// <summary>
        /// 累计真实游戏时间（秒）。GameTimeUI 按 GameDayTime 换算为游戏内天数。
        /// </summary>
        public double CurGameTime { get; set; }

        /// <inheritdoc/>
        public override void SaveData()
        {
            base.SaveData();
            GameTimeData data = new GameTimeData { CurGameTime = this.CurGameTime };
            DataTool.SaveDataByBinary(GlobalData.ConfigFile.GetPath(this.GetType().Name), data);
        }

        /// <inheritdoc/>
        public override void LoadData()
        {
            base.LoadData();
            GameTimeData data = DataTool.LoadDataByBinary<GameTimeData>(GlobalData.ConfigFile.GetPath(this.GetType().Name));
            this.CurGameTime = data?.CurGameTime ?? 0.0;
        }

        [Serializable]
        private class GameTimeData
        {
            public double CurGameTime;
        }
    }
}
