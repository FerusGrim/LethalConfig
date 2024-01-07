using System;
using LethalConfig.ConfigItems;
using LethalConfig.MonoBehaviours.Managers;
using LethalConfig.Utils;
using System.Collections;
using System.Collections.Generic;
using LethalConfig.ConfigItems.Options;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LethalConfig.MonoBehaviours.Components
{
    internal abstract class ModConfigController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private readonly List<Selectable> _selectables = new();
        
        protected BaseConfigItem baseConfigItem;

        public delegate void OnHoverHandler();
        public event OnHoverHandler OnHoverEnter;
        public event OnHoverHandler OnHoverExit;

        protected bool isOnSetup = false;

        public TextMeshProUGUI nameTextComponent;
        public TooltipTrigger tooltipTrigger;

        protected virtual void Awake()
        {
            tooltipTrigger.enabled = false;
            
            if (_selectables.Count > 0)
                return;

            _selectables.AddRange(GetComponentsInChildren<Selectable>());
        }

        public virtual bool SetConfigItem(BaseConfigItem configItem)
        {
            this.baseConfigItem = configItem;
            isOnSetup = true;
            this.OnSetConfigItem();
            isOnSetup = false;

            if (baseConfigItem.Options.CanModifyCallback is not null)
                tooltipTrigger.tooltipText = $"<b>Modifying this entry is currently disabled:</b>\n<b>{CanModify.Reason}</b>";
            
            return true;
        }

        protected CanModifyResult CanModify
        {
            get
            {
                if (baseConfigItem.Options.CanModifyCallback is null)
                    return true;

                return baseConfigItem.Options.CanModifyCallback.Invoke();
            }
        }

        protected abstract void OnSetConfigItem();
        public virtual void UpdateAppearance()
        {
            nameTextComponent.text = $"{(baseConfigItem.HasValueChanged ? "* " : "")}{baseConfigItem.Name}";

            var canModify = CanModify;
            foreach (var selectable in _selectables)
                selectable.interactable = canModify;

            tooltipTrigger.enabled = !canModify;
        }

        public virtual void ResetToDefault()
        {
            ConfigMenuManager.Instance.menuAudio.PlayConfirmSFX();
            baseConfigItem.ChangeToDefault();
            UpdateAppearance();
        }

        public virtual string GetDescription()
        {
            var description = $"<b>{baseConfigItem.Name}</b>";
            
            if (baseConfigItem.IsAutoGenerated) description += "\n\n<b>*This config entry was automatically generated and may require a restart*</b>";
            else if (baseConfigItem.RequiresRestart) description += "\n\n<b>*REQUIRES RESTART*</b>";

            return description + $"\n\n{baseConfigItem.Description}";
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            OnHoverEnter?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            OnHoverExit?.Invoke();
        }
    }

    internal abstract class ModConfigController<T, V> : ModConfigController where T : BaseValueConfigItem<V>
    {
        public T ConfigItem => (T)baseConfigItem;

        public override string GetDescription()
        {
            return $"{base.GetDescription()}\n\nDefault: {ConfigItem.Defaultvalue}";
        }

        public override bool SetConfigItem(BaseConfigItem configItem)
        {
            if (configItem is not T)
            {
                LogUtils.LogError($"Expected config item of type {typeof(T).Name}, but got {configItem.GetType().Name} instead.");
                return false;
            }

            return base.SetConfigItem(configItem);
        }
    } 
}
