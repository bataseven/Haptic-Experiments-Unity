using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectVariables
{   
    public const int RigidBodyStiffnessThreshold = 12;

    public const int FingerLowerSoftLimit = 0;
    public const int FingerUpperSoftLimit = 160000;
    public const int FingerLowerHardLimit = -5000;
    public const int FingerUpperHardLimit = 165000;

    public const int PalmLowerSoftLimit = 550;
    public const int PalmUpperSoftLimit = 7350;
    public const int PalmLowerHardLimit = 400;
    public const int PalmUpperHardLimit = 7500;

    public const float countToMm = 29.5f / 99510.0f;
    public const float countToMm2 = (6.8f * Mathf.PI) / (12 * 298);
}
