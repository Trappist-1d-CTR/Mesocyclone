using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Structure : MonoBehaviour
{
    #region Variables

    public string Name;
    public string Type;
    public Transform t;
    private Transform DroneT;
    public int Data2Link;
    public float LinkedData;
    public bool Linked = false;

    #endregion

    #region Constructors

    public void SetStructure()
    {
        Name = "Structure";
        Type = "Unknown";
        LinkedData = 0;
        Data2Link = 10;
    }

    public void SetStructure(string Name, string Type)
    {
        this.Name = Name;
        this.Type = Type;
        LinkedData = 0;
        Data2Link = 10;
    }

    public void SetStructure(string Name, string Type, int DataRequiredToLink)
    {
        this.Name = Name;
        this.Type = Type;
        LinkedData = 0;
        Data2Link = DataRequiredToLink;
    }

    #endregion

    private void Awake()
    {
        t = transform;
        DroneT = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
    }

    public bool Attempt2Link(Vector3 AttemptPosition, int TransferRate, float Range)
    {
        if (!Linked)
        {
            RaycastHit hit;
            Vector3 direction = (t.position - AttemptPosition).normalized;
            bool hasLineOfSight = Physics.Raycast(AttemptPosition, direction, out hit, Range);
            bool isPointingDown = DroneT.InverseTransformDirection(direction).y < -0.06f; // only link to structures below??   A: Y E S
            if (hasLineOfSight && isPointingDown && hit.transform == t)
            {
                //Debug.Log("Visible: " + Name);
                
                float distance = Vector3.Distance(AttemptPosition, t.position);
                float effectiveRange = Mathf.Max(1e-3f, Range);

                float Signal = 1f - Mathf.InverseLerp(0f, effectiveRange, distance);

                if (Signal > Random.Range(0, Random.Range(3, 7)))
                {
                    LinkedData += Mathf.FloorToInt(TransferRate * Signal) * Time.fixedDeltaTime;
                }

                if (LinkedData >= Data2Link)
                {
                    LinkedData = Data2Link; Linked = true; Debug.Log("Linked: " + Name);
                }

                return true;
            }
            else
            {
                /*Debug.Log("Blocked: " + Name);*/
                return false;
            }
        }
        else return false;
    }
}
