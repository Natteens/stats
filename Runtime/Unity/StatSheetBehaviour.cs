using UnityEngine;

namespace Stats
{
    public sealed class StatSheetBehaviour : MonoBehaviour
    {
        [SerializeField] private StatSheetPreset preset;

        public StatSheet Sheet { get; private set; }
        public TimedModifierService TimedModifiers { get; private set; }

        private void Awake()
        {
            Sheet = preset != null ? preset.BuildSheet() : new StatSheet();
            TimedModifiers = new TimedModifierService(new CountdownTimerScheduler());
        }

        private void OnDestroy()
        {
            // Sheet is being torn down; only kill dangling timer callbacks, do not revert modifiers.
            TimedModifiers?.CancelSchedules();
        }
    }
}
