using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParameterControl : MonoBehaviour
{
    public float stiffness = 1000f;
    public float damping = 1000f;
    public float forceLimit = 100f;

    private GameObject rightGripper;
    private GameObject leftGripper;

    private ArticulationBody rightArticulation;
    private ArticulationBody leftArticulation;
    // Start is called before the first frame update
    void Start()
    {   
        rightGripper = GameObject.Find("gripper_right");
        leftGripper = GameObject.Find("gripper_left");

        rightArticulation = rightGripper.GetComponent<ArticulationBody>();
        leftArticulation = leftGripper.GetComponent<ArticulationBody>();

        setDriveParams(rightArticulation, stiffness, damping, forceLimit);
        setDriveParams(leftArticulation, stiffness, damping, forceLimit);

    }

    // Update is called once per frame
    void Update()
    {
        setDriveParams(rightArticulation, stiffness, damping, forceLimit);
        setDriveParams(leftArticulation, stiffness, damping, forceLimit);
    }

    private void setDriveParams(ArticulationBody joint, float stiffness, float damping, float forceLimit)
    {
        ArticulationDrive drive = joint.xDrive;
        drive.stiffness = stiffness;
        drive.damping = damping;
        drive.forceLimit = forceLimit;
        joint.xDrive = drive;
    }
}
