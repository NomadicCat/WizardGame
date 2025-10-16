using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Keys : MonoBehaviour
{
    private bool collected = false;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
           collected = true;
            gameObject.SetActive(false) ;
        }
    }

    public bool getCollected()
    {
        return collected;
    }

}
