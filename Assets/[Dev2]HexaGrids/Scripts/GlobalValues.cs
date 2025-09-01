using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalValues : MonoBehaviour
{
    #region Global Values

    #region Universal Constants
    public double R = 8.31432; // Nm/molK
    #endregion

    #region Earth Data
    public double EarthAtm = 101325; // Pa = N/m^2
    public double EarthM = 5.976 * Mathf.Pow(10f, 24); //kg
    public double EarthG = 9.80665; // m/s^2 = N/kg
    public double EarthAtmMM = 0.0289644; // kg/mol
    public double EarthAvgTemp = 288; // K
    public double EarthKarman = 100000; // m
    #endregion

    #region Gale Data
    public double GaleAtm;
    public double GaleM;
    public double GaleG;
    public double GaleAtmMM;
    public double GaleAvgTemp;
    public double GaleKarman = 204210; //m
    #endregion

    #region Gale Atmosphere

    #endregion

    #region Simulation Data
    public int GridMapSize = 3;
    public int LayerNumber;
    public float CellHeight;
    #endregion

    #endregion

    private void Start()
    {
        #region Setup Gale Variables
        GaleAtm = 14.9 * EarthAtm;
        GaleM = 6.679 * EarthM;
        GaleG = 2.249 * EarthG;
        GaleAtmMM = 0.035138266; // kg/mol
        GaleAvgTemp = 1300; // K
        GaleKarman = 204210; // m
        #endregion
    }
}
