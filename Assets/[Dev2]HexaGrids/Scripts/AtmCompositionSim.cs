using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AtmCompositionSim : MonoBehaviour
{
    #region Variables

    #region Grid Fundamentals
    private List<HexaGrid> TerrainCompMap = new();
    private List<List<HexaGrid>> AirCompMap = new();
    private List<double[,]> NextTerrainCompMap;
    private List<List<double[,]>> NextAirCompMap = new();
    public Vector3 GridCenter;
    public int Radius; //InnRadius and OutRadius are always the same ; From center to corner and from center to edge are always the same
    #endregion

    #region Reference Scripts
    private GlobalValues C; //Constants List
    #endregion

    #region Substitutes
    //Empty
    public int LayerNumber = 10;
    public int MoleculesNumber;
    #endregion

    #region Cell Lists
    public List<Vector2Int> ExcludeCells;
    private Vector2Int[] CellNeighbours;
    #endregion

    #region Atmospheric Data Memory
    public List<double> GaleAtmosphericComposition;

    public double AtmosphericMolarMass;
    #endregion

    public bool StaticCompositionsSet = false;

    #endregion

    // Start is called before the first frame update
    private void Start()
    {
        C = GameObject.FindGameObjectWithTag("GameController").GetComponent<GlobalValues>();

        Radius = C.GridMapSize;
        MoleculesNumber = GaleAtmosphericComposition.Count;

        #region Terrain Compositions Map Setup

        NextTerrainCompMap = new List<double[,]>();

        for (int i = 0; i < LayerNumber; i++)
        {
            HexaGrid memory;

            TerrainCompMap.Add(new());
            TerrainCompMap[i].SetupHexagonalHexaGrid(Radius, out memory, out _);
            TerrainCompMap[i] = memory;

            //Where the FU- is the null reference exception!?!??!?!??!?
            NextTerrainCompMap.Add(new double[TerrainCompMap[i].xLength, TerrainCompMap[i].yLength]);

            NextTerrainCompMap[i] = TerrainCompMap[i].valArray;
        }

        #endregion

        #region Air Compositions Map Setup

        for (int i = 0; i < LayerNumber; i++)
        {
            for (int k = 0; k < MoleculesNumber; k++)
            {
                HexaGrid memory = new();

                AirCompMap.Add(new());
                memory.SetupHexagonalHexaGrid(Radius, out memory, out _);
                AirCompMap[k].Add(memory);

                AirCompMap[k][i].zHeight = i + 1;

                NextAirCompMap.Add(new());
                NextAirCompMap[k].Add(new double[AirCompMap[k][i].xLength, AirCompMap[k][i].yLength]);

                NextAirCompMap[k][i] = AirCompMap[k][i].valArray;
            }
        }

        #endregion

        #region Static Atmospheric Composition Setup
        
        for (int x = 0; x < TerrainCompMap[0].xLength; x++)
            for (int y = 0; y < TerrainCompMap[0].yLength; y++)
            {
                if (!ExcludeCells.Contains(new Vector2Int(x, y)))
                {
                    for (int k = 0; k < 5; k++)
                    {
                        TerrainCompMap[k].valArray[x, y] = GaleAtmosphericComposition[k];

                        for (int i = 0; i < LayerNumber; i++)
                        {
                            AirCompMap[k][i].valArray[x, y] = GaleAtmosphericComposition[k];
                        }
                    }
                }
            }
        
        //                       Oxygen (O2)                            Carbon Dioxide (CO2)                       Argon (Ar)                                Nitrogen (N2)                             Water (H2O)
        

        StaticCompositionsSet = true;

        #endregion
    }

    // Update is called once per frame
    void Update()
    {

    }
}
