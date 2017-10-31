using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundVisual : MonoBehaviour
{
    private const int SAMPLE_SIZE = 1024;

    public float rmsValue;
    public float dbValue;
    public float pitchValue;

    public float backgroundIntensity;
    public Material backgroundMaterial;
    public Color minColor;
    public Color maxColor;
    
    public float visualModifier = 1000.0f;
    public float visualiserSmoothSpeed = 10.0f;
    public float backgroundSmoothSpeed = 0.2f;
    public float keepPercentage = 1.0f;
    public int amtVisual = 64;
    public float listenerVolumeValue = 0.5f;
    public float sourceVolumeValue = 0.5f;

    private AudioSource source;
    private float[] samples;
    private float[] spectrum;
    private float sampleRate;
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
        sampleRate = AudioSettings.outputSampleRate;
        InvokeRepeating("MoveCubes", 0.0f, 0.01f);

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
            rightGo.transform.position = new Vector3(5, 0, i - 10);
            leftGo.transform.position = new Vector3(-5, 0, i - 10);
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
        int spectrumIndex = 0;
        int averageSize = (int) ((SAMPLE_SIZE * keepPercentage) / amtVisual);

        while (visualIndex < amtVisual)
        {
            int j = 0;
            float sum = 0;
            while (j < averageSize)
            {
                sum += spectrum[spectrumIndex];
                spectrumIndex++;
                j++;
            }

            float scaleY = sum / averageSize * visualModifier;
            visualScale[visualIndex] -= Time.deltaTime * visualiserSmoothSpeed;
            if (visualScale[visualIndex] < scaleY)
                visualScale[visualIndex] = scaleY;
            
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

        rmsValue = GetRmsValue(samples);
        dbValue = GetDBValue(rmsValue, 0.1f);

        // Get sound spectrum
        source.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        // Find pitch
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
        pitchValue = freqN * (sampleRate / 2) / SAMPLE_SIZE;
    }

    private float GetRmsValue(float[] samples)
    {
        float sum = 0;
        for (int i = 0; i < SAMPLE_SIZE; i += 1)
        {
            sum += samples[i] * samples[i];
        }
        Debug.Log("RMSValue is " + Mathf.Sqrt(sum / SAMPLE_SIZE));
        return Mathf.Sqrt(sum / SAMPLE_SIZE);
    }

    private float GetDBValue(float measuredVoltage, float referenceVoltage)
    {
        Debug.Log("dbValue is " + (20 * Mathf.Log10(rmsValue / 0.1f)));
        return 20 * Mathf.Log10(rmsValue / 0.1f);
    }

    private float GetVoltageRatio(float decibelValue)
    {
        return Mathf.Pow(10, (decibelValue/20));
    }
}
