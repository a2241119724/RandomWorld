namespace LAB2D.Character
{
    using LAB2D;
    using LAB2D.Domain.Common;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 角色伤害 UI 呈现器 — 从 Character.ReduceHp() 提取的 UI 职责。
    /// 通过 EventBus 订阅 CharacterDamagedEvent，负责伤害数字、浮动文字等视觉表现。
    /// 分离本类后，Character 不再依赖 ResourceManager 或 FloatingTextManager。
    /// </summary>
    public sealed class CharacterDamageUIPresenter : MonoBehaviour
    {
        private readonly Queue<CharacterDamagedEvent> pendingEvents = new Queue<CharacterDamagedEvent>();

        public void Awake()
        {
            EventBus.Instance.Subscribe<CharacterDamagedEvent>(this.OnCharacterDamaged);
        }

        public void OnDestroy()
        {
            EventBus.Instance.Unsubscribe<CharacterDamagedEvent>(this.OnCharacterDamaged);
        }

        public void LateUpdate()
        {
            while (this.pendingEvents.Count > 0)
            {
                CharacterDamagedEvent e = this.pendingEvents.Dequeue();
                this.PresentDamageUI(e);
            }
        }

        private void OnCharacterDamaged(CharacterDamagedEvent e)
        {
            this.pendingEvents.Enqueue(e);
        }

        private void PresentDamageUI(CharacterDamagedEvent e)
        {
            Vector3 worldPos = new Vector3(e.WorldPosX, e.WorldPosY, 0f);

            GameObject g = ResourceManager.Instance.Instantiate(PrefabConstant.DAMAGE);
            if (g != null)
            {
                g.GetComponent<DamageUI>().SetDamage(e.Damage, System.Convert.ToInt32(e.IsCritical));
                g.transform.SetParent(e.TargetTransform);
                g.transform.localPosition = Vector3.zero;
            }

            FloatingTextManager.Instance.SpawnDamageText(worldPos, e.Damage, e.IsCritical, e.IsCombo);
        }
    }
}
