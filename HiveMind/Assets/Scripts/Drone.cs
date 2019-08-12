using UnityEngine;
using System.Collections;

public class Drone : Enemy {

    GameManager gameManager;

    Rigidbody rb;

    //Movement & Rotation Variables
    public float speed = 50.0f;
    private float rotationSpeed = 5.0f;
    private float adjRotSpeed;
    private Quaternion targetRotation;
    public GameObject target;
    public float targetRadius = 200f;

    //Boid Steering/Flocking Variables
    public float separationDistance = 25.0f;
    public float cohesionDistance = 50.0f;
    public float separationStrength = 250.0f;
    public float cohesionStrength = 25.0f;
    private Vector3 cohesionPos = new Vector3(0f, 0f, 0f);
    private int boidIndex = 0;

    // shooting variables
    private float fireTimer = 5.0f;
    public GameObject alienLaser;
    private Vector3 shootingOffset = new Vector3(0, 0, -10);

    //Drone FSM Enumerator
    public enum DroneBehaviours
    {
        Idle,
        Scouting,
        Foraging,
        EliteForaging,
        ReturningResources,
        Attacking,
        Fleeing
    }

    public DroneBehaviours droneBehaviour;
    private Vector3 tarVel;
    private Vector3 tarPrevPos;
    private Vector3 attackPos;
    private float distanceRatio = 0.05f;
    private Vector3 fleePos;

    //Drone Behaviour Variables
    public GameObject motherShip;
    public Vector3 scoutPosition;
    private float scoutTimer;
    private float detectTimer;
    private float scoutTime = 10.0f;
    public float detectTime = 5.0f;
    public float detectionRadius = 400.0f;
    private int newResourceVal;
    public GameObject newResourceObject;
    public int currentLoad;
    public int resourceCapacity = 10;
    public Vector3 resourcePosition;
    public Asteroid resourceTarget;
    private float attackOrFlee;
    private float maxHealth;
    // Use this for initialization
    void Start() {

        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();

        rb = GetComponent<Rigidbody>();

        motherShip = gameManager.alienMothership;
        scoutPosition = motherShip.transform.position;

        //Create Random Attributes, some or all are used as fitness values
        health = Random.Range(250, 1000);
        maxHealth = health;
        speed = Random.Range(30,80);
        detectionRadius = Random.Range(300, 400);
        detectTime = Random.Range(4, 10);
        currentLoad = 0;

    }

    // Update is called once per frame
    void Update() {
        
        //Acquire player if spawned in
        if (gameManager.gameStarted)
        {
            //Altered slightly to look for health of itself and if the mothership is dead
            target = gameManager.playerDreadnaught;
            attackOrFlee = health * Friends();
            Debug.Log(attackOrFlee.ToString());
            if (attackOrFlee >= 1200)
                droneBehaviour = DroneBehaviours.Attacking;
            else if (attackOrFlee < 1200 || health < maxHealth/2)
                droneBehaviour = DroneBehaviours.Fleeing;
            else if (!motherShip)
            {
                droneBehaviour = DroneBehaviours.Attacking;
            }
        }

        //Move towards valid targets
        if (target)
            MoveTowardsTarget(target.transform.position);

        //Boid cohesion/segregation
        BoidBehaviour();
        //Drone Behaviours - State Switching
        switch (droneBehaviour)
        {
            case DroneBehaviours.Scouting:
                Scouting();
                break;
            case DroneBehaviours.Idle:
                Idle();
                break;
            case DroneBehaviours.Foraging:
                Foraging(resourceTarget);
                break;
            case DroneBehaviours.EliteForaging:
                EliteForaging(resourceTarget);
                break;
            case DroneBehaviours.ReturningResources:
                ReturningResources();
                break;
            case DroneBehaviours.Attacking:
                Attacking();
                break;
            case DroneBehaviours.Fleeing:
                Fleeing();
                break;
        }
    }

