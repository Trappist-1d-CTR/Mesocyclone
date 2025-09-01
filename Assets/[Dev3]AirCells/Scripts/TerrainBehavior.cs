using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainBehavior : MonoBehaviour
{
    #region Variables

    #region Terrain Properties
    public double Area;
    public double Temperature;
    public double Moles;
    public double MolarWeight = 0.0401;
    public double MassPerSquareMeter;
    public double Albedo;
    public double Emissivity;
    public double HeatCapacity;
    public double HeatInertia;
    public double LocalLatitude;
    #endregion

    #region Reference Scripts
    private AGlobalValues C;
    #endregion

    #endregion

    void Awake()
    {
        //Get Script Reference
        C = GameObject.FindGameObjectWithTag("GameController").GetComponent<AGlobalValues>();
    }

    void FixedUpdate()
    {
        Temperature += C.CalculateStaticInsolation(LocalLatitude) * (1 - Albedo) * C.TransparencyAtHeight(0) / (HeatCapacity * MassPerSquareMeter) * Time.fixedDeltaTime;

        // To do here: Radiative Cooling

        // To do in cell behavior: Higher Insolation from Terrain Albedo; Heating from Terrain; Conduction with Terrain
    }
}
