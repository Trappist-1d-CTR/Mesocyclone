using System.Collections.Generic;
using UnityEngine;

namespace Mesocyclone
{
    public sealed class AGlobalValues : MonoBehaviour
    {
        #region Rant
        // Astraa, wh- why...
        // Like seriously-
        // All these values are constant, but i cant use const since they're assigned in Awake()
        // I can't use readonly because Awake() is not a constructor and unity fucking hates constructors
        // and any traditional form of C# programming
        // i can't make private setter properties because then everything becomes verbose AS FUCK
        // And ontop of that properties just don't appear in the inspector for god knows what
        // and i'll have to spam [field: SerializeField] and/or double the length of this file with fucking lambd
        //
        // and sure, initializing these on declaration works... mostly
        // thing is if you assign at decleration, you have to worry about fucking when the assignments are listed
        // like i can't reference a value that is assigned below the wall of fields (if that makes any sense)
        // like, WHAT DO I DO!?!?!???!?!?

        // for anyone reading this, PLEASE DO NOT CHANGE THESE VALUES
        // JUST TRUST ME
        // *tho tbh this does open up some super weird math-manipulating mods lol*
        #endregion

        #region Global Values

        #region Universal Constants
        public float R;
        public float GreekS;
        public float NA;
        public float kb;
        public float Omega;
        public float G;
        #endregion

        #region Set Data

        #region Sun Data
        public float SunM;
        public float SunR;
        public float SunT;
        #endregion

        #region Earth Data
        public float EarthAtm;
        public float EarthM;
        public float EarthR;
        public float EarthG;
        public float EarthAtmMM;
        public float EarthAtmC;
        public float EarthAtmCp;
        public float EarthAvgTemp;
        public float EarthKarman;
        public float AU;
        public float EarthInsolation;
        public float EarthAlbedo;
        public float EarthTw, EarthTc;
        public float WaterMM;
        #endregion

        #region Star Data
        public float StarM;
        public float StarR;
        public float StarT;
        #endregion

        #region Gale Data
        public float GaleAtm;
        public float GaleM;
        public float GaleR;
        public float GaleG;
        public float GaleAtmMM;
        public float GaleAtmD;
        public float GaleAvgTemp;
        public float GaleKarman; //m
        public float GaleSemiMajor;
        public float GaleInsolation;
        #endregion

        #region Gale Atmosphere

        public List<float> GaleAtmosphericComposition = new(5)
        {
            0.0002f,
            0.134f,
            0.417f,
            0.3628f,
            0.086f
        };

        public float GaleAtmCp;
        public float LapseRate;

        public float ZincMM;

        #endregion

        #region Simulation Data
        public float StiffK;
        public float MolarHeatCapacity;
        public float AtmAbsorptionCoefficient;
        public float AtmSpecificEmissivity;
        public float PlanetaryAlbedo;

        public float MolesPerCell;
        public float SimulationHeight;
        public float SimulationRadius;
        #endregion

        #endregion

        #region Orbital Parameters
        public float OrbitTime;
        public float a, b, e, c;
        public float OrbitalPeriod;
        public float Inclination, RiAscAscendNode, ArgPeriapsis;
        public Vector3 Perpendicular, OriginPosition; // FINALLY SOMETHING THATS NOT A FLOAT :'D
        public float TrueAnomaly, MeanAnomaly, EccentricAnomaly; // AAAAAAAAAAAAAAAAAAAAAAAAAAAAA-
        #endregion

        #region Shaders
        public Material Atmosphere;
        #endregion

        #region Old EOS-ESTM Attempt

        public AnimationCurve GlobalInsolation;
        public AnimationCurve ClearSkyTransparency;

        public float[] EOS_Temperature;
        public float[] EOS_Insolation;
        public float[] EOS_X;
        public float[] EOS_D;
        public float[] EOS_Diffusion;
        public int EOS_N;

        // why are these the only declared and initialized fields -.-
        public float EOS_C = 5f;
        public float EOS_AtmC = 5f;
        public float EOS_A = 0.9f;
        public float EOS_AtmA;
        public float EOS_KV;

        public float[] D_SOM;

        public int UpdateFrame;
        public int FrameCount;
        public float TimeStepMultiplier;
        public float OrbitStepMultiplier;

        private int i3;


        #endregion

        public bool SetupComplete = false;

