using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoyantObject : MonoBehaviour
{
    public List<Transform> boyancyPoints = new List<Transform>();
    private new Rigidbody rigidbody;

    public float floatForce;

    private void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        Physics.queriesHitBackfaces = true;
    }

    void FixedUpdate()
    {
        RaycastHit hitInfo;

        foreach (Transform boyancyPoint in boyancyPoints)
            if (Physics.Raycast(boyancyPoint.position, Vector3.up, out hitInfo))//, float.PositiveInfinity, LayerMask.NameToLayer("Water")))
                rigidbody.AddForceAtPosition(floatForce * Physics.gravity.magnitude * hitInfo.distance * Vector3.Lerp(hitInfo.normal, Vector3.up, 0.9f), boyancyPoint.position, ForceMode.Acceleration);
    }
}
