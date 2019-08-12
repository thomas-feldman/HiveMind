using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Asteroid : MonoBehaviour {

    public int resource = 50;
    private int maxResource;
 
    // Use this for initialization
    void Start () {
        resource = Random.Range(30, 150);
        maxResource = resource;
    }
	
	// Update is called once per frame
	void Update () {
        //regenerate resources over time
        if (resource < maxResource)
        {
            InvokeRepeating("Healing", 10.0f, 5.0f);
            
        }
    }
    //For periodic healing of asteroids health
    void Healing()
    {
        resource += 5;
        if(resource >= maxResource)
        {
            resource = maxResource;
        }
        CancelInvoke();
    }
}
