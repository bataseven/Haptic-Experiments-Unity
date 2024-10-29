using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Text.RegularExpressions;
using UnityEngine.UI;
using TMPro;


public class ConflictExperiment : MonoBehaviour
{
    public enum ExperimentState
    {
        None,
        Intro,
        Exploration,
        Exploration2,
        Answer,
        Countdown,
        End
    }
    private GameObject objects;

    private GameObject ground;
    private Color groundColor1;
    private Color groundColor2 = new Color(0.95f, 0.05f, 0.05f);

    private SerialWriter serialWriter;
    private SerialListener serialListener;

    private GameObject canvas;
    private TextMeshProUGUI title;
    private TextMeshProUGUI body;
    private TextMeshProUGUI footer;
    private TextMeshProUGUI answer1;
    private TextMeshProUGUI answer2;
    private TextMeshProUGUI deviceStatus;

    private int selection = -1;
    private float elapsedTime = 0;
    private float answerTime = 0;
    private float totalAnswerTime = 0;
    private int demoTrialLimit = 5;
    private bool paused = false;

    [Space(5)]
    [Tooltip("If true, ground color will gradually change to red with the applied force. If false, the ground will turn red when the force warning threshold is reached.")]
    public bool lerpGroundColor = false;
    public bool stopCamera = false;

    [Header("Experiment Type")]
    [Space(5)]
    public string participantName = "";
    public bool saveAnswers = true;
    public bool includeVisual = true;
    [ReadOnlyWhenPlaying]
    public bool visualConflict = false;
    public bool randomize = true;
    [ReadOnlyWhenPlaying]
    [Rename("Practice Mode")]
    public bool isDemo = false;

    [Space(15)]
    [Header("Experiment Parameters")]
    [Space(5)]
    [ReadOnlyWhenPlaying]
    public int trialLimit = 70;
    [ReadOnlyWhenPlaying]
    public int startFrom = 0;
    [ReadOnly]
    public int trial = 1;
    [ReadOnly]
    public int trueCount = 0;
    [Tooltip("Total exploration duration for the objects")]
    public int timeLimit = 15;
    public int countdownFrom = 3;
    [HideInInspector]
    public float referenceStiffness = 2f; // [N/mm]
    [HideInInspector]
    public float adjustedStiffness = 1f; // [N/mm]
    // Color the variable name in the inspector
    // [BackgroundColor(1f,0.5f,0f,1f)]
    [RequiredField(FieldColor.Orange)]
    public float stiffness1 = 1f; // [N/mm]
    // [BackgroundColor(0f,1f,43f/255f,1f)]
    [RequiredField(FieldColor.Green)]
    public float stiffness2 = 1f; // [N/mm]
    // [BackgroundColor()] 
    public float lambda = 0f;
    // public float forceMultiplier = 2f;
    [ReadOnly]
    public ExperimentState state = ExperimentState.None;
    private ExperimentState prevState = ExperimentState.None;
    private ExperimentState prevStatePermanent = ExperimentState.None;
    public float forceWarningThreshold = 8;
    [SerializeField]
    private bool isExperimentOver = false;

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

    // Integer array of 10
    private int[] L000 = { 0, 0, 0, 0 };
    private int[] L025 = { 0, 0, 0, 0 };
    private int[] L050 = { 0, 0, 0, 0 };
    private int[] L075 = { 0, 0, 0, 0 };
    private int[] L100 = { 0, 0, 0, 0 };

    private List<float> userForceHistory = new List<float>();
    private List<float> userForceTimeHistory = new List<float>();


