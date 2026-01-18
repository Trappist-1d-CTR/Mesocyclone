using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AGlobalValues : MonoBehaviour
{
    #region Global Values

    #region Universal Constants
    public double R;
    public double GreekS;
    public double NA;
    public double kb;
    public double Omega;
    public double G;
    #endregion

    #region Set Data

    #region Sun Data
    public double SunM;
    public double SunR;
    public double SunT;
    #endregion

    #region Earth Data
    public double EarthAtm;
    public double EarthM;
    public double EarthR;
    public double EarthG;
    public double EarthAtmMM;
    public double EarthAtmC; // TODO: what value?
    public double EarthAtmCp;
    public double EarthAvgTemp;
    public double EarthKarman;
    public double AU;
    public double EarthInsolation;
    public double EarthAlbedo;
    public double EarthTw, EarthTc;
    public double WaterMM;
    #endregion

    #region Star Data
    public double StarM;
    public double StarR;
    public double StarT;
    #endregion

    #region Gale Data
    public double GaleAtm;
    public double GaleM;
    public double GaleR;
    public double GaleG;
    public double GaleAtmMM;
    public double GaleAtmD;
    public double GaleAvgTemp;
    public double GaleKarman = 204210; //m
    public double GaleSemiMajor;
    public double GaleInsolation;
    #endregion

    #region Gale Atmosphere

    public List<double> GaleAtmosphericComposition = new(5) { 0.0002, 0.134, 0.417, 0.3628, 0.086 };

    public double GaleAtmCp;
    public double LapseRate;

    public double ZincMM;

    #endregion

    #region Simulation Data
    public double StiffK;
    public double MolarHeatCapacity;
    public double AtmAbsorptionCoefficient;
    public double AtmSpecificEmissivity;
    public double PlanetaryAlbedo;

    public double MolesPerCell;
    public double SimulationHeight;
    public double SimulationRadius;
    #endregion

    #endregion

    #region Orbital Parameters
    public double OrbitTime;
    public double a, b, e, c;
    public double OrbitalPeriod;
    public double Inclination, RiAscAscendNode, ArgPeriapsis;
    public Vector3 Perpendicular, OriginPosition;
    public double TrueAnomaly, MeanAnomaly, EccentricAnomaly;
    #endregion

    #region EOS-ESTM

    public AnimationCurve GlobalInsolation;
    public AnimationCurve ClearSkyTransparency;

    public double[] EOS_Temperature;
    public double[] EOS_Insolation;
    public double[] EOS_X;
    public double[] EOS_D;
    public double[] EOS_Diffusion;
    public int EOS_N;

    public double EOS_C = 5.0;
    public double EOS_AtmC = 5.0;
    public double EOS_A = 0.9;
    public double EOS_AtmA;
    public double EOS_KV;

    public double[] D_SOM;

    public int UpdateFrame;
    public int FrameCount;
    public float TimeStepMultiplier;
    public float OrbitStepMultiplier;

    private int i3;


    #endregion

    #endregion

    private void Awake()
    {
        #region Setup Universal Constants
        //Molar Gas Constant
        R = 8.31432; // Nm/molK
        //Stefan-Boltzmann Constant
        GreekS = 5.670374419 * Mathf.Pow(10f, -8); // W/(m^2 * K^4)
        //Avogadro Constant
        NA = 6.02214076 * Mathf.Pow(10f, 23);
        //Boltzmann Constant
        kb = 1.380649 * Mathf.Pow(10f, -23); // J/K
        //Universla Gravitational Constant
        G = 6.674 * Mathf.Pow(10f, -11); // m^3/(kg * s^2)
        //Omega Constant
        Omega = 0.567143290409783873; //Omega * e^(Omega) = 1
        #endregion

        #region Setup Sun Data
        SunM = 1.988416 * Mathf.Pow(10f, 30); // kg
        SunR = 6.957 * Mathf.Pow(10f, 8); // m
        SunT = 5800; // K
        #endregion

        #region Setup Earth Data
        EarthAtm = 101325; // Pa = N/m^2
        EarthM = 5.976 * Mathf.Pow(10f, 24); //kg
        EarthR = 6371000; //m
        EarthG = 9.80665; // m/s^2 = N/kg
        EarthAtmMM = 0.0289644; // kg/mol
        EarthAtmCp = 1006; //J/kgK
        EarthAvgTemp = 288; // K
        EarthKarman = 100000; // m
        AU = 149598 * Mathf.Pow(10f, 6); //m
        EarthInsolation = GreekS * System.Math.Pow(SunR / AU, 2) * System.Math.Pow(SunT, 4);
        EarthAlbedo = 0.3;
        EarthTw = 290;
        EarthTc = 265;
        WaterMM = 0.018015;
        #endregion

        #region Setup Star Variables
        StarM = 1.896 * SunM;
        StarR = 54.7 * SunR;
        StarT = 6307; // K
        #endregion

        #region Setup Gale Variables
        GaleAtm = 14.9 * EarthAtm;
        GaleM = 6.679 * EarthM;
        GaleR = 1.723 * EarthR;
        GaleG = 2.249 * EarthG;
        GaleAtmMM = 0.035138266; // kg/mol
        GaleAtmD = 4.879; // kg/m^3
        GaleAvgTemp = 1300; // K
        GaleKarman = 204210; // m
        GaleSemiMajor = AU * 1.556;
        GaleInsolation = GreekS * System.Math.Pow(StarR / GaleSemiMajor, 2) * System.Math.Pow(StarT, 4);
        #endregion

        #region Setup Orbital Parameters

        a = GaleSemiMajor;
        if (b == 0)
        {
            b = a * (1 - System.Math.Pow(e, 2));
        }
        else
        {
            e = System.Math.Sqrt(1 - System.Math.Pow(b / a, 2));
        }

        c = System.Math.Sqrt(System.Math.Pow(a, 2) - System.Math.Pow(b, 2));

        OrbitalPeriod = System.Math.Sqrt(System.Math.Pow(a, 3) * (4 * System.Math.Pow(System.Math.PI, 2) / (G * StarM)));

        Inclination *= 0.0174532924;
        RiAscAscendNode *= 0.0174532924;
        ArgPeriapsis *= 0.0174532924;

        Perpendicular = new Vector3((float)(-System.Math.Cos(RiAscAscendNode) * System.Math.Sin(Inclination)), (float)(System.Math.Cos(Inclination) * System.Math.Cos(RiAscAscendNode)), (float)(System.Math.Sin(RiAscAscendNode) * System.Math.Sin(Inclination)));

        //OriginPosition = Quaternion.AngleAxis((float)(ArgPeriapsis / 0.0174532924), Perpendicular) * -(Quaternion.AngleAxis((float)(RiAscAscendNode / 0.0174532924), Vector3.up) * ((float)(c * System.Math.Cos(Inclination)) * Vector3.right)); // leave out commented for now

        #endregion

        #region Setup Gale Atmosphere

        GaleAtmosphericComposition[3] = 1 - (GaleAtmosphericComposition[0] + GaleAtmosphericComposition[1] + GaleAtmosphericComposition[2] + GaleAtmosphericComposition[4]);

        //          Oxygen                                  Carbon Dioxide                            Argon                                     Nitrogen                                  Water Vapor
        GaleAtmMM = GaleAtmosphericComposition[0] * 0.032 + GaleAtmosphericComposition[1] * 0.04401 + GaleAtmosphericComposition[2] * 0.03995 + GaleAtmosphericComposition[3] * 0.02802 + GaleAtmosphericComposition[4] * 0.018016;

        ZincMM = 0.06538; //kg/mol

        GaleAtmCp = (GaleAtmosphericComposition[0] * 35.99 + GaleAtmosphericComposition[1] * 57.14 + GaleAtmosphericComposition[2] * 20.79 + GaleAtmosphericComposition[3] * 34.15 + GaleAtmosphericComposition[4] * 44.94) / GaleAtmMM;
        EOS_AtmC = (GaleAtmCp / EarthAtmCp) * (GaleAtm / EarthAtm) * (EarthG / GaleG) * EarthAtmC;

        LapseRate = -0.01; //K/m

        #endregion
    }

    public double CalculateOrbit(double T)
    {
        while (T > OrbitalPeriod)
        {
            T -= OrbitalPeriod;
        }
        MeanAnomaly = 2 * System.Math.PI * T / OrbitalPeriod;
        EccentricAnomaly = MeanAnomaly;
        double delta;
        for (int i = 0; i < 100; i++)
        {
            EccentricAnomaly -= delta = (EccentricAnomaly - (e * System.Math.Sin(EccentricAnomaly)) - MeanAnomaly) / (1 - (e * System.Math.Cos(EccentricAnomaly)));

            if (delta < (double)System.Math.Pow((double)10.0, (double)-6.0))
            {
                //Debug.Log("Convergence Successful: " + EccentricAnomaly);
                break;
            }
        }

        double x = a * System.Math.Cos(EccentricAnomaly);
        double z = b * System.Math.Sin(EccentricAnomaly);

        //Elliptical Orbit - Inclination Along Z Axis
        Vector3 ioCoordinates = new((float)(x * System.Math.Cos(Inclination)), (float)(x * System.Math.Sin(Inclination)), (float)z);
        //Debug.Log("ioCoordinates: " + ioCoordinates);

        //Right Ascention Ascending Node - Rotate Orbit Around Y Axis
        Vector3 raanCoordinates = Quaternion.AngleAxis((float)(RiAscAscendNode / 0.0174532924), Vector3.up) * ioCoordinates;
        //Debug.Log("raanCoordinates: " + raanCoordinates);

        //Argument of Periapsis - Rotate Orbit Around Perpendicular
        Vector3 apCoordinates = Quaternion.AngleAxis((float)(ArgPeriapsis / 0.0174532924), Perpendicular) * raanCoordinates;
        //Debug.Log("apCoordinates: " + apCoordinates);

        return apCoordinates.magnitude;
    }


    #region General Functions

    public double StaticPressureAtHeight(double Height)
    {
        return GaleAtm * System.Math.Exp(-GaleG * GaleAtmMM * Height / (R * GaleAvgTemp));
    }

    public double GalePressGivenTemp(double Temp)
    {
        return GaleAtm * System.Math.Exp(-GaleG * GaleAtmMM * 10439 / (R * Temp));
    }

    public double GaleTempGivenHeight(double Height)
    {
        return GaleAvgTemp + (LapseRate * Height);
    }

    public double EarthPressGivenTemp(double Temp)
    {
        return EarthAtm * System.Math.Exp(-EarthG * EarthAtmMM * 5846 / (R * Temp));
    }

    public double CalculateMeanInsolation(double Latitude)
    {
        return GreekS * System.Math.Pow(StarR / GaleSemiMajor, 2) * System.Math.Pow(StarT, 4) * System.Math.Cos(0.01745329 * Latitude) * (1 + (EOS_AtmA / (1 - EOS_AtmA)));
    }

    public double TransparencyAtHeight(double Height)
    {
        //   Debug.Log("t = " + System.Math.Exp(-EOS_KV * AvgDensityAtHeight(Height) * (GaleKarman - Height)) + " ; ^(" + (-EOS_KV * AvgDensityAtHeight(Height) * (GaleKarman - Height)) + ")");
        return System.Math.Exp(-EOS_KV * AvgDensityAtHeight(Height) * (GaleKarman - Height));
    }

    public double AvgDensityAtHeight(double Height)
    {
        //   Debug.Log("p = " + (Height - GaleKarman) / ((-StaticPressureAtHeight(Height) / GaleG) + (13519.1 * GaleAtmMM / (R * GaleAvgTemp))));
        if (Height >= GaleKarman)
            return 0;
        return ((-StaticPressureAtHeight(Height) / GaleG) + (13519.1 * GaleAtmMM / (R * GaleAvgTemp))) / (Height - GaleKarman);
    }

    #endregion


    #region EOS-ESTM Climate Simulation

    public void GlobalInsolationCurve(double T) // x = lat in degrees ; y = W/m^s
    {
        double mem = CalculateStaticInsolation(T) * (1 - EOS_A);

        if (GlobalInsolation.keys.Length == 0)
        {
            _ = GlobalInsolation.AddKey(new Keyframe(0, (float)mem, 0, 0));
            _ = GlobalInsolation.AddKey(new Keyframe(90, 0, (float)(-0.01745329 * mem), 0));
            _ = GlobalInsolation.AddKey(new Keyframe(180, 0, 0, 0));
        }
        else
        {
            _ = GlobalInsolation.MoveKey(0, new Keyframe(0, (float)mem, 0, 0));
            _ = GlobalInsolation.MoveKey(1, new Keyframe(90, 0, (float)(-0.01745329 * mem), 0));
        }

        //Debug.Log(mem);
    }

    public double CalculateStaticInsolation(double T)
    {
        return GreekS * System.Math.Pow(StarR / CalculateOrbit(T), 2) * System.Math.Pow(StarT, 4);
    }

    public void ClearSkyTransparencyCurve() // x = height ; y = % (it assumes an Insolation of 1 W/m^2)
    {
        double a = 13519.1 * GaleAtmMM / (R * GaleAvgTemp);
        double b = -GaleG * GaleAtmMM / (R * GaleAvgTemp);
        double d = -GaleAtm / GaleG;

        double mem;
        double h;

        if (ClearSkyTransparency.keys.Length == 0)
        {
            h = 0;
            mem = EOS_KV * b * d * System.Math.Exp(EOS_KV * (d + a));
            _ = ClearSkyTransparency.AddKey(new Keyframe((float)h, (float)TransparencyAtHeight(h), (float)mem, (float)mem));
            for (int i = 0; i < 200; i++)
            {
                h = (i + 1) * 1000;
                mem = EOS_KV * b * d * System.Math.Exp((b * h) - (EOS_KV * (GaleKarman - h) * ((d * System.Math.Exp(b * h)) + a) / (h - GaleKarman)));
                _ = ClearSkyTransparency.AddKey(new Keyframe((float)h, (float)TransparencyAtHeight(h), (float)mem, (float)mem));
            }
            h = GaleKarman;
            mem = EOS_KV * b * d * System.Math.Exp((b * GaleKarman) - (EOS_KV * ((d * System.Math.Exp(b * GaleKarman)) + a)));
            _ = ClearSkyTransparency.AddKey(new Keyframe((float)h, 1, (float)mem, (float)mem));
        }
    }

    public double[] LatitudesMeanInsolation(int N)
    {
        double[] Result = new double[N];

        double MeanInsolation = GreekS * System.Math.Pow(StarR / GaleSemiMajor, 2) * System.Math.Pow(StarT, 4);

        for (int i = 0; i < N; i++)
        {
            Result[i] = MeanInsolation * System.Math.Cos(0.01745329 * ((i + 0.5) * (180 / N))) * (1 + (EOS_AtmA / (1 - EOS_AtmA)));
        }

        return Result;
    }

    public double MeanInsolationAtLatitude(int N, int i, bool Earth)
    {
        double MeanInsolation = !Earth
            ? GreekS * System.Math.Pow(StarR / GaleSemiMajor, 2) * System.Math.Pow(StarT, 4)
            : GreekS * System.Math.Pow(SunR / AU, 2) * System.Math.Pow(SunT, 4);

        return MeanInsolation * System.Math.Cos(0.01745329 * ((i + 0.5) * (180 / N))) * (1 + (EOS_AtmA / (1 - EOS_AtmA)));
    }

    public double CalculateD(int N, int i)
    {
        double q = 0.5; //Relative humidity

        int Tw = (int)System.Math.Round((double)((28 / (180 / N)) - (90 / N)));
        Tw = (Tw >= 0) ? ((Tw < N) ? Tw : N) : 0;
        int Tc = (int)System.Math.Round((double)((68 / (180 / N)) - (90 / N)));
        Tc = (Tc >= 0) ? ((Tc < N) ? Tc : N) : 0;

        if (D_SOM[0] == 0)
        {
            D_SOM[0] = System.Math.Pow(GaleR / EarthR, -(double)((double)6.0 / (double)5.0)) *
                System.Math.Pow((GaleAtm / EarthAtm) / (GaleG / EarthG), (double)((double)2.0 / (double)5.0)) *
                System.Math.Pow( /* Angular Rotation Speed */ 0.00000014124515 / 0.00007292123517, -(double)((double)4.0 / (double)5.0));
        }

        double Sdry = (GaleAtmCp / EarthAtmCp) * D_SOM[0] * System.Math.Pow((EOS_Insolation[i] / MeanInsolationAtLatitude(N, i, true)) *
            ((1 - EOS_A) / (1 - EarthAlbedo)) *
            (((EOS_Temperature[Tw] - EOS_Temperature[Tc]) / EOS_Temperature[Tw]) / ((EarthTw - EarthTc) / EarthTw)), (double)((double)3.0 / (double)5.0));

        if (D_SOM[1] == 0)
        {
            D_SOM[1] = (ZincMM / WaterMM) / ((GaleAtmCp / EarthAtmCp) * (GaleAtmMM / EarthAtmMM) * (GaleAtm / EarthAtm));
        }

        double Smd = (q / 0.75) * D_SOM[1] * ((GalePressGivenTemp(EOS_Temperature[Tw]) - GalePressGivenTemp(EOS_Temperature[Tc])) / (EarthPressGivenTemp(EarthTw) - EarthPressGivenTemp(EarthTc))) / ((EOS_Temperature[Tw] - EOS_Temperature[Tc]) / (EarthTw - EarthTc));

        double RelD = Sdry * (1 + (0.7 * Smd)) / (1 + 0.7);

        double Result = RelD * 0.66; // W/m^2K

        return Result;
    }

    private void Start()
    {
        #region List Setups
        EOS_Temperature = new double[EOS_N];
        EOS_Diffusion = new double[EOS_N];
        EOS_Insolation = new double[EOS_N];
        EOS_X = new double[Mathf.RoundToInt(EOS_N * (EOS_N - 1) / 2)];
        EOS_D = new double[1];

        D_SOM = new double[2];
        #endregion

        #region Variable Setups
        if (UpdateFrame <= EOS_N) UpdateFrame = EOS_N + 1;

        i3 = 0;
        #endregion

        #region Calculate Global Curves
        GlobalInsolation = new AnimationCurve();
        ClearSkyTransparency = new AnimationCurve();

        GlobalInsolationCurve(OrbitTime);
        ClearSkyTransparencyCurve();
        #endregion

        #region Starting Heat
        for (int i = 0; i < EOS_N; i++)
        {
            EOS_Insolation = LatitudesMeanInsolation(EOS_N);

            EOS_Temperature[i] = EOS_Insolation[i] * (1 - EOS_A) * 100 / EOS_C;
        }
        #endregion
    }

    private void FixedUpdate()
    {
        if (FrameCount < EOS_N)
        {
            if (EOS_D[0] == 0)
            {
                EOS_D[0] = CalculateD(EOS_N, FrameCount) * (1 - System.Math.Pow(System.Math.Sin(0.01745329 * ((FrameCount + 0.5) * (180 / EOS_N))), 2));
            }

            for (int i2 = FrameCount + 1; i2 < EOS_N; i2++)
            {
                i3 = FrameCount * EOS_N - (FrameCount * (FrameCount + 1) / 2) + i2 - FrameCount - 1;

                EOS_X[i3] = System.Math.Sin(0.01745329 * (180 / EOS_N) * (i2 - FrameCount));
            }
        }


        if (FrameCount == UpdateFrame)
        {
            FrameCount = 0;

            #region Planetary Calculations

            OrbitTime += Time.fixedDeltaTime * TimeStepMultiplier * UpdateFrame * OrbitStepMultiplier;

            GlobalInsolationCurve(OrbitTime);

            #endregion

            EOS_Insolation = LatitudesMeanInsolation(EOS_N);

            for (int i = 0; i < EOS_N; i++)
            {
                EOS_Temperature[i] += EOS_Insolation[i] * (1 - EOS_A) * (Time.fixedDeltaTime * TimeStepMultiplier * UpdateFrame) / EOS_C;
                
                for (int i2 = i + 1; i2 < EOS_N; i2++)
                {
                    int index = (i * EOS_N) - (i * (i + 1) / 2) + i2 - i - 1;
                    
                    double mem = (EOS_D[0] * ((EOS_Temperature[i2] - EOS_Temperature[i]) * 
                                 System.Math.Pow(EOS_X[index], 2))) / 2;
                    
                    EOS_Diffusion[i] += mem * Time.fixedDeltaTime;
                    EOS_Diffusion[i2] -= mem * Time.fixedDeltaTime;
                    
                    EOS_Temperature[i] += mem * (Time.fixedDeltaTime * TimeStepMultiplier * UpdateFrame) / EOS_C;
                    EOS_Temperature[i2] -= mem * (Time.fixedDeltaTime * TimeStepMultiplier * UpdateFrame) / EOS_C;
                }
            }

            EOS_D[0] = 0;
        }

        FrameCount++;
    }

    #endregion
}
