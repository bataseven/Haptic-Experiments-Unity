using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectSelector : MonoBehaviour
{   
    private SerialListener serialListener;

    private int prevIndex;

    [Space(5)]
    [Header("Object Deformation")]
    [Space(5)]
    [Rename("Deformation Param (λ)")]
    [Range(0, 1)]
    public float lambda = 0f;
    [Range(-1, 0.5f)]
    public float poissonsRatio = 0.5f;
    // [Rename()]
    public bool constantVolume = false;
    [Rename("H-Deformation")]
    public float forceMultiplier = 1f; // Berkay 5 e 2.5 tu// 1.4 - 1 Irem // 1.5 - Feras // Enes 1.5 // Berkay 1.4 // Berk 1 // bundan sonra 1
    [Rename("V-Deformation")]
    public float heightMultiplier = 1f;
    [ReadOnly]
    public float appliedForce = 0;

    [Space(15)]
    [Header("Object Positioning")]
    [Space(5)]
    [ReadOnly]
    public int selectedIndex;
    public float objectSpacing = 1f;
    public float handToObjectOffset = 0f;
    public float altitudeOffset = 0f;



    private int numberOfObjects;
    void Start()
    {
        serialListener = GameObject.Find("SerialListener").GetComponent<SerialListener>();

        numberOfObjects = transform.childCount;
        selectedIndex = 0;

        int i = 0;
        foreach (Transform child in transform)
        {
            SpringObject springObjectScript = child.gameObject.GetComponent<SpringObject>();
            if (springObjectScript != null)
            {
                springObjectScript.bodyIndex = i++;
            }
            else
            {
                ConflictingObjectHand conflictingObjectScript = child.gameObject.GetComponent<ConflictingObjectHand>();
                conflictingObjectScript.bodyIndex = i++;
            }
        }

    }

    // Update is called once per frame
    void Update()
    {   
        appliedForce = serialListener.netForce;
        SetObjectIndex();
    }

    void SetObjectIndex()
    {
        bool right = Input.GetKeyDown(KeyCode.RightArrow);
        bool left = Input.GetKeyDown(KeyCode.LeftArrow);

        if (right && Mathf.Abs(appliedForce) < 0.1)
        {
            selectedIndex = Mathf.Min(numberOfObjects - 1, selectedIndex + 1);

            foreach (Transform child in transform)
            {
                if (prevIndex != selectedIndex)
                {

                    SpringObject springObjectScript = child.gameObject.GetComponent<SpringObject>();
                    if (springObjectScript != null)
                    {
                        springObjectScript.bodyIndex--;
                    }
                    else
                    {
                        ConflictingObjectHand conflictingObjectScript = child.gameObject.GetComponent<ConflictingObjectHand>();
                        conflictingObjectScript.bodyIndex--;
                    }

                }
            }
        }

        else if (left && Mathf.Abs(appliedForce) < 0.1)
        {
            selectedIndex = Mathf.Max(0, selectedIndex - 1);
            foreach (Transform child in transform)
            {
                if (prevIndex != selectedIndex)
                {
                    SpringObject springObjectScript = child.gameObject.GetComponent<SpringObject>();
                    if (springObjectScript != null)
                    {
                        springObjectScript.bodyIndex++;
                    }
                    else
                    {
                        ConflictingObjectHand conflictingObjectScript = child.gameObject.GetComponent<ConflictingObjectHand>();
                        conflictingObjectScript.bodyIndex++;
                    }
                }
            }
        }
        prevIndex = selectedIndex;
    }
}
