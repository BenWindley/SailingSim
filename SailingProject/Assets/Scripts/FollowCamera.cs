using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Transform followPoint;
    public float followSpeed;

    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, followPoint.position, Time.deltaTime * followSpeed);
    }
}
