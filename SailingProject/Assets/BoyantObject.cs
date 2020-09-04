using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoyantObject : MonoBehaviour
{
    public List<Transform> boyancyPoints = new List<Transform>();
    private List<RaycastHit> submerged = new List<RaycastHit>();

    private new Rigidbody rigidbody;

    public float floatForce;

    public int checkfrequency;

    private void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        Physics.queriesHitBackfaces = true;

        submerged.Clear();

        foreach (Transform p in boyancyPoints)
            submerged.Add(UpdateBoyancyPoint(p));

        checkfrequency = Mathf.Min(1, checkfrequency);
    }

    void Update()
    {
        if (Time.frameCount % checkfrequency == 0)
        {
            submerged.Clear();

            foreach (var p in boyancyPoints)
                submerged.Add(UpdateBoyancyPoint(p));
        }

        foreach (RaycastHit s in submerged)
            if(s.collider)
                rigidbody.AddForceAtPosition(floatForce * Physics.gravity.magnitude * s.distance * Time.deltaTime * Vector3.Lerp(s.normal, Vector3.up, 0.9f), s.point - s.distance * Vector3.up, ForceMode.Acceleration);
    }

    private RaycastHit UpdateBoyancyPoint(Transform point)
    {
        RaycastHit hitInfo;

        Physics.Raycast(point.position, Vector3.up, out hitInfo);

        return hitInfo;
    }
}
