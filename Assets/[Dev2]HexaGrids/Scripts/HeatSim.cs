using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;

public class HeatSim : MonoBehaviour
{
    #region Variables

    #region Grid Fundamentals
    public HexaGrid TerrainHeatMap = new();
    public List<HexaGrid> AirHeatMap = new();
    public double[,] NextTerrainHeatMap;
    public List<double[,]> NextAirHeatMap = new();
    public Vector3 GridCenter;
    public int Radius; //InnRadius and OutRadius are always the same ; From center to corner and from center to edge are always the same
    #endregion

    #region Reference Scripts
    private GlobalValues C; //Constants List
    #endregion

    #region Substitutes
    public float TerrainConduction = 0.17f; //Coefficient of conduction transfer for the terrain (k)
    public float InterConduction = 0.045f; //Coefficient of conduction transfer air-terrain (k)
    public float AirConduction = 0.026f; //Coefficient of conduction transfer for the air (k)
    #endregion

    #region Cell Lists
    public List<Vector2Int> ExcludeCells;
    private Vector2Int[] CellNeighbours;
    #endregion

    #region Grid Test Variables
    private TextMesh[,] TerrainMapText;
    private List<TextMesh[,]> AirMapText = new();
    #endregion

    #region Script Optimization Memory
    private float CellHeight;
    private int LayerNumber;
    #endregion

    public bool StaticTempSet = false;

    #endregion

    // Start is called before the first frame update
    private void Start()
    {
        C = GameObject.FindGameObjectWithTag("GameController").GetComponent<GlobalValues>();
        Radius = C.GridMapSize;
        CellHeight = C.CellHeight;
        LayerNumber = C.LayerNumber;

        #region Terrain Heat Map Setup
        TerrainHeatMap.SetupHexagonalHexaGrid(Radius, out TerrainHeatMap, out ExcludeCells);

        NextTerrainHeatMap = new double[TerrainHeatMap.xLength, TerrainHeatMap.yLength];
        TerrainMapText = new TextMesh[TerrainHeatMap.xLength, TerrainHeatMap.yLength];

        NextTerrainHeatMap = TerrainHeatMap.valArray;
        #endregion

        #region Air Heat Map Setup
        for (int i = 0; i < LayerNumber; i++)
        {
            HexaGrid memory;

            AirHeatMap.Add(new());
            AirHeatMap[i].SetupHexagonalHexaGrid(Radius, out memory, out _);
            AirHeatMap[i] = memory;

            AirHeatMap[i].zHeight = i + 1;

            NextAirHeatMap.Add(new double[AirHeatMap[i].xLength, AirHeatMap[i].yLength]);
            AirMapText.Add(new TextMesh[AirHeatMap[i].xLength, AirHeatMap[i].yLength]);

            NextAirHeatMap[i] = AirHeatMap[i].valArray;
        }
        #endregion

        #region Heat Grids Test
        for (int x = 0; x < TerrainHeatMap.xLength; x++)
            for (int y = 0; y < TerrainHeatMap.yLength; y++)
                if (!ExcludeCells.Contains(new Vector2Int(x, y)))
                {
                    TerrainMapText[x, y] = UtilsClass.CreateWorldText("", gameObject.transform, GridCenter + TerrainHeatMap.GetWorldPosition(x, y), 5, Color.white, TextAnchor.MiddleCenter);

                    for (int i = 0; i < LayerNumber; i++)
                        AirMapText[i][x, y] = UtilsClass.CreateWorldText("", gameObject.transform, GridCenter + AirHeatMap[i].GetWorldPosition(x, y) + new Vector3(0, (AirHeatMap[i].zHeight + 0.6f) * 8), 5, Color.white, TextAnchor.MiddleCenter);
                }

        //TerrainHeatMap.Assign(3, 3, 35570f);
        //TerrainHeatMap.Assign(4, 8, 157.08); TerrainHeatMap.Assign(18, 11, 67.4);
        #endregion
    }

