using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class controller : MonoBehaviour
{
    // Start is called before the first frame update
    public float speed;
    private Rigidbody rb;
    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        move();
    }

    private void LateUpdate()
    {
        Shader.SetGlobalVector("_Position", gameObject.transform.position);
    }

    void move()
    {
        float movex = Input.GetAxis("Horizontal");
        float movey = Input.GetAxis("Vertical");
        Vector3 speedvec = new Vector3(movex, 0, movey);
        //speedvec.Normalize();
        rb.velocity = speed * speedvec;
    }
}
