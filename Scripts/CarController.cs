using UnityEngine;

public class CarController : MonoBehaviour
{
    // CAR SETUP
    [Range(20, 190)]
    public int maxSpeed = 90; // Maximum speed in km/h.
    [Range(10, 120)]
    public int maxReverseSpeed = 45; // Maximum reverse speed in km/h.
    [Range(1, 10)]
    public int accelerationMultiplier = 2; // Acceleration factor.
    [Range(10, 45)]
    public int maxSteeringAngle = 27; // Maximum steering angle.
    [Range(0.1f, 1f)]
    public float steeringSpeed = 0.5f; // Steering speed.
    [Range(100, 600)]
    public int brakeForce = 350; // Brake force.
    [Range(1, 10)]
    public int decelerationMultiplier = 2; // Deceleration factor.
    [Range(1, 10)]
    public int handbrakeDriftMultiplier = 5; // Drift multiplier.
    public Vector3 bodyMassCenter; // Center of mass.

    // WHEELS
    public GameObject frontLeftMesh;
    public WheelCollider frontLeftCollider;
    public GameObject frontRightMesh;
    public WheelCollider frontRightCollider;
    public GameObject rearLeftMesh;
    public WheelCollider rearLeftCollider;
    public GameObject rearRightMesh;
    public WheelCollider rearRightCollider;

    // CAR DATA
    [HideInInspector]
    public float carSpeed;
    [HideInInspector]
    public bool isDrifting;
    [HideInInspector]
    public bool isTractionLocked;

    // PRIVATE VARIABLES
    Rigidbody carRigidbody;
    float steeringAxis;
    float throttleAxis;
    float localVelocityZ;
    float localVelocityX;
    bool deceleratingCar;

    WheelFrictionCurve FLwheelFriction;
    float FLWextremumSlip;
    WheelFrictionCurve FRwheelFriction;
    float FRWextremumSlip;
    WheelFrictionCurve RLwheelFriction;
    float RLWextremumSlip;
    WheelFrictionCurve RRwheelFriction;
    float RRWextremumSlip;

    void Start()
    {
        carRigidbody = gameObject.GetComponent<Rigidbody>();
        carRigidbody.centerOfMass = bodyMassCenter;

        // Save default friction values for drifting
        FLwheelFriction = frontLeftCollider.sidewaysFriction;
        FLWextremumSlip = frontLeftCollider.sidewaysFriction.extremumSlip;

        FRwheelFriction = frontRightCollider.sidewaysFriction;
        FRWextremumSlip = frontRightCollider.sidewaysFriction.extremumSlip;

        RLwheelFriction = rearLeftCollider.sidewaysFriction;
        RLWextremumSlip = rearLeftCollider.sidewaysFriction.extremumSlip;

        RRwheelFriction = rearRightCollider.sidewaysFriction;
        RRWextremumSlip = rearRightCollider.sidewaysFriction.extremumSlip;
    }

    void Update()
    {
        // Car Data
        carSpeed = (2 * Mathf.PI * frontLeftCollider.radius * frontLeftCollider.rpm * 60) / 1000;
        localVelocityX = transform.InverseTransformDirection(carRigidbody.velocity).x;
        localVelocityZ = transform.InverseTransformDirection(carRigidbody.velocity).z;

        // Animate wheel meshes
        AnimateWheelMeshes();
    }

    // Method to get the current speed of the car
    public float GetSpeed()
    {
        return carRigidbody.velocity.magnitude * 3.6f; // Конвертируем метры в секунду в км/ч
    }

    // STEERING METHODS
    public void TurnLeft()
    {
        steeringAxis = Mathf.Clamp(steeringAxis - (Time.deltaTime * 10f * steeringSpeed), -1f, 1f);
        var steeringAngle = steeringAxis * maxSteeringAngle;
        frontLeftCollider.steerAngle = Mathf.Lerp(frontLeftCollider.steerAngle, steeringAngle, steeringSpeed);
        frontRightCollider.steerAngle = Mathf.Lerp(frontRightCollider.steerAngle, steeringAngle, steeringSpeed);
    }

    public void TurnRight()
    {
        steeringAxis = Mathf.Clamp(steeringAxis + (Time.deltaTime * 10f * steeringSpeed), -1f, 1f);
        var steeringAngle = steeringAxis * maxSteeringAngle;
        frontLeftCollider.steerAngle = Mathf.Lerp(frontLeftCollider.steerAngle, steeringAngle, steeringSpeed);
        frontRightCollider.steerAngle = Mathf.Lerp(frontRightCollider.steerAngle, steeringAngle, steeringSpeed);
    }

    public void ResetSteeringAngle()
    {
        steeringAxis = Mathf.MoveTowards(steeringAxis, 0f, Time.deltaTime * 10f * steeringSpeed);
        var steeringAngle = steeringAxis * maxSteeringAngle;
        frontLeftCollider.steerAngle = Mathf.Lerp(frontLeftCollider.steerAngle, steeringAngle, steeringSpeed);
        frontRightCollider.steerAngle = Mathf.Lerp(frontRightCollider.steerAngle, steeringAngle, steeringSpeed);
    }

