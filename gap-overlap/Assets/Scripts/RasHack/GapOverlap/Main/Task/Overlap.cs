using System;
using RasHack.GapOverlap.Main.Stimuli;
using UnityEngine;

namespace RasHack.GapOverlap.Main.Task
{
    [Serializable]
    public struct OverlapTimes
    {
        public float CentralTime;
        public float BothStimuli;
        public float ShortenOnFocusTime;
    }

    public class Overlap : Task
    {
        #region Serialized fields

        [SerializeField] private OverlapTimes times = new()
            { CentralTime = 5.0f, BothStimuli = 5.0f, ShortenOnFocusTime = 0.2f };

        #endregion

        #region Fields

        private float? waitingTime;

        #endregion

        #region API

        public override TaskType TaskType => TaskType.Overlap;

        private OverlapTimes Times => owner.Settings?.OverlapTimes ?? times;

        protected override void OnSuccessfulCentralFocus()
        {
            centralStimulus.ShortenAnimation(Times.ShortenOnFocusTime, true);
            if (waitingTime.HasValue) waitingTime = Mathf.Min(Times.ShortenOnFocusTime, waitingTime.Value);
        }

        protected override void OnSuccessfulPeripheralFocus()
        {
            var remaining = peripheralStimulus.ShortenAnimation(Times.ShortenOnFocusTime, false);
            if (centralStimulus != null) centralStimulus.ShortenAnimation(remaining, false);
        }

        public override void ReportCentralStimulusDied(CentralStimulus stimulus)
        {
            if (stimulus != centralStimulus)
            {
                Debug.LogError($"{stimulus} stimulus is not the central one, don't care if it died!");
                return;
            }

            Debug.Log($"Only {centralStimulus} has finished");
        }

        public override void ReportPeripheralStimulusDied(PeripheralStimulus stimulus)
        {
            if (stimulus != peripheralStimulus)
            {
                Debug.LogError($"{stimulus} stimulus is not the active one, don't care if it died!");
                return;
            }

            Debug.Log($"{peripheralStimulus} has finished");
            Destroy(peripheralStimulus.gameObject);
            Destroy(centralStimulus.gameObject);
            peripheralStimulus = null;
            owner.ReportTaskFinished(this);
            Destroy(gameObject);
        }

        #endregion

        #region Mono methods

        private void Start()
        {
            waitingTime = times.CentralTime;
            StartWithCentralStimulus();
        }

        private void Update()
        {
            if (!waitingTime.HasValue) return;
            waitingTime -= Time.deltaTime;
            if (waitingTime > 0f) return;
            waitingTime = null;
            StartWithStimulus();
        }

        protected override void OnDestroy()
        {
            if (centralStimulus != null) Destroy(centralStimulus.gameObject);
            if (peripheralStimulus != null) Destroy(peripheralStimulus.gameObject);
        }

        #endregion

        #region Helpers

        private void StartWithCentralStimulus()
        {
            centralStimulus = NewCentralStimulus();
            centralStimulus.StartSimulating(this, Times.CentralTime + Times.BothStimuli);
        }

        private void StartWithStimulus()
        {
            peripheralStimulus = NewPeripheralStimulus();
            peripheralStimulus.StartSimulating(stimulusType, Side, this, Times.BothStimuli);
        }

        #endregion
    }
}