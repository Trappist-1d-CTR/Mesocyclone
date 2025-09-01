using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;

public class ParticlesSim : MonoBehaviour
{
    #region Variables

    #region Grid Fundamentals
    //private List<HexaGrid> TerrainPartMap = new();
    public List<List<HexaGrid>> AirPartMap = new();
    //private List<double[,]> NextTerrainPartMap;
    private List<List<double[,]>> NextAirPartMap = new();
    public Vector3 GridCenter;
    public int Radius; //InnRadius and OutRadius are always the same ; From center to corner and from center to edge are always the same
    #endregion

    #region Reference Scripts
    private GlobalValues C; //Constants List
    private PressureSim PressMapScript;
    private HeatSim HeatMapScript;
    #endregion

    #region Substitutes    
    public double ParticleTransferCoefficient;
    public int MoleculesNumber;    
    #endregion

    #region Cell Lists
    public List<Vector2Int> ExcludeCells;
    private List<Vector2Int> CellNeighbours;
    #endregion

    #region Grid Test Variables
    public List<List<double>> TestMemory = new();
    //private TextMesh[,] TerrainMapText = new TextMesh[2, 2];
    private List<TextMesh> AirMapText = new();
    private List<TextMesh> AirMapText2 = new();
    #endregion

    #region Atmospheric Data Memory

    public List<double> GaleAtmosphericComposition;
    public double AtmosphericMolarMass;

    #endregion

    #region Script Optimization Memory
    private double CellVolume;
    private int xMem, yMem;
    private int LayerNumber;
    #endregion

    public bool AtmCompositionSet = false;

    public bool StaticParticlesSet = false;

    #endregion

