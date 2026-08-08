namespace LAB2D.UI.Panel
{
    using LAB2D.Enum;
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Worker任务信息面板
    /// </summary>
    public class WorkerTaskTogglePanel : ABasePanel<WorkerTaskTogglePanel>
    {
        /// <summary>
        /// Title 列头是否已生成（只需生成一次）
        /// </summary>
        private bool titleGenerated;

        /// <summary>
        /// WorkerTaskType 到中文名称的映射
        /// </summary>
        internal static readonly Dictionary<WorkerTaskType, string> TypeToChinese = new ()
        {
            { WorkerTaskType.Build, "建造" },
            { WorkerTaskType.Carry, "搬运" },
            { WorkerTaskType.Gather, "采摘" },
            { WorkerTaskType.Exercise, "锻炼" },
            { WorkerTaskType.Eat, "吃饭" },
            { WorkerTaskType.Wear, "穿戴" },
            { WorkerTaskType.Sleep, "睡觉" },
            { WorkerTaskType.Plant, "种植" },
            { WorkerTaskType.Demolish, "拆除" },
        };

        /// <summary>
        /// 任务类型列顺序 — Title 列头和 TaskItem 的 Toggle 列都按此顺序生成
        /// </summary>
        internal static readonly List<WorkerTaskType> TaskTypeOrder = new ()
        {
            WorkerTaskType.Build,
            WorkerTaskType.Carry,
            WorkerTaskType.Gather,
            WorkerTaskType.Exercise,
            WorkerTaskType.Eat,
            WorkerTaskType.Wear,
            WorkerTaskType.Sleep,
            WorkerTaskType.Plant,
            WorkerTaskType.Demolish,
        };

        public WorkerTaskTogglePanel()
        {
            this.Name = "WorkerTaskToggle";
            this.Init();
        }

        /// <summary>
        /// 动态生成 Title 行的列头文本。
        /// 保留第一个子对象（Worker 名称列头）作为模板，销毁其余旧子对象，
        /// 然后根据 TaskTypeOrder 动态创建新的列头。
        /// </summary>
        private void GenerateTitleColumns()
        {
            Transform title = LAB2D.Tool.Tool.GetComponentInChildren<Transform>(this.Panel, "Title");
            if (title == null || title.childCount == 0)
            {
                return;
            }

            // 保留第一个子对象（Worker 名称列头），第一列保持原宽度100
            // 销毁旧的任务类型列头（从末尾往前删，避免索引偏移问题）
            for (int i = title.childCount - 1; i > 0; i--)
            {
                Object.DestroyImmediate(title.GetChild(i).gameObject);
            }

            // 根据 TaskTypeOrder 动态创建新的列头，使用 TaskTitleItem 预制体
            foreach (WorkerTaskType taskType in TaskTypeOrder)
            {
                if (!TypeToChinese.ContainsKey(taskType))
                {
                    continue;
                }

                GameObject header = ServiceLocator.Get<ResourceManager>().Instantiate(
                    PrefabConstant.TASK_TITLE_ITEM, title, false);
                header.name = taskType.ToString();

                // 任务列宽度设为50
                RectTransform headerRect = header.GetComponent<RectTransform>();
                if (headerRect != null)
                {
                    headerRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 50f);
                }

                Text textComponent = header.GetComponentInChildren<Text>();
                if (textComponent != null)
                {
                    textComponent.text = TypeToChinese[taskType];
                }
            }
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();

            // 首次打开面板时动态生成 Title 列头（延迟到此时确保 ResourceManager 等服务已注册）
            if (!this.titleGenerated)
            {
                this.GenerateTitleColumns();
                this.titleGenerated = true;
            }
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            base.OnExit();
        }

        /// <inheritdoc/>
        public override void OnClick_Back()
        {
            this.Controller.Close();
        }
    }
}
