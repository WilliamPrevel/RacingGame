using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Car : MonoBehaviour
{
    public int numberOfGears;
    public float topSpeed = 150;
    public float maxReverseSpeed = -50;
    public float maxBrakeTorque = 100;
    public float maxTurnAngle = 10;
    public float maxTorque = 10;
    public float decelerationTorque = 30;
    public Vector3 centerOfMassAdjustment = new Vector3(0f, -0.9f, 0f);
    public float spoilerRatio = 0.1f;
    public float handbrakeForwardSlip = 0.04f;
    public float handbrakeSidewaysSlip = 0.08f;
    public WheelCollider wheelFL;
    public WheelCollider wheelFR;
    public WheelCollider wheelBL;
    public WheelCollider wheelBR;
    public Transform wheelTransformFL;
    public Transform wheelTransformFR;
    public Transform wheelTransformBL;
    public Transform wheelTransformBR;
    public GameObject leftLight;
    public GameObject rightLight;
    public Texture2D idleLightTex;
    public Texture2D brakeLightTex;
    public Texture2D reverseLightTex;

    private bool applyHandbrake = false;
    private float currentSpeed;
    private float gearSpread;
    private Rigidbody body;



    void Start()
    {
        //so we don't have to search for it every update.
        body = GetComponent<Rigidbody>();

        //calculate the spread of top speed over the number of gears.
        gearSpread = topSpeed / numberOfGears;

        //lower center of mass for roll-over resistance
        body.centerOfMass += centerOfMassAdjustment;

        //get the waypoints from the track.
    }

    void SetSlipValues(float forward, float sideways)
    {
        WheelFrictionCurve tempStruct = wheelBR.forwardFriction;
        tempStruct.stiffness = forward;
        wheelBR.forwardFriction = tempStruct;
        tempStruct = wheelBR.sidewaysFriction;
        tempStruct.stiffness = sideways;
        wheelBR.sidewaysFriction = tempStruct;

        tempStruct = wheelBL.forwardFriction;
        tempStruct.stiffness = forward;
        wheelBL.forwardFriction = tempStruct;
        tempStruct = wheelBL.sidewaysFriction;
        tempStruct.stiffness = sideways;
        wheelBL.sidewaysFriction = tempStruct;
    }

    // FixedUpdate is called once per physics frame
    void FixedUpdate()
    {
        //calculate max speed in KM/H (optimized calc)
        currentSpeed = wheelBL.radius * wheelBL.rpm * Mathf.PI * 0.12f;
        if (currentSpeed < topSpeed && currentSpeed > maxReverseSpeed)
        {
            //rear wheel drive.
            wheelBL.motorTorque = Input.GetAxis("Vertical") * maxTorque;
            wheelBR.motorTorque = Input.GetAxis("Vertical") * maxTorque;
        }
        else
        {
            //can't go faster, already at top speed that engine produces.
            wheelBL.motorTorque = 0;
            wheelBR.motorTorque = 0;
        }

        //Spoilers add down pressure based on the car’s speed. (Upside-down lift)
        Vector3 localVelocity = transform.InverseTransformDirection(body.velocity);
        body.AddForce(-transform.up * (localVelocity.z * spoilerRatio), ForceMode.Impulse);

        //front wheel steering
        wheelFL.steerAngle = Input.GetAxis("Horizontal") * maxTurnAngle;
        wheelFR.steerAngle = Input.GetAxis("Horizontal") * maxTurnAngle;

        //apply deceleration when not pressing the gas or when breaking in either direction.
        if ((Input.GetAxis("Vertical") <= -0.5f && localVelocity.z > 0) || (Input.GetAxis("Vertical") >= 0.5f && localVelocity.z < 0))
        {
            wheelBL.brakeTorque = decelerationTorque + maxTorque;
            wheelBR.brakeTorque = decelerationTorque + maxTorque;
        }
        else if (Input.GetAxis("Vertical") == 0)
        {
            wheelBL.brakeTorque = decelerationTorque;
            wheelBR.brakeTorque = decelerationTorque;
        }
        else
        {
            wheelBL.brakeTorque = 0;
            wheelBR.brakeTorque = 0;
        }
    }

    void UpdateWheelPositions()
    {
        //move wheels based on their suspension.
        WheelHit contact = new WheelHit();
        if (wheelFL.GetGroundHit(out contact))
        {
            Vector3 temp = wheelFL.transform.position;
            temp.y = (contact.point + (wheelFL.transform.up * wheelFL.radius)).y;
            wheelTransformFL.position = temp;
        }
        if (wheelFR.GetGroundHit(out contact))
        {
            Vector3 temp = wheelFR.transform.position;
            temp.y = (contact.point + (wheelFR.transform.up * wheelFR.radius)).y;
            wheelTransformFR.position = temp;
        }
        if (wheelBL.GetGroundHit(out contact))
        {
            Vector3 temp = wheelBL.transform.position;
            temp.y = (contact.point + (wheelBL.transform.up * wheelBL.radius)).y;
            wheelTransformBL.position = temp;
        }
        if (wheelBR.GetGroundHit(out contact))
        {
            Vector3 temp = wheelBR.transform.position;
            temp.y = (contact.point + (wheelBR.transform.up * wheelBR.radius)).y;
            wheelTransformBR.position = temp;
        }
    }

    void Update()
    {
        //rotate the wheels based on RPM
        float rotationThisFrame = 360 * Time.deltaTime;
        wheelTransformFL.Rotate(0, -wheelFL.rpm / rotationThisFrame, 0);
        wheelTransformFR.Rotate(0, -wheelFR.rpm / rotationThisFrame, 0);
        wheelTransformBL.Rotate(0, -wheelBL.rpm / rotationThisFrame, 0);
        wheelTransformBR.Rotate(0, -wheelBR.rpm / rotationThisFrame, 0);

        //Adjust the wheels heights based on the suspension.
        //UpdateWheelPositions();

        //Determine what texture to use on our brake lights right now.
        DetermineBreakLightState();
    }

    void DetermineBreakLightState()
    {
        if ((currentSpeed > 0 && Input.GetAxis("Vertical") < 0)
            || (currentSpeed < 0 && Input.GetAxis("Vertical") > 0)
            || applyHandbrake)
        {
            leftLight.GetComponent<Renderer>().material.mainTexture = brakeLightTex;
            rightLight.GetComponent<Renderer>().material.mainTexture = brakeLightTex;
        }
        else if (currentSpeed < 0 && Input.GetAxis("Vertical") < 0)
        {
            leftLight.GetComponent<Renderer>().material.mainTexture = reverseLightTex;
            rightLight.GetComponent<Renderer>().material.mainTexture = reverseLightTex;
        }
        else
        {
            leftLight.GetComponent<Renderer>().material.mainTexture = idleLightTex;
            rightLight.GetComponent<Renderer>().material.mainTexture = idleLightTex;

        }
    }
    
    

}
