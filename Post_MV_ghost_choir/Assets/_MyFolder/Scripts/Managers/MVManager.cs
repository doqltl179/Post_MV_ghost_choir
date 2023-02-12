using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;
using UnityEngine.Rendering.PostProcessing;
using System.Reflection;

public class MVManager : MonoBehaviour
{
    private static MVManager _instance;
    public static MVManager Instance
    {
        get
        {
            if(_instance == null)
            {
                _instance = FindObjectOfType<MVManager>();
            }

            return _instance;
        }
    }

    [Header("Sequences")]
    [SerializeField] private MVSequenceObject[] _mvSequenceObjects;
    private MVSequence[] _mvSequences;
    public int _currentSequenceIndex = -1;

    [Header("Character Props")]
    [SerializeField] private CharacterPackage[] _packages;
    private List<UsedCharacter> _currentUsedCharacterList = new List<UsedCharacter>();
    private List<UsedCharacter> _trackingCharacterList = new List<UsedCharacter>();

    [Header("Camera Proerties")]
    [SerializeField] private Camera _moveCamera;
    private Vector3 _cameraInitialPosition = new Vector3(0f, 3f, 7f);
    private Quaternion _cameraInitialRotation = Quaternion.Euler(15f, 180f, 0f);
    [Range(0.1f, 5f)] public float _cameraMoveSpeed = 0.8f;
    [Range(0.1f, 5f)] public float _cameraRotateSpeed = 1.4f;
    private float _cameraDistanceOffset = 1f;

    [Header("Light Properties")]
    [SerializeField] private Light[] _spotLights;
    [SerializeField] private Transform _allLightingPoint;
    [Range(0.1f, 10f)] public float _lightRotateSpeed = 1.2f;
    [Range(0.1f, 5f)] public float _lightSpotIntensity_spot = 1.4f;
    [Range(0.1f, 5f)] public float _lightSpotIntensity_all = 2.8f;
    [Range(5f, 180f)] public float _lightSpotAngle_spot = 9f;
    [Range(5f, 180f)] public float _lightSpotAngle_all = 78f;

    [Header("Post")]
    [SerializeField] private PostProcessProfile _postProfile;
    private Bloom _bloom;
    private const float BloomIntensity_normal = 4f;
    private const float BloomIntensity_height = 30f;
    //private Vignette _vignette;
    //private const float VignetteIntensity_beforeClimax_spot = 0.21f;
    //private const float VignetteIntensity_beforeClimax_all = 0.125f;
    //private const float VignetteIntensity_afterClimax_spot = 0.21f;
    //private const float VignetteIntensity_afterClimax_all = 0f;

    [Header("Effects")]
    [SerializeField] private ParticleSystem _climaxGlitter;

    private List<int> _inputKeypadNumList = new List<int>();

    [Header("Climax")]
    [Range(0f, 1f), SerializeField] private float _climaxStartTimeNormal;
    private bool _isClimax = false;
    private IEnumerator _climaxStartActionCoroutine = null;
    public Action ClimaxStarted;

    private int layer_DoNotShow;
    private int layer_Default;



    private void Awake()
    {
#if !UNITY_EDITOR
        GameObject testObject = GameObject.Find("Test");
        if (testObject != null)
            Destroy(testObject);
#endif

        //_mvSequences = new MVSequence[_mvSequenceObjects.Length];
        //MVSequenceObject so;
        //for(int i = 0; i < _mvSequences.Length; i++)
        //{
        //    so = _mvSequenceObjects[i];

        //    _mvSequences[i] = new MVSequence(so);
        //}
        //_mvSequences = _mvSequences.OrderBy(t => t.StartNormalTime).ToArray();
    }

