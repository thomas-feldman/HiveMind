using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Mothership : MonoBehaviour {

    public GameObject enemy;
    public int numberOfEnemies = 20;

    public GameObject spawnLocation;

    //Resource Harvesting Variables
    public List<GameObject> drones = new List<GameObject>();
    public List<GameObject> scouts = new List<GameObject>();
    public int maxScouts = 4;
    public List<GameObject> eliteHarvestors = new List<GameObject>();
    public int maxEliteHarvestors;
    public List<GameObject> harvestors = new List<GameObject>();
    public int maxHarvestors;

    //Resource object variables
    public List<GameObject> resourceObjects = new List<GameObject>();
    private float forageTimer;
    private float forageTime = 10.0f;
    public float collectedResources;

    // initialise the boids
    void Start() {
        for (int i = 0; i < numberOfEnemies; i++) {

            Vector3 spawnPosition = spawnLocation.transform.position;

            spawnPosition.x = spawnPosition.x + Random.Range(-50, 50);
            spawnPosition.y = spawnPosition.y + Random.Range(-50, 50);
            spawnPosition.z = spawnPosition.z + Random.Range(-50, 50);

            GameObject thisEnemy = Instantiate(enemy, spawnPosition, spawnLocation.transform.rotation) as GameObject;
            drones.Add(thisEnemy); 
        }
        //initialise the variables that we need
        maxEliteHarvestors = Mathf.Abs(numberOfEnemies / 5);
        maxHarvestors = numberOfEnemies - maxEliteHarvestors - maxScouts;
        collectedResources = 0;
    }

    // Update is called once per frame
    void Update() {
        //(Re)Initialise Scouts Continuously
        if (scouts.Count < maxScouts)
        {
            CreateScoutList();
        }
        //(Re)Determine best resource objects periodically
        if (resourceObjects.Count > 0 && Time.time > forageTimer)
        {
            //Sort resource objects delegated by their resource amount in decreasing order
            resourceObjects.Sort(delegate (GameObject a, GameObject b) {
                return (b.GetComponent<Asteroid>().resource).CompareTo(a.GetComponent<Asteroid>().resource);
            });
            //Only send drones if the resource list has more than 5 asteroids
            if (resourceObjects.Count >= 5)
            {
                if (eliteHarvestors.Count <= maxEliteHarvestors)
                {
                    //assign elite drones based on its fitness
                    for (int i = 0; i < drones.Count - 1; i++)
                    {
                        int obj = Random.Range(0, 1);
                        //We are looking for speed and detection radius for it to be and elite drone
                        if ((drones[i].GetComponent<Drone>().speed > 50 && drones[i].GetComponent<Drone>().detectionRadius > 340) || (drones[i].GetComponent<Drone>().speed > 50 || drones[i].GetComponent<Drone>().detectionRadius > 340) && (eliteHarvestors.Count <= maxEliteHarvestors)) {
                            eliteHarvestors.Add(drones[i]);
                            drones.Remove(drones[i]);                          
                            eliteHarvestors[eliteHarvestors.Count - 1].GetComponent<Drone>().resourceTarget = resourceObjects[obj].GetComponent<Asteroid>();
                            eliteHarvestors[eliteHarvestors.Count - 1].GetComponent<Drone>().droneBehaviour = Drone.DroneBehaviours.EliteForaging;
                        }
                    }
                }
                //Send the remaining drones
                else if (harvestors.Count <= maxHarvestors)
                {
                    for (int i = 0; i < drones.Count - 1; i++)
                    {
                        harvestors.Add(drones[i]);
                        drones.Remove(drones[i]);
                        int obj = Random.Range(2, 5);
                        harvestors[harvestors.Count - 1].GetComponent<Drone>().resourceTarget = resourceObjects[obj].GetComponent<Asteroid>();
                        harvestors[harvestors.Count - 1].GetComponent<Drone>().droneBehaviour = Drone.DroneBehaviours.Foraging;
                    }
                }
            }
                forageTimer = Time.time + forageTime;           
        }
        //check for end of game
        if (collectedResources >= (numberOfEnemies*20))
        {
            Debug.Log("Game Over");
        }
    }

    // This Method is used to filter the drones for scouts based on the 'fitness' (its random variables)
    private void CreateScoutList(){
        //for each drone in the list, check to see if they are fittest scouts
        for (int j = 0; j < drones.Count; j++)
        {
            //Look for drones that have both high values for speed, and quick detection times
            if (scouts.Count < maxScouts && (drones[j].GetComponent<Drone>().speed > 50 && drones[j].GetComponent<Drone>().detectTime < 6))
            {
                scouts.Add(drones[j]);
                drones.Remove(drones[j]);
                scouts[scouts.Count - 1].GetComponent<Drone>().droneBehaviour = Drone.DroneBehaviours.Scouting;
            }
        }
        //If the list isnt max, make some adjustments to the fitness to compensate
        if (scouts.Count != maxScouts)
        {
            for (int j = 0; j < drones.Count; j++)
            {
                //Look for drones that have  high values for speed or quick detection times
                if (scouts.Count < maxScouts && (drones[j].GetComponent<Drone>().speed > 50 || drones[j].GetComponent<Drone>().detectTime < 6))
                {
                    scouts.Add(drones[j]);
                    drones.Remove(drones[j]);
                    scouts[scouts.Count - 1].GetComponent<Drone>().droneBehaviour = Drone.DroneBehaviours.Scouting;
                    if (scouts.Count == maxScouts)
                    {
                        break;
                    }
                }
            }
        }
    }
}

