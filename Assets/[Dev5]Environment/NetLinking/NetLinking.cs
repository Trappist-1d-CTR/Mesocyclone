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
    public int SignalRange;

    #endregion

    #region UI & Animations
    public TextMeshProUGUI StructureName;
    public Image LinkingAnimation;
    public RectTransform Radar;
    public int RadarRadius;
    public List<string> RadarList;
    public List<string> SignalList;
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

            //helpers
            Transform radarChild = Radar.Find(s.Name);
            float d = Vector3.Distance(s.transform.position, transform.position);
            if (d < RadarRange)
            {
                #region Delete direction-only
                if (SignalList.Contains(s.Name))
                {
                    if (radarChild != null)
                        Destroy(radarChild.gameObject);
                    SignalList.Remove(s.Name);
                }
                #endregion

                #region Structure direction + position

                GameObject mem;

                // helpers
                Image radarImage;
                RectTransform radarRect;

                if (RadarList.Contains(s.Name))
                {
                    if (radarChild != null)
                    {
                        mem = radarChild.gameObject;
                        radarImage = mem.GetComponent<Image>();
                        radarRect = mem.GetComponent<RectTransform>();
                    }
                    else
                    {
                        // throw new System.Exception("Unable to find corresponding image"); // exceptions r dookie dookie IMO; yes, my vocabulary is consisted of a toddler, so as my cognitive abilities
                        Debug.LogWarning($"Radar element for {s.Name} not found!! Recreating {s.Name}");
                        RadarList.Remove(s.Name);

                        radarRect = CreateRadarImage(true, 7, i, out radarImage);
                    }
                }
                else
                {
                    radarRect = CreateRadarImage(true, 7, i, out radarImage);
                }
                if (s.Linked && radarImage.color != Color.green)
                {
                    radarImage.color = Color.green;
                }

                Vector3 D = Quaternion.Euler(0, -transform.eulerAngles.y, 0) * Vector3.ProjectOnPlane(s.transform.position - transform.position, Vector3.up);
                d = D.magnitude;
                d = RadarRadius * Mathf.Pow(d / RadarRange, 0.5f);

                // division by zero fix #19907
                if (d > 1e-3f)
                {
                    float ang = Mathf.Acos(Mathf.Clamp(-D.normalized.z, -1f, 1f)) * Mathf.Sign(D.x);
                    radarRect.anchoredPosition = new Vector3(d * Mathf.Cos(ang), d * Mathf.Sin(ang));
                }
                else
                {
                    radarRect.anchoredPosition = Vector3.zero;
                }

                #endregion
            }
            else if (RadarList.Contains(s.Name))
            {
                if (radarChild != null)
                    Destroy(radarChild.gameObject);
                RadarList.Remove(s.Name);
            }
            else
            {
                if (d < SignalRange)
                {
                    #region Structure direction-only

                    GameObject mem;

                    // helpers
                    Image radarImage;
                    RectTransform radarRect;

                    if (SignalList.Contains(s.Name))
                    {
                        if (radarChild != null)
                        {
                            mem = radarChild.gameObject;
                            radarImage = mem.GetComponent<Image>();
                            radarRect = mem.GetComponent<RectTransform>();
                        }
                        else
                        {
                            // throw new System.Exception("Unable to find corresponding image"); // exceptions r dookie dookie IMO; yes, my vocabulary is consisted of a toddler, so as my cognitive abilities
                            Debug.LogWarning($"Radar element for {s.Name} not found!! Recreating {s.Name}");
                            SignalList.Remove(s.Name);

                            radarRect = CreateRadarImage(false, 4, i, out radarImage);
                        }
                    }
                    else
                    {
                        radarRect = CreateRadarImage(false, 4, i, out radarImage);
                    }
                    if (s.Linked && radarImage.color != Color.green)
                    {
                        radarImage.color = Color.green;
                    }

                    Vector3 D = Quaternion.Euler(0, -transform.eulerAngles.y, 0) * Vector3.ProjectOnPlane(s.transform.position - transform.position, Vector3.up);

                    float ang = Mathf.Acos(Mathf.Clamp(-D.normalized.z, -1f, 1f)) * Mathf.Sign(D.x);
                    radarRect.anchoredPosition = RadarRadius * new Vector3(Mathf.Cos(ang), Mathf.Sin(ang));

                    #endregion
                }
                else if (SignalList.Contains(s.Name))
                {
                    if (radarChild != null)
                        Destroy(radarChild.gameObject);
                    SignalList.Remove(s.Name);
                }
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

    private RectTransform CreateRadarImage(bool knownPos,int size, int i, out Image image)
    {
        GameObject mem;
        RectTransform radarRect;

        mem = new GameObject();
        image = mem.AddComponent<Image>();
        _ = image.sprite = StructureSprite;
        radarRect = mem.GetComponent<RectTransform>();
        radarRect.SetParent(Radar);
        radarRect.sizeDelta = new(size, size);
        image.color = Structures[i].Linked ? Color.green : Color.yellow;
        _ = mem.name = Structures[i].Name;

        if (knownPos)
        {
            RadarList.Add(Structures[i].Name);
        }
        else
        {
            SignalList.Add(Structures[i].Name);
        }

        return radarRect;
    }
}
