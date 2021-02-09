using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script for the goal, for informing the car on collision that it has reached the goal.
/// </summary>
public class Goal : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.gameObject.CompareTag("Car"))
        {
            col.gameObject.transform.parent.gameObject.GetComponent<Car>().GoalReached();
        }
    }
}
