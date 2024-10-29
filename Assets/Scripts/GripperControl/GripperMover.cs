using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GripperMover : MonoBehaviour
{
    GameObject gripper;
    // Start is called before the first frame update
    void Start()
    {
        gripper = GameObject.Find("wsg_50");
    }

    // Update is called once per frame
    void Update()
    {
        // Translate the gripper object on the plane with the arrow keys
        if (Input.GetKey(KeyCode.UpArrow))
        {
            gripper.transform.Translate(0, 0, 0.1f);
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            gripper.transform.Translate(0, 0, -0.1f);
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            gripper.transform.Translate(-0.1f, 0, 0);
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            gripper.transform.Translate(0.1f, 0, 0);
        }
    }
}
