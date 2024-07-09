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
        [SerializeField]
        private TextMeshProUGUI invalidDataText; // Add a TextMeshProUGUI field for invalid data percentage

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

        private int totalDataPoints = 0; // Track total number of data points
        private int invalidDataPoints = 0; // Track number of invalid data points

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

            if (_eyeTracker == null || _calibrationObject == null)
            {
                Debug.LogError("EyeTracker or Calibration object is not found.");
                return;
            }

            startButton.onClick.AddListener(OnStartButtonClick);
        }

        private void Update()
        {
            if (_eyeTracker == null) return;  // Add null check
            UpdateGazeCursor();

            if (isRecording)
            {
                RecordGazeData();
                UpdateInvalidDataPercentage(); // Update the invalid data percentage in real time
            }

            UpdatePanelWatchTime();

            if (Input.GetKeyDown(KeyCode.Space))
            {
                LogGazePosition();
            }
        }

        private void LogGazePosition()
        {
            if (_eyeTracker == null) return;

            var data = _eyeTracker.LatestGazeData;

            if (!data.Left.GazePointValid && !data.Right.GazePointValid) return;

            Vector2 gazePositionInPixels = GetMeanGazePositionInPixels(data);
        }

        private Vector2 GetMeanGazePositionInPixels(IGazeData data)
        {
            Vector2 gazePointOnDisplayl = data.Left.GazePointValid ? new Vector2(data.Left.GazePointOnDisplayArea.x, data.Left.GazePointOnDisplayArea.y) : Vector2.zero;
            Vector2 gazePointOnDisplayr = data.Right.GazePointValid ? new Vector2(data.Right.GazePointOnDisplayArea.x, data.Right.GazePointOnDisplayArea.y) : Vector2.zero;

            Vector2 meanGazePointOnDisplay = new Vector2(
                (gazePointOnDisplayl.x + gazePointOnDisplayr.x) / 2,
                (gazePointOnDisplayl.y + gazePointOnDisplayr.y) / 2
            );

            if (!data.Left.GazePointValid) meanGazePointOnDisplay = gazePointOnDisplayr;
            if (!data.Right.GazePointValid) meanGazePointOnDisplay = gazePointOnDisplayl;

            return new Vector2(
                meanGazePointOnDisplay.x * Screen.width,
                (1 - meanGazePointOnDisplay.y) * Screen.height
            );
        }

        private void UpdateGazeCursor()
        {
            if (_eyeTracker == null || gazeCursor == null || gazeText == null) return;

            var data = _eyeTracker.LatestGazeData;
            if (data.CombinedGazeRayScreenValid)
            {
                Vector2 gazePosition = GetMeanGazePositionInPixels(data);
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

            if (strategy == 0)
            {
                float panelHeight = Screen.height * 0.5f;
                float panelWidth = Screen.width * 0.5f;
                float leftBound = (Screen.width - panelWidth) / 2;
                float rightBound = leftBound + panelWidth;
                float bottomBound = (Screen.height - panelHeight) / 2;
                float topBound = bottomBound + panelHeight;

                if (gazePosition.x >= leftBound && gazePosition.x <= rightBound && gazePosition.y >= bottomBound && gazePosition.y <= topBound)
                {
                    panelNameLooked = "FPV";
                }
            }
            else if (strategy == 1)
            {
                CalculatePanelLookedForStrategy2(gazePosition);
            }
            else if (strategy == 2)
            {
                CalculatePanelLookedForStrategy3(gazePosition);
            }
            else if (strategy == 3)
            {
                CalculatePanelLookedForStrategy4(gazePosition);
            }

            if (panelNameLooked != previousPanelNameLooked)
            {
                UpdatePanelWatchTime();
                lastPanelNameLooked = panelNameLooked;
            }
        }

        private void CalculatePanelLookedForStrategy2(Vector2 gazePosition)
        {
            float panelHeight = Screen.height * 0.5f;
            float panelWidth = Screen.width * 0.5f;
            float leftBound = (Screen.width - panelWidth) / 2;
            float rightBound = leftBound + panelWidth;

            if (gazePosition.x >= leftBound && gazePosition.x <= rightBound)
            {
                panelNameLooked = gazePosition.y > (Screen.height / 2) ? "FPV" : "MAP";
            }
        }

        private void CalculatePanelLookedForStrategy3(Vector2 gazePosition)
        {
            if (gazePosition.x < Screen.width / 2)
            {
                panelNameLooked = gazePosition.y > Screen.height / 2 ? "FPV" : "TPV";
            }
            else
            {
                panelNameLooked = "MAP";
            }
        }

        private void CalculatePanelLookedForStrategy4(Vector2 gazePosition)
        {
            float panelHeight = Screen.height * 0.5f;
            float panelWidth = Screen.width * 0.5f;
            float leftBound = (Screen.width - panelWidth) / 2;
            float rightBound = leftBound + panelWidth;
            float bottomBound = (Screen.height - panelHeight) / 2;
            float topBound = bottomBound + panelHeight;

            if (gazePosition.x >= leftBound && gazePosition.x <= rightBound && gazePosition.y >= bottomBound && gazePosition.y <= topBound)
            {
                panelNameLooked = "FPVAR";
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
                case "TPV":
                    virtualTpvLowQualityWatchTime += deltaTime;
                    break;
                case "FPVAR":
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
            if (_eyeTracker == null || gazeWriter == null) return;

            var data = _eyeTracker.LatestGazeData;

            totalDataPoints++;

            bool validLeftGaze = data.Left.GazePointValid;
            bool validRightGaze = data.Right.GazePointValid;
            bool validLeftPupil = data.Left.PupilDiameterValid;
            bool validRightPupil = data.Right.PupilDiameterValid;

            if (!validLeftGaze || !validRightGaze)
            {
                invalidDataPoints++;
                if (timer.IsTimerRunning())
                {
                    UpdateInvalidDataPercentage();
                }
                return;
            }

            DateTime dateTime = DateTime.UtcNow;
            string timestamp = dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

            Vector2 gazePositionInPixels = GetMeanGazePositionInPixels(data);

            float leftPupilDiameter = data.Left.PupilDiameter;
            float rightPupilDiameter = data.Right.PupilDiameter;

            string leftGazeData = GetGazeDataString(data.Left);
            string rightGazeData = GetGazeDataString(data.Right);

            Vector2 gazePointOnDisplayl = data.Left.GazePointValid ? new Vector2(data.Left.GazePointOnDisplayArea.x, data.Left.GazePointOnDisplayArea.y) : Vector2.zero;
            Vector2 gazePointOnDisplayr = data.Right.GazePointValid ? new Vector2(data.Right.GazePointOnDisplayArea.x, data.Right.GazePointOnDisplayArea.y) : Vector2.zero;

            Vector2 meanGazePointOnDisplay = new Vector2(
                (gazePointOnDisplayl.x + gazePointOnDisplayr.x) / 2,
                (gazePointOnDisplayl.y + gazePointOnDisplayr.y) / 2
            );

            lock (gazeWriter)
            {
                gazeWriter.WriteLine($"{timestamp},{dateTime:yyyy-MM-dd HH:mm:ss.fff},{currentCountdown},{validLeftGaze},{validRightGaze},{meanGazePointOnDisplay.x},{meanGazePointOnDisplay.y},{gazePositionInPixels.x},{gazePositionInPixels.y}," +
                                     $"{validLeftPupil},{leftPupilDiameter},{validRightPupil},{rightPupilDiameter},{leftGazeData},{rightGazeData},{panelNameLooked}");
            }

            if (timer.IsTimerRunning())
            {
                UpdateInvalidDataPercentage();
            }
        }

        private string GetGazeDataString(IGazeDataEye gazeData)
        {
            return $"{gazeData.GazeOriginInTrackBoxCoordinates.x},{gazeData.GazeOriginInTrackBoxCoordinates.y},{gazeData.GazeOriginInTrackBoxCoordinates.z}," +
                   $"{gazeData.GazeOriginInUserCoordinates.x},{gazeData.GazeOriginInUserCoordinates.y},{gazeData.GazeOriginInUserCoordinates.z}," +
                   $"{gazeData.GazePointInUserCoordinates.x},{gazeData.GazePointInUserCoordinates.y},{gazeData.GazePointInUserCoordinates.z}," +
                   $"{gazeData.GazePointOnDisplayArea.x},{gazeData.GazePointOnDisplayArea.y},{gazeData.GazeRayScreen.origin.x},{gazeData.GazeRayScreen.origin.y},{gazeData.GazeRayScreen.origin.z}," +
                   $"{gazeData.GazeRayScreen.direction.x},{gazeData.GazeRayScreen.direction.y},{gazeData.GazeRayScreen.direction.z}";
        }

        private void UpdateInvalidDataPercentage()
        {
            float invalidPercentage = (totalDataPoints == 0) ? 0 : ((float)invalidDataPoints / totalDataPoints) * 100;
            invalidDataText.text = $"{invalidPercentage:F2}% ({invalidDataPoints}/{totalDataPoints})";
        }

        public void StartRecording(string filePath)
        {
            if (gazeWriter != null)
            {
                Debug.LogError("Gaze recording is already in progress.");
                return;
            }

            gazeWriter = new StreamWriter(filePath);
            gazeWriter.WriteLine("Timestamp,DateTime,Countdown,ValidLeftGaze,ValidRightGaze,XPixels,YPixels,ValidLeftPupil,LeftPupilDiameter,ValidRightPupil,RightPupilDiameter," +
                                 "LeftGazeOriginInTrackBoxCoordinatesX,LeftGazeOriginInTrackBoxCoordinatesY,LeftGazeOriginInTrackBoxCoordinatesZ," +
                                 "LeftGazeOriginInUserCoordinatesX,LeftGazeOriginInUserCoordinatesY,LeftGazeOriginInUserCoordinatesZ," +
                                 "LeftGazePointInUserCoordinatesX,LeftGazePointInUserCoordinatesY,LeftGazePointInUserCoordinatesZ," +
                                 "LeftGazePointOnDisplayAreaX,LeftGazePointOnDisplayAreaY,LeftGazeRayScreenOriginX,LeftGazeRayScreenOriginY,LeftGazeRayScreenOriginZ," +
                                 "LeftGazeRayScreenDirectionX,LeftGazeRayScreenDirectionY,LeftGazeRayScreenDirectionZ," +
                                 "RightGazeOriginInTrackBoxCoordinatesX,RightGazeOriginInTrackBoxCoordinatesY,RightGazeOriginInTrackBoxCoordinatesZ," +
                                 "RightGazeOriginInUserCoordinatesX,RightGazeOriginInUserCoordinatesY,RightGazeOriginInUserCoordinatesZ," +
                                 "RightGazePointInUserCoordinatesX,RightGazePointInUserCoordinatesY,RightGazePointInUserCoordinatesZ," +
                                 "RightGazePointOnDisplayAreaX,RightGazePointOnDisplayAreaY,RightGazeRayScreenOriginX,RightGazeRayScreenOriginY,RightGazeRayScreenOriginZ," +
                                 "RightGazeRayScreenDirectionX,RightGazeRayScreenDirectionY,RightGazeRayScreenDirectionZ,PanelNameLooked");

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

            string invalidChars = new string(Path.GetInvalidFileNameChars());
            if (participantID.IndexOfAny(invalidChars.ToCharArray()) >= 0)
            {
                Debug.LogError("Participant ID contains invalid characters.");
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
