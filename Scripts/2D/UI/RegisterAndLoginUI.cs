using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// �ļ�����UTF-8
/// </summary>
namespace LAB2D
{
    public class RegisterAndLoginUI : MonoBehaviour
    {
        /// <summary>
        /// �û���
        /// </summary>
        private InputField _username;
        /// <summary>
        /// ����
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
        /// ע��
        /// </summary>
        private void Onclick_Register()
        {
            string username = _username.text;
            string password = _password.text;
            if (username.Length < 3 || password.Length < 3) {
                GlobalInit.Instance.showTip("ע��ʧ��!!!");
                return;
            }
            // ��ȡ��������
            UserData data = Tool.loadDataByJson<UserData>(GlobalData.ConfigFile.UserDataFilePath);
            // �����Ƿ�����
            for (int i = 0; i < data.getLength(); i++)
            {
                if (data.getUsername(i) == username) {
                    GlobalInit.Instance.showTip("���û��Ѿ�ע��!!!");
                    return;
                }
            }
            data = new UserData();
            data.addData(username,password);
            File.WriteAllText(GlobalData.ConfigFile.UserDataFilePath,JsonUtility.ToJson(data));
            GlobalInit.Instance.showTip("ע��ɹ�!!!");
        }

        /// <summary>
        /// ��¼
        /// </summary>
        private void Onclick_Login()
        {
            UserData data = Tool.loadDataByJson<UserData>(GlobalData.ConfigFile.UserDataFilePath);
            for (int i = 0; i < data.getLength(); i++)
            {
                if (data.getUsername(i) == _username.text && data.getPassword(i) == _password.text)
                {
                    SceneManager.LoadScene("Menu"); // ���س���
                    return;
                }
            }
            GlobalInit.Instance.showTip("��¼ʧ��!!!");
        }
    }
}