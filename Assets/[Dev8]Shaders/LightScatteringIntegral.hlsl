//UNITY_SHADER_NO_UPGRADE
#ifndef MYHLSLINCLUDE_INCLUDED
#define MYHLSLINCLUDE_INCLUDED
float RaySphere(float3 CamPos, float3 PlanetPos, float AtmRadius, float3 RayDirection)
{
    float t = dot((PlanetPos - CamPos), RayDirection);
    float3 P = CamPos + (RayDirection * t);
    float y = length(PlanetPos - P);

    return y;
}

float OpticalDepth(float G, float MM, float R, float T, float3 Origin, float3 Direction, float L, int N)
{
    float3 SamplePos = Origin;
    float step = L / (N - 1);
    float OptDepth = 0;

    for (int i = 0; i < N; i++)
    {
        float d = exp(-G * MM * SamplePos.y / (R * T));
        OptDepth += d * step;
        SamplePos += Direction * step;
    }
    return abs(OptDepth);
}

void LightScatteringIntegral_float(float OptDepthSlider, float3 RayDir, float3 SunDir, float G, float MM, float R, float T, float3 CamP, float3 PlanetP, float Karman, int pNum, out float Out, out float Debug)
{
    float3 PointPos = CamP;
    float AtmR = abs(PlanetP.y) + Karman;
    
    float RayLength = RaySphere(CamP, PlanetP, AtmR, RayDir);
    float stepSize = RayLength / (pNum - 1);
    float inLight = 0;

    Debug = 0;

    for (int i = 0; i < pNum; i++)
    {
        float SunRayLength = RaySphere(PointPos, PlanetP, AtmR, RayDir);
        float SunRayOpticalDepth = OpticalDepth(G, MM, R, T, PointPos, SunDir, SunRayLength, pNum);
        float CamOpticalDepth = OpticalDepth(G, MM, R, T, PointPos, -RayDir, stepSize * i, pNum);
        float t = exp(-(SunRayOpticalDepth + CamOpticalDepth));
        float DensityP = exp(-G * MM * CamP.y / (R * T));

        Debug += SunRayOpticalDepth;

        inLight += DensityP * t * stepSize;
        PointPos += RayDir * stepSize;
    }

    Out = inLight;
}
#endif //MYHLSLINCLUDE_INCLUDED

/*


    float Hp = 0; //Height of Point
    float Hc; //Height of Camera
    float Tp; //Transparency at Point
    float Tc = 1; //Transparency at Camera
    float pSize = Karman / (pNum + 1);
    float RayLength = (Karman - Hc) / sinP;
    float dp; //Local Density at point

    Out = 0;
    for (int i = 0; i < pNum; i++)
    {
        Hp = pSize * ((float)i - 0.5f);
        dp = exp(-G * MM * Hp / (R * T)); //-GaleG * GaleAtmMM * Height / (R * GaleAvgTemp)

        Tp = (1 - dp); //Find a way to reference or create the proper function

        Out += (Tp * sinP);
    }

    Out = Out;
*/