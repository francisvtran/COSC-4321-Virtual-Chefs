using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CameraMovement : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {

        if (Input.GetKey("w"))
        {
            transform.Translate(new Vector3(0, 0, (float)0.1)); // * Time.deltaTime
        }
        if (Input.GetKey("a"))
        {
            transform.Translate(new Vector3((float)-0.1, 0, 0)); // * Time.deltaTime
        }
        if (Input.GetKey("d"))
        {
            transform.Translate(new Vector3((float)0.1, 0, 0)); // * Time.deltaTime
        }
        if (Input.GetKey("s"))
        {
            transform.Translate(new Vector3(0, 0, (float)-0.1)); // * Time.deltaTime
        }
        if (Input.GetMouseButton(0))
        {
            transform.Rotate(new Vector3(0, -10, 0));
        }
        if (Input.GetMouseButton(1))
        {
            transform.Rotate(new Vector3(0, 10, 0));
        }
    }
        
}
