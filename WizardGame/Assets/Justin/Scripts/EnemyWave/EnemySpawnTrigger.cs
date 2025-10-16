using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class EnemySpawnTrigger : MonoBehaviour
{
    private bool active = false;
    [SerializeField] private bool contributeUnlock = false;
    [SerializeField] private int contributeOnWave = 0;

    private bool canUnlock = false;
    private int currentWave = 0;
    
    private void Awake()
    {
        contributeOnWave -= 1;
        for (int i = 0; i < this.gameObject.transform.childCount; i++)
        {
            var Wave = this.gameObject.transform.GetChild(i);
            Debug.Log(Wave);
            Wave.gameObject.SetActive(false);

        }
        if (!contributeUnlock)
        {
            canUnlock = true;
        }

    }
    private void OnTriggerEnter(Collider other)
    {
        active = true;
        var objectName = other.gameObject.name;
        Debug.Log(objectName);
        if (objectName.CompareTo("Character") == 0)
        {
            active = true;
            Debug.Log("spawning enemies");
            //for (int i = 0; i < this.gameObject.transform.childCount; i++)
            //{
            //    var Wave = this.gameObject.transform.GetChild(i);
            //    Debug.Log(Wave);
            //    Wave.gameObject.SetActive(true);
            //}
        var Wave = this.gameObject.transform.GetChild(0);
        //    Debug.Log(Wave);
            Wave.gameObject.SetActive(true);
        }
    }

    private void Update()
    {


        if (active && currentWave < this.gameObject.transform.childCount)
        {
            if (contributeUnlock)
            {
                if (currentWave == contributeOnWave)
                {
                    Debug.Log("can unlock");
                    canUnlock = true;
                    contributeUnlock = false; // stop constant check
                }
            }

            var Wave = this.gameObject.transform.GetChild(currentWave);
            //Debug.Log($"currentWave: {currentWave}, contributeOnWave: {contributeOnWave}, WaveChildCount: {Wave.transform.childCount}");
            if (Wave.transform.childCount == 0 )
            {
                
                if (currentWave + 1 < this.gameObject.transform.childCount)
                {
                    



                        Debug.Log("spawning Next Wave");
                    currentWave += 1;
                    this.gameObject.transform.GetChild(currentWave).gameObject.SetActive(true);
                }
                else
                {
                    enabled = false;
                }
            }
        }
    }


    public bool getUnlock()
    {
        return this.canUnlock;
    }

}
