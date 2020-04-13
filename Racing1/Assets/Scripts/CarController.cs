using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour
{

    private float m_horizontalInput;
    private float m_verticalInput;
    private float m_steeringAngle;

    public int updateInterval;

    public bool isTesting;

    public int testRpm;

    public WheelCollider wc_rf, wc_lf, wc_rr, wc_lr;
    public Transform transform_rf, transform_lf, transform_rr, transform_lr;

    public Vector3 com;
    private Rigidbody rb;
    public float maxSteerAngle = 30;

    public float brakingForce = 1000;

    public AnimationCurve torqueCurve;

    public float maxEngineSpeed = 6000;

    public float minEngineSpeed = 2000;

    public float downShiftRPM, upShiftRPM;

    public bool tc = true;
    public bool abs = true;

    public bool antiRollBar = true;
    public float antiRollBarStiffness = 0.5f;

    public int maxGear = 5;

    public float [] gearRatios;

    public float finalDrive = 3.7f;
    private float engineSpeed = 0;
    private float engineTorque = 0;
    private int gear = 1;
    private float wheelSpeed = 0f;

    private float baseWheelTorque;

    float torqueMultiplier_lr;

    private float previousEngineSpeed = 0.0f;
    private int iterSinceLastUpdate = 0;

    private WheelHit wh_lr = new WheelHit();
    private WheelHit wh_rr = new WheelHit();
    private WheelHit wh_lf = new WheelHit();
    private WheelHit wh_rf = new WheelHit();
    public float GetEngineSpeed()
    {
        return engineSpeed;
    }
    public Rigidbody GetRigidbody()
    {
        return rb;
    }

    public float GetWheelTorque() //returns torque input and output
    {
        return Mathf.Abs(m_verticalInput) * torqueMultiplier_lr;
    }

    public float GetBrakeForce()
    {
        return wc_lr.brakeTorque;
    }

    public int GetGear()
    {
        return gear;
    }

    private bool changeGear()
    {
        bool retVal = false;
        if (engineSpeed >= upShiftRPM && Mathf.Abs(wh_lr.forwardSlip) <= wc_lr.forwardFriction.asymptoteSlip && gear <= maxGear/*&& Mathf.Approximately(wh_rr.forwardSlip * wheelCollider_rr.rpm*wheelCollider_rr.radius*2*Mathf.PI/60, rb.velocity.magnitude)*/ && gear <= gearRatios.Length)
        {
            gear++;
            retVal = true;
        }
        else if (engineSpeed <= downShiftRPM && Mathf.Abs(wh_lr.forwardSlip) <= wc_lr.forwardFriction.extremumSlip &&  gear > 0/*&& Mathf.Approximately(wh_rr.forwardSlip * wheelCollider_rr.rpm*wheelCollider_rr.radius*2*Mathf.PI/60, rb.velocity.magnitude)*/ && gear > 1)
        {
            gear--;
            retVal = true;
        }
        return retVal;
    }

    private float EngineTorqueFromEngineSpeed()
    {
        engineTorque = torqueCurve.Evaluate(Mathf.Abs(GetEngineSpeed()));//2*maxEngineTorque/(1+Mathf.Exp(Mathf.Pow((2.5f*engineSpeed-1.5f*maxEngineSpeed)/(2*maxEngineSpeed),2)))+maxEngineTorque/maxEngineSpeed;
        return engineTorque;
    }

    private float WheelTorqueFromEngineTorque()
    {
        return EngineTorqueFromEngineSpeed() * gearRatios[gear-1] * finalDrive;

    }

    private void EngineSpeedFromWheelSpeed()
    {
        engineSpeed = Mathf.Abs(wheelSpeed) * gearRatios[gear-1] * finalDrive;
    }

    public void GetInput()
    {
        m_horizontalInput = Input.GetAxis("Horizontal");
        m_verticalInput = Input.GetAxis("Vertical");
    }

    private void Steer()
    {
        m_steeringAngle = maxSteerAngle * m_horizontalInput;
        float l2 = Vector3.Distance(wc_lf.transform.position, wc_rf.transform.position);
        if (m_horizontalInput > 0)
        {
            wc_rf.steerAngle =  m_steeringAngle;
            float l1 = Vector3.Distance(wc_rf.transform.position, wc_rr.transform.position);
            wc_lf.steerAngle = Mathf.Atan((l1 * Mathf.Tan(m_steeringAngle)+l2)/l1);
        }
        else if(m_horizontalInput < 0)
        {
            wc_lf.steerAngle =  m_steeringAngle;
            float l1 = Vector3.Distance(wc_lf.transform.position, wc_lr.transform.position);
            wc_rf.steerAngle = Mathf.Atan((l1 * Mathf.Tan(m_steeringAngle)+l2)/l1);
        }
    }

    private void Accelerate(bool isAccelerating)
    {
        baseWheelTorque = isAccelerating ? WheelTorqueFromEngineTorque() : 0;
        wc_lr.GetGroundHit(out wh_lr);
        wc_rr.GetGroundHit(out wh_rr);
        //TODO: use differential to apply toruqe on wheels, for now set to left rear
        torqueMultiplier_lr = engineSpeed >= maxEngineSpeed ? 0.0f : Mathf.Min(1.0f, (1f - (Mathf.Abs(wh_lr.forwardSlip) - wc_lr.forwardFriction.extremumSlip)));
        float torqueMultiplier_rr =  engineSpeed >= maxEngineSpeed ? 0.0f : Mathf.Min(1.0f, (1f - (Mathf.Abs(wh_rr.forwardSlip) - wc_rr.forwardFriction.extremumSlip)));
        wc_lr.motorTorque = m_verticalInput * baseWheelTorque * torqueMultiplier_lr;
        wc_rr.motorTorque = m_verticalInput * baseWheelTorque * torqueMultiplier_rr;
        // if (!isAccelerating)
        // {
        //     wc_lr.motorTorque -= (.1f * engineSpeed) * Mathf.Sign(transform.InverseTransformDirection(rb.velocity).z);
        //     wc_lr.motorTorque -= (.1f * engineSpeed) * Mathf.Sign(transform.InverseTransformDirection(rb.velocity).z);
        // }

    }

    private void Brake(bool isBraking)
    {
        float force = isBraking ? brakingForce : 0.0f;
        wc_lr.GetGroundHit(out wh_lr);
        wc_rr.GetGroundHit(out wh_rr);
        wc_lf.GetGroundHit(out wh_lf);
        wc_rf.GetGroundHit(out wh_rf);
        float brakeMultiplier_lr = 1f;
        float brakeMultiplier_rr = 1f;
        float brakeMultiplier_lf = 1f;
        float brakeMultiplier_rf = 1f;
        if (abs)
        {
            brakeMultiplier_lr = Mathf.Abs(Mathf.Max(-1.0f, (-1f - (wh_lr.forwardSlip - wc_lr.forwardFriction.extremumSlip))));
            brakeMultiplier_rr = Mathf.Abs(Mathf.Max(-1.0f, (-1f - (wh_rr.forwardSlip - wc_rr.forwardFriction.extremumSlip))));
            brakeMultiplier_lf = Mathf.Abs(Mathf.Max(-1.0f, (-1f - (wh_lf.forwardSlip - wc_lf.forwardFriction.extremumSlip))));
            brakeMultiplier_rf = Mathf.Abs(Mathf.Max(-1.0f, (-1f - (wh_rr.forwardSlip - wc_rf.forwardFriction.extremumSlip))));
        }
        wc_lr.brakeTorque = force * brakeMultiplier_lr;
        wc_lf.brakeTorque = force * brakeMultiplier_lf;
        wc_rf.brakeTorque = force * brakeMultiplier_rf;
        wc_rr.brakeTorque = force * brakeMultiplier_rr;
        // wheelCollider_lr.brakeTorque = (1+wh_lr.forwardSlip) * force;
        // wheelCollider_lf.brakeTorque = (1+wh_lf.forwardSlip) * force;
        // wheelCollider_rf.brakeTorque = (1+wh_rf.forwardSlip) * force;
        // wheelCollider_rr.brakeTorque = (1+wh_rr.forwardSlip) * force;
    }

    private void applyAntiRoll()
    {
        if (!antiRollBar)
        {
            return;
        }
        float travelLR = 1.0f;
        float travelRR = 1.0f;
        float travelLF = 1.0f;
        float travelRF = 1.0f;

        bool groundedLR = wc_lr.GetGroundHit(out wh_lr);

        if (groundedLR)
        {
            travelLR = (-wc_lr.transform.InverseTransformPoint(wh_lr.point).y 
                    - wc_lr.radius) / wc_lr.suspensionDistance;
        }

        bool groundedRR = wc_rr.GetGroundHit(out wh_rr);

        if (groundedRR)
        {
            travelRR = (-wc_rr.transform.InverseTransformPoint(wh_rr.point).y 
                    - wc_rr.radius) / wc_rr.suspensionDistance;
        }

        var antiRollForceR = (travelLR - travelRR) * antiRollBarStiffness;

        if (groundedLR)
            rb.AddForceAtPosition(wc_lr.transform.up * -antiRollForceR,
                wc_lr.transform.position); 
        if (groundedRR)
            rb.AddForceAtPosition(wc_rr.transform.up * antiRollForceR,
               wc_rr.transform.position); 

        bool groundedLF = wc_lr.GetGroundHit(out wh_lf);

        if (groundedLF)
        {
            travelLF = (-wc_lf.transform.InverseTransformPoint(wh_lf.point).y 
                    - wc_lf.radius) / wc_lf.suspensionDistance;
        }

        bool groundedRF = wc_lr.GetGroundHit(out wh_rf);

        if (groundedRF)
        {
            travelRF = (-wc_rf.transform.InverseTransformPoint(wh_rf.point).y 
                    - wc_rf.radius) / wc_rf.suspensionDistance;
        }

        var antiRollForceF = (travelLF - travelRF) * antiRollBarStiffness;

        if (groundedLF)
            rb.AddForceAtPosition(wc_lf.transform.up * -antiRollForceF,
                wc_lf.transform.position); 
        if (groundedRF)
            rb.AddForceAtPosition(wc_rf.transform.up * antiRollForceF,
               wc_rf.transform.position); 
    }

    private void UpdateWheelPoses()
    {
        UpdateWheelPose(wc_lf, transform_lf);
        UpdateWheelPose(wc_rf, transform_rf);
        UpdateWheelPose(wc_lr, transform_lr);
        UpdateWheelPose(wc_rr, transform_rr);
    }

    private void UpdateWheelPose(WheelCollider collider, Transform transform)
    {
        Vector3 pos = transform.position;
        Quaternion quat = transform.rotation;

        collider.GetWorldPose(out pos, out quat);
        transform.position = pos;
        transform.rotation = quat;
    }

    private void smoothRPM()
    {
        
    }

    private void FixedUpdate()
    {
        if(!isTesting)
        {
            GetInput();
            previousEngineSpeed = engineSpeed;
            bool isGearChange = changeGear();
            if (iterSinceLastUpdate >= updateInterval)
            {
                iterSinceLastUpdate = 0;
                if (m_verticalInput == 0)
                {
                    Accelerate(false);
                    Brake(false);
                }
                else if (System.Math.Round(transform.InverseTransformDirection(rb.velocity).z, 1) * m_verticalInput >= 0)
                {
                    Accelerate(true);
                    Brake(false);
                }
                else
                {
                    Accelerate(false);
                    Brake(true);
                }
            }
            iterSinceLastUpdate++;
            applyAntiRoll();
            UpdateWheelPoses();
            EngineSpeedFromWheelSpeed();
            engineSpeed = previousEngineSpeed != 0 && !isGearChange ? Mathf.Lerp(previousEngineSpeed, engineSpeed, Mathf.Abs((engineSpeed - previousEngineSpeed)) / previousEngineSpeed) : engineSpeed;
            wheelSpeed = Mathf.Abs((wc_rr.rpm + wc_lr.rpm)) / 2;
            Steer();
        }
        else
        {
            engineSpeed = testRpm;
        }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = com;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
    }

    void OnGUI()
    {
        GUI.Label(new Rect(0,0,60,20), wh_lr.forwardSlip.ToString());
        GUI.Label(new Rect(0,20,60,20), wc_lr.motorTorque.ToString());
        GUI.Label(new Rect(0,40,60,20), previousEngineSpeed.ToString());
        GUI.Label(new Rect(0,60,60,20), engineSpeed.ToString());
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position + transform.rotation * com, .2f);
    }
}
