using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndTimerTrigger : MonoBehaviour
{
    [SerializeField] private LevelTimer Timer;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")){
            Timer.StopTimer();
        }
    }
}
