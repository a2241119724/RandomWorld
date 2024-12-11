using System;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    /// <summary>
    /// 让JsonUtility可以识别
    /// </summary>
    [Serializable]
    public class UserData
    {
        public List<Row> data;

        public UserData()
        {
            data = new List<Row>();
            if (data == null)
            {
                Debug.LogError("data assign resource Error!!!");
                return;
            }
        }

        public void addData(string username, string password)
        {
            data.Add(new Row(username, password));
        }

        public int getLength()
        {
            return data.Count;
        }

        public string getUsername(int index)
        {
            if (index < 0 || index >= data.Count) return "";
            return data[index].username;
        }

        public string getPassword(int index)
        {
            if (index < 0 || index >= data.Count) return "";
            return data[index].password;
        }

        [Serializable]
        public class Row
        {
            public string username;
            public string password;

            public Row(string username, string password)
            {
                this.username = username;
                this.password = password;
            }
        }
    }
}