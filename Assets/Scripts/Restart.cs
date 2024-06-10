using UnityEngine;
using UnityEngine.SceneManagement;

public class Restart : MonoBehaviour
{
    public void RestartApp()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
