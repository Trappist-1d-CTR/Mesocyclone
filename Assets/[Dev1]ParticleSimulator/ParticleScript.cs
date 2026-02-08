using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleScript : MonoBehaviour
{
    private Rigidbody2D Physics;
    private CircleCollider2D Collider;
    public float Radius = 2;

    // Start is called before the first frame update
    void Start()
    {
        Physics = gameObject.GetComponent<Rigidbody2D>();
        Collider = gameObject.GetComponent<CircleCollider2D>();

        gameObject.transform.localScale = new Vector3(Radius * 2, Radius * 2, Radius * 2);

        float rand = Random.Range(-0.01f, 0.01f);
        Physics.linearVelocity = new Vector2(5 - rand, -2 + rand);


        //Physics.velocity
        //Physics.mass
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
