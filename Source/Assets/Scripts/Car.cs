using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
/// For representing all of the gene types.
/// </summary>
public enum GeneType
{
    LeftWheelRadius,
    RightWheelRadius,

    LeftWheelPosX,
    LeftWheelPosY,

    RightWheelPosX,
    RightWheelPosY,

    Speed,

    BaseDimensionsX,
    BaseDimensionsY
}

public class Car : MonoBehaviour
{
    public static readonly int NoOfGenes = 36;

    public Dictionary<GeneType, float> GenesToValues; // Array is in order of GeneType (guaranteed in Start())

    public float LeftWheelRadius;
    public float RightWheelRadius;

    public Vector2 LeftWheelPosition; // Percent from left
    public Vector2 RightWheelPosition; // Percent from left
    public float Speed;

    public Vector2 BaseDimensions;

    // References to game objects
    public GameObject BaseOfCar { get; private set; }
    public GameObject LeftWheel { get; private set; }
    public GameObject RightWheel { get; private set; }

    // Positions
    public Vector3 StartPosition { get; private set; }
    public Vector3 GoalPosition { get; set; }
    public Vector3 EndPosition { get; private set; }
    
    // Flags
    public bool HasReachedGoal { get; private set; }

    public bool HasRanSimulation { get; private set; }

    // Speed tracking //
    private float LastTimeCollectedSppeed;
    private const float CollectionInterval = 0.1f;

    private int AmountOfTimesUnderSpeed = 0;
    private const int MaxAmountOfTimesUnderSpeed = 100;

    private const float MinSpeed = 0.1f;
    public float MaxSpeedReached { get; private set; } // Added because otherwise the creatures had no incentive to go faster.

    // For upright tracking //
    public bool ShouldBeTrackingUpright { get; set; } = false;
    public bool HasReachedFlippingPoint { get; private set; }

    public static readonly float OverBankAngle = 70f;
    public float PeakBankAngle { get; private set; } // In radians.

    private void Start()
    {
        // Find all components.
        for (int i = 0; i < transform.childCount; ++i)
        {
            var currentGameObject = transform.GetChild(i).gameObject;

            if (currentGameObject.name == "Base")
            {
                BaseOfCar = currentGameObject;
            }
            else if (currentGameObject.name == "WheelLeft")
            {
                LeftWheel = currentGameObject;
            }
            else if (currentGameObject.name == "WheelRight")
            {
                RightWheel = currentGameObject;
            }
            else
            {
                Assert.IsTrue(false, "There was a problem here, a non matching gameobject was found.");
            }
        }

        // Check that we found all of them.
        Assert.IsNotNull(BaseOfCar, "Base of car not found!");
        Assert.IsNotNull(LeftWheel, "Left wheel not found!");
        Assert.IsNotNull(RightWheel,  "Right wheel not found!");

        StartPosition = transform.position;

        HasRanSimulation = true;
        HasReachedGoal = false;

        // Setup gene dictionary by adding all possible genes to the dictionary. The amount of genes per value is calculated by (NoOfGenes / Values in GeneType).
        GenesToValues = new Dictionary<GeneType, float>(9);
        var enumNames = Enum.GetNames(typeof(GeneType));
        for (int i = 0; i < enumNames.Length; ++i)
        {
            Enum.TryParse<GeneType>(enumNames[i], true, out var result);
            GenesToValues.Add(result, 0f);
        }
    }

    /// <summary>
    /// Begin simulating .
    /// </summary>
    public void StartSimulation()
    {
        HasRanSimulation = false;
        HasReachedGoal = false;
        HasReachedFlippingPoint = false;
        PeakBankAngle = 0f;
        AmountOfTimesUnderSpeed = 0;

        UpdateFromVariables();

        // Unfreeze rigid body.
        foreach (var childRigidBodies in GetComponentsInChildren<Rigidbody2D>())
        {
            childRigidBodies.constraints = RigidbodyConstraints2D.None;
        }

        // Reset position.
        transform.position = StartPosition;
    }

