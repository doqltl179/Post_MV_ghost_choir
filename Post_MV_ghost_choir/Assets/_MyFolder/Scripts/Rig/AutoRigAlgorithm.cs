using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AutoRigAlgorithm
{
    class AutoRigScore
    {
        public BoneInfo BoneInfo { get; private set; }
        public float Score { get; private set; }

        public AutoRigScore(BoneInfo boneInfo, float score)
        {
            BoneInfo = boneInfo;
            Score = score;
        }
    }

    private const float AutoRigBasicWeight = 0.7f;



    //-------------------------
    //-----------Top-----------
    //-------------------------
    //收收收收收收收收收收收收收收收收收收收收收收收收收 Bone End Pos
    //------------|------------
    //------------|------------
    //------------|------------
    //----------Middle--------- half
    //------------|------------
    //------------|------------
    //------------|------------
    //收收收收收收收收收收收收O收收收收收收收收收收收收 Bone Start Pos
    //-------------------------
    //----------Bottom---------
    //-------------------------



    public static RigInfo[] GetAutoRig(Vector3 vertPos, BoneInfo[] bones)
    {
        List<RigInfo> rigInfos = new List<RigInfo>();

        //BoneInfo firstBone = GetNearBone(vertPos, bones, false, true);
        //float firstBoneLerp = firstBone.GetLerp(vertPos);
        //rigInfos.Add(new RigInfo(firstBone, Mathf.Lerp(1f, 0f, Mathf.Abs(firstBoneLerp))));

        ////BoneInfo secondBone = GetNearBone(vertPos, bones, false, false);
        //BoneInfo secondBone = GetNearBoneInChildOrParent(vertPos, firstBone);
        //if (secondBone != null)
        //{
        //    float secondBoneLerp = secondBone.GetLerp(vertPos);
        //    float secondBoneWeight = Mathf.Lerp(1f, 0f, Mathf.InverseLerp(0f, 2f, secondBoneLerp));
        //    if (secondBoneWeight != 0) rigInfos.Add(new RigInfo(secondBone, secondBoneWeight));
        //}

        float lerp;
        float weight;
        for(int i = 0; i < bones.Length; i++)
        {
            lerp = bones[i].GetLerp(vertPos);
            weight = Mathf.Lerp(0.7f, 0f, Mathf.Sqrt(Mathf.InverseLerp(0f, 1.5f, Mathf.Abs(lerp))));
            if(weight != 0)
            {
                rigInfos.Add(new RigInfo(bones[i], weight));
            }
        }

        rigInfos = rigInfos.OrderByDescending(t => t.Weight).ToList();
        if(rigInfos.Count > 4)
        {
            while(rigInfos.Count > 4)
            {
                rigInfos.RemoveAt(4);
            }
        }

        return rigInfos.ToArray();
    }

    private static float GetLerp(Vector3 vertPos, Vector3 point_1, Vector3 point_2)
    {
        float startPosToVertPosDistance = Vector3.Distance(point_1, vertPos);
        float angle = Vector3.Angle((point_2 - point_1).normalized, (vertPos - point_1).normalized) * Mathf.Deg2Rad;

        float lerpLength = startPosToVertPosDistance * Mathf.Cos(angle);

        float pointDistance = Vector3.Distance(point_1, point_2);

        return lerpLength / pointDistance;
    }

    private static BoneInfo GetNearReversedChildBone(Vector3 vertPos, BoneInfo parentBone)
    {
        BoneInfo[] childBones = parentBone.GetChildBones();
        if(childBones.Length > 1)
        {
            Dictionary<BoneInfo, float> distanceSaver = new Dictionary<BoneInfo, float>();
            for(int i = 0; i < childBones.Length; i++)
            {
                distanceSaver.Add(childBones[i], childBones[i].GetDistance(vertPos));
            }

            distanceSaver = distanceSaver.OrderBy(t => t.Value).ToDictionary(t => t.Key, t => t.Value);
            KeyValuePair<BoneInfo, float> saver;
            for (int i = 0; i < distanceSaver.Count; i++)
            {
                saver = distanceSaver.ElementAt(i);
                if(Vector3.Dot(parentBone.BoneDirection, saver.Key.BoneDirection) < 0)
                {
                    return saver.Key;
                }
            }

            return null;
        }
        else if(childBones.Length == 1)
        {
            return Vector3.Dot(parentBone.BoneDirection, childBones[0].BoneDirection) < 0 ? childBones[0] : null;
        }
        else
        {
            return null;
        }
    }

    private static BoneInfo GetNearBoneInChildOrParent(Vector3 vertPos, BoneInfo bone)
    {
        List<AutoRigScore> scoreBoard = new List<AutoRigScore>();

        if(bone.transform.parent != null)
        {
            BoneInfo parentBone = bone.transform.parent.GetComponent<BoneInfo>();
            if (parentBone != null)
            {
                float parentScore = GetScore(vertPos, parentBone, true);
                if (parentScore != 0)
                {
                    scoreBoard.Add(new AutoRigScore(parentBone, parentScore));
                }
            }
        }

        if(bone.transform.childCount > 0)
        {
            Transform child;
            for(int i = 0; i < bone.transform.childCount; i++)
            {
                child = bone.transform.GetChild(i);
                if (child.gameObject.activeSelf)
                {
                    BoneInfo b = child.GetComponent<BoneInfo>();
                    if (b != null)
                    {
                        float childScore = GetScore(vertPos, b, true);
                        if (childScore != 0)
                        {
                            scoreBoard.Add(new AutoRigScore(b, childScore));
                        }
                    }
                }
            }
        }

        if (scoreBoard.Count == 0)
        {
            return null;
        }
        else
        {
            scoreBoard = scoreBoard.OrderBy(t => t.Score).ToList();

            return scoreBoard[0].BoneInfo;
        }
    }

    private static BoneInfo GetNearBone(Vector3 vertPos, BoneInfo[] bones, bool ignoreFirstBone, bool checkWithStartPos)
    {
        AutoRigScore[] scoreBoard = null;
        int index = -1;

        if(ignoreFirstBone)
        {
            scoreBoard = new AutoRigScore[bones.Length - 1];
            index = 1;
        }
        else
        {
            scoreBoard = new AutoRigScore[bones.Length];
            index = 0;
        }

        for (; index < scoreBoard.Length; index++)
        {
            scoreBoard[index] = new AutoRigScore(bones[index], GetScore(vertPos, bones[index], checkWithStartPos));
        }

        scoreBoard = scoreBoard.OrderBy(t => t.Score).ToArray();

        return scoreBoard[0].BoneInfo;
    }

    private static float GetScore(Vector3 vertPos, BoneInfo boneInfo, bool checkWithStartPos)
    {
        //float startPointDistance = Vector3.Distance(vertPos, boneInfo.BoneStartPos);
        //float endPointDistance = Vector3.Distance(vertPos, boneInfo.BoneEndPos);

        //return startPointDistance + endPointDistance;

        return Vector3.Distance(vertPos, checkWithStartPos ? boneInfo.BoneStartPos : boneInfo.BoneEndPos);
    }
}
