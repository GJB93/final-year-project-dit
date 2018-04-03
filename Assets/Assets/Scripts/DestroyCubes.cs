using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyCubes : MonoBehaviour {

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag.Equals("Beat Cube"))
        {
            Debug.Log("Cube was missed");
            Camera.main.GetComponent<BeatManager>().BreakHitStreak();
            Destroy(collision.gameObject);
        }
            
    }
}