    private void Start()
    {
        layer_DoNotShow = LayerMask.NameToLayer("DoNotShow");
        layer_Default = LayerMask.NameToLayer("Default");

        for(int i = 0; i < _packages.Length; i++)
        {
            _packages[i].Init();
        }

        if (_postProfile.TryGetSettings<Bloom>(out _bloom))
        {
            _bloom.intensity.Override(BloomIntensity_normal);
        }
        else
        {
            Debug.Log("Failed get Bloom settings");
        }

        //if (_postProfile.TryGetSettings<Vignette>(out _vignette))
        //{
        //    _vignette.intensity.Override(VignetteIntensity_beforeClimax_all);
        //}
        //else
        //{
        //    Debug.Log("Failed get Vignette settings");
        //}

        //_climaxGlitter.gameObject.SetActive(false);
        _climaxGlitter.gameObject.SetActive(true);
        _climaxGlitter.gameObject.layer = layer_DoNotShow;

        _isClimax = false;

        FrameLimit();

        ClimaxStarted += ClimaxStartAction;

        VideoManager.Instance.VideoPreparedAction += InitializeSequence;

        VideoManager.Instance.VideoPlayAction += FrameUnlimit;
        VideoManager.Instance.VideoPlayAction += StartSequences;

        VideoManager.Instance.VideoEndedAction += VideoEndAction;
    }

    private IEnumerator _videoEndActionCoroutine;
    public void VideoEndAction()
    {
        if(_videoEndActionCoroutine == null)
        {
            _videoEndActionCoroutine = VideoEndActionCoroutine();
            StartCoroutine(_videoEndActionCoroutine);
        }
    }

    private IEnumerator VideoEndActionCoroutine()
    {
        //yield return new WaitForSeconds(1f);

        float fadeTime, timer;

        //float lightSpotAngle = _spotLights[0].spotAngle;
        float lightIntensity = _spotLights[0].intensity;
        fadeTime = 5f;
        timer = 0;
        while(timer < fadeTime)
        {
            timer += Time.deltaTime;

            foreach(Light l in _spotLights)
            {
                //l.spotAngle = Mathf.Lerp(lightSpotAngle, 0f, timer / fadeTime);
                l.intensity = Mathf.Lerp(lightIntensity, 0f, timer / fadeTime);
            }

            yield return null;
        }
        foreach (Light l in _spotLights)
        {
            //l.spotAngle = 0f;
            l.intensity = 0f;
        }

        //UIManager playing Ending Action during 5s.

        _videoEndActionCoroutine = null;
    }

    public void FrameLimit()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 1;
    }

    public void FrameUnlimit()
    {
        Application.targetFrameRate = -1;
        QualitySettings.vSyncCount = 0;
    }

    public void StartSequences()
    {
        _currentSequenceIndex = 0;
        _currentUsedCharacterList.Clear();

        for (int i = 0; i < _mvSequences.Length; i++)
        {
            _mvSequences[i].Init();
        }
    }

    public void InitializeSequence()
    {
        if(_videoEndActionCoroutine != null)
        {
            StopCoroutine(_videoEndActionCoroutine);
            _videoEndActionCoroutine = null;
        }

        _mvSequences = new MVSequence[_mvSequenceObjects.Length];
        MVSequenceObject so;
        for (int i = 0; i < _mvSequences.Length; i++)
        {
            so = _mvSequenceObjects[i];

            _mvSequences[i] = new MVSequence(so);
        }
        _mvSequences = _mvSequences.OrderBy(t => t.StartNormalTime).ToArray();

        _currentSequenceIndex = -1;
        _currentUsedCharacterList.Clear();

        _climaxGlitter.gameObject.layer = layer_DoNotShow;
        _bloom.intensity.Override(BloomIntensity_normal);

        for(int i = 0; i < _packages.Length; i++)
        {
            _packages[i].Character.SetEyeBlendingValue_Boring(0.9f);
        }
    }

    private void OnDestroy()
    {
        ClimaxStarted -= ClimaxStartAction;

        VideoManager.Instance.VideoPreparedAction -= InitializeSequence;

        VideoManager.Instance.VideoPlayAction -= FrameUnlimit;
        VideoManager.Instance.VideoPlayAction -= StartSequences;

        VideoManager.Instance.VideoEndedAction -= VideoEndAction;

        if (_climaxStartActionCoroutine != null)
        {
            StopCoroutine(_climaxStartActionCoroutine);
            _climaxStartActionCoroutine = null;
        }
    }

    private void ClimaxStartAction()
    {
        if(_climaxStartActionCoroutine == null)
        {
            _climaxStartActionCoroutine = ClimaxStartActionCoroutine();
            StartCoroutine(_climaxStartActionCoroutine);
        }
    }

    private IEnumerator ClimaxStartActionCoroutine()
    {
        _climaxGlitter.gameObject.SetActive(true);

        yield return new WaitForSeconds(0.5f);

        _climaxGlitter.gameObject.layer = layer_Default;
        //_vignette.intensity.Override(VignetteIntensity_afterClimax_all);



        yield return new WaitForSeconds(1f);

        //_moveCamera.transform.position = Vector3.zero + Vector3.up * 10f + Vector3.forward * 5f;
        _moveCamera.transform.position = Vector3.right * 0f + Vector3.up * 7f + Vector3.forward * 15f;
        _moveCamera.transform.forward = Vector3.back;
        _moveCamera.transform.eulerAngles += Vector3.right * -30f;

        for (int i = 0; i < _spotLights.Length; i++)
        {
            _spotLights[i].intensity = _lightSpotIntensity_spot;
            _spotLights[i].spotAngle = _lightSpotAngle_spot;
        }

        const float bloomOffsetBefore = 0.15f;
        const float bloomOffsetAfter = 1f - bloomOffsetBefore;

        float time = 1.5f;
        float t = 0;
        while (t < time)
        {
            t += Time.deltaTime;

            _bloom.intensity.Override(Mathf.Lerp(BloomIntensity_normal, BloomIntensity_height, t / time * bloomOffsetBefore));

            yield return null;
        }



        time = 1.0f;
        t = 0;
        while(t < time)
        {
            t += Time.deltaTime;

            _bloom.intensity.Override(Mathf.Lerp(BloomIntensity_normal, BloomIntensity_height, bloomOffsetBefore + (t / time * bloomOffsetAfter)));

            yield return null;
        }
        _bloom.intensity.Override(BloomIntensity_height);

        _climaxStartActionCoroutine = null;
    }

    MVSequence currentSequence;
    private void Update()
    {
        if (VideoManager.Instance.State == VideoManager.VideoState.Playing)
        {
            for (int i = 0; i < _packages.Length; i++)
            {
                if (_packages[i].Character.gameObject.activeSelf)
                {
                    _packages[i].SetAudioSampleValue();
                    _packages[i].SetCharacterViewportPos(_moveCamera);
                }
            }



            if(0 <= _currentSequenceIndex && _currentSequenceIndex < _mvSequences.Length)
            {
                currentSequence = _mvSequences[_currentSequenceIndex];
                if (currentSequence.IsPlayed)
                {
                    _currentSequenceIndex++;
                }
                else
                {
                    if (currentSequence.StartNormalTime < VideoManager.Instance.NormalizeTime)
                    {
                        try
                        {
                            while(true)
                            {
                                if(_mvSequences[_currentSequenceIndex].StartNormalTime < VideoManager.Instance.NormalizeTime &&
                                _mvSequences[_currentSequenceIndex + 1].StartNormalTime > VideoManager.Instance.NormalizeTime)
                                {
                                    break;
                                }
                                else
                                {
                                    currentSequence.IsPlayed = true;

                                    _currentSequenceIndex++;
                                    currentSequence = _mvSequences[_currentSequenceIndex];
                                }
                            }

                            SequenceAction(currentSequence);
                        }
                        catch
                        {
                            Debug.LogError("CurrentSequenceIndex out of range");

                            SequenceAction(currentSequence);
                            _currentSequenceIndex++;
                        }
                    }
                }

                SetCamera(_trackingCharacterList);
                SetLights(_trackingCharacterList);
            }

            //SetCamera(_trackingCharacterList);
            //SetLights(_trackingCharacterList);



            if (!_isClimax)
            {
                if (VideoManager.Instance.NormalizeTime > _climaxStartTimeNormal)
                {
                    ClimaxStarted?.Invoke();

                    _isClimax = true;
                }
            }
        }
        else if (VideoManager.Instance.State == VideoManager.VideoState.Ended)
        {
            SetCamera(_trackingCharacterList);
            //SetLights(_trackingCharacterList);
        }

#if UNITY_EDITOR
        if (VideoManager.Instance.State == VideoManager.VideoState.Playing || VideoManager.Instance.State == VideoManager.VideoState.Paused)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                float normalizeTime = 0;
                VideoManager.Instance.JumpToFrontAFewSeconds(3f, out normalizeTime);

                for (int i = _mvSequences.Length - 1; i >= 0; i--)
                {
                    if (_mvSequences[i].IsPlayed)
                    {
                        _currentSequenceIndex--;
                        if (_currentSequenceIndex < 0)
                            _currentSequenceIndex = 0;

                        if (_mvSequences[i].StartNormalTime > normalizeTime)
                        {
                            _mvSequences[i].IsPlayed = false;
                        }
                        else
                        {
                            SequenceAction(_mvSequences[_currentSequenceIndex]);

                            break;
                        }
                    }
                }
            }
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                float normalizeTime = 0;
                VideoManager.Instance.JumpToBackAFewSeconds(3f, out normalizeTime);
            }
        }

        if(Input.GetKeyDown(KeyCode.R))
        {
            InitializeSequence();
            StartSequences();
        }

        if(_isClimax && VideoManager.Instance.NormalizeTime < _climaxStartTimeNormal)
        {
            _isClimax = false;
        }
