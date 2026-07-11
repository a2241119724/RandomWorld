namespace LAB2D.UI.Panel.PanelUI
{
    using LAB2D;
    using System.IO;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 注册与登录UI
    /// </summary>
    public class RegisterAndLoginUI : MonoBehaviour
    {
        private InputField username; // 用户名
        private InputField password; // 密码

        public void Start()
        {
            this.username = Tool.GetComponentInChildren<InputField>(this.gameObject, "Username");
            this.password = Tool.GetComponentInChildren<InputField>(this.gameObject, "Password");
            Tool.GetComponentInChildren<Button>(this.gameObject, "Register").onClick.AddListener(this.Onclick_Register);
            Tool.GetComponentInChildren<Button>(this.gameObject, "Login").onClick.AddListener(this.Onclick_Login);
        }

        /// <summary>
        /// 注册
        /// </summary>
        private void Onclick_Register()
        {
            string username = this.username.text;
            string password = this.password.text;
            if (username.Length < 3 || password.Length < 3)
            {
                GlobalInit.Instance.ShowTip("注册失败!!!");
                return;
            }

            // 读取所有数据
            UserData data = DataTool.LoadDataByJson<UserData>(GlobalData.ConfigFile.UserDataFilePath);

            // 遍历是否重名
            if (data != null)
            {
                for (int i = 0; i < data.GetLength(); i++)
                {
                    if (data.GetUsername(i) == username)
                    {
                        GlobalInit.Instance.ShowTip("该用户已经注册!!!");
                        return;
                    }
                }
            }

            data = new UserData();
            data.AddData(username, password);
            File.WriteAllText(GlobalData.ConfigFile.UserDataFilePath, JsonUtility.ToJson(data));
            GlobalInit.Instance.ShowTip("注册成功!!!");
        }

        /// <summary>
        /// 登录
        /// </summary>
        private void Onclick_Login()
        {
            UserData data = DataTool.LoadDataByJson<UserData>(GlobalData.ConfigFile.UserDataFilePath);
            if (data != null)
            {
                for (int i = 0; i < data.GetLength(); i++)
                {
                    if (data.GetUsername(i) == this.username.text && data.GetPassword(i) == this.password.text)
                    {
                        Tool.LoadScene("Menu");
                        return;
                    }
                }
            }

            GlobalInit.Instance.ShowTip("登录失败!!!");
        }
    }
}