    //Drone FSM Behaviour - Scouting
    private void Scouting()
    {
        //If no new resource object found
        if (!newResourceObject)
        {
            //If close to scoutPosition, randomize new position to investigate within gamespace around mothership
            if (Vector3.Distance(transform.position, scoutPosition) < detectionRadius && Time.time > scoutTimer)
            {
                //Generate new random position
                Vector3 position;
                position.x = motherShip.transform.position.x + Random.Range(-1500, 1500);
                position.y = motherShip.transform.position.y + Random.Range(-400, 400);
                position.z = motherShip.transform.position.z + Random.Range(-1500, 1500);

                scoutPosition = position;

                //Update scoutTimer
                scoutTimer = Time.time + scoutTime;

            }
            else
            {
                MoveTowardsTarget(scoutPosition);
                Debug.DrawLine(transform.position, scoutPosition, Color.yellow);
            }
            //Every few seconds, check for new resources
            if (Time.time > detectTimer)
            {
                newResourceObject = DetectNewResources();
                detectTimer = Time.time + detectTime;
            }

        }
        //Resource found, head back to Mothership
        else
        {
            target = motherShip;
            Debug.DrawLine(transform.position, target.transform.position, Color.green);
            //In range of mothership, relay information and reset to drone again
            if (Vector3.Distance(transform.position, motherShip.transform.position) < targetRadius)
            {

                motherShip.GetComponent<Mothership>().drones.Add(this.gameObject);
                motherShip.GetComponent<Mothership>().scouts.Remove(this.gameObject);

                motherShip.GetComponent<Mothership>().resourceObjects.Add(newResourceObject);

                newResourceVal = 0;
                newResourceObject = null;

                droneBehaviour = DroneBehaviours.Idle;
            }

        }

    }

    //Idle: Will make sure they hover near the mothership and that the returning harvesters are added to the drones list upon return
    private void Idle()
    {
        MoveTowardsTarget(motherShip.transform.position);
        if (motherShip.GetComponent<Mothership>().eliteHarvestors.Contains(this.gameObject))
        {       
            motherShip.GetComponent<Mothership>().eliteHarvestors.Remove(this.gameObject);
            motherShip.GetComponent<Mothership>().drones.Add(this.gameObject);
        }
        else if (motherShip.GetComponent<Mothership>().harvestors.Contains(this.gameObject))
        {
            motherShip.GetComponent<Mothership>().harvestors.Remove(this.gameObject);
            motherShip.GetComponent<Mothership>().drones.Add(this.gameObject);
        }
    }

    //Send the normal foragers to gather and then return
    private void Foraging(Asteroid target)
    {
        //Move toward target
        MoveTowardsTarget(target.transform.position);
        Debug.DrawLine(transform.position, target.transform.position, Color.blue);
        //Determine the distance from target
        Vector3 heading = target.transform.position - transform.position;
        float distance = heading.magnitude;
        //if in range, harvest what you can, then return
        if (distance <= targetRadius)
        {
            if (resourceTarget.resource > 10)
            {
                currentLoad = resourceCapacity;
                resourceTarget.resource -= currentLoad;
            }
            else
            {
                currentLoad = currentLoad + resourceTarget.resource;
                resourceTarget.resource = 0;
            }
            droneBehaviour = DroneBehaviours.ReturningResources;
        }
    }

    //Same as normal because local area search did not work correctly, cleaned up code for presentation
    private void EliteForaging(Asteroid target)
    {
        //Move toward target
        MoveTowardsTarget(target.transform.position);
        //Determine the distance from target
        Debug.DrawLine(transform.position, target.transform.position, Color.red);
        Vector3 heading = target.transform.position - transform.position;
        float distance = heading.magnitude;
        //if in range, harvest what you can, then return
        if (distance <= targetRadius)
        {
            if (resourceTarget.resource > 10) {
                currentLoad = resourceCapacity;
                resourceTarget.resource -= currentLoad;
            }
            else
            {
                currentLoad = currentLoad + resourceTarget.resource;
                resourceTarget.resource = 0;
            }
            droneBehaviour = DroneBehaviours.ReturningResources;
        }
    }

