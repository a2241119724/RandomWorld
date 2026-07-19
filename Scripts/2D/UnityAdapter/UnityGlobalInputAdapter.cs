namespace LAB2D.UnityAdapter
{
    using LAB2D;
    using UnityEngine;

    /// <summary>
    /// Converts Unity input state into global gameplay input decisions.
    /// </summary>
    public static class UnityGlobalInputAdapter
    {
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
    }
}
