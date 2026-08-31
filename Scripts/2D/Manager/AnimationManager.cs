namespace LAB2D.Manager
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 动画管理器：启动时从 Resources/Animation 加载全部 AnimatorController，
    /// 按资产名（如物品英文名 Name）提供查询。
    /// 约定：每个动画物品一个 controller，资产名 == ItemData.Name，内含对应 clip 的默认状态
    /// （Editor 右键「为选中动画生成 Controller」一键生成）。
    /// </summary>
    public class AnimationManager : Singleton<AnimationManager>
    {
        private readonly Dictionary<string, RuntimeAnimatorController> controllerDic;

        public AnimationManager()
        {
            this.controllerDic = ResourceTool.LoadResources<RuntimeAnimatorController>(ResourceConstant.ANIMATION_ROOT);
            AWorkerTask.LogProvider(
                $"[AnimationManager] Loaded {this.controllerDic.Count} animator controllers from {ResourceConstant.ANIMATION_ROOT}",
                LogManager.LogLevelEnum.Debug);
        }

        /// <summary>
        /// 按名称获取 AnimatorController。
        /// </summary>
        /// <param name="name">controller 资产名（如物品英文名 Name）。</param>
        /// <returns>controller；未找到时 null 并记 Warning。</returns>
        public RuntimeAnimatorController GetController(string name)
        {
            if (this.controllerDic.TryGetValue(name, out RuntimeAnimatorController controller))
            {
                return controller;
            }

            AWorkerTask.LogProvider($"{name} animator controller not found!!!", LogManager.LogLevelEnum.Warning);
            return null;
        }
    }
}
