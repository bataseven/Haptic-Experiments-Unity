using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// [ExecuteInEditMode]
public class HandRotationControl : MonoBehaviour
{
    private SerialListener serialListener;

    private GameObject rightHand;
    
    //Save the initial rotation of the hand
    private Quaternion initialRotation;
    private float initialYaw;
    private float initialPitch;
    private float initialRoll;
    private Quaternion quaternionOffset;

    private float yawOffset = 0;
    private float rollOffset = 0;
    private float pitchOffset = 0;


    // Start is called before the first frame update
    void Start()    
    {
        Application.targetFrameRate = 300;
        serialListener = GameObject.Find("SerialListener").GetComponent<SerialListener>();
        rightHand = GameObject.Find("RightHand");
        initialRotation = rightHand.transform.rotation;
        
        initialYaw = initialRotation.eulerAngles.y;
        initialRoll = initialRotation.eulerAngles.z;
        initialPitch = initialRotation.eulerAngles.x;

        quaternionOffset = Quaternion.identity;
    }
    
    // Update is called once per frame
    void Update()
    {
        // Get the euler angles of the device from serial listener: serialListener.eulers => {YAW, PITCH, ROLL}
        // Vector3 deviceEulerAngles = new Vector3(serialListener.eulerAngles[2] + pitchOffset, serialListener.eulerAngles[0] + yawOffset, serialListener.eulerAngles[1] + rollOffset);
        // Sum the euler angles
        // Vector3 deviceEulerAngles = new Vector3(serialListener.eulers[2] + initialPitch + pitchOffset, serialListener.eulers[0] + initialYaw + yawOffset, serialListener.eulers[1] + initialRoll + rollOffset); 

        float newYaw = serialListener.eulers[0] + initialYaw + yawOffset; 
        float newPitch = serialListener.eulers[1] + initialPitch + pitchOffset;
        float newRoll = serialListener.eulers[2] + initialRoll + rollOffset;

        // Set the rotation of the hand to the rotation of the device
        rightHand.transform.rotation = Quaternion.Euler(new Vector3(newPitch, newYaw, newRoll));

        //Detect key presses
        if (Input.GetKeyDown(KeyCode.Y))
        {
           yawOffset = -serialListener.eulers[0];
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
           rollOffset = -serialListener.eulers[2];
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
           pitchOffset = -serialListener.eulers[1];
        }
        if(Input.GetKeyDown(KeyCode.Q))
        {
            // Set an offset to the rotation of the device
            // quaternionOffset = Quaternion.Inverse(deviceRotation);
        }
        // Print the frame rate
        // Debug.Log("Frame rate: " + 1.0f / Time.deltaTime);
    }
}
