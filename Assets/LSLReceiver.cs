using UnityEngine;
using LSL;

public class LSLReceiver : MonoBehaviour
{
    // Keep a list or single reference to inlets if needed for longer than Start()
    private liblsl.StreamInlet inlet;

    void Start()
    {
        Debug.Log("Searching for LSL streams...");
        // Try resolving streams immediately on start
        var streams = liblsl.resolve_streams();

        if (streams.Length > 0)
        {
            Debug.Log($"Found {streams.Length} streams:");
            foreach (var stream in streams)
            {
                // Create an inlet
                inlet = new liblsl.StreamInlet(stream);
                var info = inlet.info();
                Debug.Log($"Stream Name: {info.name()}, Type: {info.type()}, Channel Count: {info.channel_count()}, Sampling Rate: {info.nominal_srate()}");

                // Close the inlet if not needed beyond this point
                inlet.close_stream();  // Check if there is a close or dispose method available in your LSL library
            }
        }
        else
        {
            Debug.LogError("No LSL streams found.");
        }
    }

    private void OnDestroy()
    {
        // Properly close the inlet when the GameObject is destroyed or script is disabled
        if (inlet != null)
        {
            inlet.close_stream();  // Again, ensure the method to close is correctly named based on the LSL library documentation
        }
    }
}
