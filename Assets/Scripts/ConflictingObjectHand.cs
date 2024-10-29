using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConflictingObjectHand : MonoBehaviour
{
    private Mesh mesh;

    private GameObject leftFinger;
    private GameObject rightFinger;
    private GameObject gripperController;
    private PositionControl gripperPositionController;

    private GameObject handController;
    private FingerPositionControl fingerPositionController;

    private float initialPosY;
    private float nominalX;
    private float nominalY;
    private float nominalZ;
    private float nominalVolume;
    private float qefFingerWidth;

    [Rename("Deformation Param (λ)")]
    [Range(0, 1)]
    public float lambda = 0f;
    [ReadOnlyWhenPlaying]
    public bool referenceObject = false;
    [ReadOnlyWhenPlaying]
    [Tooltip("The reference stiffness is adjusted from the experiment controller script")]
    public float referenceStiffness = 2f;
    public float stiffness = 1f; // [N/mm]
    [ReadOnly]
    public float deltaStiffness = 0f;
    public float damping = 0.05f; // [Ns/mm]
    // public float forceMultiplier = 1f;
    // public float heightMultiplier = 1f;
    [ReadOnly]
    public float poissonsRatio = 0.5f;
    [ReadOnlyWhenPlaying]
    [Tooltip("Press N to toggle the resistance of the object")]
    public bool noResistance = false;
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
        mesh = GetComponent<MeshFilter>().mesh;
        handController = GameObject.Find("HandController");
        fingerPositionController = handController.GetComponent<FingerPositionControl>();

        serialWriter = GameObject.Find("SerialWriter").GetComponent<SerialWriter>();
        serialListener = GameObject.Find("SerialListener").GetComponent<SerialListener>();

        nominalX = transform.localScale.x;
        nominalY = transform.localScale.y;
        nominalZ = transform.localScale.z;
        nominalVolume = nominalX * nominalY * nominalZ;

        initialPosY = transform.position.y;
    }

    // Update is called once per frame
    void Update()
    {
        float deltaX = 0f;
        stiffness = Mathf.Max(0, stiffness);

        // Make the object invisible if no resistance is enabled
        GetComponent<Renderer>().enabled = !noResistance;


        Vector3 fingersTouchingPoint = handController.GetComponent<FingerPositionControl>().fingersTouchingPoint;
        float m = Mathf.Abs(handController.GetComponent<FingerPositionControl>().thumbTipClosedPosition.x - handController.GetComponent<FingerPositionControl>().thumbTipOpenPosition.x);
        float n = Mathf.Abs(handController.GetComponent<FingerPositionControl>().indexTipClosedPosition.x - handController.GetComponent<FingerPositionControl>().indexTipOpenPosition.x);

        float w = transform.localScale.x;
        float a = w * m / (m + n);
        float b = w - a;
        float newPosX = m + (b - a) / 2;

        // Interpolate the vector between two positions if the previous index is not the same as the current index
        if (prevIndex != bodyIndex)
        {
            prevIndexPermament = prevIndex;
            elapsedFrames = 0;
        }
        float interPolationAmount = (float)elapsedFrames / interpolationFramesCount;


        float spacingFactor = GameObject.Find("Objects").GetComponent<ObjectSelector>().objectSpacing;
        float handToObjectOffset = GameObject.Find("Objects").GetComponent<ObjectSelector>().handToObjectOffset;
        float altitudeOffset = GameObject.Find("Objects").GetComponent<ObjectSelector>().altitudeOffset;
        poissonsRatio = GameObject.Find("Objects").GetComponent<ObjectSelector>().poissonsRatio;
        lambda = GameObject.Find("Objects").GetComponent<ObjectSelector>().lambda;
        Vector3 interpolateFrom = new Vector3(newPosX + (spacingFactor * 0.2f) * prevIndexPermament, initialPosY + altitudeOffset, fingersTouchingPoint.z + 0.01f + handToObjectOffset);
        Vector3 interpolateTo = new Vector3(newPosX + (spacingFactor * 0.2f) * bodyIndex, initialPosY + altitudeOffset, fingersTouchingPoint.z + 0.01f + handToObjectOffset);
        Vector3 interpolatedPosition = Vector3.Lerp(interpolateFrom, interpolateTo, interPolationAmount);

        transform.position = interpolatedPosition;

        qefFingerWidth = handController.GetComponent<FingerPositionControl>().qefFingerAperture;
        forceApplied = serialListener.netForce;

        deltaStiffness = stiffness - referenceStiffness;

        if (bodyIndex == 0 && qefFingerWidth <= (nominalX))
        {
            // gripperPositionController.gripperInContact = true;
            fingerPositionController.fingerInContact = true;
            float forceMultiplier = GameObject.Find("Objects").GetComponent<ObjectSelector>().forceMultiplier  * 1f;
            float heightMultiplier = GameObject.Find("Objects").GetComponent<ObjectSelector>().heightMultiplier;
            if (referenceObject)
            {
                deltaX = -forceMultiplier * forceApplied / ((1 - lambda) * 1000 * referenceStiffness + lambda * 1000 * (referenceStiffness + deltaStiffness));
                fingerPositionController.manipulatedFingerAperture = nominalX - deltaX;
            }
            else
            {
                deltaX = -forceMultiplier * forceApplied / ((1 - lambda) * 1000 * (referenceStiffness + deltaStiffness) + lambda * 1000 * referenceStiffness);
                fingerPositionController.manipulatedFingerAperture = nominalX - deltaX;
            }

            bool constantVolume = GameObject.Find("Objects").GetComponent<ObjectSelector>().constantVolume;

            if (constantVolume)
            {
                float z = nominalZ;
                float y = nominalY;
                float x = nominalX;
                float dx = -deltaX / forceMultiplier;
                float v = poissonsRatio;
                float dy = heightMultiplier * (-(dx * dx * v * y + dx * x * y + dx * v * x * y) / (dx * x + dx * dx * v + x * x + dx * v * x));
                float newY = y + dy;
                float newZ = nominalVolume / newY / fingerPositionController.manipulatedFingerAperture;
                transform.localScale = new Vector3(fingerPositionController.manipulatedFingerAperture, newY, newZ);
            }
            else
            {
                float strain = poissonsRatio * deltaX / nominalX;
                float newZ = nominalZ * (1 + strain);
                float newY = nominalY * (1 + strain * heightMultiplier);
                transform.localScale = new Vector3(fingerPositionController.manipulatedFingerAperture, newY, newZ);
            }

            // Print the diffrence between the nominal volume and the actual volume
            // Debug.Log("dV: " + ((nominalVolume - (transform.localScale.x * transform.localScale.y * transform.localScale.z)) * 1e9).ToString("0.0") + " mm^3");
        }
        else if (bodyIndex == 0 && qefFingerWidth > nominalX)
        {
            fingerPositionController.fingerInContact = false;
            transform.localScale = new Vector3(nominalX, nominalY, nominalZ);
        }

        if (bodyIndex == 0)
        {   
            fingerPositionController.objectDeterminesFingerAperture = !noResistance;
            float width = map(nominalX, fingerPositionController.minFingerAperture, fingerPositionController.maxFingerAperture, ProjectVariables.FingerLowerSoftLimit, ProjectVariables.FingerUpperSoftLimit);
            if (noResistance)
            {
                serialWriter.SendPsuedoObject(width, 0, 0);
            }
            else if (!isHard)
            {
                if (!referenceObject)
                    serialWriter.SendPsuedoObject(width, stiffness, damping);
                else
                    serialWriter.SendPsuedoObject(width, referenceStiffness, damping);
            }
            else
                serialWriter.SendPsuedoObject(width, 20, damping);

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
