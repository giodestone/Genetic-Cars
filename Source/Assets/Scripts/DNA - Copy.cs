//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.Assertions;

//public class DNA : MonoBehaviour
//{
//    public float LeftWheelRadius;
//    public float RightWheelRadius;
//    public float LeftWheelPosition; // Percent from right
//    public float RightWheelPosition; // Percent from right
//    public float Speed;

//    public float BaseLength;
//    public float BaseHeight;
//    public void Init()
//    {
//        // Get game objects.
//        GameObject leftWheelGameObject = null;
//        GameObject rightWheelGameObject = null;
//        GameObject platformGameObj = null;
        
//        for (int i = 0; i < transform.childCount; ++i)
//        {
//            var currentChild = gameObject.transform.GetChild(i).gameObject;
//            if (currentChild.name == "WheelLeft")
//            {
//                leftWheelGameObject = currentChild;
//            }
//            else if (currentChild.name == "WheelRight")
//            {
//                rightWheelGameObject = currentChild;
//            }
//            else if (currentChild.name == "Platform")
//            {
//                platformGameObj = currentChild;
//            }
//        }

//        // Setup base height and length.
//        platformGameObj.transform.localScale = new Vector3(BaseLength, BaseHeight, 0f);

//        // Setup wheel radius.
//        rightWheelGameObject.transform.localScale = new Vector3(RightWheelRadius, RightWheelRadius, 1f);
//        leftWheelGameObject.transform.localScale = new Vector3(LeftWheelRadius, LeftWheelRadius, 1f);

//        // Setup wheel position.
//        float totalBaseWidth = platformGameObj.transform.localScale.x *
//                               platformGameObj.GetComponent<BoxCollider2D>

//            rightWheelGameObject.transform.localPosition = new Vector3();

//        // Setup motor speed.
//        var newMotor = new JointMotor2D();
//        newMotor.motorSpeed = 2f;
//        newMotor.maxMotorTorque = 1000000f;
        
//        rightWheelGameObject.GetComponent<HingeJoint2D>().useMotor = true;
//        rightWheelGameObject.GetComponent<HingeJoint2D>().motor = newMotor;

//        leftWheelGameObject.GetComponent<HingeJoint2D>().useMotor = true;
//        leftWheelGameObject.GetComponent<HingeJoint2D>().motor = newMotor;
//    }
//}
