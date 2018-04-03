using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartingArea : MonoBehaviour {

    private bool loadingStarted = false;

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		if (!loadingStarted)
        {
            StartCoroutine(LoadScene());
        }
	}

    IEnumerator LoadScene()
    {
        loadingStarted = true;
        AsyncOperation async = SceneManager.LoadSceneAsync("GameScene");
        async.allowSceneActivation = false;
        while (async.progress <= 0.89f)
        {
            Debug.Log(async.progress);
            yield return null;
        }
        Debug.Log("Scene is loaded");
        async.allowSceneActivation = true;
    }
}
