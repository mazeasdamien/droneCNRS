using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentSwitcher : MonoBehaviour
{
    public GameObject[] gameObjects;
    internal int currentIndex = 0;

    void Awake()
    {
        UpdateActiveGameObject();
    }

    public void SetActiveObject(int index)
    {
        currentIndex = index;
        UpdateActiveGameObject();
    }

    private void UpdateActiveGameObject()
    {
        for (int i = 0; i < gameObjects.Length; i++)
        {
            if (gameObjects[i] != null)
            {
                gameObjects[i].SetActive(i == currentIndex);
            }
        }
    }

    public GameObject GetCurrentGameObject()
    {
        return gameObjects[currentIndex];
    }

}
