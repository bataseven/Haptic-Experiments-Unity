using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class HapticDevice : MonoBehaviour
{
    private SerialListener serialListener = null;
    private SerialWriter serialWriter = null;

    [Serializable]
    public struct Waveform
    {
        public WaveformType type;
        public float frequency;
        public float amplitude;
        public float offset;

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return this == (Waveform)obj;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(Waveform a, Waveform b)
        {
            return a.type == b.type && a.frequency == b.frequency && a.amplitude == b.amplitude && a.offset == b.offset;
        }

        public static bool operator !=(Waveform a, Waveform b)
        {
            return !(a == b);
        }
    }

    [Serializable]
    public enum ControllerMode
    {
        POSITION,
        VELOCITY,
        FORCE
    }

    [Serializable]
    public enum WaveformType
    {
        CONSTANT,
        SQUARE,
        SAWTOOTH,
        TRIANGULAR,
        SINUSOIDAL,
        TRAPEZOIDAL
    }

    [Serializable]
    public enum LimitType
    {
        SOFT,
        HARD
    }

    [Serializable]
    public struct Finger
    {
        public float position;
        [HideInInspector]
        public float positionPrev;
        public float positionMm;
        public float velocity;
        public ControllerMode controllerMode;
        public Waveform waveform;
        public float userForce;
        [MinMaxSlider(ProjectVariables.FingerLowerSoftLimit, ProjectVariables.FingerUpperSoftLimit)]
        public Vector2Int softLimits;
        [MinMaxSlider(ProjectVariables.FingerLowerHardLimit, ProjectVariables.FingerUpperHardLimit)]
        public Vector2Int hardLimits;
        public bool home;
        public bool isStopped;
        public bool isMoving;

        [HideInInspector]
        public bool[] isMovingPrev;
        [HideInInspector]
        public int isMovingPrevIndex;

        [HideInInspector]
        public ControllerMode controllerModePrev;
        [HideInInspector]
        public Waveform waveformPrev;
        [HideInInspector]
        public Vector2Int softLimitsPrev;
        [HideInInspector]
        public Vector2Int hardLimitsPrev;
        [HideInInspector]
        public bool isStoppedPrev;

        public static Finger New()
        {
            Finger finger = new Finger();
            finger.controllerMode = ControllerMode.FORCE;
            Waveform waveform = new Waveform();
            waveform.type = WaveformType.CONSTANT;
            waveform.frequency = 0;
            waveform.amplitude = 0;
            waveform.offset = 0;
            finger.waveform = waveform;
            finger.softLimits = new Vector2Int(ProjectVariables.FingerLowerSoftLimit, ProjectVariables.FingerUpperSoftLimit);
            finger.hardLimits = new Vector2Int(ProjectVariables.FingerLowerHardLimit, ProjectVariables.FingerUpperHardLimit);
            finger.isMovingPrev = new bool[15];
            finger.isStopped = false;
            return finger;
        }


    }

    [Serializable]
    public struct Palm
    {
        public float position;
        [HideInInspector]
        public float positionPrev;
        public float positionMm;
        public float velocity;
        public ControllerMode controllerMode;
        public Waveform waveform;
        [MinMaxSlider(ProjectVariables.PalmLowerSoftLimit, ProjectVariables.PalmUpperSoftLimit)]
        public Vector2Int softLimits;
        [MinMaxSlider(ProjectVariables.PalmLowerHardLimit, ProjectVariables.PalmUpperHardLimit)]
        public Vector2Int hardLimits;
        public bool home;
        public bool isStopped;
        public bool isMoving;

        [HideInInspector]
        public bool[] isMovingPrev;
        [HideInInspector]
        public int isMovingPrevIndex;

        [HideInInspector]
        public ControllerMode controllerModePrev;
        [HideInInspector]
        public Waveform waveformPrev;
        [HideInInspector]
        public Vector2Int softLimitsPrev;
        [HideInInspector]
        public Vector2Int hardLimitsPrev;
        [HideInInspector]
        public bool isStoppedPrev;


        public static Palm New()
        {
            Palm palm = new Palm();
            palm.controllerMode = ControllerMode.POSITION;
            Waveform waveform = new Waveform();
            waveform.type = WaveformType.CONSTANT;
            waveform.frequency = 0;
            waveform.amplitude = 0;
            waveform.offset = 0;
            palm.waveform = waveform;
            palm.softLimits = new Vector2Int(ProjectVariables.PalmLowerSoftLimit, ProjectVariables.PalmUpperSoftLimit);
            palm.hardLimits = new Vector2Int(ProjectVariables.PalmLowerHardLimit, ProjectVariables.PalmUpperHardLimit);
            palm.isMovingPrev = new bool[15];
            palm.isStopped = false;
            return palm;
        }
    }

    [Space(5)]
    [Header("Device Status")]

    [ReadOnly]
    public bool deviceConnected = false;

    [Space(15)]
    public Finger finger = Finger.New();

    [Space(15)]
    public Palm palm = Palm.New();

    void Start()
    {
        serialListener = GameObject.Find("SerialListener").GetComponent<SerialListener>();
        serialWriter = GameObject.Find("SerialWriter").GetComponent<SerialWriter>();

        // Quit if the serial listener or writer is not found
        if (serialListener == null || serialWriter == null)
        {
            Debug.LogError("Serial listener or writer not found.");
            Application.Quit();
        }

        // Home the motors
        Home(1);

        // Override the limit changes in the editor
        finger.softLimits = new Vector2Int(ProjectVariables.FingerLowerSoftLimit, ProjectVariables.FingerUpperSoftLimit);
        finger.hardLimits = new Vector2Int(ProjectVariables.FingerLowerHardLimit, ProjectVariables.FingerUpperHardLimit);
        palm.softLimits = new Vector2Int(ProjectVariables.PalmLowerSoftLimit, ProjectVariables.PalmUpperSoftLimit);
        palm.hardLimits = new Vector2Int(ProjectVariables.PalmLowerHardLimit, ProjectVariables.PalmUpperHardLimit);

        // Set the position limits
        SetPositionLimit(0, HapticDevice.LimitType.SOFT, finger.softLimits);
        SetPositionLimit(0, HapticDevice.LimitType.HARD, finger.hardLimits);
        SetPositionLimit(1, HapticDevice.LimitType.SOFT, palm.softLimits);
        SetPositionLimit(1, HapticDevice.LimitType.HARD, palm.hardLimits);
        palm.isStopped = false;
    }

    void Update()
    {
        deviceConnected = serialListener.isConnected;

        bool isMoving = false;

        finger.position = serialListener.positions[0];
        finger.positionMm = finger.position * ProjectVariables.countToMm;
        finger.velocity = serialListener.speeds[0];
        finger.waveform = serialWriter.waveforms[0];
        finger.userForce = serialListener.netForce;
        isMoving = finger.position != finger.positionPrev;
        finger.isMovingPrev[finger.isMovingPrevIndex++] = isMoving;
        finger.isMovingPrevIndex %= finger.isMovingPrev.Length;
        finger.isMoving = !Array.TrueForAll(finger.isMovingPrev, x => x == false);
        finger.positionPrev = finger.position;

        palm.position = serialListener.positions[1];
        palm.positionMm = palm.position * ProjectVariables.countToMm2;
        palm.velocity = serialListener.speeds[1];
        palm.waveform = serialWriter.waveforms[1];
        isMoving = palm.position != palm.positionPrev;
        palm.isMovingPrev[palm.isMovingPrevIndex++] = isMoving;
        palm.isMovingPrevIndex %= palm.isMovingPrev.Length;
        palm.isMoving = !Array.TrueForAll(palm.isMovingPrev, x => x == false);
        palm.positionPrev = palm.position;
    }

    public void Home(int motorIndex)
    {
        StartCoroutine(_Home(motorIndex));
    }

    // This code is used to send a command to the device to move the motor to the home position.
    // The motorIndex is the index of the motor to be moved to the home position.
    // The motor is commanded to move to 0 position after homing.
    private IEnumerator _Home(int motorIndex)
    {
        serialWriter.SendHomeMotor(motorIndex);
        yield return new WaitForSeconds(0.1f);
        StartCoroutine(_MoveToPosition(motorIndex, 0, true));
    }

    public void SetStop(int motorIndex, bool stop)
    {
        StartCoroutine(_SetStop(motorIndex, stop));
    }

    private IEnumerator _SetStop(int motorIndex, bool stop)
    {
        if (motorIndex == 0) finger.isStopped = stop;
        else if (motorIndex == 1) palm.isStopped = stop;
        finger.isStoppedPrev = stop;
        palm.isStoppedPrev = stop;
        serialWriter.SendStop(motorIndex, stop);
        yield return null;
    }

    public void MoveToPosition(int motorIndex, int position)
    {
        StartCoroutine(_MoveToPosition(motorIndex, position));
    }

    // This function moves a motor to a certain position
    // The motorIndex variable specifies which motor to move
    // The position variable specifies the position to move to
    // The serialWriter variable is a reference to a SerialWriter instance
    private IEnumerator _MoveToPosition(int motorIndex, int position, bool sendItAnyway = false)
    {
        // Set the controller mode to position
        bool sent = serialWriter.SendControllerMode(motorIndex, HapticDevice.ControllerMode.POSITION, sendItAnyway);
        if (motorIndex == 0) finger.controllerMode = HapticDevice.ControllerMode.POSITION;
        else if (motorIndex == 1) palm.controllerMode = HapticDevice.ControllerMode.POSITION;
        yield return sent ? new WaitForSeconds(0.05f) : null; // Wait for the controller mode to be set
                                                              // Set the wave type to constant

        serialWriter.SendWaveType(motorIndex, HapticDevice.WaveformType.CONSTANT);
        if (motorIndex == 0) finger.waveform.type = HapticDevice.WaveformType.CONSTANT;
        else if (motorIndex == 1) palm.waveform.type = HapticDevice.WaveformType.CONSTANT;

        // Set the wave parameter to amplitude
        serialWriter.SendWaveParameter(motorIndex, 'A', position);
        if (motorIndex == 0) finger.waveform.amplitude = position;
        else if (motorIndex == 1) palm.waveform.amplitude = position;
    }

    public void MoveWithVelocity(int motorIndex, int velocity)
    {
        StartCoroutine(_MoveWithVelocity(motorIndex, velocity));
    }

    // This function moves a motor at a given velocity. 
    //The motor index is a number that corresponds to the motor that will be moved. 
    //The velocity is a number that represents the velocity at which the motor will be moved.
    private IEnumerator _MoveWithVelocity(int motorIndex, int velocity)
    {
        // Set the controller mode to velocity
        bool sent = serialWriter.SendControllerMode(motorIndex, HapticDevice.ControllerMode.VELOCITY);
        if (motorIndex == 0) finger.controllerMode = HapticDevice.ControllerMode.VELOCITY;
        else if (motorIndex == 1) palm.controllerMode = HapticDevice.ControllerMode.VELOCITY;
        yield return sent ? new WaitForSeconds(0.05f) : null; // Wait for the controller mode to be set

        // Set the wave type to constant
        serialWriter.SendWaveType(motorIndex, HapticDevice.WaveformType.CONSTANT);
        if (motorIndex == 0) finger.waveform.type = HapticDevice.WaveformType.CONSTANT;
        else if (motorIndex == 1) palm.waveform.type = HapticDevice.WaveformType.CONSTANT;

        // Set the wave parameter to amplitude
        serialWriter.SendWaveParameter(motorIndex, 'A', velocity);
        if (motorIndex == 0) finger.waveform.amplitude = velocity;
        else if (motorIndex == 1) palm.waveform.amplitude = velocity;
        yield return null;
    }

    public void SetPositionLimit(int motorIndex, LimitType limitType, int lowerLimit, int upperLimit)
    {
        StartCoroutine(_SetPositionLimit(motorIndex, limitType, new Vector2Int(lowerLimit, upperLimit)));
    }

    public void SetPositionLimit(int motorIndex, LimitType limitType, Vector2Int limits)
    {
        StartCoroutine(_SetPositionLimit(motorIndex, limitType, limits));
    }

    private IEnumerator _SetPositionLimit(int motorIndex, LimitType limitType, Vector2Int limits)
    {
        if (motorIndex == 0)
        {
            if (limitType == LimitType.SOFT)
            {
                if (limits.x < ProjectVariables.FingerLowerSoftLimit) limits.x = ProjectVariables.FingerLowerSoftLimit;
                if (limits.y > ProjectVariables.FingerUpperSoftLimit) limits.y = ProjectVariables.FingerUpperSoftLimit;

                finger.softLimits = limits;
                CheckLimitValidity();
                finger.softLimitsPrev = limits;
            }
            else if (limitType == LimitType.HARD)
            {
                if (limits.x < ProjectVariables.FingerLowerHardLimit) limits.x = ProjectVariables.FingerLowerHardLimit;
                if (limits.y > ProjectVariables.FingerUpperHardLimit) limits.y = ProjectVariables.FingerUpperHardLimit;

                finger.hardLimits = limits;
                CheckLimitValidity();
                finger.hardLimitsPrev = limits;
            }
        }
        else if (motorIndex == 1)
        {
            if (limitType == LimitType.SOFT)
            {
                if (limits.x < ProjectVariables.PalmLowerSoftLimit) limits.x = ProjectVariables.PalmLowerSoftLimit;
                if (limits.y > ProjectVariables.PalmUpperSoftLimit) limits.y = ProjectVariables.PalmUpperSoftLimit;

                palm.softLimits = limits;
                CheckLimitValidity();
                palm.softLimitsPrev = limits;
            }
            else if (limitType == LimitType.HARD)
            {
                if (limits.x < ProjectVariables.PalmLowerHardLimit) limits.x = ProjectVariables.PalmLowerHardLimit;
                if (limits.y > ProjectVariables.PalmUpperHardLimit) limits.y = ProjectVariables.PalmUpperHardLimit;

                palm.hardLimits = limits;
                CheckLimitValidity();
                palm.hardLimitsPrev = limits;
            }
        }
        serialWriter.SendPositionLimit(motorIndex, limitType, limits.x, limits.y);
        yield return null;
    }

    private void CheckLimitValidity()
    {
        // Make sure the soft limit is within the hard limit
        if (finger.softLimits != finger.softLimitsPrev)
        {
            if (finger.softLimits.x < finger.hardLimits.x) finger.hardLimits.x = finger.softLimits.x;
            if (finger.softLimits.y > finger.hardLimits.y) finger.hardLimits.y = finger.softLimits.y;
        }
        if (finger.hardLimits != finger.hardLimitsPrev)
        {
            if (finger.softLimits.x < finger.hardLimits.x) finger.softLimits.x = finger.hardLimits.x;
            if (finger.softLimits.y > finger.hardLimits.y) finger.softLimits.y = finger.hardLimits.y;
            if (finger.softLimits.x > finger.hardLimits.y) finger.softLimits.x = finger.softLimits.y;
            if (finger.softLimits.y < finger.hardLimits.x) finger.softLimits.y = finger.softLimits.x;
        }

        if (palm.softLimits != palm.softLimitsPrev)
        {
            if (palm.softLimits.x < palm.hardLimits.x) palm.hardLimits.x = palm.softLimits.x;
            if (palm.softLimits.y > palm.hardLimits.y) palm.hardLimits.y = palm.softLimits.y;
        }
        if (palm.hardLimits != palm.hardLimitsPrev)
        {
            if (palm.softLimits.x < palm.hardLimits.x) palm.softLimits.x = palm.hardLimits.x;
            if (palm.softLimits.y > palm.hardLimits.y) palm.softLimits.y = palm.hardLimits.y;
            if (palm.softLimits.x > palm.hardLimits.y) palm.softLimits.x = palm.softLimits.y;
            if (palm.softLimits.y < palm.hardLimits.x) palm.softLimits.y = palm.softLimits.x;
        }

    }

    public void OnValidate()
    {
        CheckLimitValidity();

        if (serialWriter != null && Application.isPlaying)
        {
            // Check which variables have changed and send the new values to the device
            if (finger.softLimits != finger.softLimitsPrev) SetPositionLimit(0, HapticDevice.LimitType.SOFT, finger.softLimits);
            if (finger.hardLimits != finger.hardLimitsPrev) SetPositionLimit(0, HapticDevice.LimitType.HARD, finger.hardLimits);
            if (finger.controllerMode != finger.controllerModePrev) serialWriter.SendControllerMode(0, finger.controllerMode);
            if (finger.waveform != finger.waveformPrev) serialWriter.SendWaveform(0, finger.waveform);
            if (finger.isStopped) SetStop(0, finger.isStopped);
            if (finger.home) Home(0);

            if (palm.softLimits != palm.softLimitsPrev) SetPositionLimit(1, HapticDevice.LimitType.SOFT, palm.softLimits);
            if (palm.hardLimits != palm.hardLimitsPrev) SetPositionLimit(1, HapticDevice.LimitType.HARD, palm.hardLimits);
            if (palm.controllerMode != palm.controllerModePrev) serialWriter.SendControllerMode(1, palm.controllerMode);
            if (palm.waveform != palm.waveformPrev) serialWriter.SendWaveform(1, palm.waveform);
            if (palm.isStopped) SetStop(1, palm.isStopped);
            if (palm.home) Home(1);
        }


        // Update the previous values
        finger.softLimitsPrev = finger.softLimits;
        finger.hardLimitsPrev = finger.hardLimits;
        finger.controllerModePrev = finger.controllerMode;
        finger.waveformPrev = finger.waveform;
        finger.isStoppedPrev = finger.isStopped;
        finger.home = false;

        palm.softLimitsPrev = palm.softLimits;
        palm.hardLimitsPrev = palm.hardLimits;
        palm.controllerModePrev = palm.controllerMode;
        palm.waveformPrev = palm.waveform;
        palm.isStoppedPrev = palm.isStopped;
        palm.home = false;

    }
}
