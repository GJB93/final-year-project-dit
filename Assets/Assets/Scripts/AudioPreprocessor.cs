using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using UnityEngine.VR;

public class AudioPreprocessor : MonoBehaviour {

    private AudioSource source;
    private AudioClip song;
    public int windowInterval = 4096;
    private int windowIterations;
    public float windowTimeSlice;
    private int lastWindowSize;
    private float[] instantEnergyHistory;
    private List<float[]> energyHistories = new List<float[]>();
    private float overallAverageEnergy;
    public int bandToCheck = 1;
    public float sensitivity = 100.5142857f;

    public int[] ProcessSong (AudioClip song) {
        windowIterations = Mathf.FloorToInt(song.samples / windowInterval);
        lastWindowSize = song.samples - (windowIterations * windowInterval);
        Debug.Log("Window Iterations: " + windowIterations + "\nLast Window Size: " + lastWindowSize);
        int songLength = song.samples / song.frequency;
        windowTimeSlice = 1.0f / (song.frequency / windowInterval);
        int historyBufferLength = Mathf.FloorToInt(song.frequency / windowInterval);
        instantEnergyHistory = new float[historyBufferLength];

        return song.channels > 1 ? ProcessStereo(song) : ProcessMono(song);
	}

    public int[] ProcessMono(AudioClip song)
    {
        Debug.Log("Processing Mono Song");
        List<float[]> spectrum = new List<float[]>(windowIterations + 1);
        float[] samples = new float[song.samples];
        song.GetData(samples, 0);

        int[] beatTrack = new int[windowIterations + 1];

        for (int i = 0; i < spectrum.Capacity; i += 1)
        {
            float[] temp = new float[windowInterval];
            if (i < windowIterations)
            {
                for (int j = 0; j < windowInterval; j += 1)
                {
                    temp[j] = samples[(windowInterval * i) + j];
                }
            }
            else
            {
                for (int j = 0; j < lastWindowSize; j += 1)
                {
                    temp[j] = samples[(windowInterval * i) + j];
                }
            }
            List<float[]> bands = AudioAnalyser.GetDistinctBands(FastFourierTransform.FftMag(temp), song.frequency, windowInterval / 2.0f);
            beatTrack[i] = CheckForBeat(bands.ElementAt(bandToCheck), new float[temp.Length]) ? 1 : 0;

        }

        return beatTrack;
    }

    public int[] ProcessStereo(AudioClip song)
    {
        Debug.Log("Processing Stereo Song");
        int stereoSampleSize = song.samples * song.channels;
        float[] rightSamples = new float[song.samples];
        float[] leftSamples = new float[song.samples];
        List<float[]> rightSpectrum = new List<float[]>(windowIterations + 1);
        List<float[]> leftSpectrum = new List<float[]>(windowIterations + 1);
        float[] samples = new float[stereoSampleSize];
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
            List<float[]> bandsRight = AudioAnalyser.GetDistinctBands(FastFourierTransform.FftMag(tempRight), song.frequency, windowInterval / 2.0f);
            List<float[]> bandsLeft = AudioAnalyser.GetDistinctBands(FastFourierTransform.FftMag(tempLeft), song.frequency, windowInterval / 2.0f);
            beatTrack[i] = CheckForBeat(bandsRight.ElementAt(bandToCheck), bandsLeft.ElementAt(bandToCheck)) ? 1 : 0;
        }

        return beatTrack;
    }

    bool CheckForBeat(float[] rightChannel, float[] leftChannel)
    {
        float instantEnergy = AudioAnalyser.GetInstantEnergy(rightChannel, leftChannel);
        float localAverageEnergy = AudioAnalyser.GetLocalAverageEnergy(instantEnergyHistory);
        float variance = AudioAnalyser.GetEnergyVariance(instantEnergyHistory, localAverageEnergy);
        float constant = AudioAnalyser.GetEnergyFormulaConstant(variance, sensitivity);
        float[] shiftArray = new float[instantEnergyHistory.Length];

        Array.Copy(instantEnergyHistory, 0, shiftArray, 1, instantEnergyHistory.Length - 1);
        shiftArray[0] = instantEnergy;
        Array.Copy(shiftArray, instantEnergyHistory, shiftArray.Length);
        float beatEnergyTarget = constant * localAverageEnergy;
        Debug.Log("Instant Energy: " + instantEnergyHistory[0] + "\nAverage Energy: " + localAverageEnergy + "\nEnergy Constant: " + constant + "\nBeat target: " + beatEnergyTarget);
        if (instantEnergy > beatEnergyTarget && instantEnergy > Mathf.Epsilon)
        {
            return true;
        }
        return false;
    }

    bool CheckForBeatBands(List<float[]> rightChannel, List<float[]> leftChannel)
    {
        float[] instantEnergies = new float[rightChannel.Capacity];
        float[] averageEnergies = new float[rightChannel.Capacity];
        for(int i = 0; i < rightChannel.Capacity; i += 1)
        {
            instantEnergies[i] = AudioAnalyser.GetInstantEnergy(rightChannel.ElementAt(i), leftChannel.ElementAt(i));
            averageEnergies[i] = AudioAnalyser.GetLocalAverageEnergy(energyHistories.ElementAt(i));
        }
        float localAverageEnergy = AudioAnalyser.GetLocalAverageEnergy(instantEnergyHistory);
        float variance = AudioAnalyser.GetEnergyVariance(instantEnergyHistory, localAverageEnergy);
        float constant = AudioAnalyser.GetEnergyFormulaConstant(variance, sensitivity);
        float[] shiftArray = new float[instantEnergyHistory.Length];

        Array.Copy(instantEnergyHistory, 0, shiftArray, 1, instantEnergyHistory.Length - 1);
        Array.Copy(shiftArray, instantEnergyHistory, shiftArray.Length);
        float beatEnergyTarget = constant * localAverageEnergy;
        Debug.Log("Instant Energy: " + instantEnergyHistory[0] + "\nAverage Energy: " + localAverageEnergy + "\nEnergy Constant: " + constant + "\nBeat target: " + beatEnergyTarget);
        return false;
    }

    public float WindowPositionToTime(int pos)
    {
        return pos * windowTimeSlice;
    }

    public int TimeToWindowAmount(float time)
    {
        return Mathf.RoundToInt(time / windowTimeSlice);
    }

    public void Start()
    {
        
    }
}
