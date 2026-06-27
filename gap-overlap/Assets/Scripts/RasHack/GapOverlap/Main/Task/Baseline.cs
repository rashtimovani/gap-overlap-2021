using System;
using RasHack.GapOverlap.Main.Stimuli;
using UnityEngine;

namespace RasHack.GapOverlap.Main.Task
{
    [Serializable]
    public struct BaselineTimes
    {
        public float CentralTime;
        public float CentralOutStimulusIn;
        public float StimulusTime;
        public float ShortenOnFocusTime;
    }

    public class Baseline : Task
    {
        #region Serialized fields

        [SerializeField] private BaselineTimes times = new()
            { CentralTime = 5.0f, CentralOutStimulusIn = 0.0f, StimulusTime = 5.0f, ShortenOnFocusTime = 0.2f };

        #endregion

        #region Fields

        private float? centralTimeOnly;
        private float? outInTime;

        #endregion

        #region API

        public override TaskType TaskType => TaskType.Baseline;

        private BaselineTimes Times => owner.Settings?.BaselineTimes ?? times;

        protected override void OnSuccessfulCentralFocus()
        {
            peripheralStimulus.ShortenIdleAnimationOnly(Times.ShortenOnFocusTime);
            if (centralTimeOnly.HasValue) centralTimeOnly = Mathf.Min(centralTimeOnly.Value, Times.ShortenOnFocusTime);
        }

        protected override void OnSuccessfulPeripheralFocus()
        {
            var remaining = centralStimulus.ShortenAnimation(Times.ShortenOnFocusTime, false);
            if (centralStimulus != null) centralStimulus.ShortenAnimation(remaining, false);
        }

        public override void ReportCentralStimulusDied(CentralStimulus central)
        {
            if (central != centralStimulus)
            {
                Debug.LogError($"{central} stimulus is not the central one, don't care if it died!");
                return;
            }

            Debug.Log($"{centralStimulus} has finished");
            Destroy(centralStimulus.gameObject);
        }

        public override void ReportPeripheralStimulusDied(PeripheralStimulus active)
        {
            if (active != peripheralStimulus)
            {
                Debug.LogError($"{active} stimulus is not the active one, don't care if it died!");
                return;
            }

            Debug.Log($"{peripheralStimulus} has finished");
            Destroy(peripheralStimulus.gameObject);
            peripheralStimulus = null;
            owner.ReportTaskFinished(this);
            Destroy(gameObject);
        }

        #endregion

        #region Mono methods

        private void Start()
        {
            StartWithCentralStimulus();
        }

        private void Update()
        {
            if (!centralTimeOnly.HasValue) return;
            centralTimeOnly -= Time.deltaTime;
            if (centralTimeOnly > 0f) return;
            centralTimeOnly = null;
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
            centralStimulus.StartSimulating(this, Times.CentralTime + Times.CentralOutStimulusIn,
                Times.CentralOutStimulusIn);
            centralTimeOnly = Times.CentralTime;
        }

        private void StartWithStimulus()
        {
            peripheralStimulus = NewPeripheralStimulus();
            peripheralStimulus.StartSimulating(stimulusType, Side, this, Times.StimulusTime + Times.CentralOutStimulusIn,
                Times.CentralOutStimulusIn);
        }

        #endregion
    }
}