using System.Collections.Generic;
using UnityEngine;

// I don't imagine anything inheriting AirCell
public sealed class AirCell
{
    /*  Definitions
            dynamic: different for every cell and for every moment in time
            static: same for all air cells
    */

    #region Variables

    #region Tick Stuff
    public float Accumulator { get; set; }
    public float TickRate { get; set; } = Tickable.DefaultTickRate; // set this to what's best
    #endregion

    #region Cell Values
    public Vector3 CellCenter;      //Center of the air cell, dynamic
    public float Moles;            //Number of particles, static
    public float Temperature;      //Temperature of the center, dynamic

    public Vector3 Velocity;        //Velocity of the air cell, dynamic
    public Vector3 Acceleration;    //Resulting due to forces, dynamic

    public float CellStaticVolume;
    // public float CellDynamicVolume; // never initialized or referenced : what value?
    public float CellCircleArea;
    public float CellRadius;
    public float CellHeight;

    public float StiffnessConstant;
    #endregion

    #endregion

    #region Constructor

    public AirCell
    (
        Vector3 CenterPoint = default,
        float nMoles = 1000,
        float TempK = 300,
        Vector3 CellVelocity = default,
        float Stiffness = 0.5f
    )
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
        if (Velocity.sqrMagnitude > 1e-10f)
        {  
            Acceleration = Velocity.normalized * Acc;
            Velocity += Acceleration * Time.fixedDeltaTime;   
        }
    }

    public void AccAlongVelocity(float Acc, float deltaTime)
    {
        // no idea why there wasn't a guard here
        if (Velocity.sqrMagnitude > 1e-10f)
        {
            Acceleration = Velocity.normalized * Acc;
            Velocity += Acceleration * deltaTime;
        }
    }
    #endregion

    #region Volume
    public void SetSizeV(float V)
    {
        CellStaticVolume = V;
        CellHeight = System.MathF.Pow(V, 1.0f / 3.0f);
        CellCircleArea = V / CellHeight;
        CellRadius = System.MathF.Sqrt(CellCircleArea / System.MathF.PI);
    }

    public void SetSizeVL(float V, float L)
    {
        CellStaticVolume = V;
        CellHeight = L;
        CellCircleArea = V / L;
        CellRadius = System.MathF.Sqrt(CellCircleArea / System.MathF.PI);
    }

    public void SetSizeRL(float R, float L)
    {
        CellRadius = R;
        CellHeight = L;
        CellCircleArea = R * R * System.MathF.PI;
        CellStaticVolume = CellCircleArea * L;
    }
    #endregion

    #endregion
}
