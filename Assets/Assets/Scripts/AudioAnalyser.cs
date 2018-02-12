using System.Collections;
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

    public static float GetDBValue(float[] samples, float measuredVoltage, float referenceVoltage)
    {
        return 20 * Mathf.Log10(GetRmsValue(samples) / 0.1f);
    }

    public static float GetPitchValue(float[] spectrum, float sampleRate)
    {
        float maxV = 0;
        var maxN = 0;
        float hzPerInterval = GetHzPerInterval(sampleRate);
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
            instantEnergy = (rightChannel[i] * rightChannel[i]) + (leftChannel[i] * leftChannel[i]);
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

    public static float GetEnergyFormulaConstant(float variance)
    {
        return (-0.0025714f * variance) + 100.0f;
    }

    public static float GetVoltageRatio(float decibelValue)
    {
        Debug.Log("VoltageRatio is " + (Mathf.Pow(10, (decibelValue / 20))));
        return Mathf.Pow(10, (decibelValue / 20));
    }

    public static List<float> GetBandAverages(float[] spectrum, float sampleRate)
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

        List<float> averages = new List<float>();

        List<float> subBass = new List<float>();
        List<float> bass = new List<float>();
        List<float> lowMidrange = new List<float>();
        List<float> midrange = new List<float>();
        List<float> upperMidrange = new List<float>();
        List<float> presence = new List<float>();
        List<float> brilliance = new List<float>();

        float hzPerInterval = GetHzPerInterval(sampleRate);

        int subBassRange = (int)(40 / hzPerInterval);
        int bassRange = (int)(190 / hzPerInterval) + subBassRange;
        int lowMidrangeRange = (int)(250 / hzPerInterval) + bassRange;
        int midrangeRange = (int)(1500 / hzPerInterval) + lowMidrangeRange;
        int upperMidrangeRange = (int)(2000 / hzPerInterval) + midrangeRange;
        int presenceRange = (int)(2000 / hzPerInterval) + upperMidrangeRange;
        int brillianceRange = (int)(14000 / hzPerInterval) + presenceRange;

        for (int interval = 1; interval <= brillianceRange; interval += 1)
        {
            if (interval <= subBassRange)
            {
                subBass.Add(spectrum[interval]);
            }
            else if (interval > subBassRange && interval <= bassRange)
            {
                bass.Add(spectrum[interval]);
            }
            else if (interval > bassRange && interval <= lowMidrangeRange)
            {
                lowMidrange.Add(spectrum[interval]);
            }
            else if (interval > lowMidrangeRange && interval <= midrangeRange)
            {
                midrange.Add(spectrum[interval]);
            }
            else if (interval > midrangeRange && interval <= upperMidrangeRange)
            {
                upperMidrange.Add(spectrum[interval]);
            }
            else if (interval > upperMidrangeRange && interval <= presenceRange)
            {
                presence.Add(spectrum[interval]);
            }
            else
            {
                brilliance.Add(spectrum[interval]);
            }
        }

        averages.Add(subBass.Average());
        averages.Add(bass.Average());
        averages.Add(lowMidrange.Average());
        averages.Add(midrange.Average());
        averages.Add(upperMidrange.Average());
        averages.Add(presence.Average());
        averages.Add(brilliance.Average());

        return averages;
    }

    private static float GetHzPerInterval(float sampleRate)
    {
        float songMaxFrequency = sampleRate / 2;
        return songMaxFrequency / SAMPLE_SIZE;
    }
}
