using System;
using System.Collections.Generic;
using RasHack.GapOverlap.Main.Inputs;
using RasHack.GapOverlap.Main.Result;
using RasHack.GapOverlap.Main.Settings;
using RasHack.GapOverlap.Main.Stimuli;
using RasHack.GapOverlap.Main.Task;
using UnityEngine;

namespace RasHack.GapOverlap.Main
{
    public class Simulator : MonoBehaviour
    {
        #region Serialized fields

        [SerializeField] private Pointer[] pointers;

        [SerializeField] private SpriteRenderer bottomLeft;
        [SerializeField] private SpriteRenderer bottomRight;
        [SerializeField] private SpriteRenderer topLeft;
        [SerializeField] private SpriteRenderer topRight;

        [SerializeField] private TestSampler sampler;

        #endregion

        #region Fields

        private MainSettings settings = new MainSettings();

        private Scaler scaler;
        private Scaler debugScaler;
        private Camera mainCamera;
        private Background background;

        private TaskOrder tasks;
        private StimuliArea area;
        private StimuliType nextStimulus;

        private float? waitingTime;
        private Task.Task currentTask;

        #endregion

        #region API

        public bool TobiiWorking
        {
            get
            {
                var pointers = TobiiPointers;
                var tobiiWorking = pointers.Count > 0;
                foreach (var tobiiPointer in pointers)
                {
                    tobiiWorking &= tobiiPointer.Status.Enabled;
                }
                return tobiiWorking;
            }
        }

        public bool IsActive { get; private set; }

        public List<TobiiEyePointer> TobiiPointers
        {
            get
            {
                var tobiiPointers = new List<TobiiEyePointer>();
                foreach (var pointer in pointers)
                {
                    var tobiiPointer = pointer as TobiiEyePointer;
                    if (tobiiPointer != null) tobiiPointers.Add(tobiiPointer);
                }

                return tobiiPointers;
            }
        }

        private List<Pointer> NonTobiiPointers
        {
            get
            {
                var nonTobiiPointers = new List<Pointer>();
                foreach (var pointer in pointers)
                {
                    var tobiiPointer = pointer as TobiiEyePointer;
                    if (pointer != null && tobiiPointer == null) nonTobiiPointers.Add(pointer);
                }

                return nonTobiiPointers;
            }
        }

        public MainSettings Settings => settings;

        public Scaler Scaler => scaler;
        public Scaler DebugScaler => debugScaler;
        public StimuliArea Area => area;
        public TestSampler Sampler => sampler;

        private bool ShowPointer => settings.ShowPointer;

        public void UpdateBackground()
        {
            background.SetBackground(settings.Background);
        }

        public void StartTests(string usingName)
        {
            tasks.Reset(settings.TaskCount);
            area.Reset(settings.TaskCount);
            nextStimulus = StimuliTypeExtensions.Next();
            IsActive = true;

            var runName = string.IsNullOrWhiteSpace(usingName) ? $"{Guid.NewGuid()}" : usingName;

            UpdateBackground();

            settings.LastUsedName = usingName;
            settings.Store();

            AudioListener.volume = settings.SoundVolume;

            var testId = Guid.NewGuid().ToString();
            sampler.StartTest(runName, testId, settings.SamplesPerSecond);
            waitingTime = settings.PauseBeforeTasks;
        }

        public void ReportTaskFinished(Task.Task task)
        {
            if (task != currentTask)
            {
                Debug.LogError($"{task} reported as finished, but that ${currentTask} is currently active");
                return;
            }

            Debug.Log($"{currentTask} has finished");
            sampler.CompleteTask(currentTask);
            currentTask = null;
            waitingTime = tasks.HasNext ? settings.PauseBetweenTasks : settings.PauseAfterTasks;
        }

        #endregion

        #region Mono methods

        private void Awake()
        {
            var loadedSettings = MainSettings.Load();
            if (loadedSettings != null) settings = loadedSettings;
        }

        private void Start()
        {
            Application.targetFrameRate = 120;
            mainCamera = Camera.main;

            background = FindObjectOfType<Background>();
            UpdateBackground();

            scaler = new Scaler(mainCamera, -1, settings, ScreenArea.WholeScreen);
            debugScaler = new Scaler(mainCamera, -2, settings, ScreenArea.WholeScreen);

            tasks = GetComponent<TaskOrder>();
            area = GetComponent<StimuliArea>();
        }

        private void Update()
        {
            UpdateBounds();
            UpdateDebugVisibility();
            UpdatePause();
            DetectInterruptedTest();
            UpdatePointers();
        }

        #endregion

        #region Helpers

        private void UpdateBounds()
        {
            topLeft.transform.position = debugScaler.TopLeft;
            bottomLeft.transform.position = debugScaler.BottomLeft;
            bottomRight.transform.position = debugScaler.BottomRight;
            topRight.transform.position = debugScaler.TopRight;
        }

        private void UpdateDebugVisibility()
        {
            foreach (var pointer in pointers)
            {
                pointer.ShowPointer(settings.ShowPointer);
            }

            bottomLeft.enabled = ShowPointer;
            bottomRight.enabled = ShowPointer;
            topLeft.enabled = ShowPointer;
            topRight.enabled = ShowPointer;
        }

        private void UpdatePause()
        {
            if (!waitingTime.HasValue) return;
            waitingTime -= Time.deltaTime;
            if (waitingTime > 0f) return;
            waitingTime = null;
            NewTask();
        }

        private void NewTask()
        {
            currentTask = tasks.CreateNext(nextStimulus);
            if (currentTask == null)
            {
                sampler.CompleteTest(true);
                Debug.Log("All tasks finished!");
                IsActive = false;
                return;
            }

            currentTask.StartTask(this, nextStimulus, tasks.CurrentTaskOrder);
            sampler.StartTask(currentTask);
            nextStimulus = StimuliTypeExtensions.Next();
        }

        private void DetectInterruptedTest()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Debug.LogWarning("Test aborted by user!");
                tasks.End();
                if (currentTask != null)
                {
                    Destroy(currentTask.gameObject);
                    currentTask = null;
                }

                sampler.CompleteTest(false);
                IsActive = false;
                waitingTime = 0.01f;
            }
        }

        private void UpdatePointers()
        {
            var tobiiWorking = TobiiWorking;
            foreach (var pointer in NonTobiiPointers)
            {
                pointer.PointerEnabled = !tobiiWorking;
            }
        }

        #endregion
    }
}