using UnityEngine;

namespace LethalConfig.MonoBehaviours
{
    internal class TooltipSystem : MonoBehaviour
    {
        private static TooltipSystem instance;

        public Tooltip tooltip;

        private void Awake()
        {
            instance = this;
        }

        public static void Show(string content, GameObject target)
        {
            if (instance == null) return;

            instance.tooltip.gameObject.SetActive(true);
            instance.tooltip.SetText(content);
            instance.tooltip.SetTarget(target);
        }

        public static void Hide()
        {
            if (instance == null) return;

            instance.tooltip.gameObject.SetActive(false);
        }
    }
}
