using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 文件编码UTF-8
/// </summary>
namespace LAB2D
{
    public class RegisterAndLoginUI : MonoBehaviour
    {
        /// <summary>
        /// 用户名
        /// </summary>
        private InputField _username;
        /// <summary>
        /// 密码
        /// </summary>
        private InputField _password;

        void Start()
        {
            _username = Tool.GetComponentInChildren<InputField>(gameObject, "Username");
            _password = Tool.GetComponentInChildren<InputField>(gameObject, "Password");
            Tool.GetComponentInChildren<Button>(gameObject, "Register").onClick.AddListener(Onclick_Register);
            Tool.GetComponentInChildren<Button>(gameObject, "Login").onClick.AddListener(Onclick_Login);
        }

        /// <summary>
        /// 注册
        /// </summary>
        private void Onclick_Register()
        {
            string username = _username.text;
            string password = _password.text;
            if (username.Length < 3 || password.Length < 3) {
                GlobalInit.Instance.showTip("注册失败!!!");
                return;
            }
            // 读取所有数据
            UserData data = Tool.loadDataByJson<UserData>(GlobalData.ConfigFile.UserDataFilePath);
            // 遍历是否重名
            for (int i = 0; i < data.getLength(); i++)
            {
                if (data.getUsername(i) == username) {
                    GlobalInit.Instance.showTip("该用户已经注册!!!");
                    return;
                }
            }
            data = new UserData();
            data.addData(username,password);
            File.WriteAllText(GlobalData.ConfigFile.UserDataFilePath,JsonUtility.ToJson(data));
            GlobalInit.Instance.showTip("注册成功!!!");
        }

        /// <summary>
        /// 登录
        /// </summary>
        private void Onclick_Login()
        {
            UserData data = Tool.loadDataByJson<UserData>(GlobalData.ConfigFile.UserDataFilePath);
            for (int i = 0; i < data.getLength(); i++)
            {
                if (data.getUsername(i) == _username.text && data.getPassword(i) == _password.text)
                {
                    SceneManager.LoadScene("Menu"); // 加载场景
                    return;
                }
            }
            GlobalInit.Instance.showTip("登录失败!!!");
        }
    }
}