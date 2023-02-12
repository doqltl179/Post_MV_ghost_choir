using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnityUtilityMethod
{
    public static float InverseLerp(Vector3 a, Vector3 b, Vector3 value)
    {
        Vector3 AB = b - a;
        Vector3 AV = value - a;
        return Vector3.Dot(AV, AB) / Vector3.Dot(AB, AB);
    }

    public static bool IsInclude<T>(T[] array, T value)
    {
        foreach (T t in array)
        {
            if (EqualityComparer<T>.Default.Equals(t, value))
                return true;
        }

        return false;
    }
}
