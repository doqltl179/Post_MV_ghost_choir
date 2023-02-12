using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoneInfo : MonoBehaviour
{
    public bool IsConnected = false;
    public float BoneSize = 0.1f;

    public Vector3 BonePos
    {
        get
        {
            return (BoneStartPos + BoneEndPos) * 0.5f;
        }
    }
    public Vector3 BoneStartPos
    {
        get
        {
            return transform.position;
        }
    }
    public Vector3 BoneEndPos
    {
        get
        {
            return transform.position + transform.up * BoneSize;
        }
    }

    public Vector3 BoneDirection
    {
        get
        {
            return transform.up;
        }
    }

    #region Utility
    public float GetDistance(Vector3 vertPos)
    {
        return Vector3.Distance(transform.position, vertPos);
    }

    public BoneInfo GetParentBone()
    {
        return transform.parent.GetComponent<BoneInfo>();
    }

    public BoneInfo[] GetChildBones()
    {
        List<BoneInfo> childBones = new List<BoneInfo>();
        for(int i = 0; i < transform.childCount; i++)
        {
            BoneInfo child = transform.GetChild(i).GetComponent<BoneInfo>();
            if (child != null && child.gameObject.activeSelf)
                childBones.Add(child);
        }

        return childBones.ToArray();
    }

    public void GetDot(Vector3 vertPos, out float startDot, out float endDot)
    {
        startDot = Vector3.Dot(transform.up, (vertPos - BoneStartPos).normalized);
        endDot = Vector3.Dot(-transform.up, (vertPos - BoneEndPos).normalized);
    }

    /// <summary>
    /// StartPos -> EndPos, 0f -> 1f
    /// </summary>
    /// <param name="vertPos"></param>
    /// <returns></returns>
    public float GetLerp(Vector3 vertPos)
    {
        float startPosToVertPosDistance = Vector3.Distance(BoneStartPos, vertPos);
        float angle = Vector3.Angle((BoneEndPos - BoneStartPos).normalized, (vertPos - BoneStartPos).normalized) * Mathf.Deg2Rad;

        float lerpLength = startPosToVertPosDistance * Mathf.Cos(angle);

        return lerpLength / BoneSize;
    }
    #endregion
}
