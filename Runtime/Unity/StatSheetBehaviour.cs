using UnityEngine;

namespace Stats
{
    public sealed class StatSheetBehaviour : MonoBehaviour
    {
        [SerializeField] private StatSheetPreset preset;

        public StatSheet Sheet { get; private set; }

        private void Awake()
        {
            Sheet = preset != null ? preset.BuildSheet() : new StatSheet();
        }
    }
}
