using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DoorController : MonoBehaviour
{
    [SerializeField] private List<Keys> keysList = new List<Keys>();
    [SerializeField] private List<EnemySpawnTrigger> waves = new List<EnemySpawnTrigger>();
    [SerializeField] private Animator animator;

    private int unlock = 0;
    void Start()
    {

        foreach(var key in keysList)
        {
           if(key.getCollected() == false)
            {
                unlock += 1;
            }
        }
    }

    
    void Update()
    {
        int collected = 0;
        foreach (var key in keysList)
        {
            if (key.getCollected() == true)
            {
                collected += 1;
            }
        }

        bool canUnlock = true;
        foreach (var key in waves)
        {
            if(key.getUnlock() == false)
            {
                canUnlock = false;
            }
        }
        if (collected == keysList.Count && canUnlock)
        {
            animator.SetTrigger("Open");
        }



    }
}
