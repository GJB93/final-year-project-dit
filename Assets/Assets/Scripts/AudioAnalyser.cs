﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/** Used Tutorial References: 
 * https://youtu.be/wtXirrO-iNA
 * https://youtu.be/4Av788P9stk
 * 
**/

public static class AudioAnalyser {
    
    private const int SAMPLE_SIZE = 1024;
    private const int BAND_SIZE = 7;

    /**
     * The following three functions, (GetRmsValue, GetDBValue and GetPitchValue) 
     * are written with reference to code snippets found at this link: 
     * https://answers.unity.com/questions/157940/getoutputdata-and-getspectrumdata-they-represent-t.html
    **/
    public static float GetRmsValue(float[] samples)
    {
        float sum = 0;
        for (int i = 0; i < SAMPLE_SIZE; i += 1)
        {
            sum += samples[i] * samples[i];
        }
        return Mathf.Sqrt(sum / SAMPLE_SIZE);
    }

    public static float GetDBValue(float[] samples)
    {
        return 20 * Mathf.Log10(GetRmsValue(samples) / 0.1f);
    }

    public static float GetPitchValue(float[] spectrum, float sampleRate, float sampleSize)
    {
        float maxV = 0;
        var maxN = 0;
        float hzPerInterval = GetHzPerInterval(sampleRate, sampleSize);
        for (int i = 0; i < SAMPLE_SIZE; i += 1)
        {
            if (!(spectrum[i] > maxV) || !(spectrum[i] > 0.0f))
                continue;

            maxV = spectrum[i];
            maxN = i;
        }

        float freqN = maxN;
        if (maxN > 0 && maxN < SAMPLE_SIZE - 1)
        {
            var dL = spectrum[maxN - 1] / spectrum[maxN];
            var dR = spectrum[maxN - 1] / spectrum[maxN];
            freqN += 0.5f * (dR * dR - dL * dL);
        }

        return freqN * hzPerInterval;
    }
    /**
     *  End of code snippet https://answers.unity.com/questions/157940/getoutputdata-and-getspectrumdata-they-represent-t.html
    **/

    public static float GetInstantEnergy(float[] rightChannel, float[] leftChannel)
    {
        float instantEnergy = 0;
        for (int i = 0; i < rightChannel.Length; i += 1)
        {
            if (leftChannel != null)
            {
                instantEnergy += (rightChannel[i] * rightChannel[i]) + (leftChannel[i] * leftChannel[i]);
            }
            else
            {
                instantEnergy += (rightChannel[i] * rightChannel[i]);
            }
                
        }
        return instantEnergy;
    }

    public static float GetLocalAverageEnergy(float[] instantEnergyHistory)
    {
        float squaredSum = 0;

        foreach(float instantEnergy in instantEnergyHistory)
        {
            squaredSum += Mathf.Pow(instantEnergy, 2);
        }

        return squaredSum / instantEnergyHistory.Length;
    }

    public static float GetEnergyVariance(float[] instantEnergyHistory, float localAverageEnergy)
    {
        float squaredDifference = 0;

        foreach (float instantEnergy in instantEnergyHistory)
        {
            squaredDifference += Mathf.Pow(instantEnergy - localAverageEnergy, 2);
        }

        return squaredDifference / instantEnergyHistory.Length;
    }

    public static float GetEnergyFormulaConstant(float variance, float sensitivity)
    {
        return (-0.0025714f * variance) + sensitivity;
    }

    public static float GetVoltageRatio(float decibelValue)
    {
        Debug.Log("VoltageRatio is " + (Mathf.Pow(10, (decibelValue / 20))));
        return Mathf.Pow(10, (decibelValue / 20));
    }

    private static float[] BandLoop(float[] spectrum, int startPos, int endPos)
    {
        float[] temp = new float[endPos];
        for (int i = startPos; i <= endPos; i += 1)
        {
            temp[i - 1] = spectrum[i];
        }
        return temp;
    }

    public static List<float[]> GetDistinctBands(float[] spectrum, float sampleRate, float sampleSize)
    {
        /*
         * Sub-Bass:            20Hz - 60Hz         => 40Hz bandwidth
         * Bass:                60Hz - 250Hz        => 190Hz bandwidth
         * Low Midrange =       250Hz - 500Hz       => 250Hz bandwidth
         * Midrange =           500Hz - 2kHz        => 1.5kHz bandwidth
         * Upper Midrange =     2kHz - 4kHz         => 2kHz bandwidth
         * Presence =           4kHz - 6kHz         => 2kHz bandwidth
         * Brilliance =         6kHz - 20kHz        => 14kHz bandwidth
         */

        float hzPerInterval = GetHzPerInterval(sampleRate, sampleSize);

        int subBassSize = Mathf.CeilToInt(40 / hzPerInterval);
        int bassSize = Mathf.CeilToInt(190 / hzPerInterval);
        int lowMidrangeSize = Mathf.CeilToInt(250 / hzPerInterval);
        int midrangeSize = Mathf.CeilToInt(1500 / hzPerInterval);
        int upperMidrangeSize = Mathf.CeilToInt(2000 / hzPerInterval);
        int presenceSize = Mathf.CeilToInt(2000 / hzPerInterval);
        int brillianceSize = Mathf.CeilToInt(14000 / hzPerInterval);

        List<float[]> bands = new List<float[]>();
        
        int bassRange = bassSize + subBassSize;
        int lowMidrangeRange = lowMidrangeSize + bassSize;
        int midrangeRange = midrangeSize + lowMidrangeSize;
        int upperMidrangeRange = upperMidrangeSize + midrangeSize;
        int presenceRange = presenceSize + upperMidrangeSize;
        int brillianceRange = brillianceSize + presenceSize;

        float[] subBass = new float[subBassSize];
        float[] bass = new float[bassSize];
        float[] lowMidrange = new float[lowMidrangeSize];
        float[] midrange = new float[midrangeSize];
        float[] upperMidrange = new float[upperMidrangeSize];
        float[] presence = new float[presenceSize];
        float[] brilliance = new float[brillianceSize];

        subBass = BandLoop(spectrum, 1, subBassSize);
        bass = BandLoop(spectrum, subBassSize + 1, bassRange);
        lowMidrange = BandLoop(spectrum, bassRange + 1, lowMidrangeRange);
        midrange = BandLoop(spectrum, lowMidrangeRange + 1, midrangeRange);
        upperMidrange = BandLoop(spectrum, midrangeRange + 1, upperMidrangeRange);
        presence = BandLoop(spectrum, upperMidrangeRange + 1, presenceRange);
        brilliance = BandLoop(spectrum, presenceRange + 1, brillianceRange);

        bands.Add(subBass);
        bands.Add(bass);
        bands.Add(lowMidrange);
        bands.Add(midrange);
        bands.Add(upperMidrange);
        bands.Add(presence);
        bands.Add(brilliance);

        return bands;
    }

    public static List<float> GetBandAverages(float[] spectrum, float sampleRate, float sampleSize)
    {
        List<float[]> temp = GetDistinctBands(spectrum, sampleRate, sampleSize);
        List<float> averages = new List<float>();

        for (int i = 0; i < temp.Count; i += 1)
        {
            averages.Add(temp.ElementAt(i).Average());
        }

        return averages;
    }

    private static float GetHzPerInterval(float sampleRate, float sampleSize)
    {
        /*
         * Assuming a common song Nyquist frequency of ~22kHz
         * 22000 / 1024 = 21.5 Hz an interval
         */
        float songMaxFrequency = sampleRate / 2;
        return songMaxFrequency / sampleSize;
    }
}