    // ENGINE AND BRAKING METHODS
    public void GoForward()
    {
        throttleAxis = Mathf.Clamp(throttleAxis + (Time.deltaTime * 3f), 0f, 1f);
        if (localVelocityZ < -1f)
        {
            Brakes();
        }
        else if (Mathf.RoundToInt(carSpeed) < maxSpeed)
        {
            ApplyMotorTorque(accelerationMultiplier * 50f * throttleAxis);
        }
        else
        {
            StopMotorTorque();
        }
    }

    public void GoReverse()
    {
        throttleAxis = Mathf.Clamp(throttleAxis - (Time.deltaTime * 3f), -1f, 0f);
        if (localVelocityZ > 1f)
        {
            Brakes();
        }
        else if (Mathf.Abs(Mathf.RoundToInt(carSpeed)) < maxReverseSpeed)
        {
            ApplyMotorTorque(accelerationMultiplier * 50f * throttleAxis);
        }
        else
        {
            StopMotorTorque();
        }
    }

    public void ThrottleOff()
    {
        StopMotorTorque();
    }

    public void DecelerateCar()
    {
        carRigidbody.velocity *= (1f / (1f + (0.025f * decelerationMultiplier)));
        if (carRigidbody.velocity.magnitude < 0.25f)
        {
            carRigidbody.velocity = Vector3.zero;
        }
    }

    public void Brakes()
    {
        ApplyBrakeTorque(brakeForce);
    }

    public void Handbrake()
    {
        isDrifting = true;
        AdjustFrictionForDrift();
        isTractionLocked = true;
    }

    public void RecoverTraction()
    {
        isTractionLocked = false;
        ResetFrictionAfterDrift();
    }

    // PRIVATE HELPER METHODS
    void ApplyMotorTorque(float torque)
    {
        frontLeftCollider.motorTorque = torque;
        frontRightCollider.motorTorque = torque;
        rearLeftCollider.motorTorque = torque;
        rearRightCollider.motorTorque = torque;
    }

    void StopMotorTorque()
    {
        frontLeftCollider.motorTorque = 0;
        frontRightCollider.motorTorque = 0;
        rearLeftCollider.motorTorque = 0;
        rearRightCollider.motorTorque = 0;
    }

    void ApplyBrakeTorque(float torque)
    {
        frontLeftCollider.brakeTorque = torque;
        frontRightCollider.brakeTorque = torque;
        rearLeftCollider.brakeTorque = torque;
        rearRightCollider.brakeTorque = torque;
    }

    void AdjustFrictionForDrift()
    {
        float driftFactor = handbrakeDriftMultiplier;
        SetFriction(driftFactor);
    }

    void ResetFrictionAfterDrift()
    {
        SetFriction(1f);
    }

    void SetFriction(float factor)
    {
        FLwheelFriction.extremumSlip = FLWextremumSlip * factor;
        frontLeftCollider.sidewaysFriction = FLwheelFriction;

        FRwheelFriction.extremumSlip = FRWextremumSlip * factor;
        frontRightCollider.sidewaysFriction = FRwheelFriction;

        RLwheelFriction.extremumSlip = RLWextremumSlip * factor;
        rearLeftCollider.sidewaysFriction = RLwheelFriction;

        RRwheelFriction.extremumSlip = RRWextremumSlip * factor;
        rearRightCollider.sidewaysFriction = RRwheelFriction;
    }
    public void GoForward(float acceleration)
    {
        throttleAxis = Mathf.Clamp(throttleAxis + (Time.deltaTime * acceleration), 0f, 1f);
        if (localVelocityZ < -1f)
        {
            Brakes();
        }
        else if (Mathf.RoundToInt(carSpeed) < maxSpeed)
        {
            ApplyMotorTorque(accelerationMultiplier * 50f * throttleAxis);
        }
        else
        {
            StopMotorTorque();
        }
    }

    void AnimateWheelMeshes()
    {
        Quaternion FLWRotation;
        Vector3 FLWPosition;
        frontLeftCollider.GetWorldPose(out FLWPosition, out FLWRotation);
        frontLeftMesh.transform.position = FLWPosition;
        frontLeftMesh.transform.rotation = FLWRotation;

        Quaternion FRWRotation;
        Vector3 FRWPosition;
        frontRightCollider.GetWorldPose(out FRWPosition, out FRWRotation);
        frontRightMesh.transform.position = FRWPosition;
        frontRightMesh.transform.rotation = FRWRotation;

        Quaternion RLWRotation;
        Vector3 RLWPosition;
        rearLeftCollider.GetWorldPose(out RLWPosition, out RLWRotation);
        rearLeftMesh.transform.position = RLWPosition;
        rearLeftMesh.transform.rotation = RLWRotation;

        Quaternion RRWRotation;
        Vector3 RRWPosition;
        rearRightCollider.GetWorldPose(out RRWPosition, out RRWRotation);
        rearRightMesh.transform.position = RRWPosition;
        rearRightMesh.transform.rotation = RRWRotation;
    }
}
