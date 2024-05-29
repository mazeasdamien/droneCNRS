using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tobii.Research.Unity
{
    public class GazeTrail : GazeTrailBase
    {
        public string panelNameLooked;
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
        [SerializeField]
        private TMP_Dropdown strategyDropdown; // Reference to the strategy dropdown
        [SerializeField]
        private TextMeshProUGUI lookedAtText; // Reference to the TMP_Text element for displaying the looked-at panel

        // StreamWriter for recording gaze data
        private StreamWriter gazeWriter;
        private bool isRecording = false;
        private float currentCountdown = 0f; // Variable to store the current countdown value
        private bool isBaselineStart = true; // Track whether it is the start or end baseline

        // Reference to Timer class
        private Timer timer;

        protected override void OnAwake()
        {
            base.OnAwake();
            Instance = this;
            timer = FindObjectOfType<Timer>(); // Find the Timer instance
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

            if (strategyDropdown == null)
            {
                Debug.LogError("StrategyDropdown is not assigned in the inspector.");
            }

            if (lookedAtText == null)
            {
                Debug.LogError("LookedAtText is not assigned in the inspector.");
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

                // Determine the panel looked at
                DeterminePanelLooked(gazePosition);
            }
            else
            {
                gazeText.text = "Gaze data not valid.";
                panelNameLooked = "None";
            }

            // Update the looked-at text
            lookedAtText.text = $"{panelNameLooked}";
        }

        private void DeterminePanelLooked(Vector2 gazePosition)
        {
            int strategy = strategyDropdown.value;
            string previousPanelNameLooked = panelNameLooked;
            panelNameLooked = "None";

            switch (strategy)
            {
                case 0: // Strategy1
                    float panelHeight1 = Screen.height * 0.5f;
                    float panelWidth1 = Screen.width * 0.5f;
                    float leftBound1 = (Screen.width - panelWidth1) / 2;
                    float rightBound1 = leftBound1 + panelWidth1;
                    float bottomBound1 = (Screen.height - panelHeight1) / 2;
                    float topBound1 = bottomBound1 + panelHeight1;

                    if (gazePosition.x >= leftBound1 && gazePosition.x <= rightBound1 && gazePosition.y >= bottomBound1 && gazePosition.y <= topBound1)
                    {
                        panelNameLooked = "FPV";
                    }
                    break;
                case 1: // Strategy2
                    float panelHeight = 540f;
                    float panelWidth = 960f;
                    float leftBound = (Screen.width - panelWidth) / 2;
                    float rightBound = leftBound + panelWidth;

                    if (gazePosition.x >= leftBound && gazePosition.x <= rightBound)
                    {
                        if (gazePosition.y >= (Screen.height - panelHeight) / 2 && gazePosition.y <= (Screen.height + panelHeight) / 2)
                        {
                            panelNameLooked = "TPV";
                        }
                        else if (gazePosition.y >= (Screen.height - panelHeight * 1.5f) / 2 && gazePosition.y <= (Screen.height - panelHeight / 2) / 2)
                        {
                            panelNameLooked = "FPV";
                        }
                    }
                    break;
                case 2: // Strategy3
                    if (gazePosition.x < Screen.width / 2)
                    {
                        if (gazePosition.y > Screen.height / 2)
                        {
                            panelNameLooked = "VIRTUAL TPV LOW QUALITY";
                        }
                        else
                        {
                            panelNameLooked = "FPV";
                        }
                    }
                    else
                    {
                        panelNameLooked = "MAP";
                    }
                    break;
                case 3: // Strategy4
                    if (gazePosition.x < Screen.width / 2)
                    {
                        if (gazePosition.y > Screen.height / 2)
                        {
                            panelNameLooked = "VIRTUAL TPV HIGH QUALITY";
                        }
                        else
                        {
                            panelNameLooked = "FPV";
                        }
                    }
                    else
                    {
                        panelNameLooked = "MAP";
                    }
                    break;
            }

            // Update panel watch time if the panel looked at has changed
            if (previousPanelNameLooked != panelNameLooked && panelNameLooked != "None")
            {
                float currentTime = Time.time;
                if (previousPanelNameLooked != "None")
                {
                    timer.UpdatePanelWatchTime(previousPanelNameLooked, currentTime - timer.GetSessionStartTime());
                }
                timer.SetSessionStartTime(currentTime);
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

            // Additional eye-specific data
            var leftGazeOriginInTrackBox = data.Left.GazeOriginInTrackBoxCoordinates;
            var leftGazeOriginInUser = data.Left.GazeOriginInUserCoordinates;
            var leftGazePointInUser = data.Left.GazePointInUserCoordinates;
            var leftGazePointOnDisplayArea = data.Left.GazePointOnDisplayArea;
            var leftGazeRayScreen = data.Left.GazeRayScreen;

            var rightGazeOriginInTrackBox = data.Right.GazeOriginInTrackBoxCoordinates;
            var rightGazeOriginInUser = data.Right.GazeOriginInUserCoordinates;
            var rightGazePointInUser = data.Right.GazePointInUserCoordinates;
            var rightGazePointOnDisplayArea = data.Right.GazePointOnDisplayArea;
            var rightGazeRayScreen = data.Right.GazeRayScreen;

            gazeWriter.WriteLine($"{timestamp},{dateTime:yyyy-MM-dd HH:mm:ss.fff},{currentCountdown},{validLeftGaze},{validRightGaze},{gazePointOnDisplay.x},{gazePointOnDisplay.y},{gazePointInPixels.x},{gazePointInPixels.y},{validLeftPupil},{leftPupilDiameter},{validRightPupil},{rightPupilDiameter}," +
                                 $"{leftGazeOriginInTrackBox.x},{leftGazeOriginInTrackBox.y},{leftGazeOriginInTrackBox.z},{leftGazeOriginInUser.x},{leftGazeOriginInUser.y},{leftGazeOriginInUser.z},{leftGazePointInUser.x},{leftGazePointInUser.y},{leftGazePointInUser.z},{leftGazePointOnDisplayArea.x},{leftGazePointOnDisplayArea.y},{leftGazeRayScreen.origin.x},{leftGazeRayScreen.origin.y},{leftGazeRayScreen.origin.z},{leftGazeRayScreen.direction.x},{leftGazeRayScreen.direction.y},{leftGazeRayScreen.direction.z}," +
                                 $"{rightGazeOriginInTrackBox.x},{rightGazeOriginInTrackBox.y},{rightGazeOriginInTrackBox.z},{rightGazeOriginInUser.x},{rightGazeOriginInUser.y},{rightGazeOriginInUser.z},{rightGazePointInUser.x},{rightGazePointInUser.y},{rightGazePointInUser.z},{rightGazePointOnDisplayArea.x},{rightGazePointOnDisplayArea.y},{rightGazeRayScreen.origin.x},{rightGazeRayScreen.origin.y},{rightGazeRayScreen.origin.z},{rightGazeRayScreen.direction.x},{rightGazeRayScreen.direction.y},{rightGazeRayScreen.direction.z},{panelNameLooked}");
        }

        public void StartRecording(string filePath)
        {
            gazeWriter = new StreamWriter(filePath);
            gazeWriter.WriteLine("Timestamp,DateTime,Countdown,ValidLeftGaze,ValidRightGaze,X,Y,XPixels,YPixels,ValidLeftPupil,LeftPupilDiameter,ValidRightPupil,RightPupilDiameter," +
                                 "LeftGazeOriginInTrackBoxCoordinatesX,LeftGazeOriginInTrackBoxCoordinatesY,LeftGazeOriginInTrackBoxCoordinatesZ,LeftGazeOriginInUserCoordinatesX,LeftGazeOriginInUserCoordinatesY,LeftGazeOriginInUserCoordinatesZ,LeftGazePointInUserCoordinatesX,LeftGazePointInUserCoordinatesY,LeftGazePointInUserCoordinatesZ,LeftGazePointOnDisplayAreaX,LeftGazePointOnDisplayAreaY,LeftGazeRayScreenOriginX,LeftGazeRayScreenOriginY,LeftGazeRayScreenOriginZ,LeftGazeRayScreenDirectionX,LeftGazeRayScreenDirectionY,LeftGazeRayScreenDirectionZ," +
                                 "RightGazeOriginInTrackBoxCoordinatesX,RightGazeOriginInTrackBoxCoordinatesY,RightGazeOriginInTrackBoxCoordinatesZ,RightGazeOriginInUserCoordinatesX,RightGazeOriginInUserCoordinatesY,RightGazeOriginInUserCoordinatesZ,RightGazePointInUserCoordinatesX,RightGazePointInUserCoordinatesY,RightGazePointInUserCoordinatesZ,RightGazePointOnDisplayAreaX,RightGazePointOnDisplayAreaY,RightGazeRayScreenOriginX,RightGazeRayScreenOriginY,RightGazeRayScreenOriginZ,RightGazeRayScreenDirectionX,RightGazeRayScreenDirectionY,RightGazeRayScreenDirectionZ,PanelNameLooked");
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