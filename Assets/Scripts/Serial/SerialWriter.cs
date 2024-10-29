using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SerialWriter : MonoBehaviour
{
    private SerialController serialController;
    public char startChar = '<';
    public char endChar = '>';
    public char seperator = '#';
    public HapticDevice.Waveform[] waveforms = new HapticDevice.Waveform[2];
    public HapticDevice.ControllerMode[] controllerModes = new HapticDevice.ControllerMode[2];

    // Start is called before the first frame update
    void Start()
    {
        serialController = GameObject.Find("SerialController").GetComponent<SerialController>();
        serialController.SetTearDownFunction(() => SendPsuedoObject(0, 0, 0));

        waveforms[0].type = HapticDevice.WaveformType.CONSTANT;
        waveforms[0].frequency = 0;
        waveforms[0].amplitude = 0;
        waveforms[0].offset = 0;

        waveforms[1].type = HapticDevice.WaveformType.CONSTANT;
        waveforms[1].frequency = 0;
        waveforms[1].amplitude = 0;
        waveforms[1].offset = 0;

        controllerModes[0] = HapticDevice.ControllerMode.FORCE;
        controllerModes[1] = HapticDevice.ControllerMode.POSITION;
    }

    public IEnumerator SendDeviceTypeForXSeconds(float seconds)
    {
        float startTime = Time.time;
        while (Time.time < startTime + seconds)
        {
            SendDeviceType();
            yield return new WaitForSeconds(0.1f);
        }
    }

    public void SendForce(float force)
    {
        char messageType = 'D';
        string message = messageType + force.ToString();
        Write(message);
    }

    public void SendPsuedoObject(float width, float stiffness, float damping)
    {
        char messageType = 'P';
        string message = messageType + width.ToString() + seperator + stiffness.ToString() + seperator + damping.ToString();
        Write(message);
    }

    public void SendDeviceType()
    {
        char messageType = 'C';
        string message = messageType + "0";
        Write(message);
    }

    public void SendHomeMotor(int motorIndex)
    {
        char messageType = 'H';
        string message = messageType + motorIndex.ToString();
        Write(message);
    }


    public void SendWaveform(int generatorIndex, HapticDevice.Waveform waveForm, bool sendItAnyway = false)
    {
        StartCoroutine(_SendWaveform(generatorIndex, waveForm, sendItAnyway));
    }

    private IEnumerator _SendWaveform(int generatorIndex, HapticDevice.Waveform waveForm, bool sendItAnyway = false)
    {
        // Check if the waveform type is the same as the current waveform type
        if (waveforms[generatorIndex].type != waveForm.type || sendItAnyway)
        {
            SendWaveType(generatorIndex, waveForm.type);
            yield return new WaitForSeconds(0.1f);
        }

        // Check if the waveform frequency is the same as the current waveform frequency
        if (waveforms[generatorIndex].frequency != waveForm.frequency || sendItAnyway)
        {
            SendWaveParameter(generatorIndex, 'F', waveForm.frequency);
            yield return new WaitForSeconds(0.1f);
        }

        // Check if the waveform amplitude is the same as the current waveform amplitude

        if (waveforms[generatorIndex].amplitude != waveForm.amplitude || sendItAnyway)
        {
            SendWaveParameter(generatorIndex, 'A', waveForm.amplitude);
            yield return new WaitForSeconds(0.1f);
        }

        // Check if the waveform offset is the same as the current waveform offset
        if (waveforms[generatorIndex].offset != waveForm.offset || sendItAnyway)
        {
            SendWaveParameter(generatorIndex, 'O', waveForm.offset);
            yield return new WaitForSeconds(0.1f);
        }
    }

    public void SendWaveType(int generatorIndex, HapticDevice.WaveformType waveType)
    {
        char messageType = 'W';
        string message = messageType + "T" + generatorIndex.ToString() + seperator + ((int)waveType).ToString();
        waveforms[generatorIndex].type = waveType;
        Write(message);
    }

    public void SendPositionLimit(int motorIndex, HapticDevice.LimitType type, int lowerLimit, int upperLimit)
    {
        char messageType = 'L';
        char limitType = (type == HapticDevice.LimitType.SOFT) ? 'S' : 'H';
        string message = messageType + "" + limitType + "" + motorIndex.ToString() + lowerLimit.ToString() + seperator + upperLimit.ToString();
        Write(message);
    }

    public void SendWaveParameter(int generatorIndex, char paramType, float value)
    {
        generatorIndex = Mathf.Clamp(generatorIndex, 0, 1);
        char messageType = 'W';
        string message = (messageType + "") + (paramType + "") + generatorIndex.ToString() + seperator + value.ToString();

        if (paramType == 'A')
        {
            waveforms[generatorIndex].amplitude = value;
        }
        else if (paramType == 'F')
        {
            waveforms[generatorIndex].frequency = value;
        }
        else if (paramType == 'O')
        {
            waveforms[generatorIndex].offset = value;
        }

        Write(message);
    }

    public void SendStop(int motorIndex, bool stop)
    {
        char messageType = 'S';
        string message = (messageType + "") + motorIndex.ToString() + (stop ? "1" : "0");
        Write(message);
    }

    public bool SendControllerMode(int motorIndex, HapticDevice.ControllerMode controllerMode, bool sendItAnyway = false)
    {
        // Check if the controller mode is the same as the current controller mode
        if (controllerModes[motorIndex] == controllerMode && !sendItAnyway) return false;

        char messageType = 'O';
        controllerModes[motorIndex] = controllerMode;
        string message = messageType + motorIndex.ToString() + ((int)controllerMode).ToString();
        Write(message);
        return true;
    }

    public void Write(string message)
    {
        string messageToSend = startChar + message + endChar;
        // Debug.Log(messageToSend);
        serialController.SendSerialMessage(messageToSend);
    }
}
