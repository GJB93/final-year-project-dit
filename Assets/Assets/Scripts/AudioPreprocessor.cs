using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioPreprocessor : MonoBehaviour {

    private AudioSource source;
    private AudioClip song;
    private float[] samples;
    private List<float[]> spectra;
    private float[] leftSamples;
    private float[] rightSamples;
    private List<float[]> rightSampleWindows;
    private List<float[]> leftSampleWindows;
    private int stereoSampleSize;
    private int windowInterval = 2048;
    private int windowIterations;
    private int windowTimeSlice;
    private int lastWindowSize;
    private int songLength;

	// Use this for initialization
	void Awake () {
        rightSampleWindows = new List<float[]>();
        leftSampleWindows = new List<float[]>();
        source = this.gameObject.GetComponent<AudioSource>();
        if (source == null)
        {
            this.gameObject.AddComponent<AudioSource>();
        }
        song = source.clip;
        stereoSampleSize = song.samples * song.channels;
        windowIterations = Mathf.FloorToInt(song.samples / windowInterval);
        lastWindowSize = song.samples - (windowIterations * windowInterval);
        Debug.Log("Window Iterations: " + windowIterations + "\nLast Window Size: " + lastWindowSize);
        songLength = song.samples / song.frequency;
        windowTimeSlice = song.frequency / windowInterval;
        samples = new float[stereoSampleSize];
        rightSamples = new float[song.samples];
        leftSamples = new float[song.samples];
        spectra = new List<float[]>(windowIterations + 1);
        Debug.Log("Clip sample size: " + samples.Length
            + "\nSample size vs Window Interval: " + (windowIterations * windowInterval)
            + "\nSong length: " + (songLength / 60) + "m" + (songLength % 60) + "s"
            + "\nWindow Slice: " + windowTimeSlice);
        song.GetData(samples, 0);
        for (int i = 0; i < rightSamples.Length; i += 1)
        {
            rightSamples[i] = samples[i * 2];
            leftSamples[i] = samples[(i * 2) + 1];
        }

        for (int i = 0; i < spectra.Capacity; i += 1)
        {
            float[] tempRight = new float[windowInterval];
            float[] tempLeft = new float[windowInterval];
            if (i <= windowIterations)
            {
                for (int j = 0; j < windowInterval; j += 1)
                {
                    tempRight[j] = rightSamples[(windowInterval * i) + j];
                    tempLeft[j] = rightSamples[(windowInterval * i) + j];
                }
            }
            else
            {
                for (int j = 0; j < lastWindowSize; j += 1)
                {
                    tempRight[j] = rightSamples[(windowInterval * i) + j];
                    tempLeft[j] = rightSamples[(windowInterval * i) + j];
                }
            }
            spectra.Add(FastFourierTransform.FftMag(tempRight));
        }
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
