using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tobii.Research.Unity;

public class Timer : MonoBehaviour
{
    public TextMeshProUGUI timerText;
    public GameObject menu;
    public Button startButton;
    public float countdownTime;
    public AudioSource beepSound;
    public TextMeshProUGUI collisionWarningText;

    private float timeRemaining;
    private bool timerIsRunning = false;
    private bool isCountdownFrozen = false;
    private StreamWriter summaryWriter;

    [Header("Input References")]
    public InputActionReference rightstick;
    public InputActionReference leftstick;
    public InputActionReference dpadup;
    public InputActionReference dpaddown;
    public InputActionReference southbutton;

    public DroneCollisionDetector droneCollisionDetector;
    public Button recordButton;
    public TMP_InputField participantIDInput;
    public TMP_Dropdown environmentDropdown;
    public TMP_Dropdown strategyDropdown;
    public GameObject screenshotPrefab;
    public Canvas screenshotCanvas;
    public TextMeshProUGUI screenshotMessageText;
    public TextMeshProUGUI errorMessageText;
    public GameObject drone;

    public RenderTexture renderTexture;

    public AudioClip beepClip;

    private Dictionary<string, (float duration, int count)> inputSummary = new Dictionary<string, (float duration, int count)>();
    private bool isLoggingStarted = false;
    private bool previousSouthButtonState = false;
    private float sessionStartTime;
    private int screenshotCount = 0;
    private int doubleWarningPhotoAttempts = 0;
    private HashSet<GameObject> photographedBodies = new HashSet<GameObject>();
    private HashSet<Vector2Int> visitedSquares = new HashSet<Vector2Int>();

    private float lastBeepTime = -1f;
    private GridGenerator gridGenerator;

    [Header("Debug Info")]
    public int visitedSquaresCount;

    void Start()
    {
        startButton.onClick.AddListener(StartCountdown);
        recordButton.onClick.AddListener(StartLogging);
        InitializeUI();

        // Initialize grid reference
        gridGenerator = FindObjectOfType<GridGenerator>();
    }

