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
    public bool songLoaded = false;
    public int scoreIncrement = 100;
    public int hitsPerMultiplier = 10;
    public int minMultiplier = 1;
    public int maxMultiplier = 4;

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
    private float stepAmount = 1.0f;
    private float currentBeat;
    private float halfMultiplier = 0.5f;
    private float collisionTimeout = 0.1f;
    private float textTimeout = 0.35f;

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
    }

    // Update is called once per frame
    void Update () {
        if(!preprocessingThread.IsAlive)
        {
            dist = Vector3.Distance(beatLocation.transform.position, spawnLocation.transform.position);
            speed = dist / timeToReachPlayer;
            if (source.isPlaying && songBeats != null)
            {
                stopwatch.Stop();
                if (source.time + timeToReachPlayer < source.clip.length)
                {
                    windowNumber = preprocessor.TimeToWindowAmount(source.time + timeToReachPlayer);
                    SpawnCubes();
                }
            }
            else
            {
                if (stopwatch.IsRunning && songBeats != null)
                {
                    windowNumber = preprocessor.TimeToWindowAmount((float)stopwatch.Elapsed.TotalSeconds);
                    SpawnCubes();
                }
                else
                {
                    if(songLoaded)
                    {
                        StartCoroutine(WaitToPlaySong());
                        songLoaded = false;
                    }
                    
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
        songLoaded = false;
        stopwatch.Start();
        songBeats = preprocessor.ProcessSong(samples, songFrequency, songSampleNum, songChannels);
        stopwatch.Stop();
        UnityEngine.Debug.Log("Song Load Time: " + stopwatch.Elapsed);
        stopwatch.Reset();
        UnityEngine.Debug.Log("Length of beats array " + songBeats.Length);
        UnityEngine.Debug.Log("Song length using length of beat array times window length: " + 
            Mathf.FloorToInt(preprocessor.WindowPositionToTime(songBeats.Length) / 60) + "m" + 
            Mathf.FloorToInt(preprocessor.WindowPositionToTime(songBeats.Length) % 60) + "s");
        songLoaded = true;
    }

    void SpawnCubes()
    {
        if (songBeats[windowNumber] != null && windowNumber > previouslyActivatedWindow && canSpawn)
        {
            currentBeat = songBeats[windowNumber].energyValue;
            bool bestBeat = true;
            for (int i = windowNumber; i <= preprocessor.TimeToWindowAmount(timeBetweenBeats) + windowNumber; i += 1)
            {
                if (songBeats[i] != null)
                {
                    if (songBeats[i].energyValue > currentBeat)
                    {
                        UnityEngine.Debug.Log("Not best beat");
                        bestBeat = false;
                        break;
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
                            x = lastX - stepAmount;
                        else
                            x = lastX + stepAmount;
                    }
                    else
                    {
                        if (lastX <= leftmostX)
                        {
                            x = lastX + stepAmount;
                            direction = Direction.Right;
                        }
                        else
                        {
                            x = lastX - stepAmount;
                            direction = Direction.Left;
                        }
                    }
                }
                else
                {
                    x = lastX;
                }
                temp.transform.position = spawnLocation.transform.position + new Vector3(x, temp.transform.localScale.y * halfMultiplier, temp.transform.localScale.z * halfMultiplier);
                lastX = x;
                // These HSV values will generate pastel-style colours
                temp.GetComponent<Renderer>().material.color = Random.ColorHSV(0, 1, 0, 0.25f, 1, 1);
                canSpawn = false;
                previouslyActivatedWindow = windowNumber;
                StartCoroutine(WaitForNextSpawn());
            }

        }
    }

    public void UpdateScore()
    {
        if(!syncCheck)
        {
            syncCheck = true;
            UnityEngine.Debug.Log("Increasing Score");
            hitCount += 1;
            hitStreak += 1;
            if (hitStreak / hitsPerMultiplier > 0 && hitStreak / hitsPerMultiplier < maxMultiplier)
            {
                multiplier = (hitStreak / hitsPerMultiplier) + minMultiplier;
            }
            currentScore += scoreIncrement * multiplier;

            score.GetComponent<TextMesh>().text = "Score: " + currentScore + "\nMultiplier: " + multiplier + "\nHit Count: " + hitCount + "\nHit Streak: " + hitStreak;
            hitMissText.SetActive(true);
            hitMissText.GetComponent<TextMesh>().text = "Hit! +" + (scoreIncrement * multiplier);
            hitMissText.GetComponent<TextMesh>().color = Color.green;
            StartCoroutine(ConfirmTextTimeout());
            StartCoroutine(CollisionTimeout());
        }
        return;
        
    }

    public void BreakHitStreak()
    {
        UnityEngine.Debug.Log("Breaking Streak");
        hitStreak = 0;
        multiplier = minMultiplier;
        score.GetComponent<TextMesh>().text = "Score: " + currentScore + "\nMultiplier: " + multiplier + "\nHit Count: " + hitCount + "\nHit Streak: " + hitStreak;
        hitMissText.SetActive(true);
        hitMissText.GetComponent<TextMesh>().text = "Miss!";
        hitMissText.GetComponent<TextMesh>().color = Color.red;
        StartCoroutine(ConfirmTextTimeout());
    }

    IEnumerator ConfirmTextTimeout()
    {
        yield return new WaitForSeconds(textTimeout);
        hitMissText.SetActive(false);
    }

    IEnumerator CollisionTimeout()
    {
        yield return new WaitForSeconds(collisionTimeout);
        syncCheck = false;
    }
}
