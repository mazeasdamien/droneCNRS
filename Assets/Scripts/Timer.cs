using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using Tobii.Research.Unity;

public class Timer : MonoBehaviour
{
    public TextMeshProUGUI timerText;
    public GameObject menu;
    public Button startButton;
    public float countdownTime;
    public AudioSource beepSound; // Add a field for the AudioSource

    private float timeRemaining;
    private bool timerIsRunning = false;
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

    // New field for the RenderTexture
    public RenderTexture renderTexture;

    private Dictionary<string, (float duration, int count)> inputSummary = new Dictionary<string, (float duration, int count)>();
    private Dictionary<string, float> panelWatchTime = new Dictionary<string, float>();
    private bool isLoggingStarted = false;
    private bool previousSouthButtonState = false;
    private float sessionStartTime;
    private int screenshotCount = 0;
    private int goodPhotoCount = 0;
    private int doubleWarningCount = 0;
    public int totalCollisions = 0;
    public float collisionPercentage = 0f;
    private HashSet<GameObject> photographedBodies = new HashSet<GameObject>();
    private List<int> beepTimes = new List<int> { 480, 420, 360, 300, 240, 180, 120, 60 };
    private float lastBeepTime = -1f;

    public void UpdateCollisionSummary(int collisionCount, float percentage)
    {
        totalCollisions = collisionCount;
        collisionPercentage = percentage;
    }

    public void UpdateCountdownTime(float newCountdownTime)
    {
        countdownTime = newCountdownTime;
    }

    void Start()
    {
        startButton.onClick.AddListener(StartCountdown);
        recordButton.onClick.AddListener(StartLogging);
        timeRemaining = countdownTime;
        UpdateTimerDisplay(timeRemaining);
        screenshotMessageText.gameObject.SetActive(false); // Hide the message at the start
        errorMessageText.gameObject.SetActive(false); // Hide the error message at the start

        InitializePanelWatchTimes();
    }

    private void InitializePanelWatchTimes()
    {
        int strategy = strategyDropdown.value;

        panelWatchTime.Clear();
        switch (strategy)
        {
            case 0:
                panelWatchTime["FPV"] = 0f;
                break;
            case 1:
                panelWatchTime["FPV"] = 0f;
                panelWatchTime["TPV"] = 0f;
                break;
            case 2:
                panelWatchTime["FPV"] = 0f;
                panelWatchTime["VIRTUAL TPV LOW QUALITY"] = 0f;
                panelWatchTime["MAP"] = 0f;
                break;
            case 3:
                panelWatchTime["FPV"] = 0f;
                panelWatchTime["VIRTUAL TPV HIGH QUALITY"] = 0f;
                panelWatchTime["MAP"] = 0f;
                break;
        }
    }

    void Update()
    {
        if (timerIsRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                UpdateTimerDisplay(timeRemaining);
                GazeTrail.Instance?.UpdateCountdownValue(timeRemaining); // Update countdown value in GazeTrail
                DronePathRecorder.Instance?.UpdateCountdownValue(timeRemaining); // Update countdown value in DronePathRecorder
                DroneControl.Instance?.UpdateCountdownValue(timeRemaining); // Update countdown value in DroneControl
                LogInput();
                HandleBeepSound(); // Handle beep sound logic
            }
            else
            {
                EndSession();
            }
        }
    }

    private void StartCountdown()
    {
        if (!timerIsRunning)
        {
            timerIsRunning = true;
            timeRemaining = countdownTime;
            menu.SetActive(false);
            sessionStartTime = Time.time;  // Store the start time
            StartLogging();
            StartGazeRecording(); // Start recording gaze data
            StartDronePathRecording(); // Start recording drone path data
            StartInputLogging(); // Start recording input data
        }
    }

    private void StartLogging()
    {
        if (!isLoggingStarted && timerIsRunning)
        {
            isLoggingStarted = true; // Set to true to avoid multiple logs
            InitializeWriters();
        }
    }

    private void InitializeWriters()
    {
        string folderPath = Path.Combine(Application.dataPath, "Data");
        Directory.CreateDirectory(folderPath); // This will only create if not exist

        if (summaryWriter != null)
        {
            summaryWriter.Close();
        }
        string summaryFileName = $"Summary_{participantIDInput.text}_{environmentDropdown.options[environmentDropdown.value].text}_{strategyDropdown.options[strategyDropdown.value].text}.txt";
        summaryWriter = new StreamWriter(Path.Combine(folderPath, summaryFileName), false); // Open new file for each session
    }

    private void LogInput()
    {
        Vector2 leftStickValues = leftstick.action.ReadValue<Vector2>();
        Vector2 rightStickValues = rightstick.action.ReadValue<Vector2>();
        float dpadUpValue = dpadup.action.ReadValue<float>();
        float dpadDownValue = dpaddown.action.ReadValue<float>();
        bool currentSouthButtonState = southbutton.action.ReadValue<float>() > 0;

        // Check if any input has occurred
        if (leftStickValues != Vector2.zero || rightStickValues != Vector2.zero || dpadUpValue != 0 || dpadDownValue != 0 || currentSouthButtonState != previousSouthButtonState)
        {
            // Update the summary for input usage
            UpdateInputSummary("LeftStick", leftStickValues);
            UpdateInputSummary("RightStick", rightStickValues);
            UpdateInputSummary("DPadUp", new Vector2(dpadUpValue, 0));
            UpdateInputSummary("DPadDown", new Vector2(dpadDownValue, 0));

            // Specific handling for South Button press
            if (currentSouthButtonState && !previousSouthButtonState)
            {
                UpdateInputSummary("SouthButton", new Vector2(1, 0));
                GameObject nearestBody = GetNearestBody();
                if (nearestBody != null)
                {
                    if (!photographedBodies.Contains(nearestBody))
                    {
                        photographedBodies.Add(nearestBody);
                        goodPhotoCount++;
                        TakeScreenshot();  // Capture screenshot when the button is pressed and drone is close enough
                    }
                    else
                    {
                        doubleWarningCount++;
                        DisplayWarningMessage();  // Display warning message if the body is already photographed
                    }
                }
                else
                {
                    DisplayErrorMessage();  // Display error message if the drone is too far
                }
            }
        }

        previousSouthButtonState = currentSouthButtonState;
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
        GameObject nearestBody = null;
        float minDistance = float.MaxValue;

        foreach (var body in bodies)
        {
            float distance = Vector3.Distance(drone.transform.position, body.transform.position);
            if (distance < 3.0f && distance < minDistance)
            {
                minDistance = distance;
                nearestBody = body;
            }
        }

        return nearestBody;
    }

    private void TakeScreenshot()
    {
        // Increment screenshot counter
        screenshotCount++;

        // Build the screenshot file name
        string screenshotFileName = $"Screenshot_{participantIDInput.text}_{environmentDropdown.options[environmentDropdown.value].text}_{strategyDropdown.options[strategyDropdown.value].text}_{screenshotCount}.jpg";
        string folderPath = Path.Combine(Application.dataPath, "Data");
        string screenshotPath = Path.Combine(folderPath, screenshotFileName);

        // Capture the screenshot
        ScreenCapture.CaptureScreenshot(screenshotPath);

        Debug.Log($"Screenshot taken: {screenshotPath}");

        // Instantiate the prefab at the rawImage position
        InstantiatePrefabAtRawImagePosition();

        // Start coroutine to display screenshot message
        StartCoroutine(DisplayScreenshotMessage());
    }

    private void InstantiatePrefabAtRawImagePosition()
    {
        if (screenshotPrefab != null && screenshotCanvas != null)
        {
            // Instantiate the prefab
            GameObject instantiatedPrefab = Instantiate(screenshotPrefab, screenshotCanvas.transform);

            // Position the instantiated prefab at the rawImage position
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

        // Define the color orange
        Color orangeColor = new Color(1f, 0.5f, 0f);

        // Change text color to orange
        screenshotMessageText.color = orangeColor;

        screenshotMessageText.text = "This body has already been photographed!";
        screenshotMessageText.gameObject.SetActive(true);
        yield return new WaitForSeconds(3);
        screenshotMessageText.gameObject.SetActive(false);

        // Reset text color to its original color (assuming it's white)
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
        if (beepSound != null)
        {
            int roundedTimeRemaining = Mathf.CeilToInt(timeRemaining);

            // Play beep at each minute (08:00, 07:00, etc.)
            if (beepTimes.Contains(roundedTimeRemaining))
            {
                beepSound.Play();
                beepTimes.Remove(roundedTimeRemaining); // Remove to avoid replaying at the same time
            }

            // Play beep every second during the last 30 seconds
            if (roundedTimeRemaining <= 30 && roundedTimeRemaining > 0 && Time.time - lastBeepTime >= 1f)
            {
                beepSound.Play();
                lastBeepTime = Time.time;
            }
        }
    }

    private void EndSession()
    {
        if (droneCollisionDetector != null)
        {
            droneCollisionDetector.UpdateCollisionSummary(); // Ensure this gets called before finalizing logs
        }
        timerIsRunning = false;
        isLoggingStarted = false; // Reset the flag for the next session
        UpdateTimerDisplay(0);
        menu.SetActive(true);
        FinalizeLogging();
        SaveRenderTexture(); // Save the render texture as a JPG file
        StopGazeRecording(); // Stop recording gaze data
        StopDronePathRecording(); // Stop recording drone path data
        StopInputLogging(); // Stop recording input data
    }

    private void FinalizeLogging()
    {
        if (summaryWriter != null)
        {
            foreach (var item in inputSummary)
            {
                if (item.Key == "SouthButton")
                {
                    // For South Button, log both count and duration
                    summaryWriter.WriteLine($"{item.Key} was pressed {item.Value.count} times.");
                }
                else
                {
                    // For other inputs, log only the duration
                    summaryWriter.WriteLine($"{item.Key} was used for a total of {item.Value.duration} seconds.");
                }
            }

            // Write collision data to summary file
            summaryWriter.WriteLine($"Total Collisions: {totalCollisions}");
            summaryWriter.WriteLine($"Collision Percentage of Countdown Time: {collisionPercentage}%");

            // Calculate total session time
            float totalSessionTime = countdownTime;

            // Write panel watch time data to summary file
            foreach (var panel in panelWatchTime)
            {
                float watchTime = panel.Value;
                float watchPercentage = (watchTime / totalSessionTime) * 100;
                summaryWriter.WriteLine($"{panel.Key} was watched for {watchTime} seconds ({watchPercentage:F2}%).");
            }

            // Write photo data to summary file
            summaryWriter.WriteLine($"Good Photos Taken: {goodPhotoCount}");
            summaryWriter.WriteLine($"Double Warning Photo Attempts: {doubleWarningCount}");

            summaryWriter.Close();
            summaryWriter = null;
        }
    }

    private void SaveRenderTexture()
    {
        if (renderTexture != null)
        {
            // Create a new Texture2D with the same dimensions as the RenderTexture
            Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);

            // Copy the RenderTexture content to the Texture2D
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();
            RenderTexture.active = null;

            // Encode the Texture2D to a JPG
            byte[] bytes = texture.EncodeToJPG();

            // Build the file path
            string folderPath = Path.Combine(Application.dataPath, "Data");
            string fileName = $"RenderTexture_{participantIDInput.text}_{environmentDropdown.options[environmentDropdown.value].text}_{strategyDropdown.options[strategyDropdown.value].text}.jpg";
            string filePath = Path.Combine(folderPath, fileName);

            // Save the encoded JPG to the file
            File.WriteAllBytes(filePath, bytes);

            Debug.Log($"RenderTexture saved as JPG: {filePath}");
        }
        else
        {
            Debug.LogError("RenderTexture is not assigned.");
        }
    }

    public void UpdatePanelWatchTime(string panelName, float deltaTime)
    {
        if (panelWatchTime.ContainsKey(panelName))
        {
            panelWatchTime[panelName] += deltaTime;
        }
    }

    public void SetSessionStartTime(float time)
    {
        sessionStartTime = time;
    }

    public float GetSessionStartTime()
    {
        return sessionStartTime;
    }

    private void OnApplicationQuit()
    {
        FinalizeLogging(); // Ensures all logs are properly closed when application quits
        StopGazeRecording(); // Stop recording gaze data
        StopDronePathRecording(); // Stop recording drone path data
        StopInputLogging(); // Stop recording input data
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
            string folderPath = Path.Combine(Application.dataPath, "Data");
            Directory.CreateDirectory(folderPath); // Create directory if it doesn't exist
            string filePath = Path.Combine(folderPath, $"GazeData_{participantIDInput.text}_{environmentDropdown.options[environmentDropdown.value].text}_{strategyDropdown.options[strategyDropdown.value].text}.csv");
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
            string folderPath = Path.Combine(Application.dataPath, "Data");
            Directory.CreateDirectory(folderPath); // Create directory if it doesn't exist
            string filePath = Path.Combine(folderPath, $"PathData_{participantIDInput.text}_{environmentDropdown.options[environmentDropdown.value].text}_{strategyDropdown.options[strategyDropdown.value].text}.csv");
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
            string folderPath = Path.Combine(Application.dataPath, "Data");
            Directory.CreateDirectory(folderPath); // Create directory if it doesn't exist
            string filePath = Path.Combine(folderPath, $"GamepadInputLog_{participantIDInput.text}_{environmentDropdown.options[environmentDropdown.value].text}_{strategyDropdown.options[strategyDropdown.value].text}.csv");
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
}
