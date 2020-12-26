using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class IKLeg : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float turnSpeed;
    public Transform target;
    public List<Transform> bones;
    private List<float> boneLengths = new List<float>();
    private float angleTolerate = 3f;
    private float legLength;
    private float distance;
    private float ikDistanceTolerate = 0.005f;
    private Nullable<float> prevIKDistance = null;
    private Vector3 prevTargetPos = new Vector3();

    // Start is called before the first frame update
    void Start()
    {
        CalcBoneLength();
        CalcLegLength();

        boneLengths.ForEach(x => { Debug.Log("----> " + x); });
        Debug.Log("total Length: " + legLength);
        print("======>" + (bones[2].position - target.position).magnitude);
    }

    private void CalcDistance()
    {
        distance = (bones[0].position - target.position).magnitude;
    }

    private void CalcLegLength()
    {
        legLength = boneLengths.Sum();
    }

    private void CalcBoneLength()
    {
        for (int i = 0; i < bones.Count - 1; i++)
        {
            var magnitude = (bones[i].position - bones[i + 1].position).magnitude;
            boneLengths.Add(magnitude);
        }
    }

    // Update is called once per frame
    void Update()
    {
        CalcDistance();
        RotateYAxis();
        
        // Check the distance
        if (distance > legLength)
        {
            print("loooooo");
            StretchLeg();
            (var angle, var rotation) =
                RotateByPos(bones.Last().position, target.position, bones[0].position, bones[0].rotation);
            if (Mathf.Abs(angle) > angleTolerate)
            {
                bones[0].rotation = rotation;
            }
        }
        else
        {
            // IK
            IKResolve();
        }

        //
        // // right
        // if (Input.GetKey(KeyCode.LeftShift))
        // {
        //     for (int i = 0; i < bones.Count; i++)
        //     {
        //         if (Input.GetKey((i + 1).ToString()))
        //         {
        //             RotateTransform(bones[i], "down");
        //         }
        //     }
        // }
        // else
        // {
        //     // left
        //     for (int i = 0; i < bones.Count; i++)
        //     {
        //         if (Input.GetKey((i + 1).ToString()))
        //         {
        //             RotateTransform(bones[i], "up");
        //         }
        //     }
        // }
        //
        // if (Input.GetKey(KeyCode.Space))
        // {
        //     StretchLeg();
        // }

        prevTargetPos = target.position;
    }

    private (float angle, Quaternion rotation) RotateByPos(Vector3 fromPos, Vector3 toPos, Vector3 basePos, Quaternion roBone,
        bool isRotateYAxis = false)
    {
        // just rotate
        // vector1
        var v1 = basePos - toPos;
        // vector2
        var v2 = basePos - fromPos;
        float angle;
        Quaternion rotation = new Quaternion();

        if (isRotateYAxis)
        {
            angle = Vector3.SignedAngle(v2, v1, Vector3.up);
            rotation = Quaternion.Euler(0, angle * Time.deltaTime * turnSpeed, 0) * roBone;
        }
        else
        {
            angle = Vector3.SignedAngle(v2, v1, Vector3.forward);
            rotation = Quaternion.Euler(0, 0, angle * Time.deltaTime * turnSpeed) * roBone;
        }

        return (angle, rotation);
    }

    private void RotateYAxis()
    {
        // Rotate Y axis before do IK
        // Check Z diff
        var tPos = target.position;
        var b0Pos = bones[0].position;
        var b2Pos = bones[2].position;
        float zDiff = Mathf.Abs(tPos.z - prevTargetPos.z);
        if (zDiff > 0)
        {
            bones[0].rotation =
                RotateByPos(b2Pos, tPos, b0Pos, bones[0].rotation, true).rotation;
        }
    }

    private void IKResolve()
    {
        // Find the position of target
        var tPos = target.position;
        var b0Pos = bones[0].position;
        var b1Pos = bones[1].position;
        var b2Pos = bones[2].position;

        var bLen0 = boneLengths[0];
        var bLen1 = boneLengths[1];

        // IK - Frabric
        var __b1Pos = b1Pos;
        var __b2Pos = new Vector3();

        for (int i = 0; i < 10; i++)
        {
            // Backward
            var _b2Pos = tPos;
            var _b1Pos = _b2Pos + (__b1Pos - _b2Pos).normalized * bLen1;
            // Forward
            __b1Pos = b0Pos + (_b1Pos - b0Pos).normalized * bLen0;
            __b2Pos = __b1Pos + (_b2Pos - __b1Pos).normalized * bLen1;
        }

        var currentIKDistance = (__b2Pos - tPos).magnitude;

        // Find angle and rotation
        Quaternion ro0, ro1;
        ro0 = RotateByPos(b1Pos, __b1Pos, b0Pos, bones[0].rotation).rotation;
        ro1 = RotateByPos(b2Pos, __b2Pos, __b1Pos, bones[1].rotation).rotation;
        bones[0].rotation = ro0;
        bones[1].rotation = ro1;

        // Update prevIKDistance
        prevIKDistance = currentIKDistance;
    }

    private void StretchLeg()
    {
        var v1 = bones[1].position - bones[0].position;
        var v2 = bones[2].position - bones[1].position;

        var _b2Pos = bones[1].position + v1.normalized * boneLengths[1];
        bones[1].rotation =  RotateByPos(bones[2].position, _b2Pos,  bones[1].position, bones[1].rotation).rotation;
    }

    // private void RotateLegByPos(int boneIndex, string direction)
    // {
    //     var foundBone = bones[boneIndex];
    //     if (foundBone)
    //     {
    //         RotateTransform(foundBone, direction);
    //     }
    //     else
    //     {
    //         throw new NullReferenceException($"Cannot found this bone with index is {boneIndex}");
    //     }
    // }

    // private void RotateTransform(Transform bone, string direction = "down")
    // {
    //     var rotateVector = direction == "up" ? Vector3.back : Vector3.forward;
    //
    //     bone.Rotate(rotateVector, turnSpeed * Time.deltaTime);
    // }
}