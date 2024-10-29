using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class SerialListener : MonoBehaviour
{
    public float[] positions = { 0, 0 };
    public float[] speeds = { 0, 0 };
    public float[] eulers = { 0, 0, 0 };
    public float netForce = 0;
    public float desiredForce = 0;
    public Quaternion quaternion = new Quaternion(0, 0, 0, 0);
    public float[] forces = { 0, 0, 0, 0 };
    public char startChar = '<';
    public char endChar = '>';
    public char seperator = '#';
    [ReadOnly]
    public bool isConnected = false;

    private SerialWriter serialWriter;

    // Invoked when a line of data is received from the serial device.
    void OnMessageArrived(string msg)
    {
        parseMessage(msg);
        //Debug.Log("Message arrived: " + msg);
    }

    // Invoked when a connect/disconnect event occurs. The parameter 'success'
    // will be 'true' upon connection, and 'false' upon disconnection or
    // failure to connect.
    void OnConnectionEvent(bool success)
    {
        Debug.Log("ConnectionEvent: " + (success ? "connected" : "disconnected"));
        isConnected = success;
        if (success)
        {
            StartCoroutine(serialWriter.SendDeviceTypeForXSeconds(3));
        }
    }
    void parseMessage(string msg)
    {
        // Count the number of start and end characters, if they are not equal, the message is invalid
        int startChNum = msg.Split(startChar).Length - 1;
        int endChNum = msg.Split(endChar).Length - 1;

        if (startChNum != endChNum)
        {
            Debug.Log("Invalid message: " + msg);
            return;
        }

        Regex rx = new Regex(@"" + startChar + "(.*?)" + endChar + "");

        string debugString = "";
        foreach (Match match in rx.Matches(msg))
        {
            string subMsg = match.Value;
            debugString += subMsg + " ";
            // print(debugString);

            // Remove the first and last character
            subMsg = subMsg.Substring(1, subMsg.Length - 2);

            // Message type is the first character
            char type = subMsg[0];

            // Remove the type character
            subMsg = subMsg.Substring(1);
            string[] values;
            switch (type)
            {
                case 'H':
                    int motorIndex = subMsg[0] - '0';

                    // Remove the motor index character
                    subMsg = subMsg.Substring(1);

                    values = subMsg.Split(seperator);

                    // Value[0] is the position
                    // Value[1] is the speed

                    if (values[0][0] == '-')
                        positions[motorIndex] = int.Parse(values[0].Substring(1), System.Globalization.NumberStyles.HexNumber) * -1;
                    else
                        positions[motorIndex] = int.Parse(values[0], System.Globalization.NumberStyles.HexNumber);

                    if (values[1][0] == '-')
                        speeds[motorIndex] = int.Parse(values[1].Substring(1), System.Globalization.NumberStyles.HexNumber) * -1;
                    else
                        speeds[motorIndex] = int.Parse(values[1], System.Globalization.NumberStyles.HexNumber);


                    break;
                case 'F':
                    values = subMsg.Split(seperator);
                    forces[0] = float.Parse(values[0]);
                    forces[1] = float.Parse(values[1]);
                    forces[2] = float.Parse(values[2]);
                    forces[3] = float.Parse(values[3]);
                    break;

                case 'E':
                    values = subMsg.Split(seperator);
                    eulers[0] = float.Parse(values[0]);
                    eulers[1] = float.Parse(values[1]);
                    eulers[2] = -float.Parse(values[2]);
                    break;

                case 'N':
                    netForce = float.Parse(subMsg);
                    break;

                case 'D':
                    desiredForce = float.Parse(subMsg);
                    break;

                case 'Q':
                    values = subMsg.Split(seperator);

                    // int indexIndex = idx % 6;
                    // int intIdx = idx / 8;

                    // xIndex = combinations[indexIndex, 0];
                    // yIndex = combinations[indexIndex, 1];
                    // zIndex = combinations[indexIndex, 2];

                    // x = signs[intIdx, 0];
                    // y = signs[intIdx, 1];
                    // z = signs[intIdx, 2];

                    // quaternion.x = x ? float.Parse(values[xIndex]) : -float.Parse(values[xIndex]);
                    // quaternion.y = y ? float.Parse(values[yIndex]) : -float.Parse(values[yIndex]);
                    // quaternion.z = z ? float.Parse(values[zIndex]) : -float.Parse(values[zIndex]);
                    // quaternion.w = float.Parse(values[3]);

                    // quaternion.x = float.Parse(values[1]);
                    // quaternion.y = -float.Parse(values[2]);
                    // quaternion.z = float.Parse(values[0]);
                    // quaternion.w = float.Parse(values[3]);
                    break;

                default:
                    Debug.Log("Invalid message type: " + type);
                    break;
            }
        }
    }
}
