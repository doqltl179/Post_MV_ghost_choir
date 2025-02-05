using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class CharacterGenerator : Generator, IIdle
{
    private RigBone _rigBone;

    //private MeshFilter _meshFilter;
    //private MeshRenderer _meshRenderer;
    private SkinnedMeshRenderer _skinnedMeshRenderer;
    [SerializeField] private Material _material;

    //private Vector3[] _originalVertices;
    private Vector3[] _vertices;
    private Vector3[] _verticesOfAppliedNormalize;

    private int[] _triangles;
    private Vector2[] _uvs;
    private Color[] _colors;
    private BoneWeight[] _weights;
    private Matrix4x4[] _bindPos;
    private Mesh _mesh;

    private Vert[] _vertInfos;

    [Header("Character Properties")]
    [SerializeField, Tooltip("0 To 1 ==> Head To Tail")] private AnimationCurve _bodyLineGraph;
    //[SerializeField, Range(0f, 0.5f)] private float _bodyGraphStartValue = 0f;
    [SerializeField, Range(0.5f, 1f)] private float _bodyGraphEndValue = 1f;
    [SerializeField, Range(0f, 1f)] private float _bodyWidthOffset = 0.65f;
    [SerializeField] private Gradient _bodyColor;
    [SerializeField, Range(3, 120)] private int _radiusQuality = 12;
    [SerializeField, Range(1, 100)] private int _heightQuality = 3;
    [SerializeField, Range(0, 10)] private int _normalizeSensitive = 1;

    [SerializeField] private Vector3 _characterOriginalPos;

    [Header("Tracking")]
    [SerializeField] private Camera _trackingCamera;
    public bool LookAtCamera { get; private set; } = false;
    public bool CameraTracking { get; private set; } = false;
    private Transform bone_hips;
    private TransformSaver bone_hips_Saver;
    private Transform bone_head;
    private TransformSaver bone_head_Saver;

    [Header("Eyes")]
    [SerializeField] private EyeGenerator _eyeL;
    [SerializeField] private EyeGenerator _eyeR;
    [Range(3, 73)] public int _eye_quality = 3;
    [Range(0f, 1f)] public float _eye_round;

    [Header("Blend Shape")]
    [Range(0f, 1f)] public float _eye_happy = 0f;
    [Range(0f, 1f)] public float _eye_angry = 0f;
    [Range(0f, 1f)] public float _eye_boring = 0f;
    private float _eye_boring_goal = 1f;

    [Header("Mouth")]
    [SerializeField] private MouthGenerator _mouth;

    private float _audioSampleValue = 0;
    public float AudioSampleValue
    {
        get { return _audioSampleValue; }
        set
        {
            _audioSampleValue = value;
        }
    }

    private float _audioSampleNormalValue = 0;
    public float AudioSampleNormalValue
    {
        get { return _audioSampleNormalValue; }
        set
        {
            _audioSampleNormalValue = value;

            _mouth.HeightNormal = _audioSampleNormalValue;
        }
    }



    public Vector3 Position { get { return transform.position + transform.up * 0.5f * transform.localScale.y; } }
    public Vector3 HeadPosition { get { return _rigBone.GetBone("Head").position; } }



#if UNITY_EDITOR
    [Header("Gizmos Provertices")]
    public bool _showVerticesGizmo = false;
    public bool _showVertInfosGizmo = false;
    public bool _showRigBoneGizmo = false;
#endif

    private float _bodyWidthOffsetSaver;
    private int _radiusQualitySaver;
    private int _heightQualitySaver;
    private int _normalizeSensitiveSaver;

    private int _eye_qualitySaver = 3;
    private float _eye_roundSaver = 0f;
    private float _eye_happySaver = 0f;
    private float _eye_angrySaver = 0f;
    private float _eye_boringSaver = 0f;

    private void Awake()
    {
        if(GetComponents<CharacterGenerator>().Length > 1)
        {
            DestroyImmediate(this);

            return;
        }

        if (_rigBone == null)
        {
            _rigBone = GetComponentInChildren<RigBone>();
            if (_rigBone == null)
            {
                GameObject go = new GameObject("Bone");
                go.transform.SetParent(transform);

                _rigBone = go.AddComponent<RigBone>();
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if(_showVerticesGizmo && _vertices != null && _vertices.Length > 0)
        {
            DrawVertices(_vertices, Color.red, 0.002f);
        }

        if(_showVertInfosGizmo && _vertInfos != null && _vertInfos.Length > 0)
        {
            DrawVertInfos(_vertInfos, Color.black);
        }

        if (_showRigBoneGizmo && _rigBone.Bones != null && _rigBone.Bones.Length > 0)
        {
            DrawBone(_rigBone.Bones, Color.green);
        }
    }

    private void DrawVertInfos(Vert[] vertInfos, Color color)
    {
        Gizmos.color = color;

        GUIStyle textStyle = new GUIStyle();
        textStyle.fontSize = 10;
        textStyle.fontStyle = FontStyle.Bold;
        textStyle.alignment = TextAnchor.MiddleLeft;
        textStyle.normal.textColor = color;

        Vert vert;
        RigInfo rigInfo;
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < vertInfos.Length; i++)
        {
            vert = vertInfos[i];

            builder.Clear();
            if(vert.RigInfos.Length > 0)
            {
                rigInfo = vert.RigInfos[0];
                builder.Append(string.Format("{0} : {1:F2}", rigInfo.BoneInfo.name, rigInfo.Weight));

                for(int j = 1; j < vert.RigInfos.Length; j++)
                {
                    rigInfo = vert.RigInfos[j];
                    builder.Append(string.Format(", {0} : {1:F2}", rigInfo.BoneInfo.name, rigInfo.Weight));
                }

                Handles.Label(vert.VertPos + Vector3.right * 0.01f, builder.ToString(), textStyle);
            }
        }
    }

    private void DrawVertices(Vector3[] vertices, Color color, float radius)
    {
        Gizmos.color = color;

        for(int i = 0; i < vertices.Length; i++)
        {
            Gizmos.DrawSphere(vertices[i], radius);
        }
    }

    private void DrawBone(BoneInfo[] bones, Color color)
    {
        Gizmos.color = color;

        GUIStyle textStyle = new GUIStyle();
        textStyle.fontSize = 12;
        textStyle.fontStyle = FontStyle.Bold;
        textStyle.alignment = TextAnchor.UpperCenter;
        textStyle.normal.textColor = new Color(1 - color.r, 1 - color.g, 1 - color.b, 1);

        BoneInfo parent;
        for (int i = 0; i < bones.Length; i++)
        {
            parent = bones[i];

            Handles.Label(parent.transform.position, parent.name, textStyle);
            Gizmos.DrawLine(parent.BoneStartPos, parent.BoneEndPos);
        }
    }
#endif

    private void Start()
    {
        Create();

#if UNITY_EDITOR
        _bodyWidthOffsetSaver = _bodyWidthOffset;
        _radiusQualitySaver = _radiusQuality;
        _heightQualitySaver = _heightQuality;
#endif

        Init();
    }

    private void FixedUpdate()
    {
        MoveToGoalPos();
        RotateToGoalRot();
        Tracking();
        Idle();
    }

    [Header("Character Transform")]
    [Range(0.1f, 10f)] public float _moveSpeed = 4f;
    public Vector3 GoalPos { get; private set; } = Vector3.zero;
    private void MoveToGoalPos()
    {
        //Vector3 pos = transform.position;
        //transform.position = new Vector3(Mathf.Lerp(pos.x, GoalPos.x, Time.deltaTime * _moveSpeed), pos.y, Mathf.Lerp(pos.z, GoalPos.z, Time.deltaTime * _moveSpeed));
        transform.position = Vector3.Lerp(transform.position, GoalPos, Time.deltaTime * _moveSpeed);
    }

    [Range(0.1f, 10f)] public float _rotSpeed = 1f;
    public Vector3 GoalRot { get; private set; } = Vector3.zero;
    private void RotateToGoalRot()
    {
        //transform.eulerAngles = Vector3.Lerp(transform.eulerAngles, GoalRot, Time.deltaTime * _rotSpeed);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(GoalRot), Time.deltaTime * _rotSpeed);
    }

    Quaternion beforeRotation, afterRotation;
    private void Tracking()
    {
        if(LookAtCamera)
        {
            //Vector3 hipsPosDiff = _trackingCamera.transform.position - bone_hips.position;
            ////bone_hips.forward = Vector3.Lerp(bone_hips.forward, new Vector3(hipsPosDiff.x, 0, hipsPosDiff.z).normalized, Time.deltaTime * 0.85f);
            //bone_hips.forward = Vector3.Lerp(bone_hips.forward, hipsPosDiff.normalized, Time.deltaTime * 0.85f);
            ////bone_hips.eulerAngles += transform.eulerAngles;
            //bone_hips.rotation *= transform.rotation;

            //bone_head.forward = Vector3.Lerp(bone_head.forward, (_trackingCamera.transform.position - bone_head.position).normalized, Time.deltaTime * 1.05f);
            ////bone_head.eulerAngles += transform.eulerAngles;
            //bone_head.rotation *= transform.rotation;



            beforeRotation = bone_hips.rotation;
            Vector3 hipsPosDiff = _trackingCamera.transform.position - bone_hips.position;
            bone_hips.forward = new Vector3(hipsPosDiff.x, 0, hipsPosDiff.z).normalized;
            afterRotation = bone_hips.rotation;
            bone_hips.rotation = Quaternion.Lerp(beforeRotation, afterRotation * transform.rotation, Time.deltaTime * 0.85f);

            beforeRotation = bone_head.rotation;
            bone_head.forward = (_trackingCamera.transform.position - bone_head.position).normalized;
            afterRotation = bone_head.rotation;
            bone_head.rotation = Quaternion.Lerp(beforeRotation, afterRotation * transform.rotation, Time.deltaTime * 1.05f);
        }
        else
        {
            //bone_hips.forward = Vector3.Lerp(bone_hips.forward, transform.forward, Time.deltaTime * 0.8f);
            //bone_hips.forward = Vector3.Lerp(bone_hips.forward, bone_hips_Saver.Forward, Time.deltaTime * 0.8f);
            bone_hips.rotation = Quaternion.Lerp(bone_hips.rotation, bone_hips_Saver.Rotation * transform.rotation, Time.deltaTime * 0.8f);

            //bone_head.forward = Vector3.Lerp(bone_head.forward, transform.forward, Time.deltaTime * 0.8f);
            //bone_head.forward = Vector3.Lerp(bone_head.forward, bone_head_Saver.Forward, Time.deltaTime * 0.8f);
            bone_head.rotation = Quaternion.Lerp(bone_head.rotation, bone_head_Saver.Rotation * transform.rotation, Time.deltaTime * 0.8f);
        }
    }

    #region Interface
    [Header("Idle Animation Properties")]
    public Transform[] _tails;
    private Vector3 _posSaver;
    private Vector3 _movePos;
    private Vector3 _rotSaver;
    private Vector3 _rotateAngle;
    private Vector3[] _tailRotationSaver;
    private Vector3[] _tailRotationOffset;
    [Range(0f, 60f)] public float _angleSensitive = 10f;
    [Range(0.1f, 30f)] public float _idleRevert = 5f;
    [Range(1f, 500f)] public float _idleStrength = 15f;
    [Range(0.1f, 30f)] public float _tailRotateSpeed = 15f;

    public void Init()
    {
        _posSaver = transform.position;
        GoalPos = _posSaver;

        _rotSaver = transform.eulerAngles;
        GoalRot = _rotSaver;

        _tailRotationSaver = new Vector3[_tails.Length];
        for (int i = 0; i < _tailRotationSaver.Length; i++)
            _tailRotationSaver[i] = _tails[i].eulerAngles;

        _tailRotationOffset = new Vector3[_tails.Length];
        for (int i = 0; i < _tailRotationOffset.Length; i++)
            _tailRotationOffset[i] = Vector3.zero;

        if (_trackingCamera == null)
        {
            _trackingCamera = Camera.main;
        }
        bone_hips = _rigBone.GetBone("Hips");
        bone_hips_Saver = new TransformSaver(bone_hips);
        bone_head = _rigBone.GetBone("Head");
        bone_head_Saver = new TransformSaver(bone_head);

        _material.SetFloat("_BodyJiggleDistance", UnityEngine.Random.Range(0.022f, 0.03f));
        _material.SetFloat("_BodyJiggleSpeed", UnityEngine.Random.Range(4.5f, 5.5f));
        _material.SetFloat("_BodyJiggleFrequency", UnityEngine.Random.Range(22f, 28f));
    }

    Ray _idleRay;
    RaycastHit _idleHit;
    float _idleTime = 0;
    public void Idle()
    {
        for (int i = 0; i < _tailRotationOffset.Length; i++)
        {
            _tailRotationOffset[i] = new Vector3(Mathf.Lerp(_tailRotationOffset[i].x, 0, Time.deltaTime * _idleRevert), 0, Mathf.Lerp(_tailRotationOffset[i].z, 0, Time.deltaTime * _idleRevert));
        }

        _movePos = transform.position - _posSaver;

        Vector3 euler;
        for (int i = 0; i < _tailRotationOffset.Length; i++)
        {
            euler = _tailRotationOffset[i] + new Vector3(_movePos.z, 0, -_movePos.x) * _idleStrength;

            if (euler.x > _angleSensitive)
            {
                euler.x = _angleSensitive;
            }
            else if (euler.x < -_angleSensitive)
            {
                euler.x = -_angleSensitive;
            }

            if (euler.z > _angleSensitive)
            {
                euler.z = _angleSensitive;
            }
            else if (euler.z < -_angleSensitive)
            {
                euler.z = -_angleSensitive;
            }

            //_tails[i].eulerAngles = _tailRotationSaver[i] + euler;
            _tailRotationOffset[i] = euler;
        }

        for (int i = 0; i < _tails.Length; i++)
        {
            _tails[i].rotation = Quaternion.Lerp(_tails[i].rotation, Quaternion.Euler(_tailRotationSaver[i] + _tailRotationOffset[i]), Time.deltaTime * _tailRotateSpeed);
        }

        _posSaver = transform.position;



        Vector3 position = transform.position - Vector3.up * ((Mathf.Sin(_idleTime) + 1) * 0.5f) * 0.1f;

        _idleTime += Time.deltaTime * UnityEngine.Random.Range(0.95f, 1.05f);
        //_idleRay = new Ray(transform.position + Vector3.up * 100, Vector3.down);
        //if(Physics.Raycast(_idleRay, out _idleHit))
        //{
            //Vector3 position = transform.position;
            //transform.position = new Vector3(position.x, ((Mathf.Sin(Time.time) + 1) * 0.5f) * 0.1f, position.z);
            //transform.position = new Vector3(position.x, ((Mathf.Sin(_idleTime) + 1) * 0.5f) * 0.1f, position.z);
            transform.position = new Vector3(position.x, position.y + ((Mathf.Sin(_idleTime) + 1) * 0.5f) * 0.1f, position.z);
        //}
    }
    #endregion

    private void Update()
    {
        if (_mesh != null)
        {
            //if (_characterHeightPivot != _characterHeightPivotSaver)
            //{
            //    _originalVertices = GetOriginalVertices();
            //    _mesh.vertices = GetVertices(_originalVertices);

            //    //_mesh.RecalculateNormals();

            //    _characterHeightPivotSaver = _characterHeightPivot;
            //}

            if (_bodyWidthOffset != _bodyWidthOffsetSaver)
            {
                _vertices = GetVertices();
                _mesh.vertices = _vertices;

                _vertInfos = GetVertInfos(_vertices, _rigBone.Bones);

                _mesh.RecalculateNormals();

                _bodyWidthOffsetSaver = _bodyWidthOffset;
            }

            if (_radiusQuality != _radiusQualitySaver)
            {
                _mesh = GetCharacterMesh();
                //_meshFilter.mesh = _mesh;
                _skinnedMeshRenderer.sharedMesh = _mesh;

                _radiusQualitySaver = _radiusQuality;
            }

            if (_heightQuality != _heightQualitySaver)
            {
                _mesh = GetCharacterMesh();
                //_meshFilter.mesh = _mesh;
                _skinnedMeshRenderer.sharedMesh = _mesh;

                _heightQualitySaver = _heightQuality;
            }

            if(_normalizeSensitive != _normalizeSensitiveSaver)
            {
                _verticesOfAppliedNormalize = GetVerticesOfAppliedNormalize(_vertices, _radiusQuality, _heightQuality, _normalizeSensitive);
                _mesh.vertices = _verticesOfAppliedNormalize;

                _mesh.RecalculateBounds();
                _mesh.RecalculateNormals();
                _mesh.RecalculateTangents();

                _normalizeSensitiveSaver = _normalizeSensitive;
            }
        }

        if (_eyeL != null && _eyeR != null)
        {
            if (_eye_quality != _eye_qualitySaver)
            {
                _eyeL._quality = _eye_quality;
                _eyeR._quality = _eye_quality;

                _eye_qualitySaver = _eye_quality;
            }

            if (_eye_round != _eye_roundSaver)
            {
                _eyeL._round = _eye_round;
                _eyeR._round = _eye_round;

                _eye_roundSaver = _eye_round;
            }

            if (_eye_happy != _eye_happySaver)
            {
                Animation_Eye_Happy(_eye_happy, _eye_happySaver);

                _eye_happySaver = _eye_happy;
            }

            if (_eye_angry != _eye_angrySaver)
            {
                Animation_Eye_Angry(_eye_angry, _eye_angrySaver);

                _eye_angrySaver = _eye_angry;

                _eye_boring = 1 - _eye_angry;
                if (_eye_boring != _eye_boringSaver)
                {
                    Animation_Eye_Boring(_eye_boring, _eye_boringSaver);

                    _eye_boringSaver = _eye_boring;
                }
            }

            _eye_boring = Mathf.Lerp(_eye_boring, _eye_boring_goal, Time.deltaTime * 1.34f);
            if (_eye_boring != _eye_boringSaver)
            {
                Animation_Eye_Boring(_eye_boring, _eye_boringSaver);

                _eye_boringSaver = _eye_boring;

                _eye_angry = 1 - _eye_boring;
                if (_eye_angry != _eye_angrySaver)
                {
                    Animation_Eye_Angry(_eye_angry, _eye_angrySaver);

                    _eye_angrySaver = _eye_angry;
                }
            }
        }
    }

    #region Override
    public override void Create()
    {
        _rigBone.SetProperties();

        _mesh = GetCharacterMesh();

        if (_skinnedMeshRenderer == null)
        {
            _skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            if (_skinnedMeshRenderer == null)
            {
                GameObject go = new GameObject("Mesh");
                go.transform.SetParent(transform);

                _skinnedMeshRenderer = go.AddComponent<SkinnedMeshRenderer>();

                _skinnedMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _skinnedMeshRenderer.receiveShadows = false;
            }
        }
        _skinnedMeshRenderer.sharedMesh = _mesh;

        _skinnedMeshRenderer.bones = _rigBone.GetBones();
        _skinnedMeshRenderer.rootBone = _rigBone.GetRootBone();

        if (_material == null)
        {
            _material = new Material(Shader.Find("Standard"));
        }
        //_meshRenderer.material = _material;
        _skinnedMeshRenderer.material = _material;
    }
    #endregion

    #region Utility
    public void SetEyeBlendingValue_Boring(float value)
    {
        //_eye_boring = value;
        _eye_boring_goal = value;
    }

    public void SetLookAtCamer(bool lookAt)
    {
        LookAtCamera = lookAt;
    }

    public void SetCameraTracking(bool tracking)
    {
        CameraTracking = tracking;
    }

    public void SetGoalPos(Vector3 goalPos)
    {
        GoalPos = goalPos;
    }

    public void SetGoalRot(Vector3 goalRot)
    {
        GoalRot = goalRot;
    }
    #endregion

    private Mesh GetCharacterMesh()
    {
        _vertices = GetVertices();
        _verticesOfAppliedNormalize = GetVerticesOfAppliedNormalize(_vertices, _radiusQuality, _heightQuality, _normalizeSensitive);

        _triangles = GetTriangles();
        _uvs = GetUVs(_vertices);
        _colors = GetColors(_vertices);

        _vertInfos = GetVertInfos(_vertices, _rigBone.Bones);
        _weights = GetWeights(_vertInfos);

        _bindPos = GetBindPos(_rigBone.Bones);

        Mesh mesh = new Mesh();
        //mesh.vertices = _vertices;
        mesh.vertices = _verticesOfAppliedNormalize;
        mesh.triangles = _triangles;
        mesh.uv = _uvs;
        mesh.colors = _colors;
        mesh.boneWeights = _weights;
        mesh.bindposes = _bindPos;

        mesh.RecalculateNormals();

        return mesh;
    }

    private Matrix4x4[] GetBindPos(BoneInfo[] bones)
    {
        Matrix4x4[] bindPos = new Matrix4x4[bones.Length];

        for(int i = 0; i < bindPos.Length; i++)
        {
            bindPos[i] = bones[i].transform.worldToLocalMatrix * transform.localToWorldMatrix;
        }

        return bindPos;
    }

    private BoneWeight[] GetWeights(Vert[] vertInfos)
    {
        BoneWeight[] weights = new BoneWeight[vertInfos.Length];

        Vert vert;
        int rigLength;
        for(int i = 0; i < weights.Length; i++)
        {
            BoneWeight weight = new BoneWeight();

            vert = vertInfos[i];
            rigLength = vert.RigInfos.Length;
            if(rigLength == 0)
            {

            }
            else if (rigLength == 1)
            {
                weight.boneIndex0 = _rigBone.GetBoneIndex(vert.RigInfos[0].BoneInfo.name);
                weight.weight0 = vert.RigInfos[0].Weight;
                //weight.weight0 = 1;
            }
            else if(rigLength == 2)
            {
                weight.boneIndex0 = _rigBone.GetBoneIndex(vert.RigInfos[0].BoneInfo.name);
                weight.boneIndex1 = _rigBone.GetBoneIndex(vert.RigInfos[1].BoneInfo.name);
                weight.weight0 = vert.RigInfos[0].Weight;
                weight.weight1 = vert.RigInfos[1].Weight;
                //weight.weight0 = 1;
                //weight.weight1 = 1;
            }
            else if (rigLength == 3)
            {
                weight.boneIndex0 = _rigBone.GetBoneIndex(vert.RigInfos[0].BoneInfo.name);
                weight.boneIndex1 = _rigBone.GetBoneIndex(vert.RigInfos[1].BoneInfo.name);
                weight.boneIndex2 = _rigBone.GetBoneIndex(vert.RigInfos[2].BoneInfo.name);
                weight.weight2 = vert.RigInfos[2].Weight;
                weight.weight0 = vert.RigInfos[0].Weight;
                weight.weight1 = vert.RigInfos[1].Weight;
                //weight.weight0 = 1;
                //weight.weight1 = 1;
                //weight.weight2 = 1;
            }
            else if (rigLength >= 4)
            {
                weight.boneIndex0 = _rigBone.GetBoneIndex(vert.RigInfos[0].BoneInfo.name);
                weight.boneIndex1 = _rigBone.GetBoneIndex(vert.RigInfos[1].BoneInfo.name);
                weight.boneIndex2 = _rigBone.GetBoneIndex(vert.RigInfos[2].BoneInfo.name);
                weight.boneIndex3 = _rigBone.GetBoneIndex(vert.RigInfos[3].BoneInfo.name);
                weight.weight0 = vert.RigInfos[0].Weight;
                weight.weight1 = vert.RigInfos[1].Weight;
                weight.weight2 = vert.RigInfos[2].Weight;
                weight.weight3 = vert.RigInfos[3].Weight;
                //weight.weight0 = 1;
                //weight.weight1 = 1;
                //weight.weight2 = 1;
                //weight.weight3 = 1;
            }

            weights[i] = weight;
        }

        return weights;
    }

    private Color[] GetColors(Vector3[] vertices)
    {
        Color[] colors = new Color[vertices.Length];



        return colors;
    }

    private Vector2[] GetUVs(Vector3[] vertices)
    {
        Vector2[] uvs = new Vector2[vertices.Length];

        Vector3 vert;
        float angle;
        for(int i = 0; i < uvs.Length; i++)
        {
            vert = vertices[i];
            angle = Mathf.Atan2(vert.z, vert.x);

            uvs[i] = new Vector2(Mathf.InverseLerp(-Mathf.PI, Mathf.PI, angle), vert.y);
        }

        return uvs;
    }

    private int[] GetTriangles()
    {
        //Length : Triangles of Connected First Vert + Body Triangles + Triangles of Connected Last Vert
        // Triangle pointing head to tail
        int[] triangles = new int[(_radiusQuality * 3) + ((_heightQuality - 1) * _radiusQuality * 2 * 3) + (_radiusQuality * 3)];

        int triIndex = 0;
        int vertIndex = 1;

        //Triangles of Connected First Vert
        {
            for (int i = 0; i < _radiusQuality - 1; i++)
            {
                triangles[triIndex + 0] = vertIndex;
                triangles[triIndex + 1] = 0;
                triangles[triIndex + 2] = vertIndex + 1;

                triIndex += 3;
                vertIndex++;
            }

            triangles[triIndex + 0] = vertIndex;
            triangles[triIndex + 1] = 0;
            triangles[triIndex + 2] = 1;

            triIndex += 3;
            vertIndex++;
        }

        bool isBodyTriangleBreaked = false;
        //Body Triangles
        { 
            for(int i = 0; i < _heightQuality - 1; i++)
            {
                if (_verticesOfAppliedNormalize[vertIndex - _radiusQuality].y < 1 - _bodyGraphEndValue)
                {
                    isBodyTriangleBreaked = true;

                    break;
                }

                for(int j = 0; j < _radiusQuality - 1; j++)
                {
                    triangles[triIndex + 0] = vertIndex;
                    triangles[triIndex + 1] = vertIndex - _radiusQuality;
                    triangles[triIndex + 2] = vertIndex - _radiusQuality + 1;

                    triangles[triIndex + 3] = vertIndex;
                    triangles[triIndex + 4] = vertIndex - _radiusQuality + 1;
                    triangles[triIndex + 5] = vertIndex + 1;

                    triIndex += 6;
                    vertIndex++;
                }

                triangles[triIndex + 0] = vertIndex;
                triangles[triIndex + 1] = vertIndex - _radiusQuality;
                triangles[triIndex + 2] = vertIndex - _radiusQuality * 2 + 1;

                triangles[triIndex + 3] = vertIndex;
                triangles[triIndex + 4] = vertIndex - _radiusQuality * 2 + 1;
                triangles[triIndex + 5] = vertIndex - _radiusQuality + 1;

                triIndex += 6;
                vertIndex++;
            }
        }

        if(!isBodyTriangleBreaked)
        //Triangles of Connected Last Vert
        {
            for (int i = 0; i < _radiusQuality - 1; i++)
            {
                triangles[triIndex + 0] = vertIndex;
                triangles[triIndex + 1] = vertIndex - _radiusQuality + i;
                triangles[triIndex + 2] = vertIndex - _radiusQuality + i + 1;

                triIndex += 3;
            }

            triangles[triIndex + 0] = vertIndex;
            triangles[triIndex + 1] = vertIndex - 1;
            triangles[triIndex + 2] = vertIndex - _radiusQuality;
        }

        return triangles;
    }

    private Vector3[] GetVertices()
    {
        Vector3[] vertices;

        //Length : First Vert + Body Vert + Last Vert
        //Vert pointing head to tail
        vertices = new Vector3[1 + _heightQuality * _radiusQuality + 1];

        int index = 0;

        vertices[index] = new Vector3(0, 1, 0);
        index++;

        float r;
        float y;
        float angle;
        for (int h = 0; h < _heightQuality; h++)
        {
            y = Mathf.InverseLerp(0, _heightQuality + 1, h + 1);
            r = _bodyLineGraph.Evaluate(y);
            for (int i = 0; i < _radiusQuality; i++)
            {
                angle = Mathf.Lerp(0, Mathf.PI * 2, (float)i / _radiusQuality);

                vertices[index] = new Vector3(Mathf.Cos(angle) * r * _bodyWidthOffset, 1 - y, Mathf.Sin(angle) * r * _bodyWidthOffset);

                index++;
            }
        }

        vertices[index] = Vector3.zero;

        return vertices;
    }

    private Vector3[] GetVerticesOfAppliedNormalize(Vector3[] vertices, int radiusQuality, int heightQuality, int normalizeSensitive)
    {
        if (heightQuality > 1)
        {
            Vector3[] verts = new Vector3[vertices.Length];
            Array.Copy(vertices, 0, verts, 0, verts.Length);

            Vector3 vert;

            for (int j = 0; j < normalizeSensitive; j++)
            {
                //Top
                Vector3 topVert = verts[0];
                for (int i = 1; i <= radiusQuality; i++)
                {
                    vert = verts[i + radiusQuality];

                    verts[i] = new Vector3((vert.x + topVert.x) * 0.5f, (vert.y * 0.2f + topVert.y * 0.8f), (vert.z + topVert.z) * 0.5f);
                }

                //Bottom
                Vector3 bottomVert = verts[verts.Length - 1];
                for (int i = 1 + radiusQuality * (heightQuality - 1); i < verts.Length - 1; i++)
                {
                    vert = verts[i - heightQuality];

                    verts[i] = new Vector3((vert.x + bottomVert.x) * 0.5f, (vert.y * 0.2f + bottomVert.y * 0.8f), (vert.z + bottomVert.z) * 0.5f);
                }

                //Middle
                for (int i = 1 + radiusQuality; i < verts.Length - radiusQuality - 1; i++)
                {
                    //verts[i] = (verts[i - radiusQuality] + verts[i + radiusQuality]) * 0.5f;
                    verts[i] = Vector3.Slerp(verts[i - radiusQuality], verts[i + radiusQuality], 0.5f);
                }
            }

            return verts;
        }
        else
        {
            return vertices;
        }
    }

    private Vert[] GetVertInfos(Vector3[] vertices, BoneInfo[] bones)
    {
        Vert[] verts = new Vert[vertices.Length];
        for (int i = 0; i < verts.Length; i++)
        {
            Vert v = new Vert(vertices[i], AutoRigAlgorithm.GetAutoRig(vertices[i], bones));
            v.NormalizeRigWeight();

            verts[i] = v;
        }

        return verts;
    }

    #region Blend Animation
    private void Animation_Eye_Happy(float blendStrength, float blendStrengthSaver)
    {
        _eyeL.ChangeBlendStrength("Happy", _eye_happy);
        _eyeR.ChangeBlendStrength("Happy", _eye_happy);

        if (blendStrengthSaver < blendStrength)
        {
            _eye_round -= blendStrength * _eye_round;
        }
    }

    private void Animation_Eye_Angry(float blendStrength, float blendStrengthSaver)
    {
        _eyeL.ChangeBlendStrength("Angry", _eye_angry);
        _eyeR.ChangeBlendStrength("Angry", _eye_angry);

        if (blendStrengthSaver < blendStrength)
        {
            _eye_round -= blendStrength * _eye_round;
        }
    }

    private void Animation_Eye_Boring(float blendStrength, float blendStrengthSaver)
    {
        _eyeL.ChangeBlendStrength("Boring", _eye_boring);
        _eyeR.ChangeBlendStrength("Boring", _eye_boring);

        if (blendStrengthSaver < blendStrength)
        {
            _eye_round -= blendStrength * _eye_round;
        }
    }
    #endregion
}

#region Class
public class Vert
{
    public Vector3 VertPos { get; private set; }

    private RigInfo[] _rigInfos;
    public RigInfo[] RigInfos
    {
        get
        {
            return _rigInfos;
        }
    }

    public Vert(Vector3 vertPos)
    {
        VertPos = vertPos;
    }

    public Vert(Vector3 vertPos, RigInfo[] rigInfos)
    {
        VertPos = vertPos;

        _rigInfos = rigInfos;
    }

    #region Utility
    public void SetRigInfos(RigInfo[] rigInfos)
    {
        _rigInfos = rigInfos;
    }

    public void NormalizeRigWeight()
    {
        if (_rigInfos == null || _rigInfos.Length == 0)
        {
            Debug.Log("Not exist RigInfo");
        }
        else if (_rigInfos.Length > 1)
        {
            float sum = _rigInfos.Sum(t => t.Weight);
            if (sum == 0)
            {
                _rigInfos = new RigInfo[] { _rigInfos[0] };
            }
            else
            {
                for (int i = 0; i < _rigInfos.Length; i++)
                {
                    _rigInfos[i].SetWeight(_rigInfos[i].Weight / sum);
                }
            }
        }
        else //_rigInfos.Length == 1
        {
            _rigInfos[0].SetWeight(1f);
        }
    }
    #endregion
}

public class RigInfo
{
    public BoneInfo BoneInfo;
    public float Weight;

    public RigInfo(BoneInfo boneInfo)
    {
        BoneInfo = boneInfo;
    }

    public RigInfo(BoneInfo boneInfo, float weight)
    {
        BoneInfo = boneInfo;
        Weight = weight;
    }

    #region Utility
    public void SetWeight(float weight)
    {
        Weight = weight;
    }
    #endregion
}
#endregion