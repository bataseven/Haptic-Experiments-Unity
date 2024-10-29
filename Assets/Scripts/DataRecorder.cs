using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Text.RegularExpressions;
using UnityEngine.UI;
using TMPro;


public class DataRecorder : MonoBehaviour
{
    private SerialWriter serialWriter;
    private SerialListener serialListener;
    private string filePath;
    private List<float> userForceHistory = new List<float>();
    private List<float> desiredForceHistory = new List<float>();
    private List<float> userForceTimeHistory = new List<float>();
    private float forceTime = 0;
    private string folderName = "ForceData/";
    [Space(15)]
    public bool recording = false;

    [Space(15)]
    [Header("Device Status")]
    [Space(5)]
    [ReadOnly]
    public bool deviceConnected = false;
    [ReadOnly]
    [Rename("Finger Position")]
    public float fingerPos = 0f;
    [ReadOnly]
    [Rename("Finger Position [mm]")]
    public float fingerPosMm = 0f;
    [ReadOnly]
    public float userForce = 0f;
    [ReadOnly]
    public float desiredForce = 0f;


    // Start is called before the first frame update
    void Start()
    {
        serialWriter = GameObject.Find("SerialWriter").GetComponent<SerialWriter>();
        serialListener = GameObject.Find("SerialListener").GetComponent<SerialListener>();
        try
        {
            if (!Directory.Exists(folderName))
            {
                Directory.CreateDirectory(folderName);
            }
        }
        catch (IOException ex)
        {
            Debug.Log(ex.Message);
        }

        // Get the number files in the folder
        int count = Directory.GetFiles(folderName, "*.csv").Length;

        filePath = folderName + "\\" + (count + 1) + ".csv";
    }


    // Update is called once per frame
    void Update()
    {
        deviceConnected = serialListener.isConnected;
        userForce = serialListener.netForce;
        desiredForce = serialListener.desiredForce;
        fingerPos = serialListener.positions[0];
        fingerPosMm = fingerPos * ProjectVariables.countToMm;

        if (Input.GetKeyDown(KeyCode.BackQuote) || Input.GetKeyDown(KeyCode.R))
        {
            if (recording) // Stop recording and save data
            {
                appendToTxt();
            }
            else // Start recording
            {
                forceTime = 0;
                userForceHistory.Clear();
                desiredForceHistory.Clear();
                userForceTimeHistory.Clear();
            }
            recording = !recording;
        }

        if (recording)
        {
            userForceHistory.Add(userForce);
            desiredForceHistory.Add(desiredForce);
            userForceTimeHistory.Add(forceTime);
            forceTime += Time.deltaTime;
        }

    }
    void appendToTxt()
    {
        for (int i = 0; i < userForceHistory.Count; i++)
        {
            float F_user = userForceHistory[i];
            float F_desired = desiredForceHistory[i];
            float time = userForceTimeHistory[i];
            System.IO.File.AppendAllText(filePath, time + "," + F_user + "," + F_desired + "\n");
        }
    }

    void OnApplicationQuit()
    {
        if (recording)
        {
            appendToTxt();
        }
    }
}
