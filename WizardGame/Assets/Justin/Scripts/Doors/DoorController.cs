using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    [SerializeField] private List<Keys> keysList = new List<Keys>();
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
        if(collected == keysList.Count)
        {
            animator.SetTrigger("Open");
        }


    }
}
