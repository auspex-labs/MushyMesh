﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Roll : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float input = Input.GetAxisRaw ("Horizontal");
        if (input != 0f) {
            GetComponent<Rigidbody2D> ().AddTorque (30f * Time.deltaTime * input);
        }   
    }
}
