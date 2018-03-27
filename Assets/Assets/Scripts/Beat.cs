using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Beat {
    public int windowNumber;
    public float timeStamp;
    public float energyValue;
    public float averageAtTime;
    public float beatTarget;

    public float[] energyPerBand;
    public float[] averagePerBand;
    public float[] beatTargetPerBand;

    public Beat()
    {

    }

    public Beat(int windowNumber, float timeStamp)
    {
        this.windowNumber = windowNumber;
        this.timeStamp = timeStamp;
    }

    public Beat(int windowNumber, float timeStamp, float energyValue, float averageAtTime, float beatTarget)
    {
        this.windowNumber = windowNumber;
        this.timeStamp = timeStamp;
        this.energyValue = energyValue;
        this.averageAtTime = averageAtTime;
        this.beatTarget = beatTarget;
    }

    public Beat(int windowNumber, float timeStamp, float[] energyPerBand, float[] averagePerBand, float[] beatTargetPerBand)
    {
        this.windowNumber = windowNumber;
        this.timeStamp = timeStamp;
        this.energyPerBand = energyPerBand;
        this.averagePerBand = averagePerBand;
        this.beatTargetPerBand = beatTargetPerBand;

        energyValue = 0;
        for (int i = 0; i < energyPerBand.Length; i += 1)
        {
            energyValue += energyPerBand[i];
        }

        energyValue /= energyPerBand.Length;
    }
}
