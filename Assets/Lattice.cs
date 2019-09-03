using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lattice : MonoBehaviour
{
    //generation vars
    [SerializeField]
    public GameObject lineGenPrefab;
    [SerializeField]
    private Material sphereMaterial;
    [SerializeField]
    private GameObject burgersVectLine;

    //spawns the initial lattice structure into the scene starting at startPosForLattice
    //Can change the primitive type to another type if desirable
    public void latticeSpawn(int size, Vector3 startPos)
    {
        for (float x = startPos.x; x <= size; x += 2)
        {
            for (float y = startPos.y; y <= size; y += 2)
            {
                for (float z = startPos.z; z <= size; z += 2)
                {
                    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.tag = "Sphere";
                    sphere.transform.position = new Vector3(x, y, z);
                    sphere.GetComponent<Renderer>().material = sphereMaterial;
                }
            }
        }
        
        GameObject[] allSpheres = GameObject.FindGameObjectsWithTag("Sphere");
        foreach(GameObject s in allSpheres)
        {
            Vector3 currentSpherePos = s.transform.position;

            //draw x direction line from current sphere
            if (currentSpherePos.x != size)
            {
                GameObject newLineGen = Instantiate(lineGenPrefab);
                newLineGen.tag = "Line";
                LineRenderer lRend = newLineGen.GetComponent<LineRenderer>();

                //set line points
                lRend.SetPosition(0, currentSpherePos);
                lRend.SetPosition(1, currentSpherePos + new Vector3(2, 0, 0));
            }

            //draw y direction line from current sphere
            if (currentSpherePos.y != size)
            {
                GameObject newLineGen = Instantiate(lineGenPrefab);
                newLineGen.tag = "Line";
                LineRenderer lRend = newLineGen.GetComponent<LineRenderer>();

                //set line points
                lRend.SetPosition(0, currentSpherePos);
                lRend.SetPosition(1, currentSpherePos + new Vector3(0, 2, 0));
            }

            //draw z direction lines from current sphere
            if (currentSpherePos.z != size)
            {
                GameObject newLineGen = Instantiate(lineGenPrefab);
                newLineGen.tag = "Line";
                LineRenderer lRend = newLineGen.GetComponent<LineRenderer>();

                //set line points
                lRend.SetPosition(0, currentSpherePos);
                lRend.SetPosition(1, currentSpherePos + new Vector3(0, 0, 2));
            }
        }

        //create the burgers vector
        GameObject burgersVector = Instantiate(burgersVectLine);
        burgersVector.tag = "Burgers Vector";
        LineRenderer LRendB = burgersVector.GetComponent<LineRenderer>();
        
        LRendB.SetPosition(0, new Vector3(size, (size / 2) + 1, - 2));
        LRendB.SetPosition(1, new Vector3(size, (size / 2) + 1, size + 2));
    }

}
