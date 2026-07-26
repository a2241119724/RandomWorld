namespace LAB2D.Data
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 让JsonUtility可以识别
    /// </summary>
    [Serializable]
    public class UserData
    {
        /// <summary>
        /// 数据
        /// </summary>
        public List<Row> Data;

        public UserData()
        {
            this.Data = new List<Row>();
            if (this.Data == null)
            {
                AWorkerTask.LogProvider("data assign resource Error!!!", LogManager.LogLevelEnum.Error);
                return;
            }
        }

        /// <summary>
        /// 添加数据
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        public void AddData(string username, string password)
        {
            this.Data.Add(new Row(username, password));
        }

        /// <summary>
        /// 获取数据数量
        /// </summary>
        /// <returns>数量</returns>
        public int GetLength()
        {
            return this.Data.Count;
        }

        /// <summary>
        /// 通过索引获取用户名
        /// </summary>
        /// <param name="index">索引</param>
        /// <returns>用户名</returns>
        public string GetUsername(int index)
        {
            if (index < 0 || index >= this.Data.Count)
            {
                return string.Empty;
            }

            return this.Data[index].Username;
        }

        /// <summary>
        /// 通过索引获取密码
        /// </summary>
        /// <param name="index">索引</param>
        /// <returns>密码</returns>
        public string GetPassword(int index)
        {
            if (index < 0 || index >= this.Data.Count)
            {
                return string.Empty;
            }

            return this.Data[index].Password;
        }

        /// <summary>
        /// 每个用户数据
        /// </summary>
        [Serializable]
        public class Row
        {
            /// <summary>
            /// 用户名
            /// </summary>
            public string Username;

            /// <summary>
            /// 密码
            /// </summary>
            public string Password;

            public Row(string username, string password)
            {
                this.Username = username;
                this.Password = password;
            }
        }
    }
}