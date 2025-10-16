using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class LevelTimer : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText; // Reference to UI text

    private float currentTime;
    private bool isRunning = true;

    void Start()
    {
        currentTime = 0f;
    }

    void Update()
    {
        if (!isRunning) return;

        // Update the timer
        currentTime += Time.deltaTime;


        // Update the UI (optional)
        if (timerText != null)
        {
            timerText.text = FormatTime(currentTime);
        }
    }

    public void StopTimer()
    {
        isRunning = false;
        Debug.Log("Timer stopped! Final time: " + FormatTime(currentTime));
    }

    public float GetTime() => currentTime;

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        int milliseconds = Mathf.FloorToInt((time * 1000f) % 1000f);
        return $"{minutes:00}:{seconds:00}:{milliseconds:000}";
    }
}
