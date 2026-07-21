namespace LAB2D.UI.Panel
{
    using LAB2D;
    using LAB2D.AI.Dialogue.Core;
    using LAB2D.AI.Dialogue.LLM;
    using LAB2D.Character.Worker.Task;
    using UnityEngine;
    using UnityEngine.UI;

    public class LLMSettingsPanel : ABasePanel<LLMSettingsPanel>
    {
        private Dropdown modelSourceDropdown;
        private InputField apiUrlField;
        private InputField apiKeyField;
        private Toggle apiKeyToggle;
        private InputField modelNameField;
        private Toggle deepThinkingToggle;

        public LLMSettingsPanel()
        {
            this.Name = "LLMSettings";
            this.Init();
            if (this.Panel == null)
            {
                return;
            }

            this.modelSourceDropdown = LAB2D.Tool.Tool.GetComponentInChildren<Dropdown>(
                this.Panel, "ModelSource");
            if (this.modelSourceDropdown != null)
            {
                if (this.modelSourceDropdown.template == null)
                {
                    Transform tpl = this.modelSourceDropdown.transform.Find("Template");
                    if (tpl != null)
                    {
                        this.modelSourceDropdown.template = tpl as RectTransform;
                    }
                }

                if (this.modelSourceDropdown.captionText == null)
                {
                    this.modelSourceDropdown.captionText =
                        LAB2D.Tool.Tool.GetComponentInChildren<Text>(
                            this.modelSourceDropdown.gameObject, "Label");
                }

                if (this.modelSourceDropdown.itemText == null)
                {
                    Transform tpl = this.modelSourceDropdown.template;
                    if (tpl != null)
                    {
                        Transform itemLabel = tpl.Find("Viewport/Content/Item/Item Label");
                        if (itemLabel != null)
                        {
                            this.modelSourceDropdown.itemText = itemLabel.GetComponent<Text>();
                        }
                    }
                }
            }

            this.apiUrlField = LAB2D.Tool.Tool.GetComponentInChildren<InputField>(
                this.Panel, "ApiUrl");

            this.apiKeyField = LAB2D.Tool.Tool.GetComponentInChildren<InputField>(
                this.Panel, "ApiKey");

            this.apiKeyToggle = LAB2D.Tool.Tool.GetComponentInChildren<Toggle>(
                this.Panel, "ApiKeyToggle");
            if (this.apiKeyToggle != null)
            {
                this.apiKeyToggle.onValueChanged.AddListener(this.OnApiKeyToggleChanged);
            }

            this.modelNameField = LAB2D.Tool.Tool.GetComponentInChildren<InputField>(
                this.Panel, "ModelName");

            this.deepThinkingToggle = LAB2D.Tool.Tool.GetComponentInChildren<Toggle>(
                this.Panel, "DeepThinking");

            Button backBtn = LAB2D.Tool.Tool.GetComponentInChildren<Button>(
                this.Panel, "BackBtn");
            if (backBtn != null)
            {
                backBtn.onClick.AddListener(this.OnClick_Back);
            }

            Button saveBtn = LAB2D.Tool.Tool.GetComponentInChildren<Button>(
                this.Panel, "SaveBtn");
            if (saveBtn != null)
            {
                saveBtn.onClick.AddListener(this.OnClick_Save);
            }
        }

        public override void OnEnter()
        {
            base.OnEnter();
            this.PopulateFromSettings();
        }

        public override void OnClick_Back()
        {
            this.Controller.Close();
        }

        private void OnClick_Save()
        {
            this.SaveToSettings();
            DialogueManager.Instance.ResetLLMClient();
            AWorkerTask.LogProvider("LLM 模型设置已保存", LogManager.LogLevelEnum.Info);
            this.Controller.Close();
        }

        private void OnApiKeyToggleChanged(bool isOn)
        {
            if (this.apiKeyField == null)
            {
                return;
            }

            this.apiKeyField.contentType = isOn
                ? InputField.ContentType.Standard
                : InputField.ContentType.Password;
            this.apiKeyField.ForceLabelUpdate();
        }

        private void PopulateFromSettings()
        {
            if (this.modelSourceDropdown != null)
            {
                this.modelSourceDropdown.value = (int)ModelSourceSettings.Current;
            }

            if (this.apiUrlField != null)
            {
                this.apiUrlField.text = ModelSourceSettings.RemoteApiBaseUrl;
            }

            if (this.apiKeyField != null)
            {
                this.apiKeyField.text = ModelSourceSettings.RemoteApiKey;
                this.apiKeyField.contentType = InputField.ContentType.Password;
            }

            if (this.modelNameField != null)
            {
                this.modelNameField.text = ModelSourceSettings.RemoteModelName;
            }

            if (this.deepThinkingToggle != null)
            {
                this.deepThinkingToggle.isOn = ModelSourceSettings.DeepThinkingEnabled;
            }

            if (this.apiKeyToggle != null)
            {
                this.apiKeyToggle.isOn = false;
            }
        }

        private void SaveToSettings()
        {
            if (this.modelSourceDropdown != null)
            {
                ModelSourceSettings.Current = (ModelSource)this.modelSourceDropdown.value;
            }

            if (this.apiUrlField != null)
            {
                ModelSourceSettings.RemoteApiBaseUrl = this.apiUrlField.text;
            }

            if (this.apiKeyField != null)
            {
                ModelSourceSettings.RemoteApiKey = this.apiKeyField.text;
            }

            if (this.modelNameField != null)
            {
                ModelSourceSettings.RemoteModelName = this.modelNameField.text;
            }

            if (this.deepThinkingToggle != null)
            {
                ModelSourceSettings.DeepThinkingEnabled = this.deepThinkingToggle.isOn;
            }

            ModelSourceSettings.Save();
        }
    }
}