    /// <summary>
    /// Update physical car (transform, properties, scale of game objects) from variables.
    /// </summary>
    public void UpdateFromVariables()
    {
        // Apply values from dictionary
        LeftWheelRadius = GenesToValues[GeneType.LeftWheelRadius];
        RightWheelRadius = GenesToValues[GeneType.RightWheelRadius];
        LeftWheelPosition = new Vector2(GenesToValues[GeneType.LeftWheelPosX], GenesToValues[GeneType.LeftWheelPosY]);
        RightWheelPosition = new Vector2(GenesToValues[GeneType.RightWheelPosX], GenesToValues[GeneType.RightWheelPosY]);
        Speed = GenesToValues[GeneType.Speed];
        BaseDimensions = new Vector2(GenesToValues[GeneType.BaseDimensionsX], GenesToValues[GeneType.BaseDimensionsY]);

        // Reset transform
        BaseOfCar.transform.localPosition = Vector3.zero;
        BaseOfCar.transform.localRotation = Quaternion.Euler(Vector3.zero);

        // Apply values to base.
        //BaseDimensions.x *= 2f; // TO HELP FIX THE CAR JUST BEING WHEELS
        BaseOfCar.transform.localScale = new Vector3(BaseDimensions.x, BaseDimensions.y, 1f);

        Vector2 sizeOfBase = new Vector2(BaseOfCar.GetComponent<BoxCollider2D>().size.x * BaseOfCar.transform.localScale.x, 
            BaseOfCar.GetComponent<BoxCollider2D>().size.y * BaseOfCar.transform.localScale.y);

        // Apply values to left wheel
        LeftWheel.transform.localScale = new Vector3(LeftWheelRadius, LeftWheelRadius, 1f);

        var leftWheelPos = new Vector2(
            Mathf.Clamp01(LeftWheelPosition.x) - 0.5f,
            Mathf.Clamp01(LeftWheelPosition.y) - 0.5f);
        leftWheelPos.x *= (sizeOfBase.x);
        leftWheelPos.y *= (sizeOfBase.y);


        LeftWheel.transform.localPosition = new Vector3(leftWheelPos.x, leftWheelPos.y, 1f);

        // Apply values to right wheel
        RightWheel.transform.localScale = new Vector3(RightWheelRadius, RightWheelRadius, 1f);

        var rightWheelPos = new Vector2(
            Mathf.Clamp01(RightWheelPosition.x) - 0.5f,
            Mathf.Clamp01(RightWheelPosition.y) - 0.5f);
        rightWheelPos.x *= (sizeOfBase.x);
        rightWheelPos.y *= (sizeOfBase.y);

        RightWheel.transform.localPosition =  new Vector3(rightWheelPos.x, rightWheelPos.y, 1f);

        // Apply speed values to left and right wheels.
        var newMotor = new JointMotor2D { maxMotorTorque = 1000000f, motorSpeed = Speed };
        LeftWheel.GetComponent<HingeJoint2D>().useMotor = true;
        LeftWheel.GetComponent<HingeJoint2D>().motor = newMotor;

        RightWheel.GetComponent<HingeJoint2D>().useMotor = true;
        RightWheel.GetComponent<HingeJoint2D>().motor = newMotor;
    }

    /// <summary>
    /// Finish simulating, save relevant values and freeze rigid bodies.
    /// </summary>
    public void EndSimulation()
    {
        if (!HasRanSimulation)
        {
            HasRanSimulation = true;

            // Freeze all ridid bodies.
            foreach (var childRigidBodies in GetComponentsInChildren<Rigidbody2D>())
            {
                childRigidBodies.constraints = RigidbodyConstraints2D.FreezeAll;
            }

            // Set end position.
            EndPosition = BaseOfCar.GetComponent<Rigidbody2D>().position;
        }
    }

    private void Update()
    {
        CheckUprightStatus();
        
        if (!HasRanSimulation)
        {
            if (HasReachedGoal)
                EndSimulation();

            CheckUprightStatus();

            // Check speed every few intervals to reduce performance impact.
            if (LastTimeCollectedSppeed + CollectionInterval < Time.time)
            {
                CheckIfStillRunning();
            }
        }
    }

    /// <summary>
    /// Check speed, and if below a certain amount for too long, the car is marked to be stuck and should stop wasting simulation time.
    /// </summary>
    private void CheckIfStillRunning()
    {
        // Get speed
        var speed = BaseOfCar.GetComponent<Rigidbody2D>().velocity.magnitude;

        // Update max speed (for fitness)
        if (speed > MaxSpeedReached)
            MaxSpeedReached = speed;

        // If below threshhold count amount of checks below threshhold
        if (speed < MinSpeed)
        {
            AmountOfTimesUnderSpeed++;

            // If amount of times below threshold exceeds specified amount, stop running.
            if (AmountOfTimesUnderSpeed >= MaxAmountOfTimesUnderSpeed)
                EndSimulation();
        }
        else
        {
            AmountOfTimesUnderSpeed = 0;
        }
    }

    /// <summary>
    /// Inform the car that the goal has been reached.
    /// </summary>
    public void GoalReached()
    {
        HasReachedGoal = true;
        EndSimulation();
    }

    /// <summary>
    /// Check if the vehicle is upright.
    /// </summary>
    private void CheckUprightStatus()
    {
        if (!ShouldBeTrackingUpright) return;

        if (Mathf.Acos(BaseOfCar.transform.up.y) > PeakBankAngle)
            PeakBankAngle = Mathf.Acos(BaseOfCar.transform.up.y);

        if (Mathf.Acos(BaseOfCar.transform.up.y) > Mathf.Deg2Rad * OverBankAngle)
        {
            HasReachedFlippingPoint = true;
            EndSimulation();
        }
    }
}
