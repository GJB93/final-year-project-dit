using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/** References: 
 * https://youtu.be/wtXirrO-iNA
 * https://youtu.be/4Av788P9stk
 * 
**/

[RequireComponent (typeof (AudioSource))]
public class SoundVisual : MonoBehaviour
{
    private const int SAMPLE_SIZE = 1024;
    private const int BAND_SIZE = 7;

    public float rmsValue;
    public float dbValue;
    public float pitchValue;

    public float backgroundIntensity;
    public Material backgroundMaterial;
    public Color minColor;
    public Color maxColor;
    
    public float visualModifier = 100.0f;
    public float risingVisualiserSmoothSpeed = 0.5f;
    public float fallingVisualiserSmoothSpeed = 0.2f;
    public float backgroundSmoothSpeed = 0.2f;
    public float keepPercentage = 0.2f;
    public int amtVisual = 7;
    public float listenerVolumeValue = 0.5f;
    public float sourceVolumeValue = 0.5f;

    private AudioSource source;
    private float[] samples;
    private List<float> bands;
    private float[] spectrum;
    private float sampleRate;
    private float songMaxFrequency;
    private float hzPerInterval;
    private Camera mainCamera;
    private Queue<Transform> leftVisualList;
    private Queue<Transform> rightVisualList;
    private float[] visualScale;

    private void Start()
    {
        source = GetComponent<AudioSource>();
        mainCamera = Camera.main;
        samples = new float[SAMPLE_SIZE];
        spectrum = new float[SAMPLE_SIZE];
        bands = new List<float>();
        sampleRate = AudioSettings.outputSampleRate;
        songMaxFrequency = sampleRate / 2;
        /*
         * Assuming a common song frequency of ~22kHz
         * 22000 / 1024 = 21.5 Hz a interval
         */
        hzPerInterval = songMaxFrequency / SAMPLE_SIZE;
        Debug.Log(sampleRate);
        // InvokeRepeating("MoveCubes", 0.0f, 0.01f);

        SpawnLine();
    }

    private void OnGUI()
    {
        listenerVolumeValue = GUI.HorizontalSlider(new Rect(10, 10, 100, 10), listenerVolumeValue, 0.0f, 1.0f);
        AudioListener.volume = listenerVolumeValue;
        sourceVolumeValue = GUI.HorizontalSlider(new Rect(10, 40, 100, 10), sourceVolumeValue, 0.0f, 1.0f);
        source.volume = sourceVolumeValue;
    }

    private void SpawnLine()
    {
        visualScale = new float[amtVisual];
        leftVisualList = new Queue<Transform>();
        rightVisualList = new Queue<Transform>();

        for (int i = amtVisual - 1; i > -1; i -= 1)
        {
            GameObject rightGo = GameObject.CreatePrimitive(PrimitiveType.Cube) as GameObject;
            GameObject leftGo = GameObject.CreatePrimitive(PrimitiveType.Cube) as GameObject;
            rightGo.transform.position = new Vector3(5, 0, i);
            leftGo.transform.position = new Vector3(-5, 0, i);
            rightVisualList.Enqueue(rightGo.transform);
            leftVisualList.Enqueue(leftGo.transform);
        }
    }

    private void Update()
    {
        AnalyseSound();
        UpdateVisual();
        if (Input.GetKeyDown(KeyCode.A) && (mainCamera.transform.position.x > -3))
        {
            mainCamera.transform.position = mainCamera.transform.position + Vector3.left;
        }
        if (Input.GetKeyDown(KeyCode.D) && (mainCamera.transform.position.x < 3))
        {
            mainCamera.transform.position = mainCamera.transform.position + Vector3.right;
        }
    }

    private void UpdateVisual()
    {
        int visualIndex = 0;
        float risingInterpolater = 0;
        float fallingInterpolator = 0;
        float interpolation = 0;
        foreach(float bandAverage in bands)
        {
            float previousScaleY = visualScale[visualIndex];
            float scaleY = Mathf.Pow(bandAverage, 2) * visualModifier;
            risingInterpolater += Time.deltaTime * risingVisualiserSmoothSpeed;
            fallingInterpolator += Time.deltaTime * fallingVisualiserSmoothSpeed;
            interpolation = scaleY > previousScaleY ? Mathf.Lerp(previousScaleY, scaleY, risingInterpolater) : Mathf.Lerp(previousScaleY, previousScaleY/2, fallingInterpolator);
            visualScale[visualIndex] = interpolation;
            Transform leftTransform = leftVisualList.Dequeue();
            Transform rightTransform = rightVisualList.Dequeue();
            leftTransform.localScale = Vector3.one + Vector3.up * visualScale[visualIndex];
            rightTransform.localScale = Vector3.one + Vector3.up * visualScale[visualIndex];
            leftVisualList.Enqueue(leftTransform);
            rightVisualList.Enqueue(rightTransform);
            visualIndex++;
        }
    }

    private void MoveCubes()
    {
        Transform currentLeftTransform = leftVisualList.Dequeue();
        Transform currentRightTransform = rightVisualList.Dequeue();
        leftVisualList.Enqueue(currentLeftTransform);
        rightVisualList.Enqueue(currentRightTransform);
    }

    private void AnalyseSound()
    {
        source.GetOutputData(samples, 0);
        source.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        bands = GetBandAverages(samples);
        rmsValue = GetRmsValue(samples);
        dbValue = GetDBValue(rmsValue, 0.1f);
        pitchValue = GetPitchValue(spectrum);
    }

    private float GetRmsValue(float[] samples)
    {
        float sum = 0;
        for (int i = 0; i < SAMPLE_SIZE; i += 1)
        {
            sum += samples[i] * samples[i];
        }
        // Debug.Log("RMSValue is " + Mathf.Sqrt(sum / SAMPLE_SIZE));
        return Mathf.Sqrt(sum / SAMPLE_SIZE);
    }

    private float GetDBValue(float measuredVoltage, float referenceVoltage)
    {
        // Debug.Log("dbValue is " + (20 * Mathf.Log10(rmsValue / 0.1f)));
        return 20 * Mathf.Log10(rmsValue / 0.1f);
    }

    private float GetVoltageRatio(float decibelValue)
    {
        Debug.Log("VoltageRatio is " + (Mathf.Pow(10, (decibelValue / 20))));
        return Mathf.Pow(10, (decibelValue/20));
    }

    private float GetPitchValue(float[] spectrum)
    {
        float maxV = 0;
        var maxN = 0;
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

    private List<float> GetBandAverages(float[] spectrum)
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

        List<float> subBass       = new List<float>();
        List<float> bass          = new List<float>();
        List<float> lowMidrange   = new List<float>();
        List<float> midrange      = new List<float>();
        List<float> upperMidrange = new List<float>();
        List<float> presence      = new List<float>();
        List<float> brilliance    = new List<float>();

        int subBassRange          = (int)(40 / hzPerInterval);
        int bassRange             = (int)(190 / hzPerInterval) + subBassRange;
        int lowMidrangeRange      = (int)(250 / hzPerInterval) + bassRange;
        int midrangeRange         = (int)(1500 / hzPerInterval) + lowMidrangeRange;
        int upperMidrangeRange    = (int)(2000 / hzPerInterval) + midrangeRange;
        int presenceRange         = (int)(2000 / hzPerInterval) + upperMidrangeRange;
        int brillianceRange       = (int)(14000 / hzPerInterval) + presenceRange;

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
}
