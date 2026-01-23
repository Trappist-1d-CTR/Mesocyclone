using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NetLinking : MonoBehaviour
{
    #region Variables

    public List<Structure> DroneNet;
    public List<Structure> Structures;
    public int FocusStructure;

    public float NetLinkerRange;
    public int DataTransferRate;
    public int RadarRange;

    #endregion

    #region UI & Animations
    public TextMeshProUGUI StructureName;
    public Image LinkingAnimation;
    public Transform Radar;
    public List<string> RadarList;
    public Sprite StructureSprite;

    private float[] AnimTimer;
    public AnimationCurve[] AnimCurve;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        #region Setup Structures

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
        #endregion

        #region Setup Animations

        FocusStructure = 0;
        AnimTimer = new float[AnimCurve.Length];
        for (int i = 0; i < AnimTimer.Length; i++)
        {
            AnimTimer[i] = -1;
        }
        #endregion
    }

    private void Update()
    {
        #region Run Animation Timers

        if (StructureName.text != Structures[FocusStructure].Name)
        {
            StructureName.text = Structures[FocusStructure].Name;
        }

        for (int i = 0; i < AnimTimer.Length; i++)
        {
            if (AnimTimer[i] != -1 && AnimTimer[i] != 2)
            {
                AnimTimer[i] += Time.deltaTime;
                if (AnimTimer[i] >= 1) AnimTimer[i] = 0;
            }
        }
        #endregion

        #region Perform Animations

        LinkingAnimation.fillAmount = AnimCurve[0].Evaluate(Mathf.Clamp01(AnimTimer[0]));
        #endregion

        #region Structure Radar

        for (int i = 0; i < Structures.Count; i++)
        {
            Structure s = Structures[i];

            float d = Vector3.Distance(s.transform.position, transform.position);
            if (d < RadarRange)
            {
                GameObject mem;

                if (RadarList.Contains(s.Name))
                {
                    if (Radar.Find(s.Name) != null)
                    {
                        mem = Radar.Find(s.Name).gameObject;
                    }
                    else
                    {
                        throw new System.Exception("Unable to find corresponding image");
                    }
                }
                else
                {
                    RadarList.Add(s.Name);

                    mem = new GameObject();
                    _ = mem.AddComponent<Image>().sprite = StructureSprite;
                    mem.GetComponent<RectTransform>().SetParent(Radar);
                    mem.GetComponent<RectTransform>().sizeDelta = new(7, 7);
                    mem.GetComponent<Image>().color = s.Linked ? Color.green : Color.yellow;
                    _ = mem.name = s.Name;
                }
                if (s.Linked && mem.GetComponent<Image>().color != Color.green)
                {
                    mem.GetComponent<Image>().color = Color.green;
                }

                Vector3 D = Quaternion.Euler(0, -transform.eulerAngles.y, 0) * Vector3.ProjectOnPlane(s.transform.position - transform.position, Vector3.up);
                d = D.magnitude;
                d = 105 * Mathf.Sqrt(d / RadarRange);
                float ang = Mathf.Acos(-D.normalized.z) * Mathf.Sign(D.x);
                mem.GetComponent<RectTransform>().anchoredPosition = new Vector3(d * Mathf.Cos(ang), d * Mathf.Sin(ang));
            }
            else if (RadarList.Contains(s.Name))
            {
                Destroy(Radar.Find(s.Name).gameObject);
                RadarList.Remove(s.Name);
            }
        }
        #endregion
    }

    void FixedUpdate()
    {
        #region NetLink Structures

        for (int i = 0; i < Structures.Count; i++)
        {
            Structure s = Structures[i];

            if (!DroneNet.Contains(s))
            {
                if (s.Attempt2Link(transform.position, DataTransferRate, NetLinkerRange))
                {
                    if (i != FocusStructure && AnimTimer[0] == 2)
                    {
                        FocusStructure = i; AnimTimer[0] = 0;
                    }

                    if (i == FocusStructure)
                    {
                        if (AnimTimer[0] == -1) AnimTimer[0] = 0;
                    }
                    else if (AnimTimer[0] != -1)
                    {
                        AnimTimer[0] = -1;
                    }

                    if (s.Linked)
                    {
                        DroneNet.Add(s);
                        if (AnimTimer[0] != 2) AnimTimer[0] = 2;
                    }
                }
                else if (i == FocusStructure && AnimTimer[0] != -1 && AnimTimer[0] != 2) AnimTimer[0] = -1;
            }
        }
        #endregion
    }
}
