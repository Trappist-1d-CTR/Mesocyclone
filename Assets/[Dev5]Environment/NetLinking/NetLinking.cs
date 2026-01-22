using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NetLinking : MonoBehaviour
{
    #region Variables

    public List<Structure> DroneNet;
    public List<Structure> Structures;

    public float NetLinkerRange;
    public int DataTransferRate;

    #endregion

    #region UI
    public Image Linking;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        Transform t = GameObject.FindGameObjectWithTag("ArtificialStructure").transform;
        Structures = new();

        for (int i = 0; i < t.childCount; i++)
        {
            Transform c = t.GetChild(i).transform;
            Structure s;

            if (c.TryGetComponent(out s))
            {
                Structures.Add(s);
            }
        }
    }

    void FixedUpdate()
    {
        foreach (Structure s in Structures)
        {
            if (!DroneNet.Contains(s))
            {
                if (s.Attempt2Link(transform.position, DataTransferRate, NetLinkerRange))
                {
                    Linking.fillAmount += Time.fixedDeltaTime;
                    if (Linking.fillAmount >= 1) Linking.fillAmount = 0;

                    if (s.Linked) DroneNet.Add(s);
                }
            }
        }
    }
}