    void Update()
    {
        if (timerIsRunning && !isCountdownFrozen)
        {
            UpdateCountdown();
        }

        visitedSquaresCount = visitedSquares.Count;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            GameObject nearestBody = GetNearestBody();
            if (nearestBody != null)
            {
                if (!photographedBodies.Contains(nearestBody))
                {
                    photographedBodies.Add(nearestBody);
                    TakeScreenshot();
                }
                else
                {
                    doubleWarningPhotoAttempts++; // Increment double warning photo attempts
                    DisplayWarningMessage();
                }
            }
            else
            {
                DisplayErrorMessage();
            }
        }
    }

    private void InitializeUI()
    {
        timeRemaining = countdownTime;
        UpdateTimerDisplay(timeRemaining);
        screenshotMessageText.gameObject.SetActive(false);
        errorMessageText.gameObject.SetActive(false);
    }

    private void UpdateCountdown()
    {
        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            UpdateTimerDisplay(timeRemaining);
            UpdateGazeAndDroneCountdown(timeRemaining);
            LogInput();
            HandleBeepSound();
        }
        else
        {
            EndSession();
        }
    }

    private void UpdateGazeAndDroneCountdown(float time)
    {
        GazeTrail.Instance?.UpdateCountdownValue(time);
        DronePathRecorder.Instance?.UpdateCountdownValue(time);
        DroneControl.Instance?.UpdateCountdownValue(time);
    }

    public void FreezeCountdown()
    {
        isCountdownFrozen = true;
    }

    public void UnfreezeCountdown()
    {
        isCountdownFrozen = false;
    }

    public void DisplayCollisionWarning(bool display)
    {
        collisionWarningText.gameObject.SetActive(display);
    }

    private void StartCountdown()
    {
        if (!timerIsRunning)
        {
            timerIsRunning = true;
            timeRemaining = countdownTime;
            menu.SetActive(false);
            sessionStartTime = Time.time;
            StartLogging();
            StartGazeRecording();
            StartDronePathRecording();
            StartInputLogging();

            // Clear visited squares
            visitedSquares.Clear();
        }
    }

    private void StartLogging()
    {
        if (!isLoggingStarted && timerIsRunning)
        {
            isLoggingStarted = true;
            InitializeWriters();
        }
    }

    private void InitializeWriters()
    {
        string folderPath = GetExperimentFolderPath();
        if (summaryWriter != null) summaryWriter.Close();

        string summaryFileName = GetSummaryFileName();
        summaryWriter = new StreamWriter(Path.Combine(folderPath, summaryFileName), false);
        summaryWriter.WriteLine("ParticipantID,StrategyUsed,EnvironmentUsed,LeftStickUsageDuration,RightStickUsageDuration,DPadUpUsageDuration,DPadDownUsageDuration,SouthButtonPressed,TotalCollisions,TotalTimeInCollision,BodyFounded,DoubleWarningPhotoAttempts,TotalSquaresVisited,FPV,TPV,FPVAR,MAP");
    }

    private string GetParticipantFolderPath()
    {
        string participantID = participantIDInput.text;
        if (string.IsNullOrEmpty(participantID))
        {
            Debug.LogError("Participant ID is empty.");
            return string.Empty;
        }
        DateTime currentDate = DateTime.Now;
        string formattedDate = currentDate.ToString("ddMMyyyy");
        string folderPath = Path.Combine(Application.dataPath, $"{participantID}_{formattedDate}");
        Directory.CreateDirectory(folderPath);
        return folderPath;
    }

    private string GetExperimentFolderPath()
    {
        string participantFolderPath = GetParticipantFolderPath();
        string strategyUsed = strategyDropdown.options[strategyDropdown.value].text;
        string environmentUsed = environmentDropdown.options[environmentDropdown.value].text;
        string experimentFolderPath = Path.Combine(participantFolderPath, $"{strategyUsed}_{environmentUsed}");
        Directory.CreateDirectory(experimentFolderPath);
        return experimentFolderPath;
    }

    private string GetSummaryFileName()
    {
        string participantID = participantIDInput.text;
        return $"Summary_{participantID}_{strategyDropdown.options[strategyDropdown.value].text}_{environmentDropdown.options[environmentDropdown.value].text}.csv";
    }

    private void LogInput()
    {
        Vector2 leftStickValues = leftstick.action.ReadValue<Vector2>();
        Vector2 rightStickValues = rightstick.action.ReadValue<Vector2>();
        float dpadUpValue = dpadup.action.ReadValue<float>();
        float dpadDownValue = dpaddown.action.ReadValue<float>();
        bool currentSouthButtonState = southbutton.action.ReadValue<float>() > 0;

        if (IsInputOccurred(leftStickValues, rightStickValues, dpadUpValue, dpadDownValue, currentSouthButtonState))
        {
            UpdateInputSummaries(leftStickValues, rightStickValues, dpadUpValue, dpadDownValue, currentSouthButtonState);
            HandleSouthButtonPress(currentSouthButtonState);
        }

        previousSouthButtonState = currentSouthButtonState;
    }

    private bool IsInputOccurred(Vector2 leftStick, Vector2 rightStick, float dpadUp, float dpadDown, bool southButton)
    {
        return leftStick != Vector2.zero || rightStick != Vector2.zero || dpadUp != 0 || dpadDown != 0 || southButton != previousSouthButtonState;
    }

    private void UpdateInputSummaries(Vector2 leftStick, Vector2 rightStick, float dpadUp, float dpadDown, bool southButton)
    {
        UpdateInputSummary("LeftStick", leftStick);
        UpdateInputSummary("RightStick", rightStick);
        UpdateInputSummary("DPadUp", new Vector2(dpadUp, 0));
        UpdateInputSummary("DPadDown", new Vector2(dpadDown, 0));
        if (southButton && !previousSouthButtonState) UpdateInputSummary("SouthButton", Vector2.one);
    }

    private void HandleSouthButtonPress(bool currentSouthButtonState)
    {
        if (currentSouthButtonState && !previousSouthButtonState)
        {

            GameObject nearestBody = GetNearestBody();
            if (nearestBody != null)
            {
                if (!photographedBodies.Contains(nearestBody))
                {
                    photographedBodies.Add(nearestBody);
                    TakeScreenshot();
                }
                else
                {
                    doubleWarningPhotoAttempts++; // Increment double warning photo attempts
                    DisplayWarningMessage();
                }
            }
            else
            {
                DisplayErrorMessage();
            }
        }
    }

    private void UpdateInputSummary(string inputName, Vector2 inputValues)
    {
        if (inputValues != Vector2.zero)
        {
            if (!inputSummary.ContainsKey(inputName))
            {
                inputSummary[inputName] = (0, 0);
            }
            var (duration, count) = inputSummary[inputName];
            inputSummary[inputName] = (duration + (inputName == "SouthButton" ? 0 : Time.deltaTime), count + Mathf.RoundToInt(inputValues.x + inputValues.y));
        }
    }

    private GameObject GetNearestBody()
    {
        GameObject[] bodies = GameObject.FindGameObjectsWithTag("BODY");
        return bodies.OrderBy(body => Vector3.Distance(drone.transform.position, body.transform.position))
                     .FirstOrDefault(body => Vector3.Distance(drone.transform.position, body.transform.position) < 4.0f);
    }

    private void TakeScreenshot()
    {
        screenshotCount++;
        string screenshotFileName = $"Screenshot_{participantIDInput.text}_{strategyDropdown.options[strategyDropdown.value].text}_{environmentDropdown.options[environmentDropdown.value].text}_{screenshotCount}.jpg";
        string screenshotPath = Path.Combine(GetExperimentFolderPath(), screenshotFileName);
        ScreenCapture.CaptureScreenshot(screenshotPath);
        InstantiatePrefabAtRawImagePosition();
        StartCoroutine(DisplayScreenshotMessage());
    }

    private void InstantiatePrefabAtRawImagePosition()
    {
        if (screenshotPrefab != null && screenshotCanvas != null)
        {
            GameObject instantiatedPrefab = Instantiate(screenshotPrefab, screenshotCanvas.transform);
            RectTransform rawImageRect = FollowDrone.Instance.rawImage.rectTransform;
            instantiatedPrefab.GetComponent<RectTransform>().anchoredPosition = rawImageRect.anchoredPosition;
        }
    }

    private IEnumerator DisplayScreenshotMessage()
    {
        HideErrorMessage();
        screenshotMessageText.gameObject.SetActive(true);
        yield return new WaitForSeconds(3);
        screenshotMessageText.gameObject.SetActive(false);
    }

    private void DisplayErrorMessage()
    {
        StartCoroutine(ShowErrorMessage());
    }

    private IEnumerator ShowErrorMessage()
    {
        HideScreenshotMessage();
        errorMessageText.gameObject.SetActive(true);
        yield return new WaitForSeconds(3);
        errorMessageText.gameObject.SetActive(false);
    }

    private void DisplayWarningMessage()
    {
        StartCoroutine(ShowWarningMessage());
    }

    private IEnumerator ShowWarningMessage()
    {
        HideScreenshotMessage();
        HideErrorMessage();
        screenshotMessageText.color = new Color(1f, 0.5f, 0f);
        screenshotMessageText.text = "This body has already been photographed!";
        screenshotMessageText.gameObject.SetActive(true);
        yield return new WaitForSeconds(3);
        screenshotMessageText.gameObject.SetActive(false);
        screenshotMessageText.color = Color.white;
        screenshotMessageText.text = "Screenshot captured successfully!";
    }

    private void HideScreenshotMessage()
    {
        screenshotMessageText.gameObject.SetActive(false);
    }

    private void HideErrorMessage()
    {
        errorMessageText.gameObject.SetActive(false);
    }

    private void HandleBeepSound()
    {
        if (beepSound != null && beepClip != null)
        {
            int roundedTimeRemaining = Mathf.CeilToInt(timeRemaining);

            if (roundedTimeRemaining <= 30)
            {
                if (roundedTimeRemaining <= 10)
                {
                    if (Time.time - lastBeepTime >= 1f)
                    {
                        beepSound.clip = beepClip;
                        beepSound.Play();
                        lastBeepTime = Time.time;
                    }
                }
                else if (roundedTimeRemaining % 5 == 0 && Time.time - lastBeepTime >= 1f)
                {
                    beepSound.clip = beepClip;
                    beepSound.Play();
                    lastBeepTime = Time.time;
                }
            }

            // Beep sound every minute
            if (roundedTimeRemaining % 60 == 0 && Time.time - lastBeepTime >= 1f)
            {
                beepSound.clip = beepClip;
                beepSound.Play();
                lastBeepTime = Time.time;
            }
        }
    }

    private void EndSession()
    {
        timerIsRunning = false;
        isLoggingStarted = false;
        UpdateTimerDisplay(0);
        menu.SetActive(true);
        FinalizeLogging();
        SaveRenderTexture();
        StopGazeRecording();
        StopDronePathRecording();
        StopInputLogging();
    }

    private void FinalizeLogging()
    {
        if (summaryWriter != null)
        {
            string participantID = participantIDInput.text;
            string environmentUsed = environmentDropdown.options[environmentDropdown.value].text;
            string strategyUsed = strategyDropdown.options[strategyDropdown.value].text;

            // Fetching input durations
            float leftStickUsageDuration = inputSummary.ContainsKey("LeftStick") ? inputSummary["LeftStick"].duration : 0f;
            float rightStickUsageDuration = inputSummary.ContainsKey("RightStick") ? inputSummary["RightStick"].duration : 0f;
            float dpadUpUsageDuration = inputSummary.ContainsKey("DPadUp") ? inputSummary["DPadUp"].duration : 0f;
            float dpadDownUsageDuration = inputSummary.ContainsKey("DPadDown") ? inputSummary["DPadDown"].duration : 0f;
            int southButtonPressed = inputSummary.ContainsKey("SouthButton") ? inputSummary["SouthButton"].count : 0;

            // Fetching panel watch times from GazeTrail
            float fpvPanelWatchTime = GazeTrail.Instance.fpvWatchTime;
            float fpvarWatchTime = GazeTrail.Instance.FPVARWatchTime;
            float tpvWatchTime = GazeTrail.Instance.tpvWatchTime;
            float mapWatchTime = GazeTrail.Instance.mapWatchTime;

            // Fetching collision data from DroneCollisionDetector
            float totalCollisionTime = droneCollisionDetector.totalCollisionTime;
            int totalCollisions = droneCollisionDetector.CollisionNumber;

            summaryWriter.WriteLine($"{participantID},{strategyUsed},{environmentUsed},{leftStickUsageDuration},{rightStickUsageDuration},{dpadUpUsageDuration},{dpadDownUsageDuration},{southButtonPressed},{totalCollisions},{totalCollisionTime},{photographedBodies.Count},{doubleWarningPhotoAttempts},{visitedSquares.Count},{fpvPanelWatchTime},{tpvWatchTime},{fpvarWatchTime},{mapWatchTime}");
            summaryWriter.Close();
            summaryWriter = null;
        }
    }

    private void SaveRenderTexture()
    {
        Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture.Apply();
        RenderTexture.active = null;

        byte[] bytes = texture.EncodeToJPG();
        string filePath = Path.Combine(GetExperimentFolderPath(), $"Map_{participantIDInput.text}_{strategyDropdown.options[strategyDropdown.value].text}_{environmentDropdown.options[environmentDropdown.value].text}.jpg");
        File.WriteAllBytes(filePath, bytes);
    }

    private void OnApplicationQuit()
    {
        FinalizeLogging();
        StopGazeRecording();
        StopDronePathRecording();
        StopInputLogging();
    }

    private void UpdateTimerDisplay(float timeToDisplay)
    {
        timeToDisplay += 1;
        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void StartGazeRecording()
    {
        if (GazeTrail.Instance != null)
        {
            string filePath = Path.Combine(GetExperimentFolderPath(), $"GazeData_{participantIDInput.text}_{strategyDropdown.options[strategyDropdown.value].text}_{environmentDropdown.options[environmentDropdown.value].text}.csv");
            GazeTrail.Instance.StartRecording(filePath);
        }
    }

    private void StopGazeRecording()
    {
        if (GazeTrail.Instance != null)
        {
            GazeTrail.Instance.StopRecording();
        }
    }

    private void StartDronePathRecording()
    {
        if (DronePathRecorder.Instance != null)
        {
            string filePath = Path.Combine(GetExperimentFolderPath(), $"PathData_{participantIDInput.text}_{strategyDropdown.options[strategyDropdown.value].text}_{environmentDropdown.options[environmentDropdown.value].text}.csv");
            DronePathRecorder.Instance.StartRecording(filePath);
        }
    }

    private void StopDronePathRecording()
    {
        if (DronePathRecorder.Instance != null)
        {
            DronePathRecorder.Instance.StopRecording();
        }
    }

    private void StartInputLogging()
    {
        if (DroneControl.Instance != null)
        {
            string filePath = Path.Combine(GetExperimentFolderPath(), $"GamepadInputLog_{participantIDInput.text}_{strategyDropdown.options[strategyDropdown.value].text}_{environmentDropdown.options[environmentDropdown.value].text}.csv");
            DroneControl.Instance.StartInputLogging(filePath);
        }
    }

    private void StopInputLogging()
    {
        if (DroneControl.Instance != null)
        {
            DroneControl.Instance.StopInputLogging();
        }
    }

    public void RecordVisitedSquare(Vector3 position)
    {
        if (timerIsRunning && gridGenerator != null)
        {
            Vector2Int gridSquare = gridGenerator.GetGridSquare(position);
            visitedSquares.Add(gridSquare);
        }
    }

    public bool IsTimerRunning()
    {
        return timerIsRunning;
    }

    public float GetTimeRemaining()
    {
        return timeRemaining;
    }
}
