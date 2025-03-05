using NUnit.Framework.Internal;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    public class AIChatPanel : BasePanel<AIChatPanel>
    {
        public AIChatPanel()
        {
            Name = "AIChat";
            setPanel();
            Tool.GetComponentInChildren<Button>(panel, "Send").onClick.AddListener(OnClick_Send);
        }

        public void OnClick_Send()
        {
            AIChatUI.Instance.send();
        }
    }
}
