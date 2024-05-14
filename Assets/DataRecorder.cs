using System;
using System.Collections;
using System.IO;
using TMPro;
using Tobii.Research.Unity;
using UnityEngine;
using UnityEngine.UI;

public class DataRecorder : MonoBehaviour
{
    private StreamWriter _streamWriter;
    private bool _isRecording = false;
    private string _filePath;

    public TMP_InputField participantIdInput;
    public TMP_Dropdown strategyDropdown;
    public TMP_Dropdown environmentDropdown;

    public Canvas canvas;
    public RawImage left;
    public RawImage right;

    private void StartRecording()
    {
        string participantId = participantIdInput.text;
        string strategy = strategyDropdown.options[strategyDropdown.value].text;
        string environment = environmentDropdown.options[environmentDropdown.value].text;
        _filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                                 $"participant_{participantId}_strategy{strategy}_environment{environment}.csv");
        _streamWriter = new StreamWriter(_filePath, append: false);
        _streamWriter.WriteLine("FormattedTime,RawTimestamp,LeftEyeValid,LeftPupilDiameter,LeftGazePointX,LeftGazePointY,RightEyeValid,RightPupilDiameter,RightGazePointX,RightGazePointY");
        StartCoroutine(RecordData());
        _isRecording = true;
        Debug.Log("Recording started at: " + _filePath);
    }

    private IEnumerator RecordData()
    {
        EyeTracker eyeTracker = EyeTracker.Instance;
        while (_isRecording && eyeTracker != null)
        {
            IGazeData data = eyeTracker.NextData;
            if (data != null)
            {
                string formattedTime = DateTime.Now.ToString("HH:mm:ss");
                long rawTimestamp = DateTime.Now.Ticks;
                string line = $"{formattedTime},{rawTimestamp},{data.Left.PupilDiameterValid},{data.Left.PupilDiameter},{data.Left.GazePointOnDisplayArea.x},{data.Left.GazePointOnDisplayArea.y},{data.Right.PupilDiameterValid},{data.Right.PupilDiameter},{data.Right.GazePointOnDisplayArea.x},{data.Right.GazePointOnDisplayArea.y}";

                MoveRawImage(left, data.Left.GazePointOnDisplayArea);
                MoveRawImage(right, data.Right.GazePointOnDisplayArea);

                _streamWriter.WriteLine(line);
            }
            yield return new WaitForSeconds(1f / 60f); // Record at 60 FPS
        }
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

    private void StopRecording()
    {
        _isRecording = false;
        if (_streamWriter != null)
        {
            _streamWriter.Close();
        }
        Debug.Log("Recording stopped");
    }

    private void MoveRawImage(RawImage image, Vector2 normalizedPosition)
    {
        Vector2 screenPosition = new Vector2(normalizedPosition.x * Screen.width, (1 - normalizedPosition.y) * Screen.height);
        Vector2 canvasPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, screenPosition, canvas.worldCamera, out canvasPosition))
        {
            image.rectTransform.anchoredPosition = canvasPosition;
        }
    }
}
