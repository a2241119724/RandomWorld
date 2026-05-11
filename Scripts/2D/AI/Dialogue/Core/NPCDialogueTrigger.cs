namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// NPC 对话触发器，挂载在 NPC GameObject 上，玩家靠近按 T 键开始对话
    /// </summary>
    public class NPCDialogueTrigger : MonoBehaviour
    {
        /// <summary>
        /// NPC 配置名称（对应 Resources/SO/AI/ 下的 NPCPromptProfile 资源名）
        /// </summary>
        public string profileName = "Worker";

        /// <summary>
        /// 交互距离
        /// </summary>
        public float interactionRadius = 3f;

        /// <summary>
        /// 交互按键
        /// </summary>
        public KeyCode interactKey = KeyCode.T;

        private NPCPromptProfile cachedProfile;
        private string npcId;
        private bool isDialogueOpen;

        /// <summary>
        /// 设置 NPC 配置（运行时调用）
        /// </summary>
        public void SetProfile(string name)
        {
            this.profileName = name;
            this.cachedProfile = PromptBuilder.Instance.GetProfile(name);
        }

        /// <summary>
        /// 设置 NPC 配置
        /// </summary>
        public void SetProfile(NPCPromptProfile profile)
        {
            this.cachedProfile = profile;
            if (profile != null)
            {
                this.profileName = profile.name;
            }
        }

        public void Awake()
        {
            this.npcId = this.GetInstanceID().ToString();
        }

        public void Start()
        {
            if (this.cachedProfile == null)
            {
                this.cachedProfile = PromptBuilder.Instance.GetProfile(this.profileName);
            }

            if (this.cachedProfile == null)
            {
                LogManager.Instance.Log(
                    "NPCDialogueTrigger: 未找到 NPC 配置 " + this.profileName,
                    LogManager.LogLevelEnum.Warning);
            }
        }

        public void Update()
        {
            if (Tool.IsUIInputActive())
            {
                return;
            }

            if (!Input.GetKeyDown(this.interactKey))
            {
                return;
            }

            if (this.isDialogueOpen)
            {
                return;
            }

            Player player = PlayerManager.Instance?.Mine;
            if (player == null)
            {
                return;
            }

            float sqrDist = (this.transform.position - player.transform.position).sqrMagnitude;
            if (sqrDist > this.interactionRadius * this.interactionRadius)
            {
                return;
            }

            if (this.cachedProfile == null)
            {
                this.cachedProfile = PromptBuilder.Instance.GetProfile(this.profileName);
            }

            this.StartDialogue();
        }

        public void OnDestroy()
        {
            DialogueManager.Instance.OnDialogueEnded -= this.OnDialogueEndedHandler;
        }

        private void StartDialogue()
        {
            this.isDialogueOpen = true;

            // 回退：如果未找到配置，创建默认配置
            if (this.cachedProfile == null)
            {
                this.cachedProfile = ScriptableObject.CreateInstance<NPCPromptProfile>();
                this.cachedProfile.npcName = this.gameObject.name;
                this.cachedProfile.npcRole = "村民";
                this.cachedProfile.personalityDescription = "友善的村民";
                this.cachedProfile.speakingStyle = "说话简洁";
            }

            // 用 GameObject 名称覆盖 profile 中的 NPC 名称
            if (!string.IsNullOrEmpty(this.gameObject.name))
            {
                this.cachedProfile.npcName = this.gameObject.name;
            }

            DialogueManager.Instance.StartDialogue(this.npcId, this.cachedProfile);
            DialoguePanelUI.Ensure().Open(this.npcId, this.cachedProfile);
            PanelController.Instance.Show(DialoguePanel.Instance);

            DialogueManager.Instance.OnDialogueEnded += this.OnDialogueEndedHandler;
        }

        private void OnDialogueEndedHandler(string npcId)
        {
            if (npcId == this.npcId)
            {
                this.isDialogueOpen = false;
                DialogueManager.Instance.OnDialogueEnded -= this.OnDialogueEndedHandler;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(this.transform.position, this.interactionRadius);
        }
#endif
    }
}