        #endregion

        private void Awake()
        {
            SetupComplete = false;

            #region Setup Universal Constants
            //Molar Gas Constant
            R = 8.31432f; // Nm/molK
                          //Stefan-Boltzmann Constant
            GreekS = 5.670374419f * Mathf.Pow(10f, -8f); // W/(m^2 * K^4)
                                                         //Avogadro Constant
            NA = 6.02214076f * Mathf.Pow(10f, 23f);
            //Boltzmann Constant
            kb = 1.380649f * Mathf.Pow(10f, -23f); // J/K
                                                   //Universla Gravitational Constant
            G = 6.674f * Mathf.Pow(10f, -11f); // m^3/(kg * s^2)
                                               //Omega Constant
            Omega = 0.567143290409783873f; //Omega * e^(Omega) = 1
            #endregion

            #region Setup Sun Data
            SunM = 1.988416f * Mathf.Pow(10f, 30f); // kg
            SunR = 6.957f * Mathf.Pow(10f, 8f); // m
            SunT = 5800f; // K
            #endregion

            #region Setup Earth Data
            EarthAtm = 101325f; // Pa = N/m^2
            EarthM = 5.976f * Mathf.Pow(10f, 24f); //kg
            EarthR = 6371000f; //m
            EarthG = 9.80665f; // m/s^2 = N/kg
            EarthAtmMM = 0.0289644f; // kg/mol
            EarthAtmCp = 1006f; //J/kgK
            EarthAvgTemp = 288f; // K
            EarthKarman = 100000f; // m
            AU = 149598f * Mathf.Pow(10f, 6f); //m
            EarthInsolation = GreekS * System.MathF.Pow(SunR / AU, 2f) * System.MathF.Pow(SunT, 4f);
            EarthAlbedo = 0.3f;
            EarthTw = 290f;
            EarthTc = 265f;
            WaterMM = 0.018015f;
            #endregion

            #region Setup Star Variables
            StarM = 1.896f * SunM;
            StarR = 54.7f * SunR;
            StarT = 6307f; // K
            #endregion

            #region Setup Gale Variables
            GaleAtm = 14.9f * EarthAtm;
            GaleM = 6.679f * EarthM;
            GaleR = 1.723f * EarthR;
            GaleG = 2.249f * EarthG;
            GaleAtmMM = 0.035138266f; // kg/mol
            GaleAtmD = 4.879f; // kg/m^3
            GaleAvgTemp = 1300f; // K
            GaleKarman = 204210f; // m
            GaleSemiMajor = AU * 1.556f;
            GaleInsolation = GreekS * System.MathF.Pow(StarR / GaleSemiMajor, 2f) * System.MathF.Pow(StarT, 4f);
            #endregion

            #region Setup Orbital Parameters

            a = GaleSemiMajor;
            if (b == 0)
            {
                b = a * (1 - System.MathF.Pow(e, 2f));
            }
            else
            {
                e = System.MathF.Sqrt(1 - System.MathF.Pow(b / a, 2f));
            }

            c = System.MathF.Sqrt(System.MathF.Pow(a, 2) - System.MathF.Pow(b, 2f));

            OrbitalPeriod = System.MathF.Sqrt(System.MathF.Pow(a, 3) * (4 * System.MathF.Pow(System.MathF.PI, 2f) / (G * StarM)));

            Inclination *= 0.0174532924f;
            RiAscAscendNode *= 0.0174532924f;
            ArgPeriapsis *= 0.0174532924f;

            Perpendicular = new Vector3(-System.MathF.Cos(RiAscAscendNode) * System.MathF.Sin(Inclination), System.MathF.Cos(Inclination) * System.MathF.Cos(RiAscAscendNode), System.MathF.Sin(RiAscAscendNode) * System.MathF.Sin(Inclination));

            //OriginPosition = Quaternion.AngleAxis((ArgPeriapsis / 0.0174532924f), Perpendicular) * -(Quaternion.AngleAxis((RiAscAscendNode / 0.0174532924f), Vector3.up) * (c * System.MathF.Cos(Inclination)) * Vector3.right); // leave out commented for now

            #endregion

            #region Setup Gale Atmosphere

            GaleAtmosphericComposition[3] = 1f - (GaleAtmosphericComposition[0] + GaleAtmosphericComposition[1] + GaleAtmosphericComposition[2] + GaleAtmosphericComposition[4]);

            //          Oxygen                                  Carbon Dioxide                            Argon                                     Nitrogen                                  Water Vapor
            GaleAtmMM = GaleAtmosphericComposition[0] * 0.032f + GaleAtmosphericComposition[1] * 0.04401f + GaleAtmosphericComposition[2] * 0.03995f + GaleAtmosphericComposition[3] * 0.02802f + GaleAtmosphericComposition[4] * 0.018016f;

            ZincMM = 0.06538f; //kg/mol

            GaleAtmCp = (GaleAtmosphericComposition[0] * 35.99f + GaleAtmosphericComposition[1] * 57.14f + GaleAtmosphericComposition[2] * 20.79f + GaleAtmosphericComposition[3] * 34.15f + GaleAtmosphericComposition[4] * 44.94f) / GaleAtmMM;
            EOS_AtmC = (GaleAtmCp / EarthAtmCp) * (GaleAtm / EarthAtm) * (EarthG / GaleG) * EarthAtmC;

            LapseRate = -0.01f; //K/m

            #endregion


            #region Expose Values to Shaders
            Atmosphere.SetFloat("_G", GaleG);
            Atmosphere.SetFloat("_SurfaceP", GaleAtm);
            Atmosphere.SetFloat("_MM", GaleAtmMM);
            Atmosphere.SetFloat("_R", R);
            Atmosphere.SetFloat("_AvgT", GaleAvgTemp);
            Atmosphere.SetFloat("_Karman", GaleKarman);
            Atmosphere.SetVector("_PlanetPos", new Vector3(0, -GaleR, 0));
            #endregion

            SetupComplete = true;
        }

