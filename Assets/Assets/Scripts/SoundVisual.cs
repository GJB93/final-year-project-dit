using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

/** Used Tutorial References: 
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
    public List<GameObject> beatCubes;
    
    public float visualModifier = 1000.0f;
    public float risingVisualiserSmoothSpeed = 0.5f;
    public float fallingVisualiserSmoothSpeed = 0.5f;
    public float backgroundSmoothSpeed = 0.2f;
    public float keepPercentage = 1.0f;
    public float listenerVolumeValue = 1.0f;
    public float sourceVolumeValue = 1.0f;
    public bool cameraMovement = true;
    private int windowNumber = 0;

    private AudioSource source;
    private float[] samples;
    private List<float> bands;
    private float[] spectrum;
    private int[] songBeats;
    private float[] eqBandPreviousY;
    private float sampleRate;
    private float songMaxFrequency;
    private float hzPerInterval;
    private Camera mainCamera;
    private List<Transform> leftVisualList;
    private List<Transform> rightVisualList;
    private List<Transform> eqBandVisuals;
    private float previousScaleY = 0;
    private float[] instantEnergyHistory;
    private int beatCount = 0;
    private AudioPreprocessor preprocessor;
    private bool canSpawn = true;
    private int previouslyActivatedWindow;

    private void Start()
    {
        beatCubes = new List<GameObject>();
        source = GetComponent<AudioSource>();
        preprocessor = GetComponent<AudioPreprocessor>();
        mainCamera = Camera.main;
        samples = new float[SAMPLE_SIZE];
        spectrum = new float[SAMPLE_SIZE];
        eqBandPreviousY = new float[BAND_SIZE];
        bands = new List<float>();
        leftVisualList = new List<Transform>();
        rightVisualList = new List<Transform>();
        eqBandVisuals = new List<Transform>();
        sampleRate = AudioSettings.outputSampleRate;
        songMaxFrequency = sampleRate / 2;
        /*
         * Assuming a common song frequency of ~22kHz
         * 22000 / 1024 = 21.5 Hz a interval
         */
        hzPerInterval = songMaxFrequency / SAMPLE_SIZE;
        instantEnergyHistory = new float[SAMPLE_SIZE];
        Debug.Log(sampleRate);
        StartCoroutine(GetBpm());
        songBeats = preprocessor.ProcessSong(source.clip);
        source.Play();
        StartCoroutine(GetCurrentWindowPosition(preprocessor.windowTimeSlice));
        //SpawnEQLine();
    }

    private void OnGUI()
    {
        listenerVolumeValue = GUI.HorizontalSlider(new Rect(10, 10, 100, 10), listenerVolumeValue, 0.0f, 1.0f);
        AudioListener.volume = listenerVolumeValue;
        sourceVolumeValue = GUI.HorizontalSlider(new Rect(10, 40, 100, 10), sourceVolumeValue, 0.0f, 1.0f);
        source.volume = sourceVolumeValue;
    }

    /*
    private void SpawnCube(float visualScale)
    {
        GameObject rightGo = GameObject.CreatePrimitive(PrimitiveType.Cube) as GameObject;
        GameObject leftGo = GameObject.CreatePrimitive(PrimitiveType.Cube) as GameObject;
        rightGo.transform.position = new Vector3(5, 0, 10);
        leftGo.transform.position = new Vector3(-5, 0, 10);
        rightGo.transform.localScale = Vector3.one + Vector3.up * visualScale;
        leftGo.transform.localScale = Vector3.one + Vector3.up * visualScale;
        rightVisualList.Add(rightGo.transform);
        leftVisualList.Add(leftGo.transform);
    }

    private void SpawnEQLine()
    {
        for(int i=0; i<BAND_SIZE; i+=1)
        {
            GameObject eqGo = GameObject.CreatePrimitive(PrimitiveType.Cube) as GameObject;
            eqGo.transform.position = new Vector3(i-3, 0, 10);
            eqBandVisuals.Add(eqGo.transform);
        }
    }

    private void UpdateEQLineVisuals()
    {
        for(int i=0; i<eqBandVisuals.Count; i+=1)
        {
            float risingInterpolater = 0;
            float fallingInterpolator = 0;
            float interpolation = 0;
            float scaleY = Mathf.Pow(bands.ElementAt(i), 2) * visualModifier;
            risingInterpolater += Time.deltaTime * risingVisualiserSmoothSpeed;
            fallingInterpolator += Time.deltaTime * fallingVisualiserSmoothSpeed;
            interpolation = scaleY > eqBandPreviousY[i] ? Mathf.Lerp(eqBandPreviousY[i], scaleY, risingInterpolater) : Mathf.Lerp(eqBandPreviousY[i], eqBandPreviousY[i] / 2, fallingInterpolator);
            eqBandVisuals.ElementAt(i).transform.localScale = Vector3.one + Vector3.up * interpolation;
            eqBandPreviousY[i] = interpolation;
        }
    }

    */
    private void Update()
    {
        if(songBeats[windowNumber + 10] == 1 && (windowNumber + 10) != previouslyActivatedWindow)
        {
            Debug.Log(windowNumber);
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube) as GameObject;
            temp.transform.position = new Vector3(0, 0, 10);
            beatCubes.Add(temp);
            canSpawn = false;
            previouslyActivatedWindow = windowNumber + 10;
            StartCoroutine(WaitForNextSpawn());
        }
        foreach(GameObject beatCube in beatCubes)
        {
            beatCube.transform.Translate(Vector3.back);
        }
        //CheckForBeat();
        //AnalyseSound();
        /*
        UpdateEQLineVisuals();
        UpdateVisual();
        MoveCubes();
        if (Input.GetKeyDown(KeyCode.A) && (mainCamera.transform.position.x > -3))
        {
            mainCamera.transform.position += Vector3.left;
        }
        if (Input.GetKeyDown(KeyCode.D) && (mainCamera.transform.position.x < 3))
        {
            mainCamera.transform.position += Vector3.right;
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            cameraMovement = !cameraMovement;
        }
        if (!cameraMovement)
        {
            mainCamera.transform.position = new Vector3(mainCamera.transform.position.x, 1, mainCamera.transform.position.z);
        }
        */
    }

    /*
    private void UpdateVisual()
    {
        float risingInterpolater = 0;
        float fallingInterpolator = 0;
        float interpolation = 0;
        float scaleY = Mathf.Pow(bands.ElementAt(1), 2) * visualModifier;
        risingInterpolater += Time.deltaTime * risingVisualiserSmoothSpeed;
        fallingInterpolator += Time.deltaTime * fallingVisualiserSmoothSpeed;
        interpolation = scaleY > previousScaleY ? Mathf.Lerp(previousScaleY, scaleY, risingInterpolater) : Mathf.Lerp(previousScaleY, previousScaleY/2, fallingInterpolator);
        SpawnCube(interpolation);
        if (cameraMovement)
        {
            mainCamera.transform.position = interpolation > 1 ? 
                new Vector3(mainCamera.transform.position.x, 1 * interpolation, mainCamera.transform.position.z) : 
                new Vector3(mainCamera.transform.position.x, 1, mainCamera.transform.position.z);
        }
        previousScaleY = interpolation;
    }

    private void MoveCubes()
    {
        foreach(Transform cube in rightVisualList)
        {
            cube.position += new Vector3(0, 0, -1);
        }

        foreach (Transform cube in leftVisualList)
        {
            cube.position += new Vector3(0, 0, -1);
        }
    }
    */
    private void AnalyseSound()
    {
        source.GetOutputData(samples, 0);
        source.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        bands = GetBandAverages(spectrum);
        rmsValue = GetRmsValue(samples);
        dbValue = GetDBValue(rmsValue, 0.1f);
        pitchValue = GetPitchValue(spectrum);
    }

    /**
     * The following three functions, (GetRmsValue, GetDBValue and GetPitchValue) 
     * are written with reference to code snippets found at this link: 
     * https://answers.unity.com/questions/157940/getoutputdata-and-getspectrumdata-they-represent-t.html
    **/
    private float GetRmsValue(float[] samples)
    {
        float sum = 0;
        for (int i = 0; i < SAMPLE_SIZE; i += 1)
        {
            sum += samples[i] * samples[i];
        }
        return Mathf.Sqrt(sum / SAMPLE_SIZE);
    }

    private float GetDBValue(float measuredVoltage, float referenceVoltage)
    {
        return 20 * Mathf.Log10(rmsValue / 0.1f);
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
    /**
     *  End of code snippet https://answers.unity.com/questions/157940/getoutputdata-and-getspectrumdata-they-represent-t.html
    **/

    private float GetVoltageRatio(float decibelValue)
    {
        Debug.Log("VoltageRatio is " + (Mathf.Pow(10, (decibelValue / 20))));
        return Mathf.Pow(10, (decibelValue / 20));
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

    void CheckForBeat()
    {
        GameObject beatCube = GameObject.FindGameObjectWithTag("Beat Cube");
        Renderer cubeRend = beatCube.GetComponent<Renderer>();
        float[] rightChannel = new float[SAMPLE_SIZE];
        float[] leftChannel = new float[SAMPLE_SIZE];
        source.GetOutputData(rightChannel, 0);
        source.GetOutputData(leftChannel, 1);

        rightChannel = FastFourierTransform.FftMag(rightChannel);
        leftChannel = FastFourierTransform.FftMag(leftChannel);

        float instantEnergy = AudioAnalyser.GetInstantEnergy(rightChannel, leftChannel);
        float localAverageEnergy = AudioAnalyser.GetLocalAverageEnergy(instantEnergyHistory);
        float variance = AudioAnalyser.GetEnergyVariance(instantEnergyHistory, localAverageEnergy);
        float constant = AudioAnalyser.GetEnergyFormulaConstant(variance);
        float[] shiftArray = new float[instantEnergyHistory.Length];

        Array.Copy(instantEnergyHistory, 0, shiftArray, 1, instantEnergyHistory.Length - 1);
        shiftArray[0] = instantEnergy;
        Array.Copy(shiftArray, instantEnergyHistory, shiftArray.Length);
        float beatEnergyTarget = constant * localAverageEnergy;
        Debug.Log("Instant Energy: " + instantEnergyHistory[0] + "\nAverage Energy: " + localAverageEnergy + "\nEnergy Constant: " + constant +  "\nBeat target: " + beatEnergyTarget);
        if (instantEnergy > beatEnergyTarget)
        {
            Debug.Log("Beat Detected");
            cubeRend.material.color = Color.green;
            beatCount += 1;
        }
        else
        {
            cubeRend.material.color = Color.red;
        }
    }

    IEnumerator GetBpm()
    {
        yield return new WaitForSeconds(15);
        Debug.Log("Beat Count: " + beatCount + "\nBPM: " + beatCount * 4);
        beatCount = 0;
    }

    IEnumerator GetCurrentWindowPosition(float time)
    {
        Debug.Log("Incrementing window position");
        while (true)
        {
            yield return new WaitForSeconds(0.0476f);
            windowNumber += 1;
        }
    }

    IEnumerator WaitForNextSpawn()
    {
        yield return new WaitForSeconds(1);
        canSpawn = true;
    }
}
