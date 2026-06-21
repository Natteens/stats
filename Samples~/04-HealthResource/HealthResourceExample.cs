using UnityEngine;

namespace Stats.Samples.HealthResource
{
    public sealed class HealthResourceExample : MonoBehaviour
    {
        [SerializeField] private MaxChangePolicy policy = MaxChangePolicy.KeepPercent;

        private void Start()
        {
            var sheet = new StatSheet();
            var maxHealth = StatId.NewId();
            sheet.RegisterStat(maxHealth, 100f);

            var health = new RuntimeResource(sheet, maxHealth, policy);
            health.Changed += (current, max) => Debug.Log($"Health {current}/{max}");
            health.Emptied += () => Debug.Log("Health emptied.");

            health.Reduce(30f);
            health.Restore(10f);
            bool spent = health.Consume(25f);
            Debug.Log($"Consume(25) succeeded: {spent}, IsFull: {health.IsFull}, Normalized: {health.Normalized:0.00}");

            Debug.Log($"Raising MaxHealth to 200 with policy {policy}");
            sheet.SetBaseValue(maxHealth, 200f);
            Debug.Log($"Current after max change: {health.Current}/{health.Max}");
        }
    }
}
