using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimManager : MonoBehaviour
{
    public int NParticles;
    public GameObject ParticleObj;

    private float BoltzmannC;

    public float AvgMass;
    public float AvgVelocity;
    public float Temperature;
    public float Pressure;

    public float SetTemp;
    public float Timestep;

    // Start is called before the first frame update
    void Start()
    {
        BoltzmannC = 1.380649f * Mathf.Pow(10, -23);

        Timestep = Mathf.Pow(10, -11);

        for (int i = 1; i < NParticles; i++)
        {
            _ = Instantiate(ParticleObj, Vector3.zero, new Quaternion(), gameObject.transform);
        }
    }

    void FixedUpdate()
    {
        AvgMass = 1.6605402f * Mathf.Pow(10, -27);

        if (SetTemp == 0)
        {
            AvgVelocity = 0;
            for (int i = 0; i < NParticles; i++)
            {
                AvgVelocity += gameObject.transform.GetChild(i).GetComponent<Rigidbody2D>().velocity.magnitude / NParticles;
            }
            AvgVelocity /= Mathf.Pow(10, 9) * Timestep;

            Temperature = AvgMass * Mathf.Pow(AvgVelocity, 2) / BoltzmannC;
        }
        else
        {
            if (SetTemp > 0)
            {
                for (int i = 0; i < NParticles; i++)
                {
                    gameObject.transform.GetChild(i).GetComponent<Rigidbody2D>().velocity *= Mathf.Sqrt(SetTemp / Temperature);
                }
            }

            SetTemp = 0;
        }

        Pressure = NParticles * 8.31446261815324f * Temperature / (1.5f * Mathf.Pow(11.54701f * Mathf.Pow(10, -11), 2) * Mathf.Sqrt(3));
    }
}
