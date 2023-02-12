using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vertex
{
    public string GroupCode { get; private set; }

    public Vector3 Point { get; private set; }

    public Vertex(string groupCode, Vector3 point)
    {
        GroupCode = groupCode;
        Point = point;
    }
}
