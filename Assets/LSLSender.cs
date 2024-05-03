using UnityEngine;
using LSL;

public class LSLSender : MonoBehaviour
{
    private liblsl.StreamOutlet outlet;
    private float[] sampleData;

    void Start()
    {
        // Define stream information
        liblsl.StreamInfo streamInfo = new liblsl.StreamInfo(
            "UnityStream",        // stream name
            "Data",               // stream type
            1,                    // number of channels
            Time.fixedDeltaTime,  // nominal sampling rate
            liblsl.channel_format_t.cf_float32,
            "myuniquelydentifier" // unique identifier for the stream
        );

        // Create an outlet with the stream information
        outlet = new liblsl.StreamOutlet(streamInfo);

        // Initialize data array
        sampleData = new float[1];
    }

    void FixedUpdate()
    {
        // Update sample data
        sampleData[0] = Time.frameCount; // Example: sending frame count

        // Send data through the LSL outlet
        outlet.push_sample(sampleData);
    }

    private void OnDestroy()
    {
        // If explicitly needed, clean up the outlet here.
        // However, generally, StreamOutlet is cleaned up by garbage collection.
        outlet = null;
    }
}
