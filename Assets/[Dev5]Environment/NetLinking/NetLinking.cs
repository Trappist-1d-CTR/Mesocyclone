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
    public Image SignalMask;
    public Slider LinkingProgress;
    public RectTransform Radar;
    public int RadarRadius;
    public List<string> RadarList;
    public List<string> SignalList;
    public Sprite StructureSprite;

    public float SignalAnimTimer;
    public float SignalAnimSpeed;
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
        SignalAnimTimer = -1;
        #endregion

        NotifierSystem.Send("Link Structures to DroneNet to Proceed.", System.DateTime.Now.ToLongTimeString(), 2, 7);
        NotifierSystem.Send("Fly close and above the structures to link.", System.DateTime.Now.ToLongTimeString(), 2, 7);
        NotifierSystem.Send("Structures connected: 0/10", System.DateTime.Now.ToLongTimeString(), 1);
    }

    private void Update()
    {
        #region Run Animation Timers

        if (SignalAnimTimer != -1 && SignalAnimTimer != 2)
        {
            SignalAnimTimer += Time.deltaTime * SignalAnimSpeed;
            if (SignalAnimTimer >= 1) SignalAnimTimer = 0;
        }
        #endregion

        #region Linking UI

        if (StructureName.text != Structures[FocusStructure].Name)
        {
            StructureName.text = Structures[FocusStructure].Name;
        }

        float size = SignalAnimTimer == -1 ? 0 :
            SignalAnimTimer < 0.25f ? 20 :
            SignalAnimTimer < 0.5f ? 50 :
            SignalAnimTimer < 0.75f ? 90 : 150;
        SignalMask.rectTransform.sizeDelta = new Vector2(size, size);

        LinkingProgress.value = Structures[FocusStructure].LinkProgress();
        #endregion

        #region Structure Radar

        for (int i = 0; i < Structures.Count; i++)
        {
            Structure s = Structures[i];

            if (s.Detectable)
            {
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

                    Vector3 D = Quaternion.AngleAxis(Mathf.Sign(Vector3.ProjectOnPlane(transform.right, Vector3.up).z) * Vector3.Angle(Vector3.ProjectOnPlane(transform.right, Vector3.up), Vector3.right), Vector3.up) * Vector3.ProjectOnPlane(s.transform.position - transform.position, Vector3.up);
                    //Debug.Log(Vector3.ProjectOnPlane(transform.right, Vector3.up) + " ; " + -Vector3.Angle(Vector3.ProjectOnPlane(transform.forward, Vector3.up), Vector3.forward));
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

                        Vector3 D = Quaternion.AngleAxis(Mathf.Sign(Vector3.ProjectOnPlane(transform.right, Vector3.up).z) * Vector3.Angle(Vector3.ProjectOnPlane(transform.right, Vector3.up), Vector3.right), Vector3.up) * Vector3.ProjectOnPlane(s.transform.position - transform.position, Vector3.up);

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
                    if (i != FocusStructure && SignalAnimTimer == -1)
                    {
                        FocusStructure = i; SignalAnimTimer = 0;
                    }

                    if (i == FocusStructure)
                    {
                        if (SignalAnimTimer == -1) SignalAnimTimer = 0;
                    }
                    else if (SignalAnimTimer != -1)
                    {
                        SignalAnimTimer = -1;
                    }

                    if (s.Linked)
                    {
                        AddStructureToNet(s);
                        if (SignalAnimTimer != -1) SignalAnimTimer = -1;
                    }

                    break;
                }
                else if (i == FocusStructure && SignalAnimTimer != -1) SignalAnimTimer = -1;
            }
        }
        #endregion
    }

    public void AddStructureToNet(Structure s)
    {
        DroneNet.Add(s);
        NotifierSystem.Send("Structures connected: " + DroneNet.Count + "/10", System.DateTime.Now.ToLongTimeString(), 1);
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
