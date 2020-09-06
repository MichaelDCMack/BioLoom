using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSWatcher : MonoBehaviour
{
    public float targetFrameRate = 60;

    public int maxOperations = 100;
    public int minOperations = 1;

    public int AllowedOperations { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float currentFrameRate = 1f / Time.deltaTime;

        if (currentFrameRate >= targetFrameRate)
        {
            ++AllowedOperations;
        }
        else
        {
            --AllowedOperations;
        }

        if(AllowedOperations > maxOperations)
        {
            AllowedOperations = maxOperations;
        }
        if(AllowedOperations < minOperations)
        {
            AllowedOperations = minOperations;
        }
    }
}
