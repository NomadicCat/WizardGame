using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SpawnInfo : MonoBehaviour
{
    [SerializeField] private float spawnDelay = 0f;

    private void Start()
    {
        gameObject.SetActive(false);
        Invoke(nameof(Initialize), spawnDelay);
    }

    private void Initialize()
    {
        gameObject.SetActive(true);
    }



}
