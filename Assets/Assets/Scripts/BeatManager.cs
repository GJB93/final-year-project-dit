using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

[RequireComponent (typeof(AudioSource), typeof(AudioPreprocessor))]
public class BeatManager: MonoBehaviour {

    public List<GameObject> beatCubes;
    public GameObject spawnLocation;
    public GameObject hitLocation;
    public float timeToReachPlayer = 5.0f;
    public float unitsPerSecond;
    public float unitsPerFrame;
    public float listenerVolumeValue = 1.0f;
    public float sourceVolumeValue = 1.0f;

    private int[] songBeats;
    private int windowNumber = 0;
    private AudioPreprocessor preprocessor;
    private bool canSpawn = true;
    private int previouslyActivatedWindow;
    private Stopwatch stopwatch;
    private AudioSource source;

    private void OnGUI()
    {
        listenerVolumeValue = GUI.HorizontalSlider(new Rect(10, 10, 100, 10), listenerVolumeValue, 0.0f, 1.0f);
        AudioListener.volume = listenerVolumeValue;
        sourceVolumeValue = GUI.HorizontalSlider(new Rect(10, 40, 100, 10), sourceVolumeValue, 0.0f, 1.0f);
        source.volume = sourceVolumeValue;
    }

    // Use this for initialization
    void Start () {
        float dist = Vector3.Distance(hitLocation.transform.position, spawnLocation.transform.position);
        float speed = dist / timeToReachPlayer;
        unitsPerFrame = Time.deltaTime / speed;
        unitsPerSecond = dist / timeToReachPlayer;
        stopwatch = new Stopwatch();
        beatCubes = new List<GameObject>();
        source = GetComponent<AudioSource>();
        preprocessor = GetComponent<AudioPreprocessor>();
        stopwatch.Start();
        songBeats = preprocessor.ProcessSong(source.clip);
        stopwatch.Stop();
        UnityEngine.Debug.Log("Stopwatch elapsed: " + stopwatch.Elapsed);
        stopwatch.Reset();
        UnityEngine.Debug.Log("Length of beats array " + songBeats.Length);
        UnityEngine.Debug.Log("Song length using length of beat array times window length: " + Mathf.FloorToInt(preprocessor.WindowPositionToTime(songBeats.Length) / 60) + "m" + Mathf.FloorToInt(preprocessor.WindowPositionToTime(songBeats.Length) % 60) + "s");
        stopwatch.Start();
        StartCoroutine(WaitToPlaySong());
    }
	
	// Update is called once per frame
	void Update () {
        //UnityEngine.Debug.Log(stopwatch.Elapsed.Seconds);
        if (source.isPlaying)
        {
            if (source.time + timeToReachPlayer < source.clip.length)
            {
                windowNumber = preprocessor.TimeToWindowAmount(source.time + timeToReachPlayer);
                if (songBeats[windowNumber] == 1 && windowNumber > previouslyActivatedWindow && canSpawn)
                {
                    GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube) as GameObject;
                    temp.transform.position = spawnLocation.transform.position + new Vector3(0, temp.transform.localScale.y * 0.5f, temp.transform.localScale.z * 0.5f);
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
                if (songBeats[i] == 1 && i > previouslyActivatedWindow && canSpawn)
                {
                    GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube) as GameObject;
                    temp.AddComponent<BoxCollider>();
                    temp.AddComponent<Rigidbody>();
                    temp.transform.position = spawnLocation.transform.position + new Vector3(0, temp.transform.localScale.y * 0.5f, preprocessor.WindowPositionToTime(i) + (temp.transform.localScale.z * 0.5f));
                    temp.tag = "Beat Cube";
                    canSpawn = false;
                    previouslyActivatedWindow = windowNumber;
                    StartCoroutine(WaitForNextSpawn());
                }
            }
        }
        foreach (GameObject beatCube in GameObject.FindGameObjectsWithTag("Beat Cube"))
        {
            beatCube.transform.position += spawnLocation.transform.forward * unitsPerFrame;
        }
    }

    IEnumerator WaitForNextSpawn()
    {
        yield return new WaitForSeconds(0.8f);
        canSpawn = true;
    }

    IEnumerator WaitToPlaySong()
    {
        yield return new WaitForSeconds(5.0f);
        source.Play();
    }
}