#endif
    }

    private void SetCamera(List<UsedCharacter> usedCharacters)
    {
        if (usedCharacters.Count > 0)
        {
            Vector3 charactersMiddlePos = Vector3.zero;
            float characterCountOffset = 0;
            float trackingWeight;
            for (int i = 0; i < usedCharacters.Count; i++)
            {
                //charactersMiddlePos += usedCharacters[i].Position;
                //charactersMiddlePos += usedCharacters[i].HeadPosition;

                trackingWeight = usedCharacters[i].CharacterSetting.CameraTrackingWeight;

                charactersMiddlePos += usedCharacters[i].HeadPosition * trackingWeight;
                characterCountOffset += (trackingWeight - 1f);
            }
            charactersMiddlePos /= (usedCharacters.Count + characterCountOffset);

            Vector3 middlePosToCamera = (_cameraInitialPosition - charactersMiddlePos).normalized;
            Vector3 cameraPos = Vector3.zero;
            if (usedCharacters.Count > 1)
            {
                UsedCharacter mostLeftCharacter, mostRightCharacter;
                if (usedCharacters[0].ViewportPos.x < usedCharacters[1].ViewportPos.x)
                {
                    mostLeftCharacter = usedCharacters[0];
                    mostRightCharacter = usedCharacters[1];
                }
                else
                {
                    mostLeftCharacter = usedCharacters[1];
                    mostRightCharacter = usedCharacters[0];
                }

                for (int i = 2; i < usedCharacters.Count; i++)
                {
                    if (usedCharacters[i].ViewportPos.x < mostLeftCharacter.ViewportPos.x)
                        mostLeftCharacter = usedCharacters[i];
                    else if (mostRightCharacter.ViewportPos.x < usedCharacters[i].ViewportPos.x)
                        mostRightCharacter = usedCharacters[i];
                }

                float characterDistance = Vector3.Distance(mostLeftCharacter.Position, mostRightCharacter.Position) * _cameraDistanceOffset;
                cameraPos = charactersMiddlePos + middlePosToCamera * (characterDistance + 1f / usedCharacters.Count) + Vector3.up * Mathf.Pow(characterDistance * 0.02f, 2);
            }
            else
            {
                float characterDistance = 1.8f * _cameraDistanceOffset;
                cameraPos = charactersMiddlePos + middlePosToCamera * characterDistance + Vector3.up * Mathf.Pow(characterDistance * 0.02f, 2);
            }
            _moveCamera.transform.position = Vector3.Lerp(_moveCamera.transform.position, cameraPos, Time.deltaTime * _cameraMoveSpeed);

            Quaternion cameraRotation = _moveCamera.transform.rotation;
            _moveCamera.transform.LookAt(charactersMiddlePos);
            _moveCamera.transform.rotation = Quaternion.Lerp(cameraRotation, _moveCamera.transform.rotation, Time.deltaTime * _cameraRotateSpeed);
        }
        else
        {
            _moveCamera.transform.position = Vector3.Lerp(_moveCamera.transform.position, _cameraInitialPosition, Time.deltaTime * _cameraMoveSpeed);
            _moveCamera.transform.rotation = Quaternion.Lerp(_moveCamera.transform.rotation, _cameraInitialRotation, Time.deltaTime * _cameraRotateSpeed);
        }
    }

    private void SetLights(List<UsedCharacter> usedCharacters)
    {
        if (usedCharacters.Count > 0)
        {
            Vector3 charactersMiddlePos = Vector3.zero;
            for (int i = 0; i < usedCharacters.Count; i++)
            {
                charactersMiddlePos += usedCharacters[i].Position;
            }
            charactersMiddlePos /= usedCharacters.Count;

            if (usedCharacters.Count > 1)
            {
                UsedCharacter mostLeftCharacter, mostRightCharacter;
                if (usedCharacters[0].ViewportPos.x < usedCharacters[1].ViewportPos.x)
                {
                    mostLeftCharacter = usedCharacters[0];
                    mostRightCharacter = usedCharacters[1];
                }
                else
                {
                    mostLeftCharacter = usedCharacters[1];
                    mostRightCharacter = usedCharacters[0];
                }

                for (int i = 2; i < usedCharacters.Count; i++)
                {
                    if (usedCharacters[i].ViewportPos.x < mostLeftCharacter.ViewportPos.x)
                        mostLeftCharacter = usedCharacters[i];
                    else if (mostRightCharacter.ViewportPos.x < usedCharacters[i].ViewportPos.x)
                        mostRightCharacter = usedCharacters[i];
                }

                float characterDistance = Vector3.Distance(mostLeftCharacter.Position, mostRightCharacter.Position);
                float lightDistance = Vector3.Distance((mostLeftCharacter.Position + mostRightCharacter.Position) * 0.5f, _spotLights[1].transform.position);
                float lightAngle = (90f - Mathf.Atan2(lightDistance, characterDistance * 0.5f + 1) * Mathf.Rad2Deg) * 2f;
                for(int i = 0; i < _spotLights.Length; i++)
                {
                    _spotLights[i].transform.forward = Vector3.Lerp(_spotLights[i].transform.forward, (charactersMiddlePos - _spotLights[i].transform.position).normalized, Time.deltaTime * _lightRotateSpeed);
                    _spotLights[i].intensity = Mathf.Lerp(_spotLights[i].intensity, _lightSpotIntensity_spot, Time.deltaTime * 1.55f);
                    _spotLights[i].spotAngle = Mathf.Lerp(_spotLights[i].spotAngle, lightAngle, Time.deltaTime * 1.55f);
                }
            }
            else
            {
                for (int i = 0; i < _spotLights.Length; i++)
                {
                    _spotLights[i].transform.forward = Vector3.Lerp(_spotLights[i].transform.forward, (charactersMiddlePos - _spotLights[i].transform.position).normalized, Time.deltaTime * _lightRotateSpeed);
                    _spotLights[i].intensity = Mathf.Lerp(_spotLights[i].intensity, _lightSpotIntensity_spot, Time.deltaTime * 1.55f);
                    _spotLights[i].spotAngle = Mathf.Lerp(_spotLights[i].spotAngle, _lightSpotAngle_spot, Time.deltaTime * 1.55f);
                }
            }
        }
        else
        {
            for(int i = 0; i < _spotLights.Length; i++)
            {
                _spotLights[i].transform.forward = Vector3.Lerp(_spotLights[i].transform.forward, (_allLightingPoint.position - _spotLights[i].transform.position).normalized, Time.deltaTime * _lightRotateSpeed);
                _spotLights[i].intensity = Mathf.Lerp(_spotLights[i].intensity, _lightSpotIntensity_all, Time.deltaTime * 0.85f);
                _spotLights[i].spotAngle = Mathf.Lerp(_spotLights[i].spotAngle, _lightSpotAngle_all, Time.deltaTime * 0.85f);
            }
        }
    }



    #region Sequence Actions
    private void SequenceAction(MVSequence currentSequence)
    {
        Debug.Log(string.Format("Play Sequence Name : {0}", currentSequence.SequenceName));

        ChangeUsedCharacter(currentSequence);
        SetSettings(currentSequence);

        currentSequence.IsPlayed = true;
    }



    private void SetSettings(MVSequence currentSequence)
    {
        if (currentSequence.ChangeCameraSetting)
        {
            SequenceCameraSetting scs = currentSequence.SequenceCameraSetting;
            _cameraDistanceOffset = scs.CameraDistanceOffset;
        }
    }

    private void ChangeUsedCharacter(MVSequence currentSequence)
    {
        for (int i = 0; i < _currentUsedCharacterList.Count; i++)
        {
            _currentUsedCharacterList[i].Init();
        }
        _currentUsedCharacterList.Clear();
        _trackingCharacterList.Clear();

        if (currentSequence.ChangeCharacterSetting)
        {
            SequenceCharacterSetting[] characterSettings = currentSequence.SequenceCharacterSettings;
            for (int i = 0; i < characterSettings.Length; i++)
            {
                UsedCharacter character = new UsedCharacter(_packages[i], characterSettings[i]);

                if (character.CharacterSetting.CameraTracking) _trackingCharacterList.Add(character);
                _currentUsedCharacterList.Add(character);
            }
        }
    }