    private string folderName = "Conflict_Experiment";
    private string answersFilePath = "";
    private string forceFilePath = "";
    // Start is called before the first frame update
    void Start()
    {
        objects = GameObject.Find("Objects");

        ground = GameObject.Find("Ground");
        groundColor1 = ground.GetComponent<Renderer>().material.color;

        serialWriter = GameObject.Find("SerialWriter").GetComponent<SerialWriter>();
        serialListener = GameObject.Find("SerialListener").GetComponent<SerialListener>();

        canvas = GameObject.Find("Canvas");
        title = GameObject.Find("Title").GetComponent<TextMeshProUGUI>();
        body = GameObject.Find("Body").GetComponent<TextMeshProUGUI>();
        footer = GameObject.Find("Footer").GetComponent<TextMeshProUGUI>();
        answer1 = GameObject.Find("Answer1").GetComponent<TextMeshProUGUI>();
        answer2 = GameObject.Find("Answer2").GetComponent<TextMeshProUGUI>();
        deviceStatus = GameObject.Find("Position").GetComponent<TextMeshProUGUI>();
        canvas.SetActive(false);

        if (visualConflict)
        {
            includeVisual = true;
            trialLimit = ExperimentConditions.stiffnessLambdaPairs.GetLength(1);
        }
        else if (isDemo)
        {
            trialLimit = ExperimentConditions.practiceStiffnessValues.GetLength(0);
        }
        else
        {
            trialLimit = ExperimentConditions.stiffnessValues1.GetLength(0);
        }
        startFrom = Mathf.Clamp(startFrom, 0, trialLimit - 2);


        forceWarningThreshold = isDemo ? 8 : forceWarningThreshold;

        // Do not change the strings below.
        string type = isDemo ? "Demo" : "Experiment";
        string visual = includeVisual ? "Visual" : "HapticOnly";
        string conflict = visualConflict ? "Conflict" : "NoConflict";

        try
        {
            if (!Directory.Exists(folderName + "/" + type + "/" + conflict))
            {
                Directory.CreateDirectory(folderName + "/" + type + "/" + conflict);
            }
        }
        catch (IOException ex)
        {
            Debug.Log(ex.Message);
        }
        // Try to create a folder to store the force value files.
        try
        {
            if (!Directory.Exists(folderName + "/" + type + "/" + conflict + "/Forces"))
            {
                Directory.CreateDirectory(folderName + "/" + type + "/" + conflict + "/Forces");
            }
        }
        catch (IOException ex)
        {
            Debug.Log(ex.Message);
        }

        if (visualConflict)
        {
            answersFilePath = folderName + "\\" + type + "\\" + conflict + "\\" + participantName + "_Conflict_" + "_" + System.DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss") + ".csv";
            forceFilePath = folderName + "\\" + type + "\\" + conflict + "\\Forces\\" + participantName + "_Conflict_" + "_" + System.DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss") + "_force.csv";
        }
        else
        {
            answersFilePath = folderName + "\\" + type + "\\" + conflict + "\\" + participantName + "_" + visual + "_" + "_" + System.DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss") + ".csv";
            forceFilePath = folderName + "\\" + type + "\\" + conflict + "\\Forces\\" + participantName + "_" + visual + "_" + "_" + System.DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss") + "_force.csv";
        }

    }

    // Update is called once per frame
    void Update()
    {
        // Device status variables
        deviceConnected = serialListener.isConnected;
        userForce = serialListener.netForce;
        fingerPos = serialListener.positions[0];
        fingerPosMm = fingerPos * ProjectVariables.countToMm;

        // deviceStatus.text = "Pos: " + string.Format("{0:0.##}", (fingerPos) * 26.1 / 107493.0) + " mm\n" + "Force: " + string.Format("{0:0.##}", -userForce) + " N";

        if (lerpGroundColor)
            ground.GetComponent<Renderer>().material.color = Color.Lerp(groundColor1, groundColor2, Mathf.Clamp(Mathf.Abs(userForce) / forceWarningThreshold, 0, 1));
        else
            ground.GetComponent<Renderer>().material.color = Mathf.Abs(userForce) > forceWarningThreshold ? groundColor2 : groundColor1;

        if (!stopCamera)
            if (includeVisual && state != ExperimentState.Intro)
                Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, new Vector3(0.035f, 0.39f, -0.71f), 0.05f); // Move camera to the objects
            else if (!includeVisual || state == ExperimentState.Intro)
                Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, new Vector3(0.035f, 0.39f, -1.8f), 0.05f); // Move the camera away

        // Pause when pressed p
        if (Input.GetKeyDown(KeyCode.P))
        {
            paused = !paused;
            if (paused) Debug.Log("Paused");
            else Debug.Log("Unpaused");
        }

        if (!paused)
            switch (state)
            {
                case ExperimentState.None:
                    if (Input.GetKeyDown(KeyCode.N))
                    {
                        foreach (Transform obj in objects.transform)
                        {
                            ConflictingObjectHand objScript = obj.gameObject.GetComponent<ConflictingObjectHand>();
                            if (!objScript.inContact) objScript.noResistance = !objScript.noResistance;
                            else Debug.Log("Object in contact. Cannot change resistance.");
                        }
                    }
                    if (Input.GetKeyDown(KeyCode.S))
                    {

                        canvas.SetActive(true);
                        state = ExperimentState.Intro;
                        GameObject.Find("HandController").GetComponent<FingerPositionControl>().objectDeterminesFingerAperture = true;
                    }
                    break;

                case ExperimentState.Intro:
                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                    {
                        body.alignment = TextAlignmentOptions.Center;
                        state = isExperimentOver ? ExperimentState.End : ExperimentState.Countdown;
                        // If visual is enabled, move the texts to not obstruct the objects
                        if (includeVisual)
                        {
                            title.rectTransform.anchoredPosition = new Vector2(title.rectTransform.anchoredPosition.x, 375);
                            body.rectTransform.anchoredPosition = new Vector2(body.rectTransform.anchoredPosition.x, 300);
                            footer.rectTransform.anchoredPosition = new Vector2(footer.rectTransform.anchoredPosition.x, -460);
                        }
                        elapsedTime = 0;
                        //  trial = startFrom;
                        clearTexts();
                    }
                    break;

                case ExperimentState.Exploration:
                    elapsedTime += Time.deltaTime;
                    answerTime += Time.deltaTime;
                    footer.text = "Time left: " + (timeLimit - (int)elapsedTime) + " s";
                    userForceHistory.Add(userForce);
                    userForceTimeHistory.Add(totalAnswerTime + answerTime);


                    if (!includeVisual)
                    {
                        answer1.text = "Object 1";
                        answer2.text = "Object 2";
                        if (objects.GetComponent<ObjectSelector>().selectedIndex == 0)
                        {
                            highlightText(answer1, new Color(1, 0.5f, 0));
                            unhighlightText(answer2);
                        }
                        else
                        {
                            highlightText(answer2, Color.green);
                            unhighlightText(answer1);
                        }
                    }
                    else
                    {
                        answer1.text = "";
                        answer2.text = "";
                    }

                    if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                    {
                        selection = objects.GetComponent<ObjectSelector>().selectedIndex;
                        state = ExperimentState.Answer;
                        elapsedTime = 0;
                        clearTexts();
                    }
                    else if (elapsedTime > timeLimit)
                    {
                        state = ExperimentState.Exploration2;
                        elapsedTime = 0;
                        clearTexts();
                        title.text = "Second chance to answer!";
                    }
                    break;

                case ExperimentState.Exploration2:
                    elapsedTime += Time.deltaTime;
                    answerTime += Time.deltaTime;
                    footer.text = "Time left: " + (timeLimit - (int)elapsedTime) + " s";
                    userForceHistory.Add(userForce);
                    userForceTimeHistory.Add(totalAnswerTime + answerTime);

                    if (!includeVisual)
                    {
                        answer1.text = "Object 1";
                        answer2.text = "Object 2";
                        if (objects.GetComponent<ObjectSelector>().selectedIndex == 0)
                        {
                            highlightText(answer1, new Color(1, 0.5f, 0));
                            unhighlightText(answer2);
                        }
                        else
                        {
                            highlightText(answer2, Color.green);
                            unhighlightText(answer1);
                        }
                    }
                    else
                    {
                        answer1.text = "";
                        answer2.text = "";
                    }

                    if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                    {
                        selection = objects.GetComponent<ObjectSelector>().selectedIndex;
                        state = ExperimentState.Answer;
                        elapsedTime = 0;
                        clearTexts();

                    }
                    else if (elapsedTime > timeLimit)
                    {
                        state = ExperimentState.Answer;
                        elapsedTime = 0;
                        clearTexts();
                    }
                    break;

                case ExperimentState.Answer:
                    totalAnswerTime += answerTime;
                    if (saveAnswers) saveAnswer();
                    bool isTrue = ((stiffness2 >= stiffness1 && selection == 1) || (stiffness2 < stiffness1 && selection == 0));
                    trueCount = isTrue ? trueCount + 1 : trueCount;
                    selection = -1;
                    trial++;
                    isExperimentOver = (!isDemo && trial > trialLimit) || (isDemo && trial > demoTrialLimit);
                    state = isExperimentOver ? ExperimentState.End : ExperimentState.Countdown;
                    elapsedTime = 0;
                    break;

                case ExperimentState.Countdown:
                    if (Mathf.Abs(userForce) > 0.1f)
                    {
                        title.text = "Please release your finger from the force sensor";
                    }
                    else if (prevStatePermanent == ExperimentState.Intro && fingerPos > 20000)
                    {
                        title.text = "Please open your fingers";
                    }
                    else
                    {
                        elapsedTime += Time.deltaTime;
                        title.text = "" + (countdownFrom - (int)elapsedTime);
                    }
                    if (elapsedTime > countdownFrom)
                    {
                        elapsedTime = 0;
                        answerTime = 0;
                        setStiffnessValues();
                        state = ExperimentState.Exploration;
                        clearTexts();
                        title.text = "Trial " + trial + " of " + (isDemo ? demoTrialLimit : trialLimit);
                        body.alignment = TextAlignmentOptions.Center;
                        body.text = "Press the space bar to select the stiffer object";
                    }
                    break;

                case ExperimentState.End:
                    title.text = "End";
                    title.color = Color.white;
                    body.text = "The experiment is finished.\n Thank you for your participation.";
                    footer.text = isDemo ? ("" + trueCount + " out of " + demoTrialLimit + " correct") : "";
                    elapsedTime += Time.deltaTime;
                    if (elapsedTime > 3)
                    {
                        // Quit the application
                        UnityEditor.EditorApplication.isPlaying = false;
                        Application.Quit();
                    }
                    break;
            }
        if (prevState != state)
            prevStatePermanent = prevState;
        prevState = state;
    }

    void highlightText(TextMeshProUGUI text, Color color)
    {
        text.fontStyle = FontStyles.Bold;
        text.fontSize = 75;
        text.color = color;
    }

    void unhighlightText(TextMeshProUGUI text)
    {
        text.fontStyle = FontStyles.Normal;
        text.fontSize = 30;
        text.color = Color.white;
    }

    void clearTexts()
    {
        title.text = "";
        body.text = "";
        footer.text = "";
        answer1.text = "";
        answer2.text = "";
        unhighlightText(answer1);
        unhighlightText(answer2);
    }

    void onApplicationQuit()
    {
        serialWriter.SendPsuedoObject(0, 0, 0);
    }

    void setStiffnessValues()
    {
        int objIndex = 0;
        float rnd = Random.value;
        foreach (Transform obj in objects.transform)
        {
            ConflictingObjectHand objScript = obj.gameObject.GetComponent<ConflictingObjectHand>();
            objScript.noResistance = false;
            if (isDemo)
            {
                adjustedStiffness = referenceStiffness + (referenceStiffness * ExperimentConditions.practiceStiffnessValues[trial - 1] / 100f);
                objScript.stiffness = adjustedStiffness;
                objScript.referenceStiffness = referenceStiffness;
                lambda = 0;
                objScript.lambda = lambda;
                objScript.damping = 0.05f;

                if (objIndex == 0 && rnd <= 0.5f)
                {
                    objScript.referenceObject = true;
                }
                else if (objIndex == 1 && rnd > 0.5f)
                {
                    objScript.referenceObject = true;
                }
                else
                {
                    objScript.referenceObject = false;
                }
            }
            else if (includeVisual && visualConflict)
            {
                if (randomize)
                {
                    int idx = trial - 1 + startFrom;
                    adjustedStiffness = referenceStiffness + (referenceStiffness * ExperimentConditions.stiffnessLambdaPairs[0, idx] / 100f);
                    objScript.stiffness = adjustedStiffness;
                    objScript.referenceStiffness = referenceStiffness;
                    if (objIndex == ExperimentConditions.referenceValues[idx % ExperimentConditions.referenceValues.Length])
                        objScript.referenceObject = true;
                    else
                        objScript.referenceObject = false;
                    lambda = ExperimentConditions.stiffnessLambdaPairs[1, idx];
                    GameObject.Find("Objects").GetComponent<ObjectSelector>().lambda = lambda;
                    objScript.damping = 0.05f;
                }
                else
                {
                    adjustedStiffness = referenceStiffness + (referenceStiffness * ExperimentConditions.stiffnessLambdaPairs2[0, trial - 1] / 100f);
                    objScript.stiffness = adjustedStiffness;
                    objScript.referenceStiffness = referenceStiffness;
                    if (objIndex == 0)
                        objScript.referenceObject = true;
                    else
                        objScript.referenceObject = false;
                    lambda = ExperimentConditions.stiffnessLambdaPairs2[1, trial - 1];
                    GameObject.Find("Objects").GetComponent<ObjectSelector>().lambda = lambda;
                    objScript.damping = 0.05f;
                }
            }
            else if (includeVisual)
            {
                adjustedStiffness = referenceStiffness + (referenceStiffness * ExperimentConditions.stiffnessValues2[trial - 1] / 100f);
                objScript.stiffness = adjustedStiffness;
                objScript.referenceStiffness = referenceStiffness;
                if (objIndex == ExperimentConditions.referenceValues[(trial - 1) % ExperimentConditions.referenceValues.Length])
                    objScript.referenceObject = true;
                else
                    objScript.referenceObject = false;
                lambda = 0;
                objScript.lambda = lambda;
                objScript.damping = 0.05f;
            }
            else
            {
                adjustedStiffness = referenceStiffness + (referenceStiffness * ExperimentConditions.stiffnessValues1[trial - 1] / 100f);
                objScript.stiffness = adjustedStiffness;
                objScript.referenceStiffness = referenceStiffness;
                if (objIndex == ExperimentConditions.referenceValues[(trial - 1) % ExperimentConditions.referenceValues.Length])
                    objScript.referenceObject = true;
                else
                    objScript.referenceObject = false;
                lambda = 0;
                objScript.lambda = lambda;
                objScript.damping = 0.05f;

            }
            // !TO-DO:
            // TODO: This approach assumes that the left object is the first object in the list. This is not a good assumption?
            if (objIndex == 0)
            {
                if (objScript.referenceObject)
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
            objIndex++;
        }
        // foreach (Transform obj in objects.transform){}
    }

    void saveAnswer()
    {
        string line = stiffness1 + "," + stiffness2 + "," + selection + "," + answerTime + "\n";
        if (visualConflict)
        {
            line = stiffness1 + "," + stiffness2 + "," + selection + "," + answerTime + "," + lambda + "\n";
            bool isTrue = ((stiffness2 >= stiffness1 && selection == 1) || (stiffness2 < stiffness1 && selection == 0));

            if (isTrue)
            {
                float deltaStiffness = (adjustedStiffness - referenceStiffness) / referenceStiffness * 100;
                if (lambda == 0f)
                {
                    if (deltaStiffness == 25) L000[0]++;
                    else if (deltaStiffness == 50) L000[1]++;
                    else if (deltaStiffness == 75) L000[2]++;
                    else if (deltaStiffness == 100) L000[3]++;
                }
                else if (lambda == 0.25f)
                {
                    if (deltaStiffness == 25) L025[0]++;
                    else if (deltaStiffness == 50) L025[1]++;
                    else if (deltaStiffness == 75) L025[2]++;
                    else if (deltaStiffness == 100) L025[3]++;
                }
                else if (lambda == 0.5f)
                {
                    if (deltaStiffness == 25) L050[0]++;
                    else if (deltaStiffness == 50) L050[1]++;
                    else if (deltaStiffness == 75) L050[2]++;
                    else if (deltaStiffness == 100) L050[3]++;
                }
                else if (lambda == 0.75f)
                {
                    if (deltaStiffness == 25) L075[0]++;
                    else if (deltaStiffness == 50) L075[1]++;
                    else if (deltaStiffness == 75) L075[2]++;
                    else if (deltaStiffness == 100) L075[3]++;
                }
                else if (lambda == 1f)
                {
                    if (deltaStiffness == 25) L100[0]++;
                    else if (deltaStiffness == 50) L100[1]++;
                    else if (deltaStiffness == 75) L100[2]++;
                    else if (deltaStiffness == 100) L100[3]++;
                }
            }

        }
        System.IO.File.AppendAllText(answersFilePath, line);
        // Append the force list to the file
        for (int i = 0; i < userForceHistory.Count; i++)
        {
            float force = userForceHistory[i];
            float time = userForceTimeHistory[i];
            System.IO.File.AppendAllText(forceFilePath, force + "," + time + "," + trial + "\n");
        }
        userForceHistory.Clear();
        userForceTimeHistory.Clear();
    }

}

