using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformSaver
{
    public Vector3 Position { get; private set; }
    public Vector3 LocalPosition { get; private set; }

    public Vector3 EulerAngles { get; private set; }
    public Vector3 LocalEulerAngles { get; private set; }

    public Quaternion Rotation { get; private set; }
    public Quaternion LocalRotation { get; private set; }

    public Vector3 LocalScale { get; private set; }


    public Vector3 Forward { get; private set; }
    public Vector3 Right { get; private set; }
    public Vector3 Up { get; private set; }



    public TransformSaver(Transform transform)
    {
        Position = transform.position;
        LocalPosition = transform.localPosition;

        EulerAngles = transform.eulerAngles;
        LocalEulerAngles = transform.localEulerAngles;

        Rotation = transform.rotation;
        LocalRotation = transform.localRotation;

        LocalScale = transform.localScale;


        Forward = transform.forward;
        Right = transform.right;
        Up = transform.up;
    }
}
