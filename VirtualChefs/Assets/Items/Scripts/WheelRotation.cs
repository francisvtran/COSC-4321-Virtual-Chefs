using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelRotation : MonoBehaviour
{
    public float turn;
    // Start is called before the first frame update
    void Start()
    {
        //hide it
        if (turn == null)
        {
            turn = 0;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        transform.Rotate(new Vector3(0, turn, 0)); // * Time.deltaTime
    }
}
