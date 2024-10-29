using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpringObject : MonoBehaviour
{
    private GameObject leftFinger;
    private GameObject rightFinger;
    private GameObject gripperController;
    private PositionControl positionController;
    private Vector3 midFingerPoint;
    private float offset = 0.1f;

    private float nominalObjectWidth;
    private float nominalObjectHeight;
    private float nominalObjectDepth;
    private float nominalObjectVolume;
    private float actualFingerWidth;

    public float stiffness = 2f; // [N/mm]
    public float damping = 0.05f; // [Ns/mm]
    public float poisonRatio = 1.2f;
    public bool isHard = false;
    [ReadOnly]
    public bool inContact = false;
    [ReadOnly]
    public float forceApplied = 0f;

    private SerialWriter serialWriter;
    private SerialListener serialListener;

    [ReadOnly]
    public int bodyIndex = 0;

    // Start is called before the first frame update
    void Start()
    {
        leftFinger = GameObject.Find("finger_left");
        rightFinger = GameObject.Find("finger_right");
        gripperController = GameObject.Find("GripperController");
        positionController = gripperController.GetComponent<PositionControl>();

        actualFingerWidth = gripperController.GetComponent<PositionControl>().actualFingerWidth;
        serialWriter = GameObject.Find("SerialWriter").GetComponent<SerialWriter>();
        serialListener = GameObject.Find("SerialListener").GetComponent<SerialListener>();

        nominalObjectWidth = transform.localScale.x;
        nominalObjectHeight = transform.localScale.y;
        nominalObjectDepth = transform.localScale.z;
        nominalObjectVolume = nominalObjectWidth * nominalObjectHeight * nominalObjectDepth;
    }

    // Update is called once per frame
    void Update()
    {
        stiffness = Mathf.Max(0, stiffness);
        midFingerPoint = (leftFinger.transform.position + rightFinger.transform.position) / 2;
        transform.position = new Vector3(midFingerPoint.x + 0.2f * bodyIndex, transform.position.y, midFingerPoint.z + offset);
        actualFingerWidth = gripperController.GetComponent<PositionControl>().actualFingerWidth;
        forceApplied = serialListener.netForce;


        if (bodyIndex == 0 && actualFingerWidth <= nominalObjectWidth)
        {
            positionController.gripperInContact = true;
            float new_depth = (nominalObjectDepth * (poisonRatio * actualFingerWidth - actualFingerWidth + Mathf.Pow((actualFingerWidth * (actualFingerWidth + 4 * poisonRatio * nominalObjectWidth - 2 * poisonRatio * actualFingerWidth + poisonRatio * poisonRatio * actualFingerWidth)), 0.5f))) / (2 * poisonRatio * actualFingerWidth);
            float new_height = nominalObjectVolume / new_depth / actualFingerWidth;
            transform.localScale = new Vector3(actualFingerWidth, new_height, new_depth);
        }
        else if (bodyIndex == 0 && actualFingerWidth > nominalObjectWidth)
        {
            positionController.gripperInContact = false;
            transform.localScale = new Vector3(nominalObjectWidth, nominalObjectHeight, nominalObjectDepth);
        }

        if (bodyIndex == 0)
        {
            if (positionController.gripperInContact)
            {
                float width = map(nominalObjectWidth, 0, 0.11f, ProjectVariables.FingerLowerSoftLimit, ProjectVariables.FingerUpperSoftLimit);
                if (!isHard)
                    serialWriter.SendPsuedoObject(width, stiffness, damping);
                else
                    serialWriter.SendPsuedoObject(width, 20, damping);
            }
            else
            {
                float width = map(nominalObjectWidth, 0, 0.11f, ProjectVariables.FingerLowerSoftLimit, ProjectVariables.FingerUpperSoftLimit);
                if (!isHard)
                    serialWriter.SendPsuedoObject(width, stiffness, damping);
                else
                    serialWriter.SendPsuedoObject(width, 20, damping);
            }
            inContact = positionController.gripperInContact;
        }
    }

    private float map(float value, float min, float max, float newMin, float newMax)
    {
        return (value - min) * (newMax - newMin) / (max - min) + newMin;
    }
}
