using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Structure
{
    public Vector3 Position;
    public float ProgressCount = 0;
    public bool Linked = false;

    public Structure()
    {
        Position = Vector3.zero;
    }
    public Structure(Vector3 GlobalCoordinates)
    {
        Position = GlobalCoordinates;
    }
}

public class Structures_Demo : MonoBehaviour
{
    #region Variables

    private List<Structure> Structures;
    public float NetLinkerRange;
    public int LinkTime;

    #endregion

    // Start is called before the first frame update
    void Start()
    {
        Transform t = GameObject.FindGameObjectWithTag("ArtificialStructure").transform;
        Structures = new();

        for (int i = 0; i < t.childCount; i++)
        {
            Structures.Add(new(t.GetChild(i).transform.position));
            //Debug.Log(t.GetChild(i));
        }
    }

    void FixedUpdate()
    {
        foreach (Structure cube in Structures)
        {
            if (!cube.Linked)
            {
                Debug.DrawLine(transform.position, transform.position + ((cube.Position - transform.position).normalized * (100f / Vector3.Distance(cube.Position, transform.position))), Color.magenta, Time.fixedDeltaTime);

                if (Vector3.Distance(cube.Position, transform.position) < NetLinkerRange)
                {
                    cube.ProgressCount += Time.fixedDeltaTime;

                    if (cube.ProgressCount >= LinkTime)
                    {
                        cube.Linked = true;
                        Debug.LogWarning(cube + " has been NetLinked!!!");
                    }
                }
            }
        }
    }
}
