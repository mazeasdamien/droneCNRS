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

        [SerializeField]
        private Image gazeCursor;
        [SerializeField]
        private TextMeshProUGUI gazeText;
        [SerializeField]
        private TMP_InputField participantIDInput;
        [SerializeField]
        private Button startButton;
        [SerializeField]
        private RawImage blackScreen;
        [SerializeField]
        private TMP_Dropdown strategyDropdown;
        [SerializeField]
        private TextMeshProUGUI lookedAtText;

        private StreamWriter gazeWriter;
        private bool isRecording = false;
        private float currentCountdown = 0f;
        private bool isBaselineStart = true;

        private Timer timer;

        // Public fields to display in the inspector for panel watch times
        public float fpvWatchTime = 0f;
        public float visionAssistWatchTime = 0f;
        public float virtualTpvLowQualityWatchTime = 0f;
        public float virtualTpvHighQualityWatchTime = 0f;
        public float mapWatchTime = 0f;

        private float lastGazeTimeUpdate = 0f;
        private string lastPanelNameLooked = "None";

        protected override void OnAwake()
        {
            base.OnAwake();
            Instance = this;
            timer = FindObjectOfType<Timer>();
        }

        protected override void OnStart()
        {
            base.OnStart();
            _eyeTracker = EyeTracker.Instance;
            _calibrationObject = Calibration.Instance;
            startButton.onClick.AddListener(OnStartButtonClick);
        }

        private void Update()
        {
            UpdateGazeCursor();

            if (isRecording)
            {
                RecordGazeData();
            }

            UpdatePanelWatchTime();
        }

        private void UpdateGazeCursor()
        {
            if (_eyeTracker == null || gazeCursor == null || gazeText == null) return;

            var data = _eyeTracker.LatestGazeData;
            if (data.CombinedGazeRayScreenValid)
            {
                Vector2 gazePointOnDisplay = new(data.Left.GazePointOnDisplayArea.x, data.Left.GazePointOnDisplayArea.y);
                Vector2 gazePosition = new(gazePointOnDisplay.x * Screen.width, (1 - gazePointOnDisplay.y) * Screen.height);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(gazeCursor.canvas.transform as RectTransform, gazePosition, Camera.main, out Vector2 localPosition);
                gazeCursor.rectTransform.anchoredPosition = localPosition;
                gazeText.text = $"Gaze Position: ({gazePosition.x}, {gazePosition.y})";
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
                    float panelHeight = Screen.height * 0.5f;
                    float panelWidth = Screen.width * 0.5f;
                    float leftBound = (Screen.width - panelWidth) / 2;
                    float rightBound = leftBound + panelWidth;

                    if (gazePosition.x >= leftBound && gazePosition.x <= rightBound)
                    {
                        if (gazePosition.y > (Screen.height / 2))
                        {
                            panelNameLooked = "FPV";
                        }
                        else
                        {
                            panelNameLooked = "Vision_Assist";
                        }
                    }
                    break;
                case 2: // Strategy3
                    if (gazePosition.x < Screen.width / 2)
                    {
                        if (gazePosition.y > Screen.height / 2)
                        {
                            panelNameLooked = "FPV";
                        }
                        else
                        {
                            panelNameLooked = "VIRTUAL TPV LOW QUALITY";
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
                            panelNameLooked = "FPV";
                        }
                        else
                        {
                            panelNameLooked = "VIRTUAL TPV HIGH QUALITY";
                        }
                    }
                    else
                    {
                        panelNameLooked = "MAP";
                    }
                    break;
            }

            if (panelNameLooked != previousPanelNameLooked)
            {
                UpdatePanelWatchTime();
                lastPanelNameLooked = panelNameLooked;
            }
        }

        private void UpdatePanelWatchTime()
        {
            if (!timer.IsTimerRunning() || string.IsNullOrEmpty(lastPanelNameLooked)) return;

            float deltaTime = Time.time - lastGazeTimeUpdate;
            switch (lastPanelNameLooked)
            {
                case "FPV":
                    fpvWatchTime += deltaTime;
                    break;
                case "Vision_Assist":
                    visionAssistWatchTime += deltaTime;
                    break;
                case "VIRTUAL TPV LOW QUALITY":
                    virtualTpvLowQualityWatchTime += deltaTime;
                    break;
                case "VIRTUAL TPV HIGH QUALITY":
                    virtualTpvHighQualityWatchTime += deltaTime;
                    break;
                case "MAP":
                    mapWatchTime += deltaTime;
                    break;
            }
            lastGazeTimeUpdate = Time.time;
        }

        private void RecordGazeData()
        {
            if (_eyeTracker == null) return;

            var data = _eyeTracker.LatestGazeData;
            DateTime dateTime = DateTime.UtcNow;
            string timestamp = dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

            Vector2 gazePointOnDisplay = new(data.Left.GazePointOnDisplayArea.x, data.Left.GazePointOnDisplayArea.y);

            Vector2 gazePointInPixels = new(gazePointOnDisplay.x * Screen.height, (1 - gazePointOnDisplay.y) * Screen.width);

            bool validLeftGaze = data.Left.GazePointValid;
            bool validRightGaze = data.Right.GazePointValid;
            bool validLeftPupil = data.Left.PupilDiameterValid;
            bool validRightPupil = data.Right.PupilDiameterValid;
            float leftPupilDiameter = data.Left.PupilDiameter;
            float rightPupilDiameter = data.Right.PupilDiameter;

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
            lastGazeTimeUpdate = Time.time;
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

            DateTime currentDate = DateTime.Now;
            string formattedDate = currentDate.ToString("ddMMyyyy");
            string folderPath = Path.Combine(Application.dataPath, $"{participantID}_{formattedDate}");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filePath = GetAvailableFilePath(folderPath, $"BaselineEye_{participantID}_");

            blackScreen.gameObject.SetActive(true);

            StartRecording(filePath);
            Invoke(nameof(StopRecordingAndHideBlackScreen), 60f);
        }

        private string GetAvailableFilePath(string folderPath, string baseFileName)
        {
            string filePathStart = Path.Combine(folderPath, $"{baseFileName}start.csv");
            string filePathEnd = Path.Combine(folderPath, $"{baseFileName}end.csv");

            if (!File.Exists(filePathStart))
            {
                return filePathStart;
            }

            return filePathEnd;
        }

        private void StopRecordingAndHideBlackScreen()
        {
            StopRecording();

            blackScreen.gameObject.SetActive(false);

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
