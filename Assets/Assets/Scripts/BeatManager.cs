using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;


[RequireComponent (typeof(AudioSource), typeof(AudioPreprocessor))]
public class BeatManager: MonoBehaviour {

    private enum Direction
    {
        Left,
        Right
    };

    public List<GameObject> beatCubes;
    public GameObject spawnLocation;
    public GameObject hitLocation;
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
    private float dist;
    private float speed;
    private Direction direction;
    private float rightmostX = 4.0f;
    private float leftmostX = -4.0f;
    private float targetTimeDiff = 0.75f;

    private void OnGUI()
    {
        listenerVolumeValue = GUI.HorizontalSlider(new Rect(10, 10, 100, 10), listenerVolumeValue, 0.0f, 1.0f);
        AudioListener.volume = listenerVolumeValue;
        sourceVolumeValue = GUI.HorizontalSlider(new Rect(10, 40, 100, 10), sourceVolumeValue, 0.0f, 1.0f);
        source.volume = sourceVolumeValue;
    }

    // Use this for initialization
    void Start () {
        stopwatch = new Stopwatch();
        beatCubes = new List<GameObject>();
        source = GetComponent<AudioSource>();
        preprocessor = GetComponent<AudioPreprocessor>();
        stopwatch.Start();
        songBeats = preprocessor.ProcessSong(source.clip);
        stopwatch.Stop();
        UnityEngine.Debug.Log("Song Load Time: " + stopwatch.Elapsed);
        stopwatch.Reset();
        UnityEngine.Debug.Log("Length of beats array " + songBeats.Length);
        UnityEngine.Debug.Log("Song length using length of beat array times window length: " + Mathf.FloorToInt(preprocessor.WindowPositionToTime(songBeats.Length) / 60) + "m" + Mathf.FloorToInt(preprocessor.WindowPositionToTime(songBeats.Length) % 60) + "s");
        stopwatch.Start();
        StartCoroutine(WaitToPlaySong());
    }
	
	// Update is called once per frame
	void Update () {
        dist = Vector3.Distance(hitLocation.transform.position, spawnLocation.transform.position);
        speed = dist / timeToReachPlayer;
        //UnityEngine.Debug.Log(stopwatch.Elapsed.Seconds);
        if (source.isPlaying)
        {
            if (source.time + timeToReachPlayer < source.clip.length)
            {
                windowNumber = preprocessor.TimeToWindowAmount(source.time + timeToReachPlayer);
                if (songBeats[windowNumber] != null && windowNumber > previouslyActivatedWindow && canSpawn)
                {
                    GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube) as GameObject;
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
                    temp.AddComponent<BoxCollider>();
                    temp.AddComponent<Rigidbody>();
                    temp.tag = "Beat Cube";
                    canSpawn = false;
                    previouslyActivatedWindow = windowNumber;
                    StartCoroutine(WaitForNextSpawn());
                }
            }
        }
        else
        {
            for (int i = 0; i < preprocessor.TimeToWindowAmount(timeToReachPlayer); i += 1)
            {
                if (songBeats[i] != null && i > previouslyActivatedWindow && canSpawn)
                {
                    GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube) as GameObject;
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
                    temp.AddComponent<BoxCollider>();
                    temp.AddComponent<Rigidbody>();
                    temp.transform.position = spawnLocation.transform.position + new Vector3(x, temp.transform.localScale.y * 0.5f, preprocessor.WindowPositionToTime(i) + (temp.transform.localScale.z * 0.5f));
                    lastX = x;
                    temp.tag = "Beat Cube";
                    canSpawn = false;
                    previouslyActivatedWindow = windowNumber;
                    StartCoroutine(WaitForNextSpawn());
                }
            }
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
        yield return new WaitForSeconds(timeToReachPlayer);
        source.Play();
    }
}
