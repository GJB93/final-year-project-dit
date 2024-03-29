﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordToCube : MonoBehaviour {

    public AudioClip soundEffect;
    public GameObject visualEffect;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag.Equals("Beat Cube"))
        {
            if (soundEffect != null)
            {
                AudioSource.PlayClipAtPoint(soundEffect, collision.contacts[0].normal);
            }
            
            if (visualEffect != null)
            {
                // Code snippet from https://unity3d.com/learn/tutorials/topics/graphics/creating-sparks-particle-trails
                foreach (ContactPoint contact in collision.contacts)
                {
                    Instantiate(visualEffect, contact.point, Quaternion.identity);
                }
                //End of snippet
            }
            collision.gameObject.tag = "Untagged";
            Debug.Log("Cube has been hit by sword");
            Camera.main.GetComponent<BeatManager>().UpdateScore();
            Destroy(collision.gameObject.GetComponent<Collider>());
            StartCoroutine(WaitToDestroy(collision.gameObject));
        } 
    }

    IEnumerator WaitToDestroy(GameObject gameObject)
    {
        yield return new WaitForSeconds(3.0f);
        Destroy(gameObject);
    }
}
