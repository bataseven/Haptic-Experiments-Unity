using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FingerPositionControl : MonoBehaviour
{
    private SerialListener serialListener;
    public float hapticDevicePos = 0;

    GameObject indexJoint1;
    GameObject indexJoint2;
    GameObject indexJoint3;
    GameObject indexTip;
    GameObject thumbJoint2;
    GameObject thumbJoint3;
    GameObject thumbTip;

    private Quaternion indexJoint1QuaternionOpen;
    private Quaternion indexJoint2QuaternionOpen;
    private Quaternion indexJoint3QuaternionOpen;

    private Quaternion thumbJoint2QuaternionOpen;
    private Quaternion thumbJoint3QuaternionOpen;

    private Quaternion indexJoint1QuaternionClosed = Quaternion.Euler(-66.285f, -10.738f, 183.142f);
    private Quaternion indexJoint2QuaternionClosed = Quaternion.Euler(-18.045f, -7.507f, -82.933f);
    private Quaternion indexJoint3QuaternionClosed = Quaternion.Euler(0f, 0f, -20.177f);

    private Quaternion thumbJoint2QuaternionClosed = Quaternion.Euler(5.354f, 26.415f, -15.324f);
    private Quaternion thumbJoint3QuaternionClosed = Quaternion.Euler(52.949f, -15.009f, -16.717f);

    [HideInInspector]
    public Vector3 indexTipOpenPosition;
    [HideInInspector]
    public Vector3 indexTipClosedPosition;
    [HideInInspector]
    public Vector3 thumbTipOpenPosition;
    [HideInInspector]
    public Vector3 thumbTipClosedPosition;
    [HideInInspector]
    public Vector3 fingersTouchingPoint;
    public bool overrideFingerPosition = false;
    [Range(0, 1)]
    public float closeFingers = 0;
    [Range(-0.5f, 0.5f)]
    public float visualFingerOffset = -0.135f;
    [ReadOnly]
    public float qefFingerAperture = 0.0f;
    [ReadOnly]
    public float manipulatedFingerAperture = 0.0f;
    [HideInInspector]
    public float maxFingerAperture = 0.0f;
    [HideInInspector]
    public float minFingerAperture = 0.0f;
    private float fingerApertureRange = 0.0f;

    [Rename("Object Drives Hand Visual")]
    [Tooltip("When enabled, the object being gripped will set the hand visual")]
    public bool objectDeterminesFingerAperture = true;
    [ReadOnly]
    public bool fingerInContact = false;

    // Start is called before the first frame update
    void Start()
    {
        serialListener = GameObject.Find("SerialListener").GetComponent<SerialListener>();

        indexJoint1 = GameObject.Find("hands:b_r_index1");
        indexJoint2 = GameObject.Find("hands:b_r_index2");
        indexJoint3 = GameObject.Find("hands:b_r_index3");
        indexTip = GameObject.Find("hands:b_r_index_ignore");

        thumbJoint2 = GameObject.Find("hands:b_r_thumb2");
        thumbJoint3 = GameObject.Find("hands:b_r_thumb3");
        thumbTip = GameObject.Find("hands:b_r_thumb_ignore");

        // Before the scene starts, the hand is open
        indexJoint1QuaternionOpen = indexJoint1.transform.localRotation;
        indexJoint2QuaternionOpen = indexJoint2.transform.localRotation;
        indexJoint3QuaternionOpen = indexJoint3.transform.localRotation;

        thumbJoint2QuaternionOpen = thumbJoint2.transform.localRotation;
        thumbJoint3QuaternionOpen = thumbJoint3.transform.localRotation;

        // Measure the maximum finger aperture (fingers open)
        maxFingerAperture = Mathf.Abs(indexTip.transform.position.x - thumbTip.transform.position.x);

        // Save the finger tip positions when the fingers are open
        indexTipOpenPosition = indexTip.transform.position;
        thumbTipOpenPosition = thumbTip.transform.position;

        // Close the Fingers
        indexJoint1.transform.localRotation = indexJoint1QuaternionClosed;
        indexJoint2.transform.localRotation = indexJoint2QuaternionClosed;
        indexJoint3.transform.localRotation = indexJoint3QuaternionClosed;

        thumbJoint2.transform.localRotation = thumbJoint2QuaternionClosed;
        thumbJoint3.transform.localRotation = thumbJoint3QuaternionClosed;

        // Save the finger tip positions when the fingers are closed
        indexTipClosedPosition = indexTip.transform.position;
        thumbTipClosedPosition = thumbTip.transform.position;

        fingersTouchingPoint = (indexTipClosedPosition + thumbTipClosedPosition) / 2;

        // Measure the minimum finger aperture (fingers closed)
        minFingerAperture = Mathf.Abs(indexTip.transform.position.x - thumbTip.transform.position.x);

        // Calculate the range of the aperture
        fingerApertureRange = maxFingerAperture - minFingerAperture;
    }

    // Update is called once per frame
    void Update()
    {
        if(!overrideFingerPosition){
        hapticDevicePos = serialListener.positions[0];
        }

        qefFingerAperture = map(hapticDevicePos, ProjectVariables.FingerLowerSoftLimit, ProjectVariables.FingerUpperSoftLimit, maxFingerAperture, minFingerAperture);


        if (objectDeterminesFingerAperture && fingerInContact)
        {
        closeFingers = (fingerApertureRange - manipulatedFingerAperture) / fingerApertureRange;
        closeFingers = closeFingers + visualFingerOffset;
        }
        else
        {
        closeFingers = (fingerApertureRange - qefFingerAperture) / fingerApertureRange;
        closeFingers = closeFingers + visualFingerOffset;
        }

        //  Interpolate the finger joints between open and closed positions
        indexJoint1.transform.localRotation = Quaternion.LerpUnclamped(indexJoint1QuaternionOpen, indexJoint1QuaternionClosed, closeFingers);
        indexJoint2.transform.localRotation = Quaternion.LerpUnclamped(indexJoint2QuaternionOpen, indexJoint2QuaternionClosed, closeFingers);
        indexJoint3.transform.localRotation = Quaternion.LerpUnclamped(indexJoint3QuaternionOpen, indexJoint3QuaternionClosed, closeFingers);

        thumbJoint2.transform.localRotation = Quaternion.LerpUnclamped(thumbJoint2QuaternionOpen, thumbJoint2QuaternionClosed, closeFingers);
        thumbJoint3.transform.localRotation = Quaternion.LerpUnclamped(thumbJoint3QuaternionOpen, thumbJoint3QuaternionClosed, closeFingers);
    }

    private float map(float value, float min, float max, float newMin, float newMax)
    {
        return (value - min) * (newMax - newMin) / (max - min) + newMin;
    }
}
