using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public struct PauseEvent
{
    public float pauseTime;
    public float pauseDuration;

    public PauseEvent(float pauseTime, float pauseDuration)
    {
        this.pauseTime = pauseTime;
        this.pauseDuration = pauseDuration;
    }
};

public class LatticeController : MonoBehaviour
{
    //the main lattice to used in the scene
    [SerializeField]
    public Lattice mainLattice;

    //defines when the aninmation begins after program start
    [SerializeField]
    private int animationStartTime = 3;

    //defines the material for a sphere after being translated to show translation more clearly
    [SerializeField]
    private Material translatedSphereMaterial;

    //defines material for sphere during translation to show translation more clearly
    [SerializeField]
    private Material translatingSphereMaterial;

    //defines the rate at which the translation occurs
    [SerializeField]
    private float speed = 1;

    //defines how far the sphere moves during the animation in stepInterval units
    [SerializeField]
    private int moveDist = 1;

    //defines the size of the overall lattice. Cubes only.
    [SerializeField]
    private int size = 20;

    //state controllers
    private bool animFinished = false;
    private bool animPaused = false;
    private float pauseTime = 0;
    private List<PauseEvent> pauseTimes = new List<PauseEvent>();

    //synthetic time used to control sphere animations
    private float animationTime = 0;
    private bool pauseOneCalled = false;

    //used for line updates
    private List<GameObject> allLines;
    private List<GameObject> allSpheres;
    private List<Vector3> prevSpherePositions = new List<Vector3>();
    private GameObject burgersVect;

    //used for first animation
    List<Vector3> sphereStartLocations = new List<Vector3>();
    List<GameObject> spheresToTranslate = new List<GameObject>();

    

    // Start is called before the first frame update
    void Start()
    {
        Physics.autoSimulation = false;
        mainLattice.latticeSpawn(size, mainLattice.transform.position);

        //get the intitial objects and positions
        allLines = new List<GameObject>(GameObject.FindGameObjectsWithTag("Line"));
        allSpheres = new List<GameObject>(GameObject.FindGameObjectsWithTag("Sphere"));
        foreach(GameObject s in allSpheres)
        {
            prevSpherePositions.Add(s.transform.position);
        }

        foreach (GameObject s in allSpheres)
        {
            if (s.transform.position.y > size / 2)
            {
                sphereStartLocations.Add(s.transform.position);
                spheresToTranslate.Add(s);
            }
        }

        burgersVect = GameObject.FindGameObjectWithTag("Burgers Vector");

        /**********settup desired pause times here. Input format is (pauseTime, pauseDuration)***************/
        
        //DO NOT REMOVE THIS PAUSE TIME. PART OF CORE SIMULATION.
        pauseTimes.Add(new PauseEvent(6.9f, 3.0f));

        /****************************************************************************************************/
    }

    // Update is called once per frame
    void Update()
    {
        if (!animPaused)
        {
            //update animation
            animationTime += Time.deltaTime;
            if (animationTime >= animationStartTime && !animFinished)
            {
                updateAnim();
            }
            if (!animFinished)
            {
                updateLines();
            }

            //pause if needed
            foreach(PauseEvent p in pauseTimes)
            {
                if (animationTime >= p.pauseTime)
                {
                    Debug.Log("Anim pausing.");
                    pauseTime = p.pauseTime;
                    pauseTimes.Remove(p);
                    animPaused = true;
                    break;
                }
            }
            
            //check if done
            if (animFinished)
            {
                Debug.Log("Animation Done.");
            }
        }
        //sim paused, check for unpause condition
        else
        {
            if (Time.realtimeSinceStartup >= animationTime + pauseTime)
            {
                pauseTime = 0;
                Debug.Log("Anim unpausing.");
                animPaused = false;
            }
        }
    }

