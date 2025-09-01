using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;

public class PressureSim : MonoBehaviour
{
    #region Variables

    #region Grid Fundamentals
    //public HexaGrid TerrainPressMap = new();
    public List<double> StaticPressMap;
    public List<HexaGrid> AirPressMap = new();
    //private double[,] NextTerrainPressMap;
    private List<double[,]> NextAirPressMap = new();
    public Vector3 GridCenter;
    public int Radius; //InnRadius and OutRadius are always the same ; From center to corner and from center to edge are always the same
    #endregion

    #region Reference Scripts
    private GlobalValues C; //Constants List
    private HeatSim HeatMapScript;
    private ParticlesSim PartMapScript;
    #endregion

    #region Substitutes
    //Empty    
    #endregion

    #region Cell Lists
    public List<Vector2Int> ExcludeCells;
    private Vector2Int[] CellNeighbours;
    #endregion

    #region Grid Test Variables
    //private TextMesh[,] TerrainMapText;
    private List<TextMesh> AirMapText = new();
    #endregion

    #region Script Optimization Memory
    public double CellVolume;
    private int xMem, yMem;
    private float CellHeight;
    private int LayerNumber;
    #endregion

    public bool StaticPressureSet = false;

    #endregion

    private void Start()
    {
        C = GameObject.FindGameObjectWithTag("GameController").GetComponent<GlobalValues>();
        HeatMapScript = GameObject.FindGameObjectWithTag("HeatMap").GetComponent<HeatSim>();
        PartMapScript = GameObject.FindGameObjectWithTag("PartMap").GetComponent<ParticlesSim>();

        Radius = C.GridMapSize;
        CellHeight = C.CellHeight;
        LayerNumber = C.LayerNumber;

        #region (unused) Terrain Pressure Map Setup
        /*
        TerrainPressMap.SetupHexagonalHexaGrid(Radius, out TerrainPressMap, out ExcludeCells);

        NextTerrainPressMap = new double[TerrainPressMap.xLength, TerrainPressMap.yLength];
        TerrainMapText = new TextMesh[TerrainPressMap.xLength, TerrainPressMap.yLength];

        NextTerrainPressMap = TerrainPressMap.valArray;
        */
        #endregion

        #region Air Pressure Map Setup

        for (int i = 0; i < LayerNumber; i++)
        {
            HexaGrid memory;

            AirPressMap.Add(new());
            AirPressMap[i].SetupHexagonalHexaGrid(Radius, out memory, out ExcludeCells);
            AirPressMap[i] = memory;

            AirPressMap[i].zHeight = i + 1;

            NextAirPressMap.Add(new double[AirPressMap[i].xLength, AirPressMap[i].yLength]);
            AirMapText.Add(new());

            NextAirPressMap[i] = AirPressMap[i].valArray;

        }
        #endregion        
    }

    private void FixedUpdate()
    {
        if (PartMapScript.AtmCompositionSet && !StaticPressureSet)
        {
            #region Set SOM

            xMem = AirPressMap[0].xLength;
            yMem = AirPressMap[0].yLength;
            CellVolume = CellHeight * 6 / Mathf.Sqrt(3) * System.Math.Pow(AirPressMap[0].CellSize, 2);

            #endregion

            #region Press Grids Test Setup

            for (int i = 0; i < LayerNumber; i++)
            {
                AirMapText[i] = UtilsClass.CreateWorldText("", gameObject.transform, GridCenter + new Vector3(0, AirPressMap[i].zHeight * 20), 10, Color.white, TextAnchor.MiddleCenter);
            }

            #endregion

            #region Calculate Static Pressure

            for (int i = 0; i < LayerNumber; i++)
            {
                StaticPressMap.Add(C.GaleAtm * System.Math.Exp(-C.GaleG * C.GaleAtmMM * (i + 1) * CellHeight / (C.R * C.GaleAvgTemp)));
                AirMapText[i].text = StaticPressMap[i].ToString();
            }

            StaticPressureSet = true;

            #endregion
        }
        else if (StaticPressureSet && PartMapScript.StaticParticlesSet)
        {
            #region Calculate Actual Pressure

            double n;

            for (int x = 0; x < xMem; x++)
                for (int y = 0; y < yMem; y++)
                    if (!ExcludeCells.Contains(new Vector2Int(x, y)))
                        for (int i = 0; i < LayerNumber; i++)
                        {
                            n = 0;
                            for (int k = 0; k < PartMapScript.MoleculesNumber; k++)
                            {
                                n += PartMapScript.AirPartMap[k][i].valArray[x, y];
                            }

                            AirPressMap[i].Assign(x, y, n * C.R * HeatMapScript.AirHeatMap[i].valArray[x, y] / CellVolume);
                        }

            #endregion
        }
    }
}
