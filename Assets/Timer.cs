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
    public float countdownTime = 180;

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

    public Button recordButton;
    public TMP_InputField participantIDInput;
    public TMP_Dropdown environmentDropdown;
    public TMP_Dropdown strategyDropdown;

    private Dictionary<string, (float duration, int count)> inputSummary = new Dictionary<string, (float duration, int count)>();
    private bool isLoggingStarted = false; // Add this at the class level
    private bool previousSouthButtonState = false;

    // Inside Timer class

    private int totalCollisions = 0;
    private float collisionPercentage = 0f;

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

            // Update for South Button
            if (currentSouthButtonState && !previousSouthButtonState)  // Only log if button was pressed and was previously not pressed
            {
                UpdateInputSummary("SouthButton", new Vector2(1, 0));
            }
        }

        previousSouthButtonState = currentSouthButtonState; // Update the state for the next frame
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
}
