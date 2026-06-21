using UnityEngine;

namespace Stats
{
    [CreateAssetMenu(menuName = "Stats/Stat Definition", fileName = "StatDefinition")]
    public sealed class StatDefinition : ScriptableObject
    {
        [SerializeField, HideInInspector] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private float defaultBase;
        [SerializeField] private bool hasMin;
        [SerializeField] private float min;
        [SerializeField] private bool hasMax;
        [SerializeField] private float max;
        [SerializeField] private bool percentDisplay;
        [SerializeField, HideInInspector] private string description;

        public string DisplayName => displayName;
        public float DefaultBase => defaultBase;
        public float Min => hasMin ? min : float.NegativeInfinity;
        public float Max => hasMax ? max : float.PositiveInfinity;
        public bool PercentDisplay => percentDisplay;
        public string Description => description;

        public StatId Id => string.IsNullOrEmpty(id) ? StatId.Empty : StatId.FromString(id);

        // Editor-time identity: assign once when missing; never regenerate on rename/revalidation.
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
                id = System.Guid.NewGuid().ToString("N");
        }
    }
}
