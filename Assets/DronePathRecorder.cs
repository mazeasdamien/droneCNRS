using System;
using System.IO;
using UnityEngine;

public class DronePathRecorder : MonoBehaviour
{
    public static DronePathRecorder Instance { get; private set; }

    private StreamWriter pathWriter;
    private bool isRecording = false;
    private GameObject originObject;
    private Transform droneTransform;
    private float currentCountdown = 0f;

    private void Awake()
    {
        Instance = this;
    }

    public void Initialize(GameObject origin, Transform drone)
    {
        originObject = origin;
        droneTransform = drone;
    }

    private void Update()
    {
        if (isRecording)
        {
            RecordPathData();
        }
    }

    public void StartRecording(string filePath)
    {
        pathWriter = new StreamWriter(filePath);
        pathWriter.WriteLine("Timestamp,DateTime,Countdown,LocalPosX,LocalPosY,LocalPosZ,LocalRotX,LocalRotY,LocalRotZ,WorldPosX,WorldPosY,WorldPosZ,WorldRotX,WorldRotY,WorldRotZ");
        isRecording = true;
    }

    public void StopRecording()
    {
        isRecording = false;
        if (pathWriter != null)
        {
            pathWriter.Close();
            pathWriter = null;
        }
    }

    public void UpdateCountdownValue(float countdownValue)
    {
        currentCountdown = countdownValue;
    }

    private void RecordPathData()
    {
        if (droneTransform == null || originObject == null) return;

        DateTime dateTime = DateTime.UtcNow;
        string timestamp = dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

        // Local position and rotation
        Vector3 localPosition = originObject.transform.InverseTransformPoint(droneTransform.position);
        Vector3 localRotation = droneTransform.localEulerAngles;

        // World position and rotation
        Vector3 worldPosition = droneTransform.position;
        Vector3 worldRotation = droneTransform.eulerAngles;

        pathWriter.WriteLine($"{timestamp},{dateTime:yyyy-MM-dd HH:mm:ss.fff},{currentCountdown},{localPosition.x},{localPosition.y},{localPosition.z},{localRotation.x},{localRotation.y},{localRotation.z},{worldPosition.x},{worldPosition.y},{worldPosition.z},{worldRotation.x},{worldRotation.y},{worldRotation.z}");
    }
}