        public float CalculateOrbit(float T)
        {
            while (T > OrbitalPeriod)
            {
                T -= OrbitalPeriod;
            }
            MeanAnomaly = 2 * System.MathF.PI * T / OrbitalPeriod;
            EccentricAnomaly = MeanAnomaly;
            float delta;
            for (int i = 0; i < 100; i++)
            {
                EccentricAnomaly -= delta = (EccentricAnomaly - (e * System.MathF.Sin(EccentricAnomaly)) - MeanAnomaly) / (1 - (e * System.MathF.Cos(EccentricAnomaly)));

                if (delta < System.MathF.Pow(10, -6))
                {
                    //Debug.Log("Convergence Successful: " + EccentricAnomaly);
                    break;
                }
            }

            float x = a * System.MathF.Cos(EccentricAnomaly);
            float z = b * System.MathF.Sin(EccentricAnomaly);

            //Elliptical Orbit - Inclination Along Z Axis
            Vector3 ioCoordinates = new(x * System.MathF.Cos(Inclination), x * System.MathF.Sin(Inclination), z);
            //Debug.Log("ioCoordinates: " + ioCoordinates);

            //Right Ascention Ascending Node - Rotate Orbit Around Y Axis
            Vector3 raanCoordinates = Quaternion.AngleAxis((RiAscAscendNode / 0.0174532924f), Vector3.up) * ioCoordinates;
            //Debug.Log("raanCoordinates: " + raanCoordinates);

            //Argument of Periapsis - Rotate Orbit Around Perpendicular
            Vector3 apCoordinates = Quaternion.AngleAxis((ArgPeriapsis / 0.0174532924f), Perpendicular) * raanCoordinates;
            //Debug.Log("apCoordinates: " + apCoordinates);

            return apCoordinates.magnitude;
        }


        #region General Functions

        public float StaticPressureAtHeight(float Height)
        {
            return GaleAtm * System.MathF.Exp(-GaleG * GaleAtmMM * Height / (R * GaleAvgTemp));
        }

        public float DensityAtHeight(float Height)
        {
            return GaleAtmD * System.MathF.Exp(-GaleG * GaleAtmMM * Height / (R * GaleAvgTemp));
        }

        public float GalePressGivenTemp(float Temp)
        {
            return GaleAtm * System.MathF.Exp(-GaleG * GaleAtmMM * 10439 / (R * Temp));
        }

        public float GaleTempGivenHeight(float Height)
        {
            return GaleAvgTemp + (LapseRate * Height);
        }

        public float EarthPressGivenTemp(float Temp)
        {
            return EarthAtm * System.MathF.Exp(-EarthG * EarthAtmMM * 5846 / (R * Temp));
        }

        public float CalculateMeanInsolation(float Latitude)
        {
            return GreekS * System.MathF.Pow(StarR / GaleSemiMajor, 2) * System.MathF.Pow(StarT, 4) * System.MathF.Cos(0.01745329f * Latitude) * (1 + (EOS_AtmA / (1 - EOS_AtmA)));
        }

        public float TransparencyAtHeight(float Height)
        {
            //   Debug.Log("t = " + System.MathF.Exp(-EOS_KV * AvgDensityAtHeight(Height) * (GaleKarman - Height)) + " ; ^(" + (-EOS_KV * AvgDensityAtHeight(Height) * (GaleKarman - Height)) + ")");
            return System.MathF.Exp(-EOS_KV * AvgDensityAtHeight(Height) * (GaleKarman - Height));
        }

        public float AvgDensityAtHeight(float Height)
        {
            //   Debug.Log("p = " + (Height - GaleKarman) / ((-StaticPressureAtHeight(Height) / GaleG) + (13519.1 * GaleAtmMM / (R * GaleAvgTemp))));
            if (Height >= GaleKarman)
                return 0;
            return ((-StaticPressureAtHeight(Height) / GaleG) + (13519.1f * GaleAtmMM / (R * GaleAvgTemp))) / (Height - GaleKarman);
        }

        #endregion


        #region EOS-ESTM Climate Simulation

        public void GlobalInsolationCurve(float T) // x = lat in degrees ; y = W/m^s
        {
            float mem = CalculateStaticInsolation(T) * (1 - EOS_A);

            if (GlobalInsolation.keys.Length == 0)
            {
                _ = GlobalInsolation.AddKey(new Keyframe(0, mem, 0, 0));
                _ = GlobalInsolation.AddKey(new Keyframe(90, 0, -0.01745329f * mem, 0));
                _ = GlobalInsolation.AddKey(new Keyframe(180, 0, 0, 0));
            }
            else
            {
                _ = GlobalInsolation.MoveKey(0, new Keyframe(0, (float)mem, 0, 0));
                _ = GlobalInsolation.MoveKey(1, new Keyframe(90, 0, (float)(-0.01745329 * mem), 0));
            }

            //Debug.Log(mem);
        }

        public float CalculateStaticInsolation(float T)
        {
            return GreekS * System.MathF.Pow(StarR / CalculateOrbit(T), 2) * System.MathF.Pow(StarT, 4);
        }

        public void ClearSkyTransparencyCurve() // x = height ; y = % (it assumes an Insolation of 1 W/m^2)
        {
            float a = 13519.1f * GaleAtmMM / (R * GaleAvgTemp);
            float b = -GaleG * GaleAtmMM / (R * GaleAvgTemp);
            float d = -GaleAtm / GaleG;

            float mem;
            float h;

            if (ClearSkyTransparency.keys.Length == 0)
            {
                h = 0;
                mem = EOS_KV * b * d * System.MathF.Exp(EOS_KV * (d + a));
                _ = ClearSkyTransparency.AddKey(new Keyframe(h, TransparencyAtHeight(h), mem, mem));
                for (int i = 0; i < 200; i++)
                {
                    h = (i + 1) * 1000;
                    mem = EOS_KV * b * d * System.MathF.Exp((b * h) - (EOS_KV * (GaleKarman - h) * ((d * System.MathF.Exp(b * h)) + a) / (h - GaleKarman)));
                    _ = ClearSkyTransparency.AddKey(new Keyframe(h, TransparencyAtHeight(h), mem, mem));
                }
                h = GaleKarman;
                mem = EOS_KV * b * d * System.MathF.Exp((b * GaleKarman) - (EOS_KV * ((d * System.MathF.Exp(b * GaleKarman)) + a)));
                _ = ClearSkyTransparency.AddKey(new Keyframe(h, 1, mem, mem));
            }
        }

        public float[] LatitudesMeanInsolation(int N)
        {
            float[] Result = new float[N];

            float MeanInsolation = GreekS * System.MathF.Pow(StarR / GaleSemiMajor, 2) * System.MathF.Pow(StarT, 4);

            for (int i = 0; i < N; i++)
            {
                Result[i] = MeanInsolation * System.MathF.Cos(0.01745329f * ((i + 0.5f) * (180 / N))) * (1 + (EOS_AtmA / (1 - EOS_AtmA)));
            }

            return Result;
        }

