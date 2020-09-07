using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoatController : MonoBehaviour
{
    private new Rigidbody rigidbody;

    public float forwardSpeed;
    public float turnSpeed;

    public float speedTarget;
    public float speedFactor;
    public float speed;

    public Transform motor;
    public float motorTurnSpeed;
    public float motorTurnBounds;
    public float motorTurnTarget;

    public Transform hull;
    public float tiltBounds;

    private void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        motorTurnTarget = 0;
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.W))
        {
            speedTarget = 1.0f;
        }
        else if (Input.GetKeyUp(KeyCode.W))
        {
            speedTarget = 0.0f;
        }
        if (Input.GetKey(KeyCode.D))
        {
            rigidbody.AddTorque(Mathf.Max(speed, 0.5f) * turnSpeed * transform.up * Time.deltaTime, ForceMode.Acceleration);
            motorTurnTarget = -motorTurnBounds;
        }
        if (Input.GetKey(KeyCode.A))
        {
            rigidbody.AddTorque(-Mathf.Max(speed, 0.5f) * turnSpeed * transform.up * Time.deltaTime, ForceMode.Acceleration);
            motorTurnTarget = motorTurnBounds;
        }
        if((Input.GetKeyUp(KeyCode.D) || Input.GetKeyUp(KeyCode.A)) &&
            !(Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.A)))
        {
            motorTurnTarget = 0;
        }

        motor.localRotation = Quaternion.RotateTowards(motor.localRotation, Quaternion.Euler(motor.localRotation.eulerAngles.x, motor.localRotation.eulerAngles.y, motorTurnTarget), Time.deltaTime * motorTurnSpeed);
        speed = Mathf.MoveTowards(speed, speedTarget, Time.deltaTime * speedFactor);
        rigidbody.AddForce(speed * forwardSpeed * transform.forward * Time.deltaTime, ForceMode.Acceleration);
        hull.localEulerAngles = new Vector3(0, tiltBounds * speed, 0);
    }
}
