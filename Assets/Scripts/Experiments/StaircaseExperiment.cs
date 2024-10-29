using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Text.RegularExpressions;
using UnityEngine.UI;
using TMPro;

public class StaircaseExperiment : MonoBehaviour
{
    enum ExperimentState
    {
        Intro,
        First,
        Second,
        Answer,
        Countdown,
        End
    }
    private TextMeshProUGUI title;
    private TextMeshProUGUI body;
    private TextMeshProUGUI footer;
    private TextMeshProUGUI answer1;
    private TextMeshProUGUI answer2;

    private GameObject ground;
    private Color groundColor1;
    private Color groundColor2 = new Color(0.9f, 0.1f, 0.1f);

    private TextMeshProUGUI deviceStatus;

    private GameObject forceBar;

    private SerialWriter serialWriter;
    private SerialListener serialListener;

    private bool fingerClosed = false;
    private bool fingerOpen = false;

    private int selection = 0;
    private float elapsedTime = 0;
    [SerializeField]
    private int trial = 1;
    private int maxDemoTrials = 5;
    private string folderName = "JND_Experiment";

    public bool continueFromFile = false;
    public string participantName = "";
    public bool saveAnswers = true;
    public bool isDemo = true;
    [SerializeField]
    private ExperimentState state = ExperimentState.Intro;
    [Tooltip("Exploration duration for each stiffness value")]
    public int timeLimit = 10;
    public int countdownFrom = 3;
    public float nominalWidth = 100000; // In encoder ticks
    public float damping = 0.05f;
    public float forceWarningThreshold = 10;

    [SerializeField]
    private bool isExperimentOver = false;

    private string filePath = "";

    [Space(15)]
    [Header("Experiment Parameters")]
    [Space(5)]
    // Staircase Method Variables
    public float stiffness1 = 0;
    public float stiffness2 = 0;
    public float referenceStiffness = 1.0f;
    public float adjustedStiffness = 10.0f;
    public float stepSize = 2.0f;
    public float instantaneousWR = 1.0f;
    public float stepReductionFactor = 0.5f;
    public bool isAnswerCorrect = true;
    public bool isAnswerCorrectPrev = true;
    public bool isStairDown = true;
    public int N = 2;
    public int M = 1;
    public int correctStreak = 0;
    public int falseStreak = 0;
    public int reversalCount = 0;
    public int totalReversalCount = 0;
    public int reversalPerStepChange = 3;
    public int reversalLimit = 12;

    // Start is called before the first frame update
    void Start()
    {
        title = GameObject.Find("Title").GetComponent<TextMeshProUGUI>();
        body = GameObject.Find("Body").GetComponent<TextMeshProUGUI>();
        footer = GameObject.Find("Footer").GetComponent<TextMeshProUGUI>();
        answer1 = GameObject.Find("Answer1").GetComponent<TextMeshProUGUI>();
        answer2 = GameObject.Find("Answer2").GetComponent<TextMeshProUGUI>();
        deviceStatus = GameObject.Find("Position").GetComponent<TextMeshProUGUI>();
        forceBar = GameObject.Find("ForceBar");
        forceBar.SetActive(false);

        serialWriter = GameObject.Find("SerialWriter").GetComponent<SerialWriter>();
        serialListener = GameObject.Find("SerialListener").GetComponent<SerialListener>();

        ground = GameObject.Find("Ground");
        groundColor1 = ground.GetComponent<Renderer>().material.color;

        // Do not change the strings below.
        string type = isDemo ? "Demo" : "Experiment";

        if (!continueFromFile)
        {
            try
            {
                if (!Directory.Exists(folderName + "/" + type))
                {
                    Directory.CreateDirectory(folderName + "/" + type);
                }
            }
            catch (IOException ex)
            {
                Debug.Log(ex.Message);
            }
            filePath = folderName + "\\" + type + "\\" + participantName + "_" + System.DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss") + ".csv";
        }
        else
        {
            // Open a dialog to select the file
            filePath = UnityEditor.EditorUtility.OpenFilePanel("Select file", "", "csv");
            if (filePath == "")
            {
                Debug.Log("No file selected. Exiting...");
                UnityEditor.EditorApplication.isPlaying = false;
                Application.Quit();
            }
            else
            {
                parseParametersFromFile();
            }
        }

    }

