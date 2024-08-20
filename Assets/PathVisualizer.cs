using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;
using System;

[ExecuteInEditMode]
public class PathVisualizer : MonoBehaviour
{
    public string csvDirectory = "CSVFiles/e2"; // Directory relative to Assets where CSV files are stored
    public GameObject originObject;
    public Material pathMaterial;

    private float totalTimeInsideBuildings;
    [TextArea]
    public string allTimings; // This will display the timings for all files

    private List<Vector3> positions = new List<Vector3>();
    private List<float> timestamps = new List<float>();  // List to store timestamps from CSV as seconds
    private string participantID; // Store ParticipantID for naming
    private List<string> timingsList = new List<string>(); // List to store timings for all CSV files

    void OnEnable()
    {
        if (!Application.isPlaying)
        {
            ClearPreviousVisualizations();
            ProcessAllCsvFiles();
            DisplayAllTimings();
        }
    }

    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            ClearPreviousVisualizations();
            ProcessAllCsvFiles();
            DisplayAllTimings();
        }
    }

    void ProcessAllCsvFiles()
    {
        string folderPath = Path.Combine(Application.dataPath, csvDirectory);
        string[] csvFiles = Directory.GetFiles(folderPath, "*.csv");

        string resultFilePath = Path.Combine(Application.dataPath, "Results", "Result.csv");
        Directory.CreateDirectory(Path.GetDirectoryName(resultFilePath)); // Ensure the directory exists

        using (StreamWriter writer = new StreamWriter(resultFilePath))
        {
            // Write the header for the result CSV
            writer.WriteLine("ParticipantID,InterfaceID,Environment,TimeInsideBuilding");

            foreach (string csvFile in csvFiles)
            {
                LoadAndProcessCsvFile(csvFile, writer);
                VisualizePath();
            }
        }

        Debug.Log($"Results written to {resultFilePath}");
    }

    void LoadAndProcessCsvFile(string csvFilePath, StreamWriter writer)
    {
        positions.Clear();
        timestamps.Clear();

        if (string.IsNullOrEmpty(csvFilePath))
        {
            Debug.LogError("CSV file path is not set.");
            return;
        }

        if (!File.Exists(csvFilePath))
        {
            Debug.LogError($"CSV file not found at path: {csvFilePath}");
            return;
        }

        try
        {
            using (StreamReader reader = new StreamReader(csvFilePath))
            {
                bool isFirstLine = true;
                DateTime startTime = DateTime.MinValue;

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (isFirstLine)
                    {
                        isFirstLine = false;
                        continue; // Skip the header line
                    }

                    var values = line.Split(',');

                    if (values.Length < 6)
                    {
                        Debug.LogError("CSV file format is incorrect or missing data.");
                        return;
                    }

                    DateTime dateTime;
                    float posX, posY, posZ;

                    if (DateTime.TryParseExact(values[0], "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime) &&
                        float.TryParse(values[3], out posX) &&
                        float.TryParse(values[4], out posY) &&
                        float.TryParse(values[5], out posZ))
                    {
                        if (startTime == DateTime.MinValue)
                        {
                            startTime = dateTime;
                        }

                        Vector3 position = new Vector3(posX, posY, posZ);
                        positions.Add(position);
                        timestamps.Add((float)(dateTime - startTime).TotalSeconds);
                    }
                    else
                    {
                        Debug.LogError("Error parsing data from CSV.");
                        return;
                    }
                }
            }

            totalTimeInsideBuildings = CalculateTimeInsideBuildings();
            timingsList.Add($"Participant {participantID}: {totalTimeInsideBuildings:F2} seconds inside");

            WriteResultToCsv(csvFilePath, writer);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to load CSV: " + e.Message);
        }
    }

    float CalculateTimeInsideBuildings()
    {
        float totalTimeInside = 0f;
        bool currentlyInside = false;
        float entryTime = 0f;

        for (int i = 0; i < positions.Count - 1; i++)
        {
            Vector3 start = originObject.transform.TransformPoint(positions[i]);
            Vector3 end = originObject.transform.TransformPoint(positions[i + 1]);
            float startTime = timestamps[i];
            float endTime = timestamps[i + 1];

            bool isInside = IsInsideBuilding(start, end);

            if (isInside && !currentlyInside)
            {
                entryTime = startTime;
                currentlyInside = true;
            }
            else if (!isInside && currentlyInside)
            {
                totalTimeInside += (startTime - entryTime);
                currentlyInside = false;
            }

            if (i == positions.Count - 2 && currentlyInside)
            {
                totalTimeInside += (endTime - entryTime);
            }
        }

        return totalTimeInside;
    }

    void WriteResultToCsv(string csvFilePath, StreamWriter writer)
    {
        string fileName = Path.GetFileNameWithoutExtension(csvFilePath);

        // Extract ParticipantID, InterfaceID, and Environment number from the file name
        Match participantMatch = Regex.Match(fileName, @"PathData_(\d+)_");
        Match interfaceMatch = Regex.Match(fileName, @"Interface(\d+)");
        Match environmentMatch = Regex.Match(fileName, @"Environment(\d+)");

        string environmentID = environmentMatch.Success ? environmentMatch.Groups[1].Value : ExtractEnvironmentIDFromDirectory(csvFilePath);

        if (!participantMatch.Success || !interfaceMatch.Success || !environmentMatch.Success)
        {
            Debug.LogError($"Could not extract ParticipantID, InterfaceID, and Environment from the file name: {fileName}");
            return;
        }

        participantID = participantMatch.Groups[1].Value;
        string interfaceID = interfaceMatch.Groups[1].Value;

        // Append the result to the output CSV
        writer.WriteLine($"{participantID},{interfaceID},{environmentID},{totalTimeInsideBuildings:F2}");
    }

    string ExtractEnvironmentIDFromDirectory(string csvFilePath)
    {
        // Extract the directory name that contains the CSV file (e.g., "e1" or "e2")
        string directoryName = Path.GetFileName(Path.GetDirectoryName(csvFilePath));
        Match environmentMatch = Regex.Match(directoryName, @"e(\d+)");

        if (environmentMatch.Success)
        {
            return environmentMatch.Groups[1].Value;
        }

        Debug.LogError($"Could not extract EnvironmentID from directory name: {directoryName}");
        return "Unknown";
    }

    void VisualizePath()
    {
        if (positions.Count == 0)
        {
            Debug.LogWarning("No positions loaded from the CSV.");
            return;
        }

        if (originObject == null)
        {
            Debug.LogError("Origin Object is not assigned.");
            return;
        }

        GameObject pathObject = new GameObject($"DronePath_{participantID}");
        pathObject.transform.parent = this.transform;

        LineRenderer lineRenderer = pathObject.AddComponent<LineRenderer>();
        lineRenderer.material = pathMaterial;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.positionCount = positions.Count;
        lineRenderer.useWorldSpace = true;

        for (int i = 0; i < positions.Count; i++)
        {
            Vector3 worldPosition = originObject.transform.TransformPoint(positions[i]);
            lineRenderer.SetPosition(i, worldPosition);
        }
    }

    bool IsInsideBuilding(Vector3 start, Vector3 end)
    {
        Vector3 midPoint = (start + end) / 2;
        Vector3 segmentDirection = (end - start).normalized;
        float segmentLength = Vector3.Distance(start, end);

        Collider[] hitColliders = Physics.OverlapBox(midPoint, new Vector3(0.1f, 0.1f, segmentLength / 2), Quaternion.LookRotation(segmentDirection));

        return hitColliders.Length > 0;
    }

    void ClearPreviousVisualizations()
    {
        foreach (Transform child in transform)
        {
            DestroyImmediate(child.gameObject);
        }
    }

    void DisplayAllTimings()
    {
        allTimings = string.Join("\n", timingsList);
    }
}