#endregion



    class UsedCharacter
    {
        public CharacterPackage Package { get; private set; }
        public SequenceCharacterSetting CharacterSetting { get; private set; }


        public Vector3 Position { get { return Package.Character.Position; } }
        public Vector3 HeadPosition { get { return Package.Character.HeadPosition; } }
        public Vector3 ViewportPos { get { return Package.ViewportPos; } }



        public UsedCharacter(CharacterPackage package, SequenceCharacterSetting characterSetting)
        {
            package.SetCharacterGoalPos(characterSetting.SequencePos);
            package.SetCharacterGoalRot(characterSetting.SequenceRot);
            package.ChangeAudioRigOctave(characterSetting.AudioSampleOctaveIndexes, characterSetting.InverseOctave);
            package.SetCharacterLookAtCamera(characterSetting.LookAtCamera);
            package.SetCharacterCameraTracking(characterSetting.CameraTracking);
            package.SetAudioSamplesIndexRange(characterSetting.SamplesIndexRange);
            package.SetCharacterEyeBlendingValue_Boring(characterSetting.EyeBoring);

            Package = package;
            CharacterSetting = characterSetting;
        }

        public void Init()
        {
            if(Package == null || CharacterSetting == null)
            {
                Debug.Log("Properties are NULL");

                return;
            }

            Package.InitializeCharacterTransform();
            Package.ChangeAudioRigOctave(new int[] { 2 }, false);
            Package.SetCharacterCameraTracking(false);
            Package.SetCharacterLookAtCamera(false);
        }
    }



    [Serializable]
    class CharacterPackage
    {
        [Header("Character")]
        [SerializeField] private CharacterGenerator _characterGenerator;
        public CharacterGenerator Character { get { return _characterGenerator; } }
        public Vector3 ViewportPos { get; private set; }
        public Vector3 InitializePos { get; private set; }
        public Vector3 InitializeRot { get; private set; }



        [Header("Materials")]
        [SerializeField] private Material mat_body;
        [SerializeField] private Material mat_eye;
        [SerializeField] private Material mat_mouse;
        private const float AlphaMin = 0.001f;
        private const float AlphaMax = 0.325f;
        private float _materialAlpha = AlphaMin;



        [Header("Audio Sample")]
        [SerializeField] private float _audioSampleMaxValue;
        [Range(0f, 2f), SerializeField] private float _audioSampleMaxValueOffset = 0.65f;
        //[SerializeField] private int _audioSampleIndex;
        [SerializeField] private int[] _audioRigOctaves;
        //private int _currentAudioRigOctave = -1;
        public int _currentAudioRigOctave = -1;
        public float _currentFrequency = 0;
        public float _normalizedCurrentFrequency = 0;

        private float _audioSampleValue = 0f;
        public float AudioSampleValue
        {
            get { return _audioSampleValue; }
            set
            {
                if (_audioSampleValue != value)
                {
                    float sampleNormal = Mathf.Clamp01(Mathf.InverseLerp(0, _audioSampleMaxValue * _audioSampleMaxValueOffset, value));
                    AudioSampleNormal = sampleNormal;
                }
                else
                {
                    AudioSampleNormal = Mathf.Clamp01(AudioSampleNormal - Time.deltaTime * 1.15f);
                }



                _characterGenerator.AudioSampleValue = value;
                _characterGenerator.AudioSampleNormalValue = AudioSampleNormal;



                float alphaValue = Mathf.Lerp(AlphaMin, AlphaMax, AudioSampleNormal);
                _materialAlpha = Mathf.Lerp(_materialAlpha, alphaValue, Time.deltaTime * 10f);
                mat_body.SetFloat("_Alpha", _materialAlpha);
                mat_eye.SetFloat("_Alpha", _materialAlpha);
                mat_mouse.SetFloat("_Alpha", _materialAlpha);



                _audioSampleValue = value;
            }
        }

        public float AudioSampleNormal { get; private set; } = 0;
        /// <summary>
        /// 0 <= indexes < 8192
        /// </summary>
        public Vector2Int AudioSamplesIndexRange { get; private set; } = new Vector2Int(0, 8192);



        public void Init()
        {
            InitializePos = Character.transform.position;
            InitializeRot = Character.transform.eulerAngles;

            mat_body.SetFloat("_Alpha", AlphaMin);
            mat_eye.SetFloat("_Alpha", AlphaMin);
            mat_mouse.SetFloat("_Alpha", AlphaMin);
        }

        public void InitializeCharacterTransform()
        {
            Character.SetGoalPos(InitializePos);
            Character.SetGoalRot(InitializeRot);
        }

        public void ChangeAudioRigOctave(int[] octaves, bool inverse)
        {
            _audioRigOctaves = inverse ? VideoManager.Instance.GetInverseOvtaves(octaves) : octaves;
            //_audioSampleMaxValue = VideoManager.Instance.GetOctaveMaxSample(_audioRigOctave);
        }

        public void SetAudioSampleValue()
        {
            int videoCurrentOctave = 0;
            float audioSampleValue = 0;
            if(AudioSamplesIndexRange.x == 0 && AudioSamplesIndexRange.y == 8192)
            {
                videoCurrentOctave = VideoManager.Instance.CurrentOctave;
                audioSampleValue = VideoManager.Instance.CurrentOctaveSample;

                _currentFrequency = VideoManager.Instance.Frequency;
                _normalizedCurrentFrequency = VideoManager.Instance.NormalizedFrequency;
            }
            else
            {
                _currentFrequency = VideoManager.Instance.GetFrequency(AudioSamplesIndexRange, out audioSampleValue);
                _normalizedCurrentFrequency = VideoManager.Instance.GetNormalizedFrequency(_currentFrequency);

                videoCurrentOctave = VideoManager.Instance.GetOctave(_currentFrequency);
            }

            if (videoCurrentOctave == _currentAudioRigOctave)
            {
                AudioSampleValue = audioSampleValue;
            }
            else
            {
                bool isIncludeOctave = false;
                for (int i = 0; i < _audioRigOctaves.Length; i++)
                {
                    if (_audioRigOctaves[i] == videoCurrentOctave)
                    {
                        isIncludeOctave = true;

                        break;
                    }
                }

                if (isIncludeOctave)
                {
                    _currentAudioRigOctave = videoCurrentOctave;
                    _audioSampleMaxValue = VideoManager.Instance.GetOctaveMaxSample(_currentAudioRigOctave);

                    //AudioSampleValue = VideoManager.Instance.CurrentOctaveSample;
                    VideoManager.Instance.GetFrequency(AudioSamplesIndexRange, out audioSampleValue);
                    AudioSampleValue = audioSampleValue;
                }
                else
                {
                    AudioSampleValue = AudioSampleValue;
                }
            }
        }

        public void SetCharacterViewportPos(Camera useCamera)
        {
            ViewportPos = useCamera.WorldToViewportPoint(Character.Position);
        }

        public void SetCharacterLookAtCamera(bool lookAt)
        {
            Character.SetLookAtCamer(lookAt);
        }

        public void SetCharacterCameraTracking(bool tracking)
        {
            Character.SetCameraTracking(tracking);
        }

        public void SetCharacterGoalPos(Vector3 goalPos)
        {
            Character.SetGoalPos(goalPos);
        }

        public void SetCharacterGoalRot(Vector3 goalRot)
        {
            Character.SetGoalRot(goalRot);
        }

        public void SetAudioSamplesIndexRange(Vector2Int indexRange)
        {
            AudioSamplesIndexRange = indexRange;
        }

        public void SetCharacterEyeBlendingValue_Boring(float value)
        {
            Character.SetEyeBlendingValue_Boring(value);
        }
    }
}