    // Update is called once per frame
    void Update()
    {
        float userForce = serialListener.netForce;
        float fingerPos = serialListener.positions[0];
        deviceStatus.text = "Pos: " + string.Format("{0:0.##}", (fingerPos) * 26.1 / 107493.0) + " mm\n" + "Force: " + string.Format("{0:0.##}", -userForce) + " N";

        if (Mathf.Abs(userForce) > forceWarningThreshold)
        {
            ground.GetComponent<Renderer>().material.color = groundColor2;
        }
        else
        {
            ground.GetComponent<Renderer>().material.color = groundColor1;
        }

        float sliderWidth = Mathf.Min(Mathf.Abs(userForce) / forceWarningThreshold * 100, 100.0f);

        // Set the width of the slider
        forceBar.transform.GetChild(0).gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(sliderWidth, forceBar.transform.GetChild(0).gameObject.GetComponent<RectTransform>().sizeDelta.y);
        // Set the text of the slider
        forceBar.transform.GetChild(0).gameObject.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = string.Format("{0:0.#}", Mathf.Abs(userForce)) + " N";


        switch (state)
        {
            case ExperimentState.Intro:
                serialWriter.SendPsuedoObject(nominalWidth, 0, 0);
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                {
                    body.alignment = TextAlignmentOptions.Center;
                    state = isExperimentOver ? ExperimentState.End : ExperimentState.Countdown;
                    elapsedTime = 0;
                    footer.text = "";
                }
                break;

            case ExperimentState.First:
                title.text = "Trial " + trial + "\nStiffness #1";
                title.color = Color.red;
                body.text = "Pinch & relax your fingers to feel the stiffness.";
                footer.text = "" + (timeLimit - (int)elapsedTime);
                footer.color = Color.red;
                forceBar.SetActive(true);
                serialWriter.SendPsuedoObject(nominalWidth, stiffness1, damping);

                elapsedTime += Time.deltaTime;
                if (elapsedTime > timeLimit)
                {
                    if (Mathf.Abs(userForce) > 0.1f)
                    {
                        body.text = "Stop exerting force to proceed.";
                    }
                    else
                    {
                        title.text = "";
                        footer.text = "";

                        elapsedTime = 0;
                        state = ExperimentState.Second;

                    }
                }
                break;

            case ExperimentState.Second:
                title.text = "Trial " + trial + "\nStiffness #2";
                title.color = Color.blue;
                body.text = "Pinch & relax your fingers to feel the stiffness.";
                footer.text = "" + (timeLimit - (int)elapsedTime);
                footer.color = Color.blue;

                serialWriter.SendPsuedoObject(nominalWidth, stiffness2, damping);

                elapsedTime += Time.deltaTime;

                if (elapsedTime > timeLimit)
                {
                    if (Mathf.Abs(userForce) > 0.1f)
                    {
                        body.text = "Stop exerting force to proceed.";
                    }
                    else
                    {
                        title.text = "";
                        body.text = "Which of the stimuli felt stiffer?\nMake a selection then press enter.";
                        footer.text = "";
                        footer.color = Color.black;
                        answer1.text = "1";
                        answer2.text = "2";
                        state = ExperimentState.Answer;
                        elapsedTime = 0;
                    }
                }
                break;

            case ExperimentState.Answer:
                forceBar.SetActive(false);
                serialWriter.SendPsuedoObject(nominalWidth, 0, 0);

                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    selection = 1;
                    highlightText(answer1, Color.red);
                    unhighlightText(answer2);
                    footer.text = "";

                }
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    selection = 2;
                    highlightText(answer2, Color.blue);
                    unhighlightText(answer1);
                    footer.text = "";
                }
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    if (selection == 0)
                    {
                        footer.text = "Please select an answer.";
                    }
                    else
                    {
                        unhighlightText(answer1);
                        unhighlightText(answer2);
                        answer1.text = "";
                        answer2.text = "";
                        footer.text = "";

                        // Save answers to file
                        if (saveAnswers) saveAnswer();

                        // Save the last answer
                        isAnswerCorrectPrev = isAnswerCorrect;

                        // Check if the given answer is correct
                        isAnswerCorrect = (selection == 1 && stiffness1 > stiffness2) || (selection == 2 && stiffness2 > stiffness1);

                        // Increase the correct streak if the answer is correct
                        correctStreak = isAnswerCorrect ? correctStreak + 1 : 0;

                        // Increase the false streak if the answer is false
                        falseStreak = !isAnswerCorrect ? falseStreak + 1 : 0;

                        // Increase the reversal count if the last 2 answers were different                    
                        if (isAnswerCorrectPrev != isAnswerCorrect)
                        {
                            totalReversalCount++;
                            reversalCount++;
                        }

                        // End the experiment when the reversal limit is reached or demo trials are completed
                        isExperimentOver = totalReversalCount >= reversalLimit || (isDemo && trial >= maxDemoTrials);

                        // Reset the selection
                        selection = 0;
                        // stiffness1 = 0.0f;
                        state = isExperimentOver ? ExperimentState.End : ExperimentState.Countdown;
                        trial++;
                    }
                }
                break;

            case ExperimentState.Countdown:

                title.color = Color.white;
                body.text = "";

                Debug.Log("Pos: " + fingerPos);

                if (fingerPos >= ProjectVariables.FingerUpperSoftLimit && Mathf.Abs(userForce) > 2) fingerClosed = true;
                if (fingerClosed && fingerPos <= ProjectVariables.FingerLowerSoftLimit) fingerOpen = true;

                // TODO - Comment next 2 lines out during the experiment
                fingerClosed = true;
                // fingerOpen = true;


                if (!fingerClosed && !fingerOpen)
                {
                    title.text = "Close Your Fingers\n";
                }
                else if (fingerClosed && !fingerOpen)
                {
                    title.text = "Open Your Fingers\n";
                    serialWriter.SendPsuedoObject(nominalWidth, 0, 0);
                }
                else if (fingerClosed && fingerOpen)
                {
                    title.text = "" + (countdownFrom - (int)elapsedTime);
                    elapsedTime += Time.deltaTime;
                }

                if (elapsedTime > countdownFrom)
                {
                    setStiffnessValues();
                    state = ExperimentState.First;
                    elapsedTime = 0;
                    fingerOpen = false;
                    fingerClosed = false;
                }
                break;

            case ExperimentState.End:
                title.text = "End";
                title.color = Color.white;
                body.text = "The experiment is finished.\n Thank you for your participation.";
                footer.text = "";
                elapsedTime += Time.deltaTime;
                if (elapsedTime > 4)
                {
                    // Quit the application
                    UnityEditor.EditorApplication.isPlaying = false;
                    Application.Quit();
                }
                break;
        }
    }

    void highlightText(TextMeshProUGUI text, Color color)
    {
        text.fontStyle = FontStyles.Bold;
        text.fontSize = 50;
        text.color = color;
    }

    void unhighlightText(TextMeshProUGUI text)
    {
        text.fontStyle = FontStyles.Normal;
        text.fontSize = 30;
        text.color = Color.white;
    }

    void setStiffnessValues()
    {
        if (trial == 1)
        {
            if (Random.value > 0.5)
            {
                stiffness1 = referenceStiffness;
                stiffness2 = adjustedStiffness;
            }
            else
            {
                stiffness1 = adjustedStiffness;
                stiffness2 = referenceStiffness;
            }
            return;
        }

        instantaneousWR = (adjustedStiffness - referenceStiffness) / referenceStiffness * 100;

        // Don't change the step size unless there are at least 1 reversal
        if (reversalCount == reversalPerStepChange && totalReversalCount != 0)
        {
            stepSize *= stepReductionFactor;
            reversalCount = 0;
        }

        if (isStairDown)
        {
            if (correctStreak == N)
            {
                adjustedStiffness -= stepSize;
                while (adjustedStiffness <= referenceStiffness)
                {
                    adjustedStiffness += stepSize;
                    stepSize *= stepReductionFactor;
                    adjustedStiffness -= stepSize;
                }
                correctStreak = 0;
            }
            if (falseStreak == M)
            {
                adjustedStiffness += stepSize;
                falseStreak = 0;
            }
        }
        else
        {
            // TO-DO 
        }

        if (Random.value > 0.5)
        {
            stiffness1 = referenceStiffness;
            stiffness2 = adjustedStiffness;
        }
        else
        {
            stiffness1 = adjustedStiffness;
            stiffness2 = referenceStiffness;
        }
    }

    void saveAnswer()
    {
        string line = stiffness1 + "," + stiffness2 + "," + selection + "\n";
        System.IO.File.AppendAllText(filePath, line);
    }

    void parseParametersFromFile()
    {
        // Make sure the selected file is valid
        Regex regex = new Regex(folderName + @"\/(Experiment|Demo)\/[a-zA-Z0-9]+(.)*(\.csv)");
        var capturedText = regex.Match(filePath).Value;
        if (capturedText == "")
        {
            Debug.LogError("Could not parse parameters from file path: " + filePath);
            Debug.LogError("Exiting...");
            UnityEditor.EditorApplication.isPlaying = false;
            Application.Quit();
            return;
        }
        Debug.Log("Parsing parameters from file: " + filePath);

        // Parse the file name to get the experiment type
        string[] parameters = capturedText.Split('/');
        isDemo = parameters[1] == "Demo";
        participantName = string.Join("", parameters[2]).Split('_')[0];

        // Parse the file to get the parameters
        string[] lines = System.IO.File.ReadAllLines(filePath);

        List<float> adjustedStiffnessList = new List<float>();

        for (int i = 0; i < lines.Length; i++)
        {
            trial = i + 1;
            string line = lines[i];
            string trimmedLine = line.Trim();
            string[] values = trimmedLine.Split(',');
            stiffness1 = float.Parse(values[0]);
            stiffness2 = float.Parse(values[1]);
            int selection = int.Parse(values[2]);

            adjustedStiffness = Mathf.Max(stiffness1, stiffness2);
            referenceStiffness = Mathf.Min(stiffness1, stiffness2);

            if (adjustedStiffnessList.Count > 0)
            {
                float deltaAdjustedStiffness = adjustedStiffness - adjustedStiffnessList[adjustedStiffnessList.Count - 1];
                if (deltaAdjustedStiffness != 0)
                    stepSize = Mathf.Abs(deltaAdjustedStiffness);
            }

            adjustedStiffnessList.Add(adjustedStiffness);

            isAnswerCorrectPrev = isAnswerCorrect;
            isAnswerCorrect = selection == 1 ? stiffness1 > stiffness2 : stiffness1 < stiffness2;

            // Increase the correct streak if the answer is correct
            correctStreak = isAnswerCorrect ? correctStreak + 1 : 0;

            // Increase the false streak if the answer is false
            falseStreak = !isAnswerCorrect ? falseStreak + 1 : 0;

            if (isAnswerCorrectPrev != isAnswerCorrect)
            {
                totalReversalCount++;
                reversalCount++;
            }

            if (i != lines.Length - 1)
            {
                correctStreak = correctStreak == N ? 0 : correctStreak;
                falseStreak = falseStreak == M ? 0 : falseStreak;
                if (reversalCount == reversalPerStepChange && totalReversalCount != 0) reversalCount = 0;
            }

        }
        // End the experiment when the reversal limit is reached or demo trials are completed
        isExperimentOver = totalReversalCount >= reversalLimit || (isDemo && trial >= maxDemoTrials);
    }
}