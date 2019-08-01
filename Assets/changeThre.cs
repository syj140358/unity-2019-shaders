using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class changeThre : MonoBehaviour
{
    // Start is called before the first frame update
    private Material m;
    private float threshold = 0;
    private bool state = false; //false: 0-1, true: 1-0
    void Start()
    {
        m = gameObject.GetComponent<MeshRenderer>().material;
    }

    // Update is called once per frame
    void Update()
    {
        if (!state)
        {
            threshold += 0.01f;
            m.SetFloat("_Threshold", threshold);
            if (threshold > 0.99f) state = !state;
        }
        else
        {
            threshold -= 0.01f;
            m.SetFloat("_Threshold", threshold);
            if (threshold < 0.01f) state = !state;
        }
    }
}
