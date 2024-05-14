using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

        // StreamWriter for recording gaze data
        private StreamWriter gazeWriter;
        private bool isRecording = false;
        private float currentCountdown = 0f; // Variable to store the current countdown value

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
                Vector2 gazePointOnDisplay = new Vector2(data.Left.GazePointOnDisplayArea.x, data.Left.GazePointOnDisplayArea.y);

                // Convert normalized coordinates to screen coordinates
                Vector2 gazePosition = new Vector2(gazePointOnDisplay.x * Screen.width, (1 - gazePointOnDisplay.y) * Screen.height);

                // Convert screen coordinates to local coordinates
                Vector2 localPosition;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(gazeCursor.canvas.transform as RectTransform, gazePosition, Camera.main, out localPosition);

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
            Vector2 gazePointOnDisplay = new Vector2(data.Left.GazePointOnDisplayArea.x, data.Left.GazePointOnDisplayArea.y);

            // Convert normalized coordinates to pixel coordinates (1920x1080)
            Vector2 gazePointInPixels = new Vector2(gazePointOnDisplay.x * 1920, (1 - gazePointOnDisplay.y) * 1080);

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
                return _calibrationObject != null ? _calibrationObject.CalibrationInProgress : false;
            }
        }
    }
}
