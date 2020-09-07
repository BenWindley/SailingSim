using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterFocus : MonoBehaviour
{
    public Material waterMat;
    public Transform water;

    void Update()
    {
        waterMat.SetVector("FocusPosition", transform.position);
        water.transform.position = Vector3.Scale(transform.position, new Vector3(1, 0, 1));
    }
}
