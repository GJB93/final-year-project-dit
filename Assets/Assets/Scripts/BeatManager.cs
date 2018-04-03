using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.Threading;


[RequireComponent (typeof(AudioSource), typeof(AudioPreprocessor))]
public class BeatManager: MonoBehaviour {

    private enum Direction
    {
        Left,
        Right
    };

    public List<GameObject> beatCubes;
    public GameObject beatCube;
    public GameObject score;
    public GameObject hitMissText;
    public GameObject spawnLocation;
    public GameObject beatLocation;
    public float timeToReachPlayer = 5.0f;
    public float timeBetweenBeats = 0.25f;
    public float unitsPerSecond;
    public float listenerVolumeValue = 1.0f;
    public float sourceVolumeValue = 1.0f;

    private Beat[] songBeats;
    private int windowNumber = 0;
    private AudioPreprocessor preprocessor;
    private bool canSpawn = true;
    private int previouslyActivatedWindow;
    private float lastX;
    private Stopwatch stopwatch;
    private AudioSource source;
    private AudioClip clip;
    private float dist;
    private float speed;
    private Direction direction;
    private float rightmostX = 4.0f;
    private float leftmostX = -4.0f;
    private float targetTimeDiff = 0.75f;
    private Thread preprocessingThread;
    private float[] samples;
    private int songFrequency;
    private int songSampleNum;
    private int songChannels;
    private int currentScore = 0;
    private int multiplier = 1;
    private int hitCount = 0;
    private int hitStreak = 0;
    private bool syncCheck = false;

    private void OnGUI()
    {
        listenerVolumeValue = GUI.HorizontalSlider(new Rect(10, 10, 100, 10), listenerVolumeValue, 0.0f, 1.0f);
        AudioListener.volume = listenerVolumeValue;
        sourceVolumeValue = GUI.HorizontalSlider(new Rect(10, 40, 100, 10), sourceVolumeValue, 0.0f, 1.0f);
        source.volume = sourceVolumeValue;
    }

    // Use this for initialization
    void Awake () {
        stopwatch = new Stopwatch();
        beatCubes = new List<GameObject>();
        source = GetComponent<AudioSource>();
        clip = source.clip;
        songFrequency = clip.frequency;
        songSampleNum = clip.samples;
        songChannels = clip.channels;
        if (songChannels > 1)
        {
            samples = new float[songSampleNum * 2];
        }
        else
        {
            samples = new float[songSampleNum];
        }
        
        clip.GetData(samples, 0);
        preprocessor = GetComponent<AudioPreprocessor>();
        preprocessingThread = new Thread(LoadSong);
        preprocessingThread.Start();
        //LoadSong();
    }

    // Update is called once per frame
    void Update () {
        if(!preprocessingThread.IsAlive)
        {
            dist = Vector3.Distance(beatLocation.transform.position, spawnLocation.transform.position);
            speed = dist / timeToReachPlayer;
            //UnityEngine.Debug.Log(stopwatch.Elapsed.Seconds);
            if (source.isPlaying && songBeats != null)
            {
                stopwatch.Stop();
                if (source.time + timeToReachPlayer < source.clip.length)
                {
                    windowNumber = preprocessor.TimeToWindowAmount(source.time + timeToReachPlayer);
                    if (songBeats[windowNumber] != null && windowNumber > previouslyActivatedWindow && canSpawn)
                    {
                        float currentBeat = songBeats[windowNumber].energyValue;
                        bool bestBeat = true;
                        for (int i = windowNumber; i <= preprocessor.TimeToWindowAmount(timeBetweenBeats) + windowNumber; i += 1)
                        {
                            if (songBeats[i] != null)
                            {
                                if (songBeats[i].energyValue > currentBeat)
                                {
                                    UnityEngine.Debug.Log("Not best beat");
                                    bestBeat = false;
                                }
                            }
                        }
                        if (bestBeat)
                        {
                            GameObject temp = Instantiate(beatCube);
                            float x = 0;
                            float timeDiff = preprocessor.WindowPositionToTime(windowNumber) - preprocessor.WindowPositionToTime(previouslyActivatedWindow);
                            if (timeDiff > targetTimeDiff)
                            {
                                if (lastX > leftmostX && lastX < rightmostX)
                                {
                                    if (direction == Direction.Left)
                                        x = lastX - 1.0f;
                                    else
                                        x = lastX + 1.0f;
                                }
                                else
                                {
                                    if (lastX <= leftmostX)
                                    {
                                        x = lastX + 1.0f;
                                        direction = Direction.Right;
                                    }
                                    else
                                    {
                                        x = lastX - 1.0f;
                                        direction = Direction.Left;
                                    }
                                }
                            }
                            else
                            {
                                x = lastX;
                            }
                            temp.transform.position = spawnLocation.transform.position + new Vector3(x, temp.transform.localScale.y * 0.5f, temp.transform.localScale.z * 0.5f);
                            lastX = x;
                            temp.GetComponent<Renderer>().material.color = Random.ColorHSV(0, 1, 0, 0.25f, 1, 1);
                            canSpawn = false;
                            previouslyActivatedWindow = windowNumber;
                            StartCoroutine(WaitForNextSpawn());
                        }

                    }
                }
            }
            else
            {
                if (stopwatch.IsRunning && songBeats != null)
                {
                    windowNumber = preprocessor.TimeToWindowAmount((float)stopwatch.Elapsed.TotalSeconds);
                    if (songBeats[windowNumber] != null && windowNumber > previouslyActivatedWindow && canSpawn)
                    {
                        float currentBeat = songBeats[windowNumber].energyValue;
                        bool bestBeat = true;
                        for (int j = windowNumber; j <= preprocessor.TimeToWindowAmount(timeBetweenBeats) + windowNumber; j += 1)
                        {
                            if (songBeats[j] != null)
                            {
                                if (songBeats[j].energyValue > currentBeat)
                                {
                                    UnityEngine.Debug.Log("Not best beat");
                                    bestBeat = false;
                                }
                            }
                        }
                        if (bestBeat)
                        {
                            GameObject temp = Instantiate(beatCube);
                            float x = 0;
                            float timeDiff = preprocessor.WindowPositionToTime(windowNumber) - preprocessor.WindowPositionToTime(previouslyActivatedWindow);
                            if (timeDiff > targetTimeDiff)
                            {
                                if (lastX > leftmostX && lastX < rightmostX)
                                {
                                    if (direction == Direction.Left)
                                        x = lastX - 1.0f;
                                    else
                                        x = lastX + 1.0f;
                                }
                                else
                                {
                                    if (lastX <= leftmostX)
                                    {
                                        x = lastX + 1.0f;
                                        direction = Direction.Right;
                                    }
                                    else
                                    {
                                        x = lastX - 1.0f;
                                        direction = Direction.Left;
                                    }
                                }
                            }
                            else
                            {
                                x = lastX;
                            }
                            temp.transform.position = spawnLocation.transform.position + new Vector3(x, temp.transform.localScale.y * 0.5f, preprocessor.WindowPositionToTime(windowNumber) + (temp.transform.localScale.z * 0.5f));
                            lastX = x;
                            temp.GetComponent<Renderer>().material.color = Random.ColorHSV(0, 1, 0, 0.25f, 1, 1);
                            canSpawn = false;
                            previouslyActivatedWindow = windowNumber;
                            StartCoroutine(WaitForNextSpawn());
                        }
                    }
                }
                else
                {
                    StartCoroutine(WaitToPlaySong());
                }
            }
        }
        else
        {
            score.GetComponent<TextMesh>().text = "Loading song...";
        }
        
        foreach (GameObject beatCube in GameObject.FindGameObjectsWithTag("Beat Cube"))
        {
            beatCube.transform.position += spawnLocation.transform.forward * speed * Time.deltaTime;
        }
    }

