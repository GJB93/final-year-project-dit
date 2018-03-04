using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

public class AudioPreprocessor : MonoBehaviour {

    private AudioSource source;
    private AudioClip song;
    private float[] samples;
    private List<float[]> rightSpectrum;
    private List<float[]> leftSpectrum;
    private float[] leftSamples;
    private float[] rightSamples;
    private List<float[]> rightSampleWindows;
    private List<float[]> leftSampleWindows;
    private int stereoSampleSize;
    public int windowInterval = 2048;
    private int windowIterations;
    public float windowTimeSlice;
    private int lastWindowSize;
    private int songLength;
    private float[] instantEnergyHistory;

    public int[] ProcessSong (AudioClip song) {
        stereoSampleSize = song.samples * song.channels;
        windowIterations = Mathf.FloorToInt(song.samples / windowInterval);
        lastWindowSize = song.samples - (windowIterations * windowInterval);
        Debug.Log("Window Iterations: " + windowIterations + "\nLast Window Size: " + lastWindowSize);
        songLength = song.samples / song.frequency;
        windowTimeSlice = 1.0f / (song.frequency / windowInterval);
        samples = new float[stereoSampleSize];
        rightSamples = new float[song.samples];
        leftSamples = new float[song.samples];
        instantEnergyHistory = new float[windowInterval];
        rightSpectrum = new List<float[]>(windowIterations + 1);
        leftSpectrum = new List<float[]>(windowIterations + 1);
        Debug.Log("Clip sample size: " + song.samples
            + "\nSample size vs Window Interval: " + (windowIterations * windowInterval)
            + "\nSong length: " + (songLength / 60) + "m" + (songLength % 60) + "s"
            + "\nWindow Slice: " + windowTimeSlice + "s");
        song.GetData(samples, 0);
        for (int i = 0; i < rightSamples.Length; i += 1)
        {
            rightSamples[i] = samples[i * 2];
            leftSamples[i] = samples[(i * 2) + 1];
        }

        int[] beatTrack = new int[windowIterations + 1];

        for (int i = 0; i < rightSpectrum.Capacity; i += 1)
        {
            float[] tempRight = new float[windowInterval];
            float[] tempLeft = new float[windowInterval];
            if (i < windowIterations)
            {
                for (int j = 0; j < windowInterval; j += 1)
                {
                    tempRight[j] = rightSamples[(windowInterval * i) + j];
                    tempLeft[j] = leftSamples[(windowInterval * i) + j];
                }
            }
            else
            {
                for (int j = 0; j < lastWindowSize; j += 1)
                {
                    tempRight[j] = rightSamples[(windowInterval * i) + j];
                    tempLeft[j] = leftSamples[(windowInterval * i) + j];
                }
            }
            beatTrack[i] = CheckForBeat(FastFourierTransform.FftMag(tempRight), FastFourierTransform.FftMag(tempLeft)) ? 1 : 0;
            
        }

        return beatTrack;
	}

    bool CheckForBeat(float[] rightChannel, float[] leftChannel)
    {
        float instantEnergy = AudioAnalyser.GetInstantEnergy(rightChannel, leftChannel);
        float localAverageEnergy = AudioAnalyser.GetLocalAverageEnergy(instantEnergyHistory);
        float variance = AudioAnalyser.GetEnergyVariance(instantEnergyHistory, localAverageEnergy);
        float constant = AudioAnalyser.GetEnergyFormulaConstant(variance);
        float[] shiftArray = new float[instantEnergyHistory.Length];

        Array.Copy(instantEnergyHistory, 0, shiftArray, 1, instantEnergyHistory.Length - 1);
        shiftArray[0] = instantEnergy;
        Array.Copy(shiftArray, instantEnergyHistory, shiftArray.Length);
        float beatEnergyTarget = constant * localAverageEnergy;
        //Debug.Log("Instant Energy: " + instantEnergyHistory[0] + "\nAverage Energy: " + localAverageEnergy + "\nEnergy Constant: " + constant + "\nBeat target: " + beatEnergyTarget);
        if (instantEnergy > beatEnergyTarget && instantEnergy > Mathf.Epsilon)
        {
            return true;
        }
        return false;
    }

    public float WindowPositionToTime(int pos)
    {
        return pos * windowTimeSlice;
    }

    public int TimeToWindowAmount(float time)
    {
        return Mathf.CeilToInt(time / windowTimeSlice);
    }
}
