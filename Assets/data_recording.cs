using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Tobii.Research.Unity
{
    public class data_recording : MonoBehaviour
    {
        public static data_recording Instance { get; private set; }
        public float participantID;
        private StreamWriter _file;
        private string _folder = "Assets/Data"; // Path to save the CSV files.
        private bool _saveUnityData = true;
        private bool _saveRawData = true;

        [SerializeField]
        [Tooltip("If true, data is saved.")]
        private bool _saveData;

        private EyeTracker _eyeTracker;
        private bool _isRecording = false;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            _eyeTracker = EyeTracker.Instance;
        }

        public bool SaveData
        {
            get { return _saveData; }
            set { _saveData = value; }
        }

        public void ToggleRecording()
        {
            if (_isRecording)
            {
                StopRecording();
            }
            else
            {
                StartRecording();
            }
        }

        private void StartRecording()
        {
            OpenDataFile();
            _isRecording = true;
        }

        private void StopRecording()
        {
            if (_file != null)
            {
                _file.Close();
                _file = null;
            }
            _isRecording = false;
        }

        private void OpenDataFile()
        {
            if (!Directory.Exists(_folder))
            {
                Directory.CreateDirectory(_folder);
            }

            var fileName = $"gaze_data_{System.DateTime.Now:yyyyMMddTHHmmss}.csv";
            string filePath = Path.Combine(_folder, fileName);

            _file = new StreamWriter(filePath);

            // Write the CSV header.
            string header = "TimeStamp,LeftEye,RightEye,CombinedGazeRayScreen,RawGazeData";
            _file.WriteLine(header);
        }

        public void WriteGazeData(IGazeData gazeData)
        {
            if (!_isRecording || _file == null)
            {
                Debug.LogError("Recording is not started or CSV file is not open.");
                return;
            }

            string line = gazeData.TimeStamp.ToString();

            if (_saveUnityData)
            {
                line += "," + gazeData.Left.GazeOriginInTrackBoxCoordinates + "," + gazeData.Left.GazeOriginInUserCoordinates + "," + gazeData.Left.GazePointInUserCoordinates + "," + gazeData.Left.GazePointOnDisplayArea + "," + gazeData.Left.GazeRayScreen + "," + gazeData.Left.PupilDiameter;
                line += "," + gazeData.Right.GazeOriginInTrackBoxCoordinates + "," + gazeData.Right.GazeOriginInUserCoordinates + "," + gazeData.Right.GazePointInUserCoordinates + "," + gazeData.Right.GazePointOnDisplayArea + "," + gazeData.Right.GazeRayScreen + "," + gazeData.Right.PupilDiameter;
            }

            _file.WriteLine(line);
        }

        private void OnDestroy()
        {
            StopRecording(); // Ensure recording stops and data is saved when the object is destroyed.
        }
    }
}