    IEnumerator WaitForNextSpawn()
    {
        yield return new WaitForSeconds(timeBetweenBeats);
        canSpawn = true;
    }

    IEnumerator WaitToPlaySong()
    {
        stopwatch.Start();
        yield return new WaitForSeconds(timeToReachPlayer);
        source.Play();
    }

    void LoadSong()
    {
        stopwatch.Start();
        songBeats = preprocessor.ProcessSong(clip, samples, songFrequency, songSampleNum, songChannels);
        stopwatch.Stop();
        UnityEngine.Debug.Log("Song Load Time: " + stopwatch.Elapsed);
        stopwatch.Reset();
        UnityEngine.Debug.Log("Length of beats array " + songBeats.Length);
        UnityEngine.Debug.Log("Song length using length of beat array times window length: " + Mathf.FloorToInt(preprocessor.WindowPositionToTime(songBeats.Length) / 60) + "m" + Mathf.FloorToInt(preprocessor.WindowPositionToTime(songBeats.Length) % 60) + "s"); 
    }

    public void UpdateScore()
    {
        if(!syncCheck)
        {
            syncCheck = true;
            UnityEngine.Debug.Log("Increasing Score");
            int scoreIncrement = 100;
            hitCount += 1;
            hitStreak += 1;
            if (hitStreak / 10 > 0 && hitStreak / 10 < 4)
            {
                multiplier = (hitStreak / 10) + 1;
            }
            currentScore += scoreIncrement * multiplier;

            score.GetComponent<TextMesh>().text = "Score: " + currentScore + "\nMultiplier: " + multiplier + "\nHit Count: " + hitCount + "\nHit Streak: " + hitStreak;
            hitMissText.SetActive(true);
            hitMissText.GetComponent<TextMesh>().text = "Hit! +" + (scoreIncrement * multiplier);
            hitMissText.GetComponent<TextMesh>().color = Color.green;
            StartCoroutine(ConfirmTextTimeout());
            syncCheck = false;
        }
        return;
        
    }

    public void BreakHitStreak()
    {
        UnityEngine.Debug.Log("Breaking Streak");
        hitStreak = 0;
        multiplier = 1;
        score.GetComponent<TextMesh>().text = "Score: " + currentScore + "\nMultiplier: " + multiplier + "\nHit Count: " + hitCount + "\nHit Streak: " + hitStreak;
        hitMissText.SetActive(true);
        hitMissText.GetComponent<TextMesh>().text = "Miss!";
        hitMissText.GetComponent<TextMesh>().color = Color.red;
        StartCoroutine(ConfirmTextTimeout());
    }

    IEnumerator ConfirmTextTimeout()
    {
        yield return new WaitForSeconds(0.35f);
        hitMissText.SetActive(false);
    }
}
