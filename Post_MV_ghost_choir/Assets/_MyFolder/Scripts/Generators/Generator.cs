using System;
using System.Linq;
using UnityEngine;

public class Generator : MonoBehaviour
{
    [SerializeField] protected Blend[] _blends;

    #region Animation
    [Serializable]
    protected class AnimationClip
    {

    }
    #endregion

    #region Virtual
    public virtual void Create() { }

    public virtual void Animation() { }
    #endregion

    #region Utility
    public void ChangeBlendStrength(string blendName, float strength)
    {
        Blend b = _blends.Where(t => t.BlendName == blendName).FirstOrDefault();
        if (b != null)
        {
            b.BlendStrength = strength;
        }
        else
        {
            Debug.Log(string.Format("Blend '{0}' not found", blendName));
        }
    }
    #endregion
}
