using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Text.RegularExpressions;
using UnityEngine.UI;
using TMPro;

public class PalmExperiment : MonoBehaviour
{
    public enum ExperimentState
    {
        None,
        Intro,
        Exploration,
        DirectionAnswer,
        MagnitudeAnswer,
        Countdown,
        End
    }

    private GameObject ground;
    private Color groundColor1;
    private Color groundColor2 = new Color(0.95f, 0.05f, 0.05f);

    private GameObject canvas;
    private TextMeshProUGUI title;
    private TextMeshProUGUI body;
    private TextMeshProUGUI footer;
    private TextMeshProUGUI answer1;
    private TextMeshProUGUI answer2;
    private TextMeshProUGUI deviceStatus;
    private TMP_InputField inputField;
    // Floating numbers between 0 and 9999.99
    private string pattern = @"^([0-9]{1,4}([.][0-9]{0,2})?)$";

    private RawImage upArrow;
    private RawImage downArrow;

    [Header("Experiment Type")]
    [Space(5)]
    public string participantName = "";
    public bool saveAnswers = true;
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
    public int countdownFrom = 3;
    public int retractionSpeed = 3000;
    [ReadOnly]
    public float trialVelocity = 0;
    [ReadOnly]
    public float trialPosition = 0;
    [ReadOnly]
    public float deltaPosition = 0;
    [ReadOnly]
    public float deltaPositionMm = 0;
    [HideInInspector]
    public float initialPosition = 0;

    [ReadOnly]
    public ExperimentState state = ExperimentState.None;
    private ExperimentState prevState = ExperimentState.None;
    private ExperimentState prevStatePermanent = ExperimentState.None;


    private int directionAnswer = 0;
    private float magnitudeAnswer = 0;
    private float elapsedTime = 0;
    private float totalAnswerTime = 0;
    private int demoTrialLimit = 36;
    private bool paused = false;
    [SerializeField]
    private bool isExperimentOver = false;
    private HapticDevice hapticDevice;

    [Space(15)]
    [Header("Palm Control")]
    [Space(5)]
    [Range(ProjectVariables.PalmLowerSoftLimit, ProjectVariables.PalmUpperSoftLimit)]
    public int positionToMove = ProjectVariables.PalmLowerSoftLimit;
    private int positionToMovePrev = ProjectVariables.PalmLowerSoftLimit;
    [ReadOnly]
    [Rename("Position To Move (mm)")]
    public float positionToMoveMm = ProjectVariables.PalmLowerSoftLimit * ProjectVariables.countToMm2;

    [Range(-10000, 10000)]
    public int speedToMove = 0;
    private int speedToMovePrev = 0;
    [ReadOnly]
    [Rename("Speed To Move (mm/s)")]
    public float speedToMoveMm = 0;
    private int midPoint = (ProjectVariables.PalmUpperSoftLimit + ProjectVariables.PalmLowerSoftLimit) / 2;


    [MinMaxSlider(ProjectVariables.PalmLowerSoftLimit, ProjectVariables.PalmUpperSoftLimit)]
    public Vector2Int softLimit = new Vector2Int(ProjectVariables.PalmLowerSoftLimit, ProjectVariables.PalmUpperSoftLimit);
    private Vector2Int softLimitPrev = new Vector2Int(ProjectVariables.PalmLowerSoftLimit, ProjectVariables.PalmUpperSoftLimit);

    [MinMaxSlider(ProjectVariables.PalmLowerHardLimit, ProjectVariables.PalmUpperHardLimit)]
    public Vector2Int hardLimit = new Vector2Int(ProjectVariables.PalmLowerHardLimit, ProjectVariables.PalmUpperHardLimit);
    private Vector2Int hardLimitPrev = new Vector2Int(ProjectVariables.PalmLowerHardLimit, ProjectVariables.PalmUpperHardLimit);

    public bool stopPalmMotor = false;
    private bool stopPalmMotorPrev = false;

    private string folderName = "Palm_Experiment";
    private string answersFilePath = "";

    private Vector3 titlePos = new Vector3(0, 0, 0);
    private Vector3 bodyPos = new Vector3(0, 0, 0);
    private Vector3 footerPos = new Vector3(0, 0, 0);
    private Vector3 answer1Pos = new Vector3(0, 0, 0);
    private Vector3 answer2Pos = new Vector3(0, 0, 0);
    private Vector3 deviceStatusPos = new Vector3(0, 0, 0);
    private Vector3 inputFieldPos = new Vector3(0, 0, 0);

    [HideInInspector]
    public Texture2D redArrowTexture;
    [HideInInspector]
    public Texture2D whiteArrowTexture;

    void Start()
    {
        // Set the screen resolution to 720p
        // Screen.SetResolution(1280, 720, false);
        hapticDevice = GameObject.Find("HapticDevice").GetComponent<HapticDevice>();

        canvas = GameObject.Find("Canvas");
        title = GameObject.Find("Title").GetComponent<TextMeshProUGUI>();
        body = GameObject.Find("Body").GetComponent<TextMeshProUGUI>();
        footer = GameObject.Find("Footer").GetComponent<TextMeshProUGUI>();
        answer1 = GameObject.Find("Answer1").GetComponent<TextMeshProUGUI>();
        answer2 = GameObject.Find("Answer2").GetComponent<TextMeshProUGUI>();
        deviceStatus = GameObject.Find("Position").GetComponent<TextMeshProUGUI>();
        inputField = GameObject.Find("InputField").GetComponent<TMP_InputField>();
        upArrow = GameObject.Find("UpArrow").GetComponent<RawImage>();
        downArrow = GameObject.Find("DownArrow").GetComponent<RawImage>();

        inputField.text = "";
        inputField.placeholder.GetComponent<TextMeshProUGUI>().text = "Enter up to 4 digits";

        inputField.onValidateInput += delegate (string input, int charIndex, char addedChar)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(input + addedChar, pattern))
                return addedChar;
            else
                return '\0';
        };

        trialLimit = isDemo ? ExperimentConditions.practicePositionVelocityPairs.GetLength(1) : ExperimentConditions.positionVelocityPairs.GetLength(1);
        // Do not change the strings below.
        string type = isDemo ? "Demo" : "Experiment";
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

        if (isDemo)
            answersFilePath = folderName + "\\" + type + "\\" + participantName + "_" + System.DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss") + ".csv";
        else
            answersFilePath = folderName + "\\" + type + "\\" + participantName + "_" + System.DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss") + ".csv";

        softLimit = hapticDevice.palm.softLimits;
        hardLimit = hapticDevice.palm.hardLimits;
        stopPalmMotor = hapticDevice.palm.isStopped;

        // Save the initial positions of the text items
        titlePos = title.transform.position;
        bodyPos = body.transform.position;
        footerPos = footer.transform.position;
        answer1Pos = answer1.transform.position;
        answer2Pos = answer2.transform.position;
        deviceStatusPos = deviceStatus.transform.position;
        inputFieldPos = inputField.transform.position;

        // Set the initial positions of the text items
        title.transform.position = new Vector3(-10000, -10000, 0);
        body.transform.position = new Vector3(-10000, -10000, 0);
        footer.transform.position = new Vector3(-10000, -10000, 0);
        answer1.transform.position = new Vector3(-10000, -10000, 0);
        answer2.transform.position = new Vector3(-10000, -10000, 0);
        deviceStatus.transform.position = new Vector3(-10000, -10000, 0);
        // inputField.transform.position = new Vector3(-10000, -10000, 0);
        inputField.gameObject.SetActive(false);
        upArrow.gameObject.SetActive(false);
        downArrow.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            StartCoroutine(MovePalmTo(true, 0));
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            stopPalmMotor = !stopPalmMotor;
            hapticDevice.SetStop(1, stopPalmMotor);
            stopPalmMotorPrev = stopPalmMotor;
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            StartCoroutine(MovePalmTo(false, midPoint));
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            paused = !paused;
        }

        if (!paused)
        {
            switch (state)
            {
                case ExperimentState.None:
                    if (Input.GetKeyDown(KeyCode.S))
                    {
                        canvas.SetActive(true);
                        state = ExperimentState.Intro;
                        title.transform.position = titlePos;
                        body.transform.position = bodyPos;
                        footer.transform.position = footerPos;
                        answer1.transform.position = answer1Pos;
                        answer2.transform.position = answer2Pos;
                        deviceStatus.transform.position = deviceStatusPos;
                    }
                    break;
                case ExperimentState.Intro:
                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                    {
                        body.alignment = TextAlignmentOptions.Center;
                        state = isExperimentOver ? ExperimentState.End : ExperimentState.Countdown;
                        if (state == ExperimentState.Countdown)
                        {
                            hapticDevice.SetPositionLimit(1, HapticDevice.LimitType.HARD, midPoint - 10, midPoint + 10);
                            hapticDevice.MoveWithVelocity(1, hapticDevice.palm.position > midPoint ? -retractionSpeed : retractionSpeed);
                        }
                        elapsedTime = 0;
                        ClearTexts();
                    }
                    break;

                case ExperimentState.Exploration:
                    elapsedTime += Time.deltaTime;
                    // footer.text = "Time left: " + (timeLimit - (int)elapsedTime) + " s";
                    deltaPosition = hapticDevice.palm.position - initialPosition;
                    deltaPositionMm = deltaPosition * ProjectVariables.countToMm2;
                    if (elapsedTime > 1 && !hapticDevice.palm.isMoving)
                    {
                        state = ExperimentState.DirectionAnswer;
                        StartCoroutine(HighlightArrows());
                        elapsedTime = 0;
                        ClearTexts();
                        title.text = "In which direction was the tactor moving?";
                    }

                    break;

                case ExperimentState.DirectionAnswer:
                    // Capture up and down arrow keys
                    if (Input.GetKeyDown(KeyCode.UpArrow)) directionAnswer = 1;
                    else if (Input.GetKeyDown(KeyCode.DownArrow)) directionAnswer = -1;

                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                    {
                        if (directionAnswer == 0)
                        {
                            footer.text = "Please answer the question before continuing.";
                            break;
                        }
                        trueCount = (trialVelocity * directionAnswer > 0) ? trueCount + 1 : trueCount;
                        state = ExperimentState.MagnitudeAnswer;
                        // Show the input field
                        StartCoroutine(ShowInputField());
                        elapsedTime = 0;
                        ClearTexts();
                        title.text = "How would you rate the magnitude of the stretch you felt in your palm?";
                    }
                    break;

                case ExperimentState.MagnitudeAnswer:
                    bool repeatTrial = Input.GetKeyDown(KeyCode.R);
                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || repeatTrial)
                    {
                        if (inputField.text == "" && !repeatTrial)
                        {
                            footer.text = "Please answer the question before continuing.";
                            StartCoroutine(ShowInputField());
                            break;
                        }
                        if (!repeatTrial)
                            trial++;
                        isExperimentOver = (!isDemo && trial > trialLimit) || (isDemo && trial > demoTrialLimit);
                        state = isExperimentOver ? ExperimentState.End : ExperimentState.Countdown;
                        magnitudeAnswer = 0;
                        float.TryParse(inputField.text, out magnitudeAnswer);

                        if (state == ExperimentState.Countdown)
                        {
                            hapticDevice.SetPositionLimit(1, HapticDevice.LimitType.HARD, midPoint - 10, midPoint + 10);
                            hapticDevice.MoveWithVelocity(1, hapticDevice.palm.position > midPoint ? -retractionSpeed : retractionSpeed);
                        }
                        elapsedTime = 0;
                        ClearTexts();
                        // Hide the input field
                        inputField.gameObject.SetActive(false);
                        if (saveAnswers && !repeatTrial)
                            SaveAnswer();
                        directionAnswer = 0;
                    }
                    break;

                case ExperimentState.Countdown:
                    if (hapticDevice.palm.isMoving)
                    {
                        title.text = "Moving to the middle position...";
                    }
                    else
                    {
                        elapsedTime += Time.deltaTime;
                        title.text = "" + (countdownFrom - (int)elapsedTime);
                    }

                    if (elapsedTime > countdownFrom)
                    {
                        elapsedTime = 0;
                        SetLimitAndSpeed();
                        state = ExperimentState.Exploration;
                        initialPosition = hapticDevice.palm.position;
                        ClearTexts();
                        title.text = "Trial " + trial + " of " + (isDemo ? demoTrialLimit : trialLimit);
                        body.alignment = TextAlignmentOptions.Center;
                        body.text = "Tactor is moving...";
                    }
                    break;

                case ExperimentState.End:
                    title.text = "End";
                    title.color = Color.white;
                    body.text = "The experiment is finished.\n Thank you for your participation.";
                    elapsedTime += Time.deltaTime;
                    if (elapsedTime > 3)
                    {
                        // Quit the application
                        UnityEditor.EditorApplication.isPlaying = false;
                        Application.Quit();
                    }
                    break;
            }
        }
        // footer.text = ((hapticDevice.palm.position - midPoint) * ProjectVariables.countToMm2).ToString() + " mm";
        positionToMoveMm = positionToMove * ProjectVariables.countToMm2;
        speedToMoveMm = speedToMove * ProjectVariables.countToMm2;
        stopPalmMotor = hapticDevice.palm.isStopped;
        softLimit = hapticDevice.palm.softLimits;
        hardLimit = hapticDevice.palm.hardLimits;
        if (prevState != state)
            prevStatePermanent = prevState;
        prevState = state;
    }

    IEnumerator ShowInputField()
    {
        yield return new WaitForEndOfFrame();
        inputField.gameObject.SetActive(true);
        inputField.Select();
        inputField.ActivateInputField();
    }

    IEnumerator HighlightArrows()
    {
        upArrow.gameObject.SetActive(true);
        downArrow.gameObject.SetActive(true);
        Vector3 upArrowPos = upArrow.transform.position;
        Vector3 downArrowPos = downArrow.transform.position;
        float counter = 0;
        float amplitude = 12f;
        int directionAnswerPrev = 0;
        while (state == ExperimentState.DirectionAnswer)
        {
            counter = directionAnswer != directionAnswerPrev ? 0 : counter;
            if (directionAnswer == 1)
            {
                // Set the image of up arrow to "Assets/Sprites/ArrowRed" 
                upArrow.texture = redArrowTexture;
                downArrow.texture = whiteArrowTexture;
                // Bounce the up arrow
                upArrow.transform.position = Vector3.Lerp(upArrow.transform.position, upArrowPos + amplitude * (new Vector3(0, Mathf.Sin(counter), 0)), 0.8f);
                downArrow.transform.position = Vector3.Lerp(downArrow.transform.position, downArrowPos, 0.8f);
                footer.text = "";
            }
            else if (directionAnswer == -1)
            {
                // Set the image of down arrow to "Assets/Sprites/ArrowRed" 
                upArrow.texture = whiteArrowTexture;
                downArrow.texture = redArrowTexture;
                // Bounce the down arrow
                upArrow.transform.position = Vector3.Lerp(upArrow.transform.position, upArrowPos, 0.8f);
                downArrow.transform.position = Vector3.Lerp(downArrow.transform.position, downArrowPos - amplitude * (new Vector3(0, Mathf.Sin(counter), 0)), 0.8f);
                footer.text = "";
            }
            else
            {
                // Set the image of both arrows to "Assets/Sprites/ArrowWhite" 
                upArrow.texture = whiteArrowTexture;
                downArrow.texture = whiteArrowTexture;
                upArrow.transform.position = upArrowPos;
                downArrow.transform.position = downArrowPos;
            }
            counter += 0.4f;
            directionAnswerPrev = directionAnswer;
            yield return new WaitForSeconds(0.1f);
        }
        upArrow.transform.position = upArrowPos;
        downArrow.transform.position = downArrowPos;
        upArrow.gameObject.SetActive(false);
        downArrow.gameObject.SetActive(false);
    }

    void SaveAnswer()
    {
        string line = trialPosition + "," + trialVelocity + "," + directionAnswer + "," + magnitudeAnswer + "," + (deltaPositionMm).ToString() + "\n";
        System.IO.File.AppendAllText(answersFilePath, line);
    }

    void ClearTexts()
    {
        title.text = "";
        body.text = "";
        footer.text = "";
        answer1.text = "";
        answer2.text = "";
        inputField.text = "";
        UnhighlightText(answer1);
        UnhighlightText(answer2);
    }

    void HighlightText(TextMeshProUGUI text, Color color)
    {
        text.fontStyle = FontStyles.Bold;
        text.fontSize = 75;
        text.color = color;
    }

    void UnhighlightText(TextMeshProUGUI text)
    {
        text.fontStyle = FontStyles.Normal;
        text.fontSize = 30;
        text.color = Color.white;
    }

    void SetLimitAndSpeed()
    {
        //  !TODO DEMO
        float veloFactor = 1.09f; // Correct for the shitty PID velocity gains of the haptic device

        if (!isDemo)
        {
            trialVelocity = ExperimentConditions.positionVelocityPairs[1, trial - 1];
            trialPosition = ExperimentConditions.positionVelocityPairs[0, trial - 1];
        }
        else
        {
            trialVelocity = ExperimentConditions.practicePositionVelocityPairs[1, trial - 1];
            trialPosition = ExperimentConditions.practicePositionVelocityPairs[0, trial - 1];
        }

        float countToMm = ProjectVariables.countToMm2;

        bool adjustForLooseWire = trialPosition <= 6 && Mathf.Abs(trialVelocity) <= 6;

        float upperLimit = midPoint + (trialPosition / countToMm) * (adjustForLooseWire ? 1.15f : 1);
        float lowerLimit = midPoint - (trialPosition / countToMm) * (adjustForLooseWire ? 1.15f : 1);

        hapticDevice.SetPositionLimit(1, HapticDevice.LimitType.HARD, (int)lowerLimit, (int)upperLimit);
        hapticDevice.SetPositionLimit(1, HapticDevice.LimitType.SOFT, (int)lowerLimit, (int)upperLimit);
        hapticDevice.MoveWithVelocity(1, (int)(trialVelocity * veloFactor / countToMm));
    }

    IEnumerator MovePalmTo(bool homeBeforeMove, int position)
    {
        if (homeBeforeMove)
        {
            hapticDevice.Home(1);
            yield return new WaitForSeconds(0.1f);
        }
        hapticDevice.MoveToPosition(1, position);
    }

    void OnValidate()
    {
        // Make sure the soft limit is within the hard limit
        if (softLimit != softLimitPrev)
        {
            if (softLimit.x < hardLimit.x) hardLimit.x = softLimit.x;
            if (softLimit.y > hardLimit.y) hardLimit.y = softLimit.y;
        }
        if (hardLimit != hardLimitPrev)
        {
            if (softLimit.x < hardLimit.x) softLimit.x = hardLimit.x;
            if (softLimit.y > hardLimit.y) softLimit.y = hardLimit.y;
            if (softLimit.y < hardLimit.x) softLimit.y = hardLimit.x;
            if (softLimit.x > hardLimit.y) softLimit.x = hardLimit.y;
            // Make sure the lower soft limit is not lower than the upper soft limit
        }


        if (hapticDevice != null)
        {
            if (positionToMove != positionToMovePrev) StartCoroutine(MovePalmTo(false, positionToMove));
            if (speedToMove != speedToMovePrev) hapticDevice.MoveWithVelocity(1, speedToMove);
            if (stopPalmMotor != stopPalmMotorPrev) hapticDevice.SetStop(1, stopPalmMotor);
            if (softLimit != softLimitPrev) hapticDevice.SetPositionLimit(1, HapticDevice.LimitType.SOFT, softLimit);
            if (hardLimit != hardLimitPrev) hapticDevice.SetPositionLimit(1, HapticDevice.LimitType.HARD, hardLimit);
        }

        positionToMovePrev = positionToMove;
        speedToMovePrev = speedToMove;
        softLimitPrev = softLimit;
        hardLimitPrev = hardLimit;
        stopPalmMotorPrev = stopPalmMotor;
    }

}
