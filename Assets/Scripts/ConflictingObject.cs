using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConflictingObject : MonoBehaviour
{
    private GameObject leftFinger;
    private GameObject rightFinger;
    private GameObject gripperController;
    private PositionControl gripperPositionController;

    private GameObject fingerController;
    private FingerPositionControl fingerPositionController;
    private Vector3 midFingerPoint;
    private float offset = 0.1f;

    private float nominalX;
    private float nominalY;
    private float nominalZ;
    private float nominalVolume;
    private float actualFingerWidth;

    [Rename("Deformation Param (λ)")]
    [Range(0, 1)]
    public float lambda = 0f;
    [ReadOnlyWhenPlaying]
    public bool referenceObject = false;
    [ReadOnlyWhenPlaying]
    [Tooltip("The reference stiffness is adjusted from the experiment controller script")]
    public float referenceStiffness = 1f;
    public float stiffness = 1f; // [N/mm]
    [ReadOnly]
    public float deltaStiffness = 0f;
    public float damping = 0.05f; // [Ns/mm]
    public float forceMultiplier = 2f;
    public float heightMultiplier = 2f;
    // public bool isotropic = true;
    // public bool slaughter = false;
    public float poisonRatio1 = 1.2f;
    [ReadOnly]
    private float poissonRatio2 = 1.2f;
    public bool isHard = false;
    [ReadOnly]
    public bool inContact = false;
    [ReadOnly]
    public float forceApplied = 0f;

    private SerialWriter serialWriter;
    private SerialListener serialListener;

    [ReadOnly]
    public int bodyIndex = 0;
    private int prevIndex = 0;
    private int prevIndexPermament = 0;
    int elapsedFrames = 0;
    int interpolationFramesCount = 7;

    // Start is called before the first frame update
    void Start()
    {
        leftFinger = GameObject.Find("finger_left");
        rightFinger = GameObject.Find("finger_right");
        gripperController = GameObject.Find("GripperController");
        gripperPositionController = gripperController.GetComponent<PositionControl>();

        fingerController = GameObject.Find("FingerController");
        fingerPositionController = fingerController.GetComponent<FingerPositionControl>();

        actualFingerWidth = gripperController.GetComponent<PositionControl>().actualFingerWidth;
        serialWriter = GameObject.Find("SerialWriter").GetComponent<SerialWriter>();
        serialListener = GameObject.Find("SerialListener").GetComponent<SerialListener>();

        nominalX = transform.localScale.x;
        nominalY = transform.localScale.y;
        nominalZ = transform.localScale.z;
        nominalVolume = nominalX * nominalY * nominalZ;
    }

    // Update is called once per frame
    void Update()
    {
        float deltaX = 0f;
        stiffness = Mathf.Max(0, stiffness);
        midFingerPoint = (leftFinger.transform.position + rightFinger.transform.position) / 2;

        // Interpolate the vector between two positions if the previous index is not the same as the current index
        if (prevIndex != bodyIndex)
        {
            prevIndexPermament = prevIndex;
            elapsedFrames = 0;
        }
        Vector3 interpolatedPosition = Vector3.Lerp(new Vector3(midFingerPoint.x + 0.2f * prevIndexPermament, transform.position.y, midFingerPoint.z + offset), new Vector3(midFingerPoint.x + 0.2f * bodyIndex, transform.position.y, midFingerPoint.z + offset), (float)elapsedFrames / interpolationFramesCount);
        transform.position = interpolatedPosition;
        
        actualFingerWidth = gripperController.GetComponent<PositionControl>().actualFingerWidth;
        forceApplied = serialListener.netForce;
        
        deltaStiffness = stiffness - referenceStiffness;

        if (bodyIndex == 0 && actualFingerWidth <= nominalX)
        {
            gripperPositionController.gripperInContact = true;
            fingerPositionController.fingerInContact = true;
            if (referenceObject)
            {
                deltaX = -forceMultiplier * forceApplied / ((1 - lambda) * 1000 * referenceStiffness + lambda * 1000 * (referenceStiffness + deltaStiffness));
                gripperPositionController.manipulatedFingerWidth = nominalX - deltaX;
            }
            else
            {
                deltaX = -forceMultiplier * forceApplied / ((1 - lambda) * 1000 * (referenceStiffness + deltaStiffness) + lambda * 1000 * referenceStiffness);
                gripperPositionController.manipulatedFingerWidth = nominalX - deltaX;
            }
            // float newZ = (nominalObjectDepth * (poisonRatio * positionController.manipulatedFingerWidth - positionController.manipulatedFingerWidth + Mathf.Pow((positionController.manipulatedFingerWidth * (positionController.manipulatedFingerWidth + 4 * poisonRatio * nominalObjectWidth - 2 * poisonRatio * positionController.manipulatedFingerWidth + poisonRatio * poisonRatio * positionController.manipulatedFingerWidth)), 0.5f))) / (2 * poisonRatio * positionController.manipulatedFingerWidth);
            // float newY = nominalObjectVolume / newZ / positionController.manipulatedFingerWidth;
            // float newVolume = newZ * newY * positionController.manipulatedFingerWidth;
            // float z = nominalZ;
            // float y = nominalY;
            // float x = nominalX;
            // float dx = -deltaX;
            // float v = poisonRatio;

            // float dz = -(z * (dx + x - Mathf.Pow((dx + x) * (dx + x - 2 * dx * v + 2 * v * x + dx * v * v + v * v * x), 0.5f) + dx * v + v * x)) / (2 * (dx * v + v * x));
            // float dy = v * dz * y / z;
            // float newZ = z + dz;
            // float newY = y + dy;
            // float newVolume = newZ * newY * positionController.manipulatedFingerWidth;
            // Debug.Log("New Volume: " + newVolume + " Old Volume: " + nominalVolume);
            // float newDepth = 0;
            // float newHeight = 0;

            // if(isotropic){
            //     newDepth = deltaX * poisonRatio * nominalObjectDepth + nominalObjectDepth;
            //     newHeight = deltaX * poisonRatio * nominalObjectHeight + nominalObjectHeight;
            // }
    
            // float newZ = nominalZ - deltaX;
            // float Y = nominalVolume / newZ / positionController.manipulatedFingerWidth;
            // float newY = nominalY + heightMultiplier * (Y - nominalY);


            float z = nominalZ;
            float y = nominalY;
            float x = nominalX;
            float dx = -deltaX;
            float v = poisonRatio1;
            float dy = -(dx * dx * v * y + dx * x * y + dx * v * x * y) / (dx * x + dx * dx * v + x * x + dx * v * x);
            float newY = y + dy;
            float newZ = nominalVolume / newY / gripperPositionController.manipulatedFingerWidth;
            poissonRatio2 = (dy / y) / (dx / x);
            transform.localScale = new Vector3(gripperPositionController.manipulatedFingerWidth, newY, newZ);
        }
        else if (bodyIndex == 0 && actualFingerWidth > nominalX)
        {
            gripperPositionController.gripperInContact = false;
            fingerPositionController.fingerInContact = false;
            transform.localScale = new Vector3(nominalX, nominalY, nominalZ);
        }
        // Debug.Log("Actual: " + actualFingerWidth + " Manipulated: " + deltaX + "Nominal: " + nominalObjectWidth);


        if (bodyIndex == 0)
        {
            float width = map(nominalX, 0.0027f * 2, 0.11f, ProjectVariables.FingerLowerSoftLimit, ProjectVariables.FingerUpperSoftLimit);
            if (!isHard)
            {
                if (!referenceObject)
                    serialWriter.SendPsuedoObject(width, stiffness, damping);
                else
                    serialWriter.SendPsuedoObject(width, referenceStiffness, damping);
            }
            else
                serialWriter.SendPsuedoObject(width, 20, damping);

            inContact = gripperPositionController.gripperInContact;
            inContact = fingerPositionController.fingerInContact;
        }
        prevIndex = bodyIndex;
        elapsedFrames++;
        elapsedFrames = Mathf.Min(elapsedFrames, interpolationFramesCount);
    }

    private float map(float value, float min, float max, float newMin, float newMax)
    {
        return (value - min) * (newMax - newMin) / (max - min) + newMin;
    }
}
