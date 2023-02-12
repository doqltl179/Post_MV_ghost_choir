using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GD.MinMaxSlider;

[Serializable, CreateAssetMenu(fileName = "MVSequenceObject_", menuName = "MV/MVSequenceObject")]
public class MVSequenceObject : ScriptableObject
{
    public bool IsPlayed = false;


    [SerializeField, Range(0f, 1f)] private float _startNormalTime;
    public float StartNormalTime { get { return _startNormalTime; } }


    [Header("Sequence Camera Setting")]
    [SerializeField] private bool _changeCameraSetting = false;
    public bool ChangeCameraSetting { get { return _changeCameraSetting; } }


    [SerializeField] private SequenceCameraSetting _sequenceCameraSetting;
    public SequenceCameraSetting SequenceCameraSetting { get { return _sequenceCameraSetting; } }


    [Header("Sequence Character Setting")]
    [SerializeField] private bool _changeCharacterSetting = false;
    public bool ChangeCharacterSetting { get { return _changeCharacterSetting; } }


    [SerializeField] private SequenceCharacterSetting[] _sequenceCharacterSettings;
    public SequenceCharacterSetting[] SequenceCharacterSettings { get { return _sequenceCharacterSettings; } }



    [Header("Memo")]
    [SerializeField, TextArea] private string _tooltip = string.Empty;
}

public class MVSequence
{
    public string SequenceName { get; private set; } = string.Empty;

    public bool IsPlayed = false;
    public float StartNormalTime { get; private set; }



    #region Camera

    public bool ChangeCameraSetting { get; private set; }
    public SequenceCameraSetting SequenceCameraSetting { get; private set; } = null;

    #endregion



    #region Character

    public bool ChangeCharacterSetting { get; private set; }
    public SequenceCharacterSetting[] SequenceCharacterSettings { get; private set; } = null;

    #endregion



    public MVSequence(MVSequenceObject sequenceObject)
    {
        SequenceName = sequenceObject.name;

        StartNormalTime = sequenceObject.StartNormalTime;

        ChangeCameraSetting = sequenceObject.ChangeCameraSetting;
        if (ChangeCameraSetting)
            SequenceCameraSetting = sequenceObject.SequenceCameraSetting;

        ChangeCharacterSetting = sequenceObject.ChangeCharacterSetting;
        if (ChangeCharacterSetting)
            SequenceCharacterSettings = sequenceObject.SequenceCharacterSettings;
    }

    #region Utility
    public void Init()
    {
        IsPlayed = false;
    }
    #endregion
}



[Serializable] 
public class SequenceCharacterSetting
{
    [Header("Transform")]
    [SerializeField] private Vector3 _sequencePos;
    /// <summary>
    /// Sequence character position to this value
    /// </summary>
    public Vector3 SequencePos { get { return _sequencePos; } }


    [SerializeField] private Vector3 _sequenceRot;
    /// <summary>
    /// Sequence character eulerAngle to this value
    /// </summary>
    public Vector3 SequenceRot { get { return _sequenceRot; } }


    [Header("Blend Shape")]
    [SerializeField, Range(0f, 1f)] private float _eyeBoring = 1f;
    public float EyeBoring { get { return _eyeBoring; } }


    [Header("Camera")]
    [SerializeField] private bool _lookAtCamera;
    /// <summary>
    /// true : Character look at the mainCamera
    /// </summary>
    public bool LookAtCamera { get { return _lookAtCamera; } }


    [SerializeField] private bool _cameraTracking;
    /// <summary>
    /// true : Camera follow this character
    /// </summary>
    public bool CameraTracking { get { return _cameraTracking; } }


    [SerializeField, Range(1f, 5f)] private float _cameraTrackingWeight = 1f;
    public float CameraTrackingWeight { get { return _cameraTrackingWeight; } }


    [Header("Audio")]
    [SerializeField, MinMaxSlider(0, 8192)] private Vector2Int _samplesIndexRange = new Vector2Int(0, 8192);
    /// <summary>
    /// 0 <= indexes < 8192
    /// </summary>
    public Vector2Int SamplesIndexRange { get { return _samplesIndexRange; } }


    [SerializeField] private bool _inverseOctave = false;
    public bool InverseOctave { get { return _inverseOctave; } }


    [SerializeField, Range(0, 9)] private int[] _audioSampleOctaveIndexes;
    public int[] AudioSampleOctaveIndexes { get { return _audioSampleOctaveIndexes; } }
}



[Serializable]
public class SequenceCameraSetting
{
    [SerializeField, Range(0.1f, 5f)] private float _cameraDistanceOffset = 1f;
    public float CameraDistanceOffset { get { return _cameraDistanceOffset; } }
}