using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine.Jobs;

namespace Mesocyclone.GlobalData
{
    public struct GlobalData
    {
        /// <summary>
        /// Class containing Universal constants.
        /// </summary>
        public struct Const
        {
            /// <summary>
            /// Speed of Light in a vacuum. Unit: meters per second
            /// </summary>
            public const double c = 299792458f; // m / s

            /// <summary>
            /// Newtonian Constant of Gravitation. Unit: m^3 / kg s^2
            /// </summary>
            public const double G = 6.67430e-11f; // m^3 / kg s^2

            /// <summary>
            /// Stefan-Boltzmann Constant. Unit: W / m^2 T^4
            /// </summary>
            public const float StefBoltz = 5.670374419e-8f;

            /// <summary>
            /// Boltzmann Constant. Unity: J/K
            /// </summary>
            public const float Boltzmann = 1.380649e-23f;

            /// <summary>
            /// Omega Constant. Unit: none
            /// </summary>
            public const float Omega = 0.567143290409783873f;

            /// <summary>
            /// Molar Gas Constant. Unit: J / mol K
            /// </summary>
            public const float R = 8.31446261815324f;

            /// <summary>
            /// Avogadro's Numer. Unit: none
            /// </summary>
            public const float NA = 6.02214076e23f;

            /// <summary>
            /// 1/2 is Universally constant. Unit: none
            /// </summary>
            public const float Zeta = 1f / 2f;
        }

        /// <summary>
        /// Class containing reference values, often useful as units (AU, Earth radius, etc).
        /// </summary>
        public struct Unit
        {
            /// <summary>
            /// Astrnomical Unit, equal to the semi-major axis of the Earth's orbit. Unit: m
            /// </summary>
            public const float AU = 1.495978707e11f;

            /// <summary>
            /// Earth's characteristics.
            /// </summary>
            public struct Earth
            {
                /// <summary>
                /// The Radius of the Earth, as per SI standard. Unit: m
                /// </summary>
                public const float Radius = 6.3781e6f;

                /// <summary>
                /// The Mass of the Eartg, as per SI standard. Unit: kg
                /// </summary>
                public const float Mass = 5.9722e24f;

                /// <summary>
                /// Earth's Gravitational Acceleration, as per SI standard. Unity: m / s^2
                /// </summary>
                public const float g = 9.80665f;

                /// <summary>
                /// Earth's Atmospheric Pressure at sea level, as per SI standard. Unit: Pa = N / m^2
                /// </summary>
                public const float AtmPressure = 101325f;

                /// <summary>
                /// Earth's average Amtospheric Heat Capacity. Unit: J / kg K
                /// </summary>
                public const float AtmHeatCapacity = 1006f;
            }

            /// <summary>
            /// The Sun's characteristics.
            /// </summary>
            public struct Sun
            {
                /// <summary>
                /// The Mass of the Sun, as per SI standard. Unit: kg
                /// </summary>
                public const float Mass = 1.988416e30f;

                /// <summary>
                /// The Radius of the Sun, as per SI standard. Unit: m
                /// </summary>
                public const float Radius = 6.957e8f;

                /// <summary>
                /// The Temperature of the Sun's photosphere. Unit: K
                /// </summary>
                public const float SurfaceTemp = 5777f;
            }

            /// <summary>
            /// Molar Weight of Water. Unit: kg / mol
            /// </summary>
            public const float WaterMM = 0.018015f;
        }

        public struct Data
        {
            /// <summary>
            /// Gale's characteristics.
            /// </summary>
            public struct Gale
            {
                /// <summary>
                /// Semi-Major Axis: the planet's average distance from its star. Unit: m
                /// </summary>
                public const float SemiMajor = 1.556f * Unit.AU;

                /// <summary>
                /// Mean Radius: the planet's average radius. Unit: meters
                /// </summary>
                public const float Radius = 1.723f * Unit.Earth.Radius;

                /// <summary>
                /// Surface Gravity: the planet's average surface gravitational acceleration. Unit: m / s^2
                /// </summary>
                public const float SurfGravity = 2.249f * Unit.Earth.g;

                /// <summary>
                /// Surface Temperature: the planet's average surface temperature. Unit: K
                /// </summary>
                public const float SurfTemp = 1300f;

                /// <summary>
                /// Atmospheric Pressure: the planet's atmospheric pressure at sea level. Unit: Pa = N / m^2
                /// </summary>
                public const float AtmPressure = 14.9f * Unit.Earth.AtmPressure;

                /// <summary>
                /// Atmospheric Molar Weight: the planet's atmosphere's average molar weight. Unit: kg / mol
                /// </summary>
                public const float AtmMM = 0.035138266f;

                /// <summary>
                /// Atmospheric Surface Density: the planet's atmosphere's average density on the surface. Unit: kg / m^3
                /// </summary>
                public const float AtmSurfDensity = 4.879f;

                /// <summary>
                /// Karman Line: conventional boundary height that separates atmosphere from space. Unit: m
                /// </summary>
                public const float KarmanLine = 204210f;

                /// <summary>
                /// Albedo: percentage of incoming radiation that gets reflected out the system. Unit: none
                /// </summary>
                public const float Albedo = 0.9f;

                /// <summary>
                /// Greenhouse: percentage of outgoing radiation that gets reflected into the system. Unit: none
                /// </summary>
                public const float Greenhouse = 0.8f;

                /// <summary>
                /// Insolation: amount of radiation the planet receives from its star. Unit: W / m^2
                /// </summary>
                public const float Insolation = (Const.StefBoltz * (Glare.SurfTemp * Glare.SurfTemp * Glare.SurfTemp * Glare.SurfTemp)) * (Glare.Radius / SemiMajor) * (Glare.Radius / SemiMajor);

                /// <summary>
                /// Planetary Heat Capacity: amount of heat required to heat up the planet by 1 K. Unit: J / K
                /// </summary>
                public const float HeatCapacity = 5000f * 4f * math.PI * Data.Gale.Radius * Data.Gale.Radius;
            }

            public struct Glare
            {
                /// <summary>
                /// Stellar Mass: the star's mass. Unit: kg
                /// </summary>
                public const float Mass = 1.896f * Unit.Sun.Mass;

                /// <summary>
                /// Stellar Radius: the star's average radius. Unit: m
                /// </summary>
                public const float Radius = 54.74f * Unit.Sun.Radius;

                /// <summary>
                /// Surface Temperature: the temperature of the star's photosphere. Unit: K
                /// </summary>
                public const float SurfTemp = 3783f;
            }

            public const float StiffK = 0.01f;
        }
    }

    public partial struct GlobalCalc : ISystem
    {
        public static float StaticPressureAtHeight(float Height)
        {
            return GlobalData.Data.Gale.AtmPressure * math.exp(-GlobalData.Data.Gale.SurfGravity * GlobalData.Data.Gale.AtmMM * Height / (GlobalData.Const.R * GlobalData.Data.Gale.SurfTemp));
        }

        public static float DensityAtHeight(float Height)
        {
            return GlobalData.Data.Gale.AtmSurfDensity * math.exp(-GlobalData.Data.Gale.SurfGravity * GlobalData.Data.Gale.AtmMM * Height / (GlobalData.Const.R * GlobalData.Data.Gale.SurfTemp));
        }
    }
}