    //Drone FSM Behaviour - Attacking
    private void Attacking()
    {
        //Calculate target's velocity (without using RB)
        tarVel = (target.transform.position - tarPrevPos) / Time.deltaTime;
        tarPrevPos = target.transform.position;
        //Calculate intercept attack position (p = t + r * d * v)
        attackPos = target.transform.position + distanceRatio * Vector3.Distance(transform.position, target.transform.position) * tarVel;
        attackPos.y = attackPos.y + 10;
        Debug.DrawLine(transform.position, attackPos, Color.red);
        // Not in range of intercept vector - move into position
        if (Vector3.Distance(transform.position, attackPos) > targetRadius)
            MoveTowardsTarget(attackPos);
        else
        {
            //Look at target - Lerp Towards target
            targetRotation = Quaternion.LookRotation(target.transform.position - transform.position);
            adjRotSpeed = Mathf.Min(rotationSpeed * Time.deltaTime, 1);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, adjRotSpeed);
            //Fire Weapons at target
            //...
            if (Time.time > fireTimer)
            {
                Instantiate(alienLaser, (transform.position + shootingOffset), transform.rotation);
                fireTimer = Time.time + fireTimer;
            }
        }
    }

    // Drone FSM Behaviour - Fleeing
    private void Fleeing()
    {
        //Calculate target's velocity (without using RB)
        tarVel = (target.transform.position - tarPrevPos) / Time.deltaTime;
        tarPrevPos = target.transform.position;
        //Calculate intercept position (p = t + r * d * v) and multiply it by -1 to get the flee vector
        fleePos = (target.transform.position + distanceRatio * Vector3.Distance(transform.position, target.transform.position) * tarVel) * -1.0f;
        //Flee if mothership alive, else attack instead
        if (Vector3.Distance(transform.position, target.transform.position) > (targetRadius * 5.0f))
        {
            if (motherShip)
            {
                if (Vector3.Distance(transform.position, motherShip.transform.position) > (targetRadius))
                {
                    MoveTowardsTarget(motherShip.transform.position);
                    Debug.DrawLine(transform.position, motherShip.transform.position, Color.green);
                }
            }
            else
            {
                droneBehaviour = DroneBehaviours.Attacking;
            }
        }
        else
        {
            MoveTowardsTarget(fleePos);
            Debug.DrawLine(transform.position, fleePos, Color.yellow);
        }
    }

    //state for returning the resources it collected to the mothership
    private void ReturningResources()
    {
        //Move till you get to the mothership, then reset cargo, add it to mothership
        if (Vector3.Distance(transform.position, motherShip.transform.position) < targetRadius)
        {
            motherShip.GetComponent<Mothership>().collectedResources += currentLoad;
            currentLoad = 0;
            droneBehaviour = DroneBehaviours.Idle;
        }
        else
        {
            MoveTowardsTarget(motherShip.transform.position);
        }
    }

    //For predator prey
    private int Friends()
    {
        int clusterStrength = 0;
        for (int i = 0; i < gameManager.enemyList.Length; i++)
        {
            if (Vector3.Distance(transform.position, gameManager.enemyList[i].transform.position) < targetRadius)
            {
                clusterStrength++;
            }
        }
        return clusterStrength;
    }


    //Method used periodically by scouts/elite forages to check for new valid resources
    private GameObject DetectNewResources()
    {
        //Go through list of asteroids and ...
        for (int i = 0; i < gameManager.asteroids.Length; i++)
        {

            //... check if they are within detection distance
            if (Vector3.Distance(transform.position, gameManager.asteroids[i].transform.position) <= detectionRadius)
            {

                //Find the best one
                if (gameManager.asteroids[i].GetComponent<Asteroid>().resource > newResourceVal)
                {
                    newResourceObject = gameManager.asteroids[i];
                }
            }
        }
        //Double check to see if the Mothership already knows about it and return it if not
        if (motherShip.GetComponent<Mothership>().resourceObjects.Contains(newResourceObject))
        {
            return null;
        }
        else
            return newResourceObject;

    }


    private void BoidBehaviour()
    {
        //Increment boid index reference
        boidIndex++;
        //Check if last boid in Enemy list
        if (boidIndex >= gameManager.enemyList.Length)
        {
            //Re-Compute the cohesionForce
            Vector3 cohesiveForce = (cohesionStrength / Vector3.Distance(cohesionPos, transform.position)) * (cohesionPos - transform.position);
            //Apply Force
            rb.AddForce(cohesiveForce);
            //Reset boidIndex
            boidIndex = 0;
            //Reset cohesion position
            cohesionPos.Set(0f, 0f, 0f);
        }
        //Currently analysed boid variables
        Vector3 pos = gameManager.enemyList[boidIndex].transform.position;
        Quaternion rot = gameManager.enemyList[boidIndex].transform.rotation;
        float dist = Vector3.Distance(transform.position, pos);
        //If not this boid
        if (dist > 0f)
        {
            //If within separation
            if (dist <= separationDistance)
            {
                //Compute scale of separation
                float scale = separationStrength / dist;
                //Apply force to ourselves
                rb.AddForce(scale * Vector3.Normalize(transform.position - pos));
            }
            //Otherwise if within cohesion distance of other boids
            else if (dist < cohesionDistance && dist > separationDistance)
            {

                //Calculate the current cohesionPos
                cohesionPos = cohesionPos + pos * (1f / (float)gameManager.enemyList.Length);
                //Rotate slightly towards current boid
                transform.rotation = Quaternion.RotateTowards(transform.rotation, rot, 1f);
            }
        }
    }

    private void MoveTowardsTarget(Vector3 targetPos) {
        //Rotate and move towards target if out of range
        if (Vector3.Distance(targetPos, transform.position) > targetRadius) {
            //Lerp Towards target
            targetRotation = Quaternion.LookRotation(targetPos - transform.position);
            adjRotSpeed = Mathf.Min(rotationSpeed * Time.deltaTime, 1);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, adjRotSpeed);
            rb.AddRelativeForce(Vector3.forward * speed * 20 * Time.deltaTime);
        }
    }

}