        public float MeanInsolationAtLatitude(int N, int i, bool Earth)
        {
            float MeanInsolation = !Earth
                ? GreekS * System.MathF.Pow(StarR / GaleSemiMajor, 2) * System.MathF.Pow(StarT, 4)
                : GreekS * System.MathF.Pow(SunR / AU, 2) * System.MathF.Pow(SunT, 4);

            return MeanInsolation * System.MathF.Cos(0.01745329f * ((i + 0.5f) * (180f / N))) * (1 + (EOS_AtmA / (1 - EOS_AtmA)));
        }

        public float CalculateD(int N, int i)
        {
            float q = 0.5f; //Relative humidity

            int Tw = (int)System.MathF.Round((28 / (180 / N)) - (90 / N));
            Tw = (Tw >= 0) ? ((Tw < N) ? Tw : N) : 0;
            int Tc = (int)System.MathF.Round((68 / (180 / N)) - (90 / N));
            Tc = (Tc >= 0) ? ((Tc < N) ? Tc : N) : 0;

            if (D_SOM[0] == 0)
            {
                D_SOM[0] = System.MathF.Pow(GaleR / EarthR, -(6f / 5f)) *
                    System.MathF.Pow(GaleAtm / EarthAtm / (GaleG / EarthG), (2f / 5f)) *
                    System.MathF.Pow( /* Angular Rotation Speed */ 0.00000014124515f / 0.00007292123517f, -(4f / 5f)); // i love how not even double has this precision, which is what all of this was based off-of
            }

            float Sdry = (GaleAtmCp / EarthAtmCp) * D_SOM[0] * System.MathF.Pow((EOS_Insolation[i] / MeanInsolationAtLatitude(N, i, true)) *
                ((1 - EOS_A) / (1 - EarthAlbedo)) *
                (((EOS_Temperature[Tw] - EOS_Temperature[Tc]) / EOS_Temperature[Tw]) / ((EarthTw - EarthTc) / EarthTw)), (3f / 5f));

            if (D_SOM[1] == 0)
            {
                D_SOM[1] = (ZincMM / WaterMM) / ((GaleAtmCp / EarthAtmCp) * (GaleAtmMM / EarthAtmMM) * (GaleAtm / EarthAtm));
            }

            float Smd = (q / 0.75f) * D_SOM[1] * ((GalePressGivenTemp(EOS_Temperature[Tw]) - GalePressGivenTemp(EOS_Temperature[Tc])) / (EarthPressGivenTemp(EarthTw) - EarthPressGivenTemp(EarthTc))) / ((EOS_Temperature[Tw] - EOS_Temperature[Tc]) / (EarthTw - EarthTc));

            float RelD = Sdry * (1 + (0.7f * Smd)) / (1 + 0.7f);

            float Result = RelD * 0.66f; // W/m^2K

            return Result;
        }

        private void Start()
        {
            Debug.Log(CalculateStaticInsolation(GaleSemiMajor));

            #region List Setups
            EOS_Temperature = new float[EOS_N];
            EOS_Diffusion = new float[EOS_N];
            EOS_Insolation = new float[EOS_N];
            EOS_X = new float[Mathf.RoundToInt(EOS_N * (EOS_N - 1) / 2)]; // fun fact: this code originally used doubles, and Mathf uses floats. just think about that
            EOS_D = new float[1];

            D_SOM = new float[2];
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
                    EOS_D[0] = CalculateD(EOS_N, FrameCount) * (1 - System.MathF.Pow(System.MathF.Sin(0.01745329f * ((FrameCount + 0.5f) * (180 / EOS_N))), 2));
                }

                for (int i2 = FrameCount + 1; i2 < EOS_N; i2++)
                {
                    i3 = FrameCount * EOS_N - (FrameCount * (FrameCount + 1) / 2) + i2 - FrameCount - 1;

                    EOS_X[i3] = System.MathF.Sin(0.01745329f * (180f / EOS_N) * (i2 - FrameCount));
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

                        float mem = (EOS_D[0] * ((EOS_Temperature[i2] - EOS_Temperature[i]) *
                                     System.MathF.Pow(EOS_X[index], 2))) / 2;

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
}