using UnityEngine;

namespace Stats.Samples.TimedBuff
{
    public sealed class TimedBuffExample : MonoBehaviour
    {
        [SerializeField] private float buffFraction = 0.5f;
        [SerializeField] private float buffSeconds = 3f;

        private StatSheet sheet;
        private StatId attack;
        private TimedModifierService timed;
        private float logTimer;

        private void Start()
        {
            sheet = new StatSheet();
            attack = StatId.NewId();
            sheet.RegisterStat(attack, 10f);

            timed = new TimedModifierService(new CountdownTimerScheduler());
            timed.AddTimedModifier(sheet, attack, ModifierOperation.PercentAdd, buffFraction, "rage", buffSeconds);
            Debug.Log($"Buff applied: Attack = {sheet.GetValue(attack)} for {buffSeconds}s");
        }

        private void Update()
        {
            logTimer += Time.deltaTime;
            if (logTimer < 0.5f) return;
            logTimer = 0f;
            Debug.Log($"Attack = {sheet.GetValue(attack)}");
        }
    }
}
