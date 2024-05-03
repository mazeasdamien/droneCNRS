using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class Timer : MonoBehaviour
{
    public TextMeshProUGUI timerText;
    public GameObject menu;
    public Button startButton;
    public float countdownTime;

    private float timeRemaining;
    private bool timerIsRunning = false;
    private StreamWriter writer;
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

    private Dictionary<string, (float duration, int count)> inputSummary = new Dictionary<string, (float duration, int count)>();
    private bool isLoggingStarted = false; // Add this at the class level
    private bool previousSouthButtonState = false;
    private float sessionStartTime;

    private int screenshotCount = 0;

    // Inside Timer class

    public int totalCollisions = 0;
    public float collisionPercentage = 0f;

    public void UpdateCollisionSummary(int collisionCount, float percentage)
    {
        totalCollisions = collisionCount;
        collisionPercentage = percentage;
    }

    // Inside Timer class
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
    }

    void Update()
    {
        if (timerIsRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                UpdateTimerDisplay(timeRemaining);
                LogInput();
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
        }
    }

    private void StartLogging()
    {
        if (!isLoggingStarted && timerIsRunning)
        {
            isLoggingStarted = true; // Set to true to avoid multiple logs
            InitializeWriters();
            writer.WriteLine($"Logging Start for Participant: {participantIDInput.text}, Environment: {environmentDropdown.options[environmentDropdown.value].text}, Strategy: {strategyDropdown.options[strategyDropdown.value].text}");
        }
    }

    private void InitializeWriters()
    {
        string folderPath = Path.Combine(Application.dataPath, "Data");
        Directory.CreateDirectory(folderPath); // This will only create if not exist

        if (writer != null)
        {
            writer.Close();
        }
        string logFileName = $"Log_{participantIDInput.text}_{environmentDropdown.options[environmentDropdown.value].text}_{strategyDropdown.options[strategyDropdown.value].text}.txt";
        writer = new StreamWriter(Path.Combine(folderPath, logFileName), true);
        writer.WriteLine($"Unity Time Before Countdown: {sessionStartTime}s");

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

        if (leftStickValues != Vector2.zero || rightStickValues != Vector2.zero || dpadUpValue != 0 || dpadDownValue != 0 || currentSouthButtonState != previousSouthButtonState)
        {
            string inputLog = $"Time Remaining: {Mathf.FloorToInt(timeRemaining / 60):00}:{Mathf.FloorToInt(timeRemaining % 60):00}, Left Stick: {leftStickValues}, Right Stick: {rightStickValues}, D-Pad Up: {dpadUpValue}, D-Pad Down: {dpadDownValue}, South Button: {currentSouthButtonState}";
            writer.WriteLine(inputLog);

            UpdateInputSummary("LeftStick", leftStickValues);
            UpdateInputSummary("RightStick", rightStickValues);
            UpdateInputSummary("DPadUp", new Vector2(dpadUpValue, 0));
            UpdateInputSummary("DPadDown", new Vector2(dpadDownValue, 0));

            // Check for South Button press
            if (currentSouthButtonState && !previousSouthButtonState)
            {
                UpdateInputSummary("SouthButton", new Vector2(1, 0));
                TakeScreenshot();  // Capture screenshot when the button is pressed
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
    }


    private void FinalizeLogging()
    {
        if (writer != null)
        {
            writer.WriteLine("Logging Session Ended");
            writer.Close();
            writer = null;
        }

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

            summaryWriter.Close();
            summaryWriter = null;
        }
    }

    private void OnApplicationQuit()
    {
        FinalizeLogging(); // Ensures all logs are properly closed when application quits
    }

    private void UpdateTimerDisplay(float timeToDisplay)
    {
        timeToDisplay += 1;
        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
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
    }

}