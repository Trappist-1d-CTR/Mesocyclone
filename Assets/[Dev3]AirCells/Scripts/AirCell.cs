using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AirCell
{
    /*  Definitions
            dynamic: different for every cell and for every moment in time
            static: same for all air cells
    */

    #region Variables

    #region Cell Values
    public Vector3 CellCenter;      //Center of the air cell, dynamic
    public double Moles;            //Number of particles, static
    public double Temperature;      //Temperature of the center, dynamic

    public Vector3 Velocity;        //Velocity of the air cell, dynamic
    public Vector3 Acceleration;    //Resulting due to forces, dynamic

    public double CellStaticVolume;
    public double CellDynamicVolume;
    public double CellCircleArea;
    public double CellRadius;
    public double CellHeight;

    public double StiffnessConstant;
    #endregion

    private double memory;

    #endregion

    #region Constructors

    public AirCell()
    {
        CellCenter = Velocity = Vector3.zero;
        Moles = 1000;
        Temperature = 300;
        StiffnessConstant = 0.5;
    }

    public AirCell(Vector3 CenterPoint, double nMoles)
    {
        CellCenter = CenterPoint;
        Moles = nMoles;
        Temperature = 300;
        Velocity = Vector3.zero;
        StiffnessConstant = 0.5;
    }

    public AirCell(Vector3 CenterPoint, double nMoles, double TempK)
    {
        CellCenter = CenterPoint;
        Moles = nMoles;
        Temperature = TempK;
        Velocity = Vector3.zero;
        StiffnessConstant = 0.5;
    }

    public AirCell(Vector3 CenterPoint, double nMoles, double TempK, Vector3 CellVelocity)
    {
        CellCenter = CenterPoint;
        Moles = nMoles;
        Temperature = TempK;
        Velocity = CellVelocity;
        StiffnessConstant = 0.5;
    }

    public AirCell(Vector3 CenterPoint, double nMoles, double TempK, Vector3 CellVelocity, double Stiffness)
    {
        CellCenter = CenterPoint;
        Moles = nMoles;
        Temperature = TempK;
        Velocity = CellVelocity;
        StiffnessConstant = Stiffness;
    }

    #endregion

    #region Public Functions

    #region Physics
    public void PerformVelocity()
    {
        CellCenter += Velocity * Time.fixedDeltaTime;
    }

    public void PerformVelocity(float deltaTime)
    {
        CellCenter += Velocity * deltaTime;
    }

    public void PerformAcceleration(Vector3 Acc)
    {
        Acceleration = Acc;
        Velocity += Acceleration * Time.fixedDeltaTime;
    }

    public void PerformAcceleration(Vector3 Acc, float deltaTime)
    {
        Acceleration = Acc;
        Velocity += Acceleration * deltaTime;
    }

    public void AccAlongVelocity(float Acc)
    {
        Acceleration = Velocity.normalized * Acc;
        Velocity += Acceleration * Time.deltaTime;
    }

    public void AccAlongVelocity(float Acc, float deltaTime)
    {
        Acceleration = Velocity.normalized * Acc;
        Velocity += Acceleration * deltaTime;
    }
    #endregion

    #region Volume
    public void SetSizeV(double V)
    {
        CellStaticVolume = V;
        CellHeight = System.Math.Pow(V, 0.3333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333);
        CellCircleArea = V / CellHeight;
        CellRadius = System.Math.Sqrt(CellCircleArea / System.Math.PI);
    }

    public void SetSizeVL(double V, double L)
    {
        CellStaticVolume = V;
        CellHeight = L;
        CellCircleArea = V / L;
        CellRadius = System.Math.Sqrt(CellCircleArea / System.Math.PI);
    }

    public void SetSizeRL(double R, double L)
    {
        CellRadius = R;
        CellHeight = L;
        CellCircleArea = R * R * System.Math.PI;
        CellStaticVolume = CellCircleArea * L;
    }
    #endregion

    #endregion
}
