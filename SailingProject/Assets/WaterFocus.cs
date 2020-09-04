using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterFocus : MonoBehaviour
{
    public Material waterMat;
    public Transform water;

    public new Rigidbody rigidbody;

    void Update()
    {
        if (!rigidbody)
            rigidbody = GetComponent<Rigidbody>();

        waterMat.SetVector("FocusPosition", transform.position);
        water.transform.position = Vector3.Scale(transform.position, new Vector3(1, 0, 1));

        if(Input.GetKey(KeyCode.W))
        {
            rigidbody.AddForce(100 * transform.forward * Time.deltaTime, ForceMode.Acceleration);
        }
        if(Input.GetKey(KeyCode.D))
        {
            rigidbody.AddTorque(100 * transform.up * Time.deltaTime, ForceMode.Acceleration);
        }
        if (Input.GetKey(KeyCode.A))
        {
            rigidbody.AddTorque(-100 * transform.up * Time.deltaTime, ForceMode.Acceleration);
        }
    }
}