    //handles real-time updating of lines (bonds)
    void updateLines()
    {
        List<Vector3> currentSpherePositions = new List<Vector3>();
        foreach (GameObject s in allSpheres)
        {
            currentSpherePositions.Add(s.transform.position);
        }
        List<Vector3> changedSpherePosPrev = new List<Vector3>();
        List<Vector3> changedSpherePosCurr = new List<Vector3>();
        for (int i = 0; i < currentSpherePositions.Count; i++)
        {
            //only update if the positions for the spheres (atoms) have changed to eliminate unnecessary processing
            if (currentSpherePositions[i] != prevSpherePositions[i])
            {
                changedSpherePosPrev.Add(prevSpherePositions[i]);
                changedSpherePosCurr.Add(currentSpherePositions[i]);
            }
        }
        
        if(changedSpherePosPrev.Count > 0)
        {
            //destroy the y lines which are no longer viable
            for (int i = allLines.Count - 1; i >= 0; i--)
            {
                LineRenderer tempLR = allLines[i].GetComponent<LineRenderer>();
                Vector3 tempOrigin = tempLR.GetPosition(0);
                Vector3 tempEnd = tempLR.GetPosition(1);

                //only remove the line if its position is not within the animation range
                if(tempOrigin.y <= size / 2)
                {
                    //get the lines with an origin at the previous sphere position
                    if (changedSpherePosPrev.Contains(tempOrigin))
                    {
                        //get the y direction line and remove it
                        if (tempEnd.y - tempOrigin.y > 0)
                        {
                            //Debug.Log("Removing y line at " + tempOrigin);
                            Destroy(allLines[i]);
                            allLines.RemoveAt(i);
                        }
                    }

                    //get the lines with an endpoint at the previous sphere positon
                    if (changedSpherePosPrev.Contains(tempEnd))
                    {
                        //get the y direction line and remove it
                        if (tempEnd.y - tempOrigin.y > 0)
                        {
                            //Debug.Log("Removing y line at " + tempOrigin);
                            Destroy(allLines[i]);
                            allLines.RemoveAt(i);
                        }
                    }
                }
            }
            
            //add y lines which may now be viable
            for(int i = 0; i < changedSpherePosCurr.Count; i++)
            {
                if (changedSpherePosCurr[i].x % 2 == 0 && changedSpherePosCurr[i].y % 2 == 0 && changedSpherePosCurr[i].z % 2 == 0)
                {
                    //Debug.Log("Drawing lines from moved sphere");
                    //draw lines from the sphere
                    if (changedSpherePosCurr[i].y + 2 < size)
                    {
                        GameObject newLineGen = Instantiate(mainLattice.lineGenPrefab);
                        newLineGen.tag = "Line";
                        LineRenderer lRend = newLineGen.GetComponent<LineRenderer>();

                        //set line points
                        lRend.SetPosition(0, changedSpherePosCurr[i]);
                        lRend.SetPosition(1, changedSpherePosCurr[i] + new Vector3(0, 2, 0));

                        allLines.Add(newLineGen);
                    }

                    //draw lines to the sphere
                    foreach (Vector3 v in currentSpherePositions)
                    {
                        if (v + new Vector3(0, 2, 0) == changedSpherePosCurr[i])
                        {
                            GameObject newLineGen = Instantiate(mainLattice.lineGenPrefab);
                            newLineGen.tag = "Line";
                            LineRenderer lRend = newLineGen.GetComponent<LineRenderer>();

                            //set line points
                            lRend.SetPosition(0, v);
                            lRend.SetPosition(1, changedSpherePosCurr[i]);

                            allLines.Add(newLineGen);
                        }
                    }
                }
            }

            //update all other lines
            //changedSpherePosCurr and Prev are both same size so either will work here as the bounding variable
            //their interators are also the same sphere, only differentiated by time
            for(int i = 0; i < allLines.Count; i++)
            {
                LineRenderer tempLR = allLines[i].GetComponent<LineRenderer>();
                Vector3 tempOrigin = tempLR.GetPosition(0);
                Vector3 tempEnd = tempLR.GetPosition(1);

                //culling check for this version of the program. If you want to change how the dislocation moves, YOU WILL NEED TO CHANGE THIS IF STATEMENT
                if(tempOrigin.y > size / 2)
                {
                    for (int j = 0; j < changedSpherePosCurr.Count; j++)
                    {
                        if (tempOrigin == changedSpherePosPrev[j])
                        {
                            tempLR.SetPosition(0, changedSpherePosCurr[j]);
                        }

                        if (tempEnd == changedSpherePosPrev[j])
                        {
                            tempLR.SetPosition(1, changedSpherePosCurr[j]);
                        }
                    }
                }
            }
        }
        prevSpherePositions = currentSpherePositions;
    }
    
    void updateAnim()
    {
        //default settup to only allow translation in the Z direction for easier implementation
        //animation's default is in dislocation motion to reduce framerate drops. If too many spheres or lines are moved at once, the framrate drops tremendously
        //animation lock controls which spheres are allowed to move given the current time
        List<float> animationLock = new List<float>();
        switch (Math.Floor((animationTime - animationStartTime)))
        {
            case 0:
                animationLock.Add(size);
                break;
            case 1:
                animationLock.Add(size);
                animationLock.Add(size - 2);
                break;
            case 2:
                animationLock.Add(size);
                animationLock.Add(size - 2);
                animationLock.Add(size - 4);
                break;
            case 3:
                animationLock.Add(size - 2);
                animationLock.Add(size - 4);
                animationLock.Add(size - 6);
                break;
            case 4:
                animationLock.Add(size - 4);
                animationLock.Add(size - 6);
                animationLock.Add(size - 8);
                break;
            case 5:
                animationLock.Add(size - 6);
                animationLock.Add(size - 8);
                animationLock.Add(size - 10);
                break;
            case 6:
                animationLock.Add(size - 8);
                animationLock.Add(size - 10);
                animationLock.Add(size - 12);
                break;
            case 7:
                animationLock.Add(size - 10);
                animationLock.Add(size - 12);
                animationLock.Add(size - 14);
                break;
            case 8:
                animationLock.Add(size - 12);
                animationLock.Add(size - 14);
                animationLock.Add(size - 16);
                break;
            case 9:
                animationLock.Add(size - 14);
                animationLock.Add(size - 16);
                animationLock.Add(size - 18);
                break;
            case 10:
                animationLock.Add(size - 16);
                animationLock.Add(size - 18);
                animationLock.Add(size - 20);
                break;
            case 11:
                animationLock.Add(size - 18);
                animationLock.Add(size - 20);
                break;
            case 12:
                animationLock.Add(size - 20);
                break;
            default:
                animFinished = true;
                break;
        }
        Vector3 moveDir = new Vector3(0, 0, 1);
        for (int i = 0; i < spheresToTranslate.Count; i++)
        {
            
            if (animationLock.Contains(spheresToTranslate[i].transform.position.x))
            {
                //Debug.Log(spheresToTranslate[i].transform.position.x);

                if ((spheresToTranslate[i].transform.position + (moveDir * speed * Time.deltaTime) - sphereStartLocations[i]).magnitude < 2 * moveDist)
                {
                    if(spheresToTranslate[i].GetComponent<Renderer>().material != translatingSphereMaterial)
                    {
                        spheresToTranslate[i].GetComponent<Renderer>().material = translatingSphereMaterial;
                    }
                    spheresToTranslate[i].transform.position += moveDir * speed * Time.deltaTime;
                }
                else
                {
                    Vector3 desiredTrans = moveDir * moveDist * 2;
                    spheresToTranslate[i].transform.position = sphereStartLocations[i] + desiredTrans;
                    spheresToTranslate[i].GetComponent<Renderer>().material = translatedSphereMaterial;
                }
            }
        }
        Vector3 burgersMoveDir = new Vector3(-1, 0, 0);
        LineRenderer LR = burgersVect.GetComponent<LineRenderer>();
        LR.SetPosition(0, LR.GetPosition(0) + burgersMoveDir * speed * 2 * Time.deltaTime);
        LR.SetPosition(1, LR.GetPosition(1) + burgersMoveDir * speed * 2 * Time.deltaTime);
    }

    /*
    void pauseAnim(float duration)
    {
        animPaused = true;
        while(Time.realtimeSinceStartup <= animationTime + duration)
        {
            //do nothing
        }
        Debug.Log("Anim restarting");
        animPaused = false;
    }
    */
}
