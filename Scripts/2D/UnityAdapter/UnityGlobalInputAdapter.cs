namespace LAB2D.UnityAdapter
{
    using LAB2D;
    using LAB2D.Domain.Common;
    using UnityEngine;

    /// <summary>
    /// Converts Unity input state into global gameplay input decisions.
    /// </summary>
    public static class UnityGlobalInputAdapter
    {
        /// <summary>
        /// Esc 已被消费的帧号（-1=无）。对话面板聚焦态 Esc 补位关闭后标记——关闭动作本身
        /// 会清输入框 selection（IsUIInputActive 翻转），若不消费，同帧稍后的全局 Esc 分发
        /// 会放行并在栈空时误开 BuildPanel（GlobalInputProcessor.ProcessCloseOrBuildMenu）。
        /// </summary>
        private static int closeMenuKeyConsumedFrame = -1;

        /// <summary>
        /// 声明本帧 Esc 已被消费（如对话面板补位关闭），所有 GetCloseOrBuildMenuDown 监听同帧跳过。
        /// </summary>
        public static void ConsumeCloseMenuKey()
        {
            closeMenuKeyConsumedFrame = Time.frameCount;
        }

        public static bool GetToggleWorkerTaskAndAchievementHudDown()
        {
            return !LAB2D.Tool.Tool.IsUIInputActive() &&
                Input.GetKeyDown(InputKeyConstant.ToggleWorkerTaskAndAchievementHud);
        }

        public static bool GetToggleColonyCommandCenterHudDown()
        {
            return Input.GetKeyDown(InputKeyConstant.ToggleColonyCommandCenterHud);
        }

        public static bool GetCloseOrBuildMenuDown()
        {
            return !LAB2D.Tool.Tool.IsUIInputActive() &&
                Time.frameCount != closeMenuKeyConsumedFrame &&
                Input.GetKeyDown(InputKeyConstant.CloseOrBuildMenu);
        }

        public static bool GetItemInfoCloseClickDown()
        {
            if (LAB2D.Tool.Tool.IsUIInputActive())
            {
                return false;
            }

            return Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(2);
        }

        public static bool GetNpcDialogueInteractDown(KeyCode interactKey)
        {
            return !LAB2D.Tool.Tool.IsUIInputActive() &&
                Input.GetKeyDown(interactKey);
        }

        public static bool GetDialogueSubmitDown()
        {
            return Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter);
        }

        public static bool CanUseHudHotkey()
        {
            try
            {
                return !LAB2D.Tool.Tool.IsUIInputActive();
            }
            catch (System.Exception)
            {
                return true;
            }
        }

        public static bool GetHudToggleDown(KeyCode toggleKey)
        {
            return CanUseHudHotkey() && Input.GetKeyDown(toggleKey);
        }

        public static bool GetShowTileInfoReleased()
        {
            return !LAB2D.Tool.Tool.IsUIInputActive() &&
                Input.GetKeyUp(InputKeyConstant.ShowTileInfo);
        }

        public static bool GetShowTileInfoHeld()
        {
            return !LAB2D.Tool.Tool.IsUIInputActive() &&
                Input.GetKey(InputKeyConstant.ShowTileInfo);
        }

        public static Vector3 GetMouseWorldPosition(Camera camera)
        {
            return camera.ScreenToWorldPoint(Input.mousePosition);
        }

        public static Vector3 GetMouseScreenPosition()
        {
            return Input.mousePosition;
        }

        public static float GetMouseScrollDeltaY()
        {
            return Input.mouseScrollDelta.y;
        }

        public static bool TryGetWaveBossRewardOptionDown(out int optionIndex)
        {
            optionIndex = -1;
            if (LAB2D.Tool.Tool.IsUIInputActive())
            {
                return false;
            }

            if (Input.GetKeyDown(WaveBossRewardConstant.RewardOptionOneKey))
            {
                optionIndex = 0;
                return true;
            }

            if (Input.GetKeyDown(WaveBossRewardConstant.RewardOptionTwoKey))
            {
                optionIndex = 1;
                return true;
            }

            if (Input.GetKeyDown(WaveBossRewardConstant.RewardOptionThreeKey))
            {
                optionIndex = 2;
                return true;
            }

            return false;
        }

        public static bool GetEquipmentPanelToggleDown(bool isPanelVisible)
        {
            if (!Input.GetKeyDown(EquipmentLootConstant.EquipmentPanelToggleKey))
            {
                return false;
            }

            return isPanelVisible || !LAB2D.Tool.Tool.IsUIInputActive();
        }

        public static bool TryGetToolMenuIndexDown(KeyCode[] keyCodes, int menuCount, out int menuIndex)
        {
            menuIndex = -1;
            if (LAB2D.Tool.Tool.IsUIInputActive() || !Input.anyKeyDown)
            {
                return false;
            }

            int count = System.Math.Min(menuCount, keyCodes == null ? 0 : keyCodes.Length);
            for (int i = 0; i < count; i++)
            {
                if (Input.GetKeyDown(keyCodes[i]))
                {
                    menuIndex = i;
                    return true;
                }
            }

            return false;
        }

        public static bool GetSecondaryMouseDown()
        {
            return Input.GetMouseButtonDown(1);
        }

        public static bool GetPrimaryMouseDown()
        {
            return Input.GetMouseButtonDown(0);
        }

        public static bool GetPrimaryMouseUp()
        {
            return Input.GetMouseButtonUp(0);
        }

        public static bool GetMiddleMouseDown()
        {
            return Input.GetMouseButtonDown(2);
        }

        public static bool GetMiddleMouseUp()
        {
            return Input.GetMouseButtonUp(2);
        }

        public static bool GetWorkerBedDismissDown()
        {
            return !LAB2D.Tool.Tool.IsUIInputActive() &&
                (Input.GetMouseButtonDown(0) ||
                Input.GetMouseButtonDown(2) ||
                Input.GetKeyDown(InputKeyConstant.CloseOrBuildMenu));
        }
    }
}
