using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class InverseDistanceWeighting
{
    public static float R = 1000;
    public static Vector3 Query;
    public static List<int> Indexes = new();

    public static double[] SUM_wu;
    public static double SUM_w;

    public static bool FollowDrone;

    public static float[] Values { get; private set; }
    private static bool LockValues;

    public static void DronePos(Vector3 pos)
    {
        Query = FollowDrone ? new Vector3(0, pos.y, 0) : pos;
    }

    public static void Add(int Index)
    {
        //if (!Indexes.Contains(Index))
            Indexes.Add(Index);
    }

    public static void Remove(int Index)
    {
        Indexes.Remove(Index);
    }

    public static void BeginInterp()
    {
        SUM_wu = new[] { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };
        SUM_w = 0;
        LockValues = false;
    }

    public static void InterpolationStep(Vector3 Xi, float[] u)
    {
        if (R == 0) throw new System.Exception("Interpolation Step attempt with null radius");

        if (!LockValues)
        {
            double d = Vector3.Distance(Query, Xi);
            if (d == 0)
            {
                Values = new float[SUM_wu.Length];
                Values = u;

                LockValues = true;
            }
            double w = System.Math.Pow(System.Math.Max(R - d, 0) / (R * d), 2);

            SUM_w += w;

            for (int i = 0; i < SUM_wu.Length; i++)
            {
                SUM_wu[i] += w * u[i];
            }
        }

        return;
    }

    public static void BroadcastInterp(bool TerrainAlreadyInterpolated)
    {
        if (SUM_w == 0) throw new System.Exception("Interpolation attempt with null weight");

        if (!LockValues)
        {
            Values = new float[SUM_wu.Length];

            for (int i = 0; i < SUM_wu.Length; i++)
            {
                Values[i] = (float)(SUM_wu[i] / SUM_w);
            }
        }

        //Debug.Log(Values[0] + " ; " + Values[1] + " ; " + Values[2]);

        if (!TerrainAlreadyInterpolated && Query.y < 10)
        {
            Values[0] *= Query.y / 10;
            Values[1] *= Query.y / 10;
            Values[2] *= Query.y / 10;
        }
    }
}