    // Start is called before the first frame update
    private void Start()
    {
        C = GameObject.FindGameObjectWithTag("GameController").GetComponent<GlobalValues>();
        PressMapScript = GameObject.FindGameObjectWithTag("PressMap").GetComponent<PressureSim>();
        HeatMapScript = GameObject.FindGameObjectWithTag("HeatMap").GetComponent<HeatSim>();

        Radius = C.GridMapSize;
        LayerNumber = C.LayerNumber;
        MoleculesNumber = GaleAtmosphericComposition.Count;        

        #region (unused) Terrain Particles Map Setup
        /*
        NextTerrainPartMap = new List<double[,]>();

        for (int k = 0; k < MoleculesNumber; k++)
        {
            HexaGrid memory;

            TerrainPartMap.Add(new());
            TerrainPartMap[k].SetupHexagonalHexaGrid(Radius, out memory, out ExcludeCells);
            TerrainPartMap[k] = memory;

            //Where the FU- is the null reference exception!?!??!?!??!?
            NextTerrainPartMap.Add(new double[TerrainPartMap[k].xLength, TerrainPartMap[k].yLength]);

            NextTerrainPartMap[k] = TerrainPartMap[k].valArray;
        }
        */
        #endregion

        #region Air Particles Map Setup

        for (int k = 0; k < MoleculesNumber; k++)
        {
            AirPartMap.Add(new());

            for (int i = 0; i < LayerNumber; i++)
            {
                HexaGrid memory = new();

                memory.SetupHexagonalHexaGrid(Radius, out memory, out ExcludeCells);
                AirPartMap[k].Add(memory);

                AirPartMap[k][i].zHeight = i + 1;

                NextAirPartMap.Add(new());
                NextAirPartMap[k].Add(new double[AirPartMap[k][i].xLength, AirPartMap[k][i].yLength]);

                NextAirPartMap[k][i] = AirPartMap[k][i].valArray;
            }
        }

        #endregion

        #region Part Grids Test

        TestMemory.Add(new());
        TestMemory.Add(new());

        for (int i = 0; i <= LayerNumber; i++)
        {
            AirMapText.Add(new());
            AirMapText[i] = UtilsClass.CreateWorldText("", gameObject.transform, GridCenter + new Vector3(0, i * 10), 10, Color.white, TextAnchor.MiddleCenter);
            AirMapText2.Add(new());
            AirMapText2[i] = UtilsClass.CreateWorldText("", gameObject.transform, GridCenter + new Vector3(20, i * 10), 10, Color.white, TextAnchor.MiddleCenter);

            TestMemory[0].Add(0);
            TestMemory[1].Add(0);
        }

        #endregion
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        if (!AtmCompositionSet)
        {
            #region Atmospheric Data Setup

            AtmosphericMolarMass = GaleAtmosphericComposition[0] * 0.032 + GaleAtmosphericComposition[1] * 0.04401 + GaleAtmosphericComposition[2] * 0.03995 + GaleAtmosphericComposition[3] * 0.02802 + GaleAtmosphericComposition[4] * 0.018016;
            C.GaleAtmMM = AtmosphericMolarMass;
            AtmCompositionSet = true;

            #endregion
        }
        else if (PressMapScript.StaticPressureSet && HeatMapScript.StaticTempSet)
        {
            if (!StaticParticlesSet)
            {
                #region Set SOM

                xMem = AirPartMap[0][0].xLength;
                yMem = AirPartMap[0][0].yLength;
                CellVolume = PressMapScript.CellVolume;

                #endregion

                #region Particles from Static Pressure

                for (int x = 0; x < xMem; x++)
                    for (int y = 0; y < yMem; y++)
                        if (!ExcludeCells.Contains(new Vector2Int(x, y)))
                            for (int k = 0; k < MoleculesNumber; k++)
                                for (int i = 0; i < LayerNumber; i++)
                                {
                                    NextAirPartMap[k][i][x, y] = GaleAtmosphericComposition[k] * PressMapScript.StaticPressMap[i] * CellVolume / (C.R * C.GaleAvgTemp);
                                    AirPartMap[k][i].Assign(x, y, NextAirPartMap[k][i][x, y]);
                                }

                StaticParticlesSet = true;

                #endregion
            }
            else
            {
                #region Setup Grids Test

                for (int i = 0; i <= LayerNumber; i++)
                {
                    AirMapText[i].text = TestMemory[0][i].ToString();
                    AirMapText2[i].text = TestMemory[1][i].ToString();
                    TestMemory[0][i] = 0;
                    TestMemory[1][i] = 0;
                }

                #endregion

                #region Iterate Through Coordinates

                double VolumeExpansion;
                double ParticleTransfer = 0;
                //double TerParticlesNumber;
                double AirParticlesNumber;
                double DebugHeat, DebugPress;
                double deltan;

                for (int x = 0; x < xMem; x++)
                    for (int y = 0; y < yMem; y++)
                    {
                        if (!ExcludeCells.Contains(new Vector2Int(x, y)))
                        {
                            #region (unused) Terrain Particle Transfer
                            /*
                            TerParticlesNumber = 0;
                            for (int k = 0; k < MoleculesNumber; k++)
                            {
                                TerParticlesNumber += TerrainPartMap[k].valArray[x, y];
                            }

                            for (int k = 0; k < MoleculesNumber; k++)
                            {
                                DebugHeat = HeatMapScript.TerrainHeatMap.valArray[x, y];
                                DebugPress = PressMapScript.TerrainPressMap.valArray[x, y];

                                VolumeExpansion = CellVolume / (TerParticlesNumber * C.R * HeatMapScript.TerrainHeatMap.valArray[x, y] / PressMapScript.TerrainPressMap.valArray[x, y]);

                                ParticleTransfer = (1 - VolumeExpansion) * TerrainPartMap[k].valArray[x, y] / (CellNeighbours.Count + (LayerNumber != 0 ? 2 : 1));

                                //Grid Test - Total Particle Transfer
                                TestMemory[1][0] += ParticleTransfer * ParticleTransferCoefficient * (CellNeighbours.Count + (LayerNumber != 0 ? 2 : 1)) * Time.fixedDeltaTime;
                                TestMemory[1][LayerNumber + 1] += ParticleTransfer * ParticleTransferCoefficient * (CellNeighbours.Count + (LayerNumber != 0 ? 2 : 1)) * Time.fixedDeltaTime;

                                //Grid Test - Total Particles
                                TestMemory[0][0] += TerParticlesNumber;
                                TestMemory[0][LayerNumber + 1] += TerParticlesNumber;

                                foreach (Vector2Int Neighbour in CellNeighbours)
                                {
                                    NextTerrainPartMap[k][Neighbour.x, Neighbour.y] += deltan = ParticleTransfer * ParticleTransferCoefficient * Time.fixedDeltaTime;

                                    //Thermal exchange from particle transfer
                                    if (deltan > 0)
                                    {
                                        HeatMapScript.NextTerrainHeatMap[Neighbour.x, Neighbour.y] += ((HeatMapScript.TerrainHeatMap.valArray[x, y] - HeatMapScript.TerrainHeatMap.valArray[Neighbour.x, Neighbour.y]) / 2) * deltan / (deltan + TerrainPartMap[k].valArray[Neighbour.x, Neighbour.y]);
                                    }
                                    else
                                    {
                                        HeatMapScript.NextTerrainHeatMap[x, y] += ((HeatMapScript.TerrainHeatMap.valArray[Neighbour.x, Neighbour.y] - HeatMapScript.TerrainHeatMap.valArray[x, y]) / 2) * deltan / (deltan - TerrainPartMap[k].valArray[x, y]);
                                    }
                                }
                                if (LayerNumber != 0)
                                {
                                    NextAirPartMap[k][0][x, y] += deltan = ParticleTransfer * ParticleTransferCoefficient * Time.fixedDeltaTime;

                                    //Thermal exchange from particle transfer
                                    if (deltan > 0)
                                    {
                                        HeatMapScript.NextAirHeatMap[0][x, y] += ((HeatMapScript.TerrainHeatMap.valArray[x, y] - HeatMapScript.AirHeatMap[0].valArray[x, y]) / 2) * deltan / (deltan + AirPartMap[k][0].valArray[x, y]);
                                    }
                                }

                                NextTerrainPartMap[k][x, y] -= ParticleTransfer * (CellNeighbours.Count + 1) * ParticleTransferCoefficient * Time.fixedDeltaTime;
                            }
                            */
                            #endregion

                            CellNeighbours = AirPartMap[0][0].GetNeighbours(x, y, ExcludeCells, xMem, yMem);

                            #region Air Particle Transfer

                            for (int k = 0; k < MoleculesNumber; k++)
                                for (int i = 0; i < LayerNumber; i++)
                                {
                                    AirParticlesNumber = 0;
                                    for (int k2 = 0; k2 < MoleculesNumber; k2++)
                                    {
                                        AirParticlesNumber += AirPartMap[k2][i].valArray[x, y];
                                    }

                                    DebugHeat = HeatMapScript.AirHeatMap[i].valArray[x, y];
                                    DebugPress = PressMapScript.StaticPressMap[i];

                                    VolumeExpansion = CellVolume / (AirParticlesNumber * C.R * HeatMapScript.AirHeatMap[i].valArray[x, y] / PressMapScript.StaticPressMap[i]);

                                    ParticleTransfer = deltan = ParticleTransferCoefficient * Time.fixedDeltaTime * (1 - VolumeExpansion) * AirPartMap[k][i].valArray[x, y] / (CellNeighbours.Count + ((i == 0 || i == LayerNumber - 1) ? 2 : 3));

                                    //Grid Test - Total Particle Transfer
                                    TestMemory[1][i + 1] += ParticleTransfer * (CellNeighbours.Count + ((i == 0 || i == LayerNumber - 1) ? 2 : 3));
                                    TestMemory[1][0] += ParticleTransfer * (CellNeighbours.Count + ((i == 0 || i == LayerNumber - 1) ? 2 : 3));

                                    //Grid Test - Total Particles
                                    TestMemory[0][i + 1] += AirParticlesNumber;
                                    TestMemory[0][0] += AirParticlesNumber;

                                    foreach (Vector2Int Neighbour2 in CellNeighbours)
                                    {
                                        NextAirPartMap[k][i][Neighbour2.x, Neighbour2.y] += ParticleTransfer;

                                        //Thermal exchange from particle transfer
                                        if (deltan > 0)
                                        {
                                            HeatMapScript.NextAirHeatMap[i][Neighbour2.x, Neighbour2.y] += ((HeatMapScript.AirHeatMap[i].valArray[x, y] - HeatMapScript.AirHeatMap[i].valArray[Neighbour2.x, Neighbour2.y]) / 2) * deltan / (deltan + AirPartMap[k][i].valArray[Neighbour2.x, Neighbour2.y]);
                                        }
                                        else
                                        {
                                            HeatMapScript.NextAirHeatMap[i][x, y] += ((HeatMapScript.AirHeatMap[i].valArray[Neighbour2.x, Neighbour2.y] - HeatMapScript.AirHeatMap[i].valArray[x, y]) / 2) * deltan / (deltan - AirPartMap[k][i].valArray[x, y]);
                                        }
                                    }
                                    if (i != 0)
                                    {
                                        NextAirPartMap[k][i - 1][x, y] += ParticleTransfer;

                                        //Thermal exchange from particle transfer
                                        if (deltan > 0)
                                        {
                                            HeatMapScript.NextAirHeatMap[i - 1][x, y] += ((HeatMapScript.AirHeatMap[i].valArray[x, y] - HeatMapScript.AirHeatMap[i - 1].valArray[x, y]) / 2) * deltan / (deltan + AirPartMap[k][i - 1].valArray[x, y]);
                                        }
                                    }
                                    if (i != LayerNumber - 1)
                                    {
                                        NextAirPartMap[k][i + 1][x, y] += ParticleTransfer;

                                        //Thermal exchange from particle transfer
                                        if (deltan > 0)
                                        {
                                            HeatMapScript.NextAirHeatMap[i + 1][x, y] += ((HeatMapScript.AirHeatMap[i].valArray[x, y] - HeatMapScript.AirHeatMap[i + 1].valArray[x, y]) / 2) * deltan / (deltan + AirPartMap[k][i + 1].valArray[x, y]);
                                        }
                                    }

                                    NextAirPartMap[k][i][x, y] -= ParticleTransfer * (CellNeighbours.Count + ((i == 0 || i == LayerNumber - 1) ? 1 : 2));
                                }

                            #endregion

                        }
                    }
                #endregion

                #region Setup Current Maps

                for (int k = 0; k < MoleculesNumber; k++)
                    for (int i = 0; i < LayerNumber; i++)
                        AirPartMap[k][i].valArray = NextAirPartMap[k][i];

                #endregion

                
            }
        }
    }
}