    private void FixedUpdate()
    {
        if (!StaticTempSet)
        {
            #region Starting Conditions

            for (int x = 0; x < TerrainHeatMap.xLength; x++)
                for (int y = 0; y < TerrainHeatMap.yLength; y++)
                    if (!ExcludeCells.Contains(new Vector2Int(x, y)))
                    {
                        TerrainHeatMap.Assign(x, y, C.GaleAvgTemp);

                        for (int i = 0; i < LayerNumber; i++)
                            AirHeatMap[i].Assign(x, y, C.GaleAvgTemp);
                    }

            StaticTempSet = true;

            #endregion
        }
        else
        {
            #region Setup Current Maps
            for (int i = 0; i < LayerNumber; i++)
                AirHeatMap[i].valArray = NextAirHeatMap[i];

            //Debug.Log(TerrainHeatMap.valArray[3, 3] + " ; " + NextTerrainHeatMap[3, 3]);

            TerrainHeatMap.valArray = NextTerrainHeatMap;
            #endregion

            #region Iterate Through Coordinates
            for (int x = 0; x < TerrainHeatMap.xLength; x++)
                for (int y = 0; y < TerrainHeatMap.yLength; y++)
                {
                    if (!ExcludeCells.Contains(new Vector2Int(x, y)))
                    {
                        CellNeighbours = TerrainHeatMap.GetNeighbours(x, y);

                        #region Simulate Horizontal Conductions

                        foreach (Vector2Int Neighbour in CellNeighbours)
                        {
                            #region Simulate Terrain Horizontal Conduction

                            if (0 <= Neighbour.x && Neighbour.x < TerrainHeatMap.xLength && 0 <= Neighbour.y && Neighbour.y < TerrainHeatMap.yLength && !ExcludeCells.Contains(Neighbour))
                            {
                                NextTerrainHeatMap[x, y] += TerrainConduction * Mathf.Sqrt(1f / 3f) * TerrainHeatMap.CellSize * CellHeight * (TerrainHeatMap.valArray[Neighbour.x, Neighbour.y] - TerrainHeatMap.valArray[x, y]) * Time.fixedDeltaTime / 2;
                                //Debug.Log(x + ", " + y + " ; " + Neighbour.x + ", " + Neighbour.y + ": " + TerrainHeatMap.valArray[x, y] + " ; " + TerrainHeatMap.valArray[Neighbour.x, Neighbour.y]);
                            }

                            #endregion

                            #region Simulate Air Horizontal Conduction

                            for (int i = 0; i < LayerNumber; i++)
                            {
                                if (0 <= Neighbour.x && Neighbour.x < TerrainHeatMap.xLength && 0 <= Neighbour.y && Neighbour.y < TerrainHeatMap.yLength && !ExcludeCells.Contains(Neighbour))
                                {
                                    NextAirHeatMap[i][x, y] += AirConduction * Mathf.Sqrt(1f / 3f) * AirHeatMap[0].CellSize * CellHeight * (AirHeatMap[i].valArray[Neighbour.x, Neighbour.y] - AirHeatMap[i].valArray[x, y]) * Time.fixedDeltaTime / 2;
                                    //Debug.Log(x + ", " + y + " ; " + Neighbour.x + ", " + Neighbour.y + ": " + TerrainHeatMap.valArray[x, y] + " ; " + TerrainHeatMap.valArray[Neighbour.x, Neighbour.y]);
                                }
                            }

                            #endregion
                        }

                        #endregion

                        #region Simulate Inter-Conduction

                        NextTerrainHeatMap[x, y] += /*First part... Unsure tbh, likely to change*/ AirConduction * Mathf.Sqrt(3) * System.Math.Pow(TerrainHeatMap.CellSize, 2) * (AirHeatMap[0].valArray[x, y] - TerrainHeatMap.valArray[x, y]) * Time.fixedDeltaTime / (4 * CellHeight);

                        NextAirHeatMap[0][x, y] += /*First part... Unsure tbh, likely to change*/ AirConduction * Mathf.Sqrt(3) * System.Math.Pow(TerrainHeatMap.CellSize, 2) * (TerrainHeatMap.valArray[x, y] - AirHeatMap[0].valArray[x, y]) * Time.fixedDeltaTime / (4 * CellHeight);

                        #endregion

                        #region Simulate Air Vertical Conduction

                        for (int i = 1; i < LayerNumber - 1; i++)
                        {
                            NextAirHeatMap[i][x, y] += InterConduction * Mathf.Sqrt(3) * System.Math.Pow(TerrainHeatMap.CellSize, 2) * (AirHeatMap[i - 1].valArray[x, y] - AirHeatMap[i].valArray[x, y]) * Time.fixedDeltaTime / (4 * CellHeight);

                            NextAirHeatMap[i][x, y] += InterConduction * Mathf.Sqrt(3) * System.Math.Pow(TerrainHeatMap.CellSize, 2) * (AirHeatMap[i + 1].valArray[x, y] - AirHeatMap[i].valArray[x, y]) * Time.fixedDeltaTime / (4 * CellHeight);
                        }

                        #endregion

                        #region Setup Test Grids
                        /*
                        TerrainMapText[x, y].text = Mathf.RoundToInt((float)TerrainHeatMap.valArray[x, y]).ToString();

                        for (int i = 0; i < LayerNumber; i++)
                            AirMapText[i][x, y].text = Mathf.RoundToInt((float)AirHeatMap[i].valArray[x, y]).ToString();
                        */
                        #endregion
                    }
                }
            #endregion            
        }
    }
}
