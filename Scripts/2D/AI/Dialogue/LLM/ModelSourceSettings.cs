namespace LAB2D.AI.Dialogue.LLM
{
    using System;
    using System.Text;
    using UnityEngine;

    public enum ModelSource
    {
        Local,
        Remote,
    }

    public static class ModelSourceSettings
    {
        private const string KeySource = "ModelSource_Current";
        private const string KeyRemoteUrl = "ModelSource_RemoteApiBaseUrl";
        private const string KeyRemoteKey = "ModelSource_RemoteApiKey";
        private const string KeyRemoteModel = "ModelSource_RemoteModelName";
        private const string KeyDeepThinking = "ModelSource_DeepThinkingEnabled";

        public static ModelSource Current
        {
            get => (ModelSource)PlayerPrefs.GetInt(KeySource, 0);
            set => PlayerPrefs.SetInt(KeySource, (int)value);
        }

        public static string RemoteApiBaseUrl
        {
            get => PlayerPrefs.GetString(KeyRemoteUrl, LLMClientConfig.DEFAULT_REMOTE_API_BASE_URL);
            set => PlayerPrefs.SetString(KeyRemoteUrl, value);
        }

        public static string RemoteApiKey
        {
            get
            {
                string raw = PlayerPrefs.GetString(KeyRemoteKey, string.Empty);
                if (string.IsNullOrEmpty(raw))
                {
                    return string.Empty;
                }

                string decoded = TryDecodeBase64(raw);
                return decoded ?? raw;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    PlayerPrefs.SetString(KeyRemoteKey, string.Empty);
                }
                else
                {
                    string encoded = EncodeBase64(value);
                    PlayerPrefs.SetString(KeyRemoteKey, encoded);
                }
            }
        }

        public static string RemoteModelName
        {
            get => PlayerPrefs.GetString(KeyRemoteModel, LLMClientConfig.DEFAULT_REMOTE_MODEL);
            set => PlayerPrefs.SetString(KeyRemoteModel, value);
        }

        public static bool DeepThinkingEnabled
        {
            get => PlayerPrefs.GetInt(KeyDeepThinking, 0) == 1;
            set => PlayerPrefs.SetInt(KeyDeepThinking, value ? 1 : 0);
        }

        public static void Save()
        {
            PlayerPrefs.Save();
        }

        private static string EncodeBase64(string plain)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(plain));
        }

        private static string TryDecodeBase64(string value)
        {
            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(value));
            }
            catch
            {
                return null;
            }
        }
    }
}
