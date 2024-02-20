using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class HeadFollow : MonoBehaviour
{
    public Vector3 offset;
    public float smoothAmount = 1;
    private Transform head;
    // Start is called before the first frame update
    void Start()
    {
        head = Camera.main.transform;
    }
    private void OnEnable()
    {
        head = Camera.main.transform;
        Vector3 targetPos = head.TransformPoint(offset);
        Quaternion targetRot = Quaternion.Euler(new Vector3(0, head.eulerAngles.y, 0));

        transform.position = targetPos;
        transform.rotation = targetRot;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 targetPos = head.TransformPoint(offset);
        Quaternion targetRot = Quaternion.Euler(new Vector3(0, head.eulerAngles.y, 0));

        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * smoothAmount);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * smoothAmount);
    }
}