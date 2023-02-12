using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class RigBone : MonoBehaviour
{
    [HideInInspector] public BoneInfo[] Bones;
    [HideInInspector] public BoneInfo RootBone;

    [SerializeField] private string _rootBoneName = "Hips";

    private void Start()
    {
        SetProperties();
    }

    private void Update()
    {
        for(int i = 1; i < Bones.Length; i++)
        {
            SetBonePos(Bones[i]);
        }
    }

    BoneInfo parentBone;
    private void SetBonePos(BoneInfo boneInfo)
    {
        if (boneInfo.IsConnected)
        {
            parentBone = boneInfo.transform.parent.GetComponent<BoneInfo>();
            if (parentBone != null)
            {
                boneInfo.transform.position = parentBone.transform.position + parentBone.transform.up * parentBone.BoneSize;
            }
        }
    }

    #region Utility
    public Transform GetRootBone()
    {
        return RootBone.transform;
    }

    public Transform GetBone(string boneName)
    {
        BoneInfo bone = Bones.Where(t => t.name == boneName).FirstOrDefault();
        return bone.transform;
    }

    public Transform[] GetBones()
    {
        Transform[] bones = new Transform[Bones.Length];
        for (int i = 0; i < bones.Length; i++)
        {
            bones[i] = Bones[i].transform;
        }

        return bones;

        //return GetComponentsInChildren<Transform>();
    }

    public int GetBoneIndex(string boneName)
    {
        int index = Array.FindIndex(Bones, t => t.name == boneName);

        return index;
    }

    public void SetProperties()
    {
        Transform[] bones = transform.GetComponentsInChildren<Transform>();

        bones = bones.Where(t => t.tag != "BoneIgnore").ToArray();

        if (!CheckBoneUsingCorrectly(bones))
        {
            return;
        }

        //Remove this
        Bones = new BoneInfo[bones.Length - 1];
        BoneInfo bone;
        for (int i = 0; i < Bones.Length; i++)
        {
            bone = bones[i + 1].GetComponent<BoneInfo>();
            if (bone == null)
            {
                bone = bones[i + 1].gameObject.AddComponent<BoneInfo>();
            }

            Bones[i] = bone;
        }

        RootBone = Bones.Where(t => t.name == _rootBoneName).FirstOrDefault();
        if (RootBone == null)
        {
            Debug.LogError(string.Format("Can not found root bone. Name : {0}", _rootBoneName));

            return;
        }
    }

    private bool CheckBoneUsingCorrectly(Transform[] bones)
    {
        //Check Name
        string checkName;
        string compareName;
        for(int i = 0; i < bones.Length - 1; i++)
        {
            checkName = bones[i].name;
            for(int j = i + 1; j < bones.Length; j++)
            {
                compareName = bones[j].name;

                if(checkName == compareName)
                {
                    Debug.LogError(string.Format("Exist same bone. Name : {0}", checkName));

                    return false;
                }
            }
        }

        return true;
    }
    #endregion
}
