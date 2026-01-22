using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Structure : MonoBehaviour
{
    #region Variables

    public string Name;
    public string Type;
    public Transform t;
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
    }

    public bool Attempt2Link(Vector3 AttemptPosition, int TransferRate, float Range)
    {
        if (!Linked)
        {
            RaycastHit hit;
            Vector3 direction = (t.position - AttemptPosition).normalized;
            bool a = Physics.Raycast(AttemptPosition, direction, out hit, Range) && direction.y < -0.1f;
            if (a && hit.transform == t)
            {
                //Debug.Log("Visible: " + Name);

                float Signal = 1 - (Vector3.Distance(AttemptPosition, t.position) / Range);

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