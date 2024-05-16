using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tobii.Research.Unity
{
    public class GazeTrail : GazeTrailBase
    {
        public static GazeTrail Instance { get; private set; }

        private EyeTracker _eyeTracker;
        private Calibration _calibrationObject;

        // References to the UI elements
        [SerializeField]
        private Image gazeCursor;
        [SerializeField]
        private TextMeshProUGUI gazeText;
        [SerializeField]
        private TMP_InputField participantIDInput;
        [SerializeField]
        private Button startButton;
        [SerializeField]
        private RawImage blackScreen; // Reference to the black screen RawImage

        // StreamWriter for recording gaze data
        private StreamWriter gazeWriter;
        private bool isRecording = false;
        private float currentCountdown = 0f; // Variable to store the current countdown value
        private bool isBaselineStart = true; // Track whether it is the start or end baseline

        protected override void OnAwake()
        {
            base.OnAwake();
            Instance = this;
        }

        protected override void OnStart()
        {
            base.OnStart();
            _eyeTracker = EyeTracker.Instance;
            _calibrationObject = Calibration.Instance;

            // Ensure gazeCursor and gazeText are assigned
            if (gazeCursor == null)
            {
                Debug.LogError("GazeCursor is not assigned in the inspector.");
            }

            if (gazeText == null)
            {
                Debug.LogError("GazeText is not assigned in the inspector.");
            }

            // Ensure participantIDInput and startButton are assigned
            if (participantIDInput == null)
            {
                Debug.LogError("ParticipantIDInput is not assigned in the inspector.");
            }

            if (startButton == null)
            {
                Debug.LogError("StartButton is not assigned in the inspector.");
            }

            if (blackScreen == null)
            {
                Debug.LogError("BlackScreen is not assigned in the inspector.");
            }

            // Add listener to the start button
            startButton.onClick.AddListener(OnStartButtonClick);
        }

        private void Update()
        {
            UpdateGazeCursor();

            if (isRecording)
            {
                RecordGazeData();
            }
        }

        private void UpdateGazeCursor()
        {
            if (_eyeTracker == null || gazeCursor == null || gazeText == null) return;

            var data = _eyeTracker.LatestGazeData;
            if (data.CombinedGazeRayScreenValid)
            {
                // Use the GazePointOnDisplayArea data (normalized values)
                Vector2 gazePointOnDisplay = new(data.Left.GazePointOnDisplayArea.x, data.Left.GazePointOnDisplayArea.y);

                // Convert normalized coordinates to screen coordinates
                Vector2 gazePosition = new(gazePointOnDisplay.x * Screen.width, (1 - gazePointOnDisplay.y) * Screen.height);

                // Convert screen coordinates to local coordinates
                RectTransformUtility.ScreenPointToLocalPointInRectangle(gazeCursor.canvas.transform as RectTransform, gazePosition, Camera.main, out Vector2 localPosition);

                // Update the position of the gazeCursor
                gazeCursor.rectTransform.anchoredPosition = localPosition;

                // Display the gaze values in the TextMeshPro element
                gazeText.text = $"Gaze Position: ({gazePosition.x}, {gazePosition.y})";
            }
            else
            {
                gazeText.text = "Gaze data not valid.";
            }
        }

        private void RecordGazeData()
        {
            if (_eyeTracker == null) return;

            var data = _eyeTracker.LatestGazeData;
            DateTime dateTime = DateTime.UtcNow;
            string timestamp = dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

            // Retrieve normalized gaze point
            Vector2 gazePointOnDisplay = new(data.Left.GazePointOnDisplayArea.x, data.Left.GazePointOnDisplayArea.y);

            // Convert normalized coordinates to pixel coordinates (1920x1080)
            Vector2 gazePointInPixels = new(gazePointOnDisplay.x * 1920, (1 - gazePointOnDisplay.y) * 1080);

            // Pupil data and validity
            bool validLeftGaze = data.Left.GazePointValid;
            bool validRightGaze = data.Right.GazePointValid;
            bool validLeftPupil = data.Left.PupilDiameterValid;
            bool validRightPupil = data.Right.PupilDiameterValid;
            float leftPupilDiameter = data.Left.PupilDiameter;
            float rightPupilDiameter = data.Right.PupilDiameter;

            gazeWriter.WriteLine($"{timestamp},{dateTime:yyyy-MM-dd HH:mm:ss.fff},{currentCountdown},{validLeftGaze},{validRightGaze},{gazePointOnDisplay.x},{gazePointOnDisplay.y},{gazePointInPixels.x},{gazePointInPixels.y},{validLeftPupil},{leftPupilDiameter},{validRightPupil},{rightPupilDiameter}");
        }

        public void StartRecording(string filePath)
        {
            gazeWriter = new StreamWriter(filePath);
            gazeWriter.WriteLine("Timestamp,DateTime,Countdown,ValidLeftGaze,ValidRightGaze,X,Y,XPixels,YPixels,ValidLeftPupil,LeftPupilDiameter,ValidRightPupil,RightPupilDiameter");
            isRecording = true;
        }

        public void StopRecording()
        {
            isRecording = false;
            if (gazeWriter != null)
            {
                gazeWriter.Close();
                gazeWriter = null;
            }
        }

        public void UpdateCountdownValue(float countdownValue)
        {
            currentCountdown = countdownValue;
        }

        private void OnStartButtonClick()
        {
            string participantID = participantIDInput.text;
            if (string.IsNullOrEmpty(participantID))
            {
                Debug.LogError("Participant ID is empty.");
                return;
            }

            string fileSuffix = isBaselineStart ? "start" : "end";
            string folderPath = Path.Combine(Application.dataPath, "Data");

            // Ensure the folder exists
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filePath = Path.Combine(folderPath, $"baseline_{participantID}_{fileSuffix}.csv");

            // Show the black screen
            blackScreen.gameObject.SetActive(true);

            StartRecording(filePath);
            Invoke(nameof(StopRecordingAndHideBlackScreen), 60f); // Record for 60 seconds
        }

        private void StopRecordingAndHideBlackScreen()
        {
            StopRecording();

            // Hide the black screen
            blackScreen.gameObject.SetActive(false);

            // Toggle baseline start/end
            isBaselineStart = !isBaselineStart;
        }

        protected override bool GetRay(out Ray ray)
        {
            if (_eyeTracker == null)
            {
                return base.GetRay(out ray);
            }

            var data = _eyeTracker.LatestGazeData;
            ray = data.CombinedGazeRayScreen;
            return data.CombinedGazeRayScreenValid;
        }

        protected override bool HasEyeTracker
        {
            get
            {
                return _eyeTracker != null;
            }
        }

        protected override bool CalibrationInProgress
        {
            get
            {
                return _calibrationObject != null && _calibrationObject.CalibrationInProgress;
            }
        }
    }
}
