using System;
using RasHack.GapOverlap.Main.Stimuli;
using UnityEngine;

namespace RasHack.GapOverlap.Main.Task
{
    [Serializable]
    public struct GapTimes
    {
        public float CentralTime;
        public float PauseTime;
        public float StimulusTime;
        public float ShortenOnFocusTime;
    }

    public class Gap : Task
    {
        #region Serialized fields

        [SerializeField] private GapTimes times = new()
            { CentralTime = 5.0f, PauseTime = 0.0f, StimulusTime = 5.0f, ShortenOnFocusTime = 0.2f };

        #endregion

        #region Fields

        private float? waitingTime;

        #endregion

        #region API

        public override TaskType TaskType => TaskType.Gap;

        private GapTimes Times => owner.Settings?.GapTimes ?? times;

        protected override void OnSuccessfulCentralFocus()
        {
            centralStimulus.ShortenAnimation(Times.ShortenOnFocusTime, false);
        }

        protected override void OnSuccessfulPeripheralFocus()
        {
            peripheralStimulus.ShortenAnimation(Times.ShortenOnFocusTime, false);
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
            centralStimulus = null;
            waitingTime = Times.PauseTime;
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
            centralStimulus.StartSimulating(this, Times.CentralTime);
        }

        private void StartWithStimulus()
        {
            peripheralStimulus = NewPeripheralStimulus();
            peripheralStimulus.StartSimulating(stimulusType, Side, this, Times.StimulusTime);
        }

        #endregion
    }
}