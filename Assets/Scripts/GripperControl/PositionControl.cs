using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PositionControl : MonoBehaviour
{
    private GameObject rightGripper;
    private GameObject leftGripper;

    private ArticulationBody rightArticulation;
    private ArticulationBody leftArticulation;


    private GameObject serialListener;

    // Max and min values for the gripper fingers from the center of the gripper
    private float minFingerWidth;
    private float maxFingerWidth;

    public float manipulatedFingerWidth;
    public float manipulatedDistanceFromCenter = 1;

    public float actualFingerWidth;

    [Rename("Gripper Drives Object")]
    // [Tooltip("When enabled, the object being gripped will ")]
    public bool gripperDeterminesObjectSize = true;

    [ReadOnly]
    public bool gripperInContact = false;

    // Start is called before the first frame update
    void Start()
    {
        rightGripper = GameObject.Find("gripper_right");
        leftGripper = GameObject.Find("gripper_left");

        rightArticulation = rightGripper.GetComponent<ArticulationBody>();
        leftArticulation = leftGripper.GetComponent<ArticulationBody>();

        maxFingerWidth = Math.Max(Math.Abs(rightArticulation.xDrive.upperLimit), Math.Abs(rightArticulation.xDrive.lowerLimit));
        minFingerWidth = Math.Min(Math.Abs(leftArticulation.xDrive.upperLimit), Math.Abs(leftArticulation.xDrive.lowerLimit));

        serialListener = GameObject.Find("SerialListener");
    }

    // Update is called once per frame
    void Update()
    {
        float[] hapticDevicePositions = serialListener.GetComponent<SerialListener>().positions;
        float hapticFingerPosition = hapticDevicePositions[0];

        float actualDistanceFromCenter = Mathf.Clamp(map(hapticFingerPosition / 2, ProjectVariables.FingerUpperSoftLimit / 2, ProjectVariables.FingerLowerSoftLimit, minFingerWidth, maxFingerWidth), minFingerWidth, maxFingerWidth);

        // calculatedWidth = actualDistanceFromCenter;
        // actualDistanceFromCenter = 0.03f;
        actualFingerWidth = actualDistanceFromCenter * 2;
        manipulatedDistanceFromCenter = manipulatedFingerWidth / 2;

        if (!gripperDeterminesObjectSize && gripperInContact)
        {
            setPosition(rightArticulation, manipulatedDistanceFromCenter);
            setPosition(leftArticulation, manipulatedDistanceFromCenter);
        }
        else
        {
            setPosition(rightArticulation, actualDistanceFromCenter);
            setPosition(leftArticulation, actualDistanceFromCenter);
        }
    }

    // Set the gripper to the given position
    private void setPosition(ArticulationBody joint, float position)
    {
        ArticulationDrive drive = joint.xDrive;
        drive.target = drive.upperLimit > 0 ? position : -position;
        joint.xDrive = drive;
    }

    private float map(float value, float min, float max, float newMin, float newMax)
    {
        return (value - min) * (newMax - newMin) / (max - min) + newMin;
    }
}
