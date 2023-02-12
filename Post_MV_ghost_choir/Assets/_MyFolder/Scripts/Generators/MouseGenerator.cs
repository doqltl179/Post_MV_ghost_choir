using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class MouseGenerator : Generator, IMouseAnimation
{
    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;

    [Header("Mesh Ingredient")]
    public Material mat_mouse;
    private const float AlphaMin = 0.001f;
    private const float AlphaMax = 0.435f;

    [Header("Mesh Properties")]
    [Range(3, 73)] public int _mouseQuality = 3;
    [Range(0, 37)] public int _mouseFaceQuality = 3;
    [Range(0, 10)] public int _thicknessQuality = 1;
    [Range(0.1f, 1f)] public float _thickness = 0.1f;
    private Vector3[] _vertices;
    private int[] _triangles;
    private Mesh _mesh;

    private Vector3[] _blendVertices;
    private Vector3[] _blendVerticesSum;

    #region For Editor
    private int _mouseQualitySaver = -1;
    private int _mouseFaceQualitySaver = -1;
    private int _thicknessQualitySaver = -1;
    private float _thicknessSaver = -1f;
    #endregion

    private void Start()
    {
        Init();
    }

    private void FixedUpdate()
    {
        MouseAnimation();
    }

    #region Interface
    [Header("Mouse Animation Properties")]
    [HideInInspector] public float HeightNormal = 0;
    private const float ScaleMin = 0.05f;
    private const float ScaleMax = 0.5f;

    public void Init()
    {
        HeightNormal = 0;
    }

    private Vector3 _mouseScale;
    public void MouseAnimation()
    {
        _mouseScale = transform.localScale;
        _mouseScale.y = Mathf.Lerp(ScaleMin, ScaleMax, HeightNormal);
        transform.localScale = _mouseScale;
    }
    #endregion

#if UNITY_EDITOR
    bool isPropertyChanged = false;

    private void Update()
    {
        if (_mouseQuality != _mouseQualitySaver)
        {
            Create();

            _mouseQualitySaver = _mouseQuality;

            isPropertyChanged = true;
        }

        if(_mouseFaceQuality != _mouseFaceQualitySaver)
        {
            Create();

            _mouseFaceQualitySaver = _mouseFaceQuality;

            isPropertyChanged = true;
        }

        if (_thicknessQuality != _thicknessQualitySaver)
        {
            Create();

            _thicknessQualitySaver = _thicknessQuality;

            isPropertyChanged = true;
        }

        if (_thickness != _thicknessSaver)
        {
            Create();

            _thicknessSaver = _thickness;

            isPropertyChanged = true;
        }

        if(_blends != null)
        {
            Blend b;
            bool blendStrengthChanged = false;
            for (int i = 0; i < _blends.Length; i++)
            {
                b = _blends[i];

                b.StrengthChanged(ref blendStrengthChanged);
            }

            if (blendStrengthChanged || isPropertyChanged)
            {
                Array.Clear(_blendVerticesSum, 0, _blendVerticesSum.Length);

                for (int i = 0; i < _blends.Length; i++)
                {
                    b = _blends[i];
                    for (int j = 0; j < _blendVerticesSum.Length; j++)
                    {
                        _blendVerticesSum[j] += b.BlendVerts[j];
                    }
                }

                for (int i = 0; i < _blendVertices.Length; i++)
                {
                    _blendVertices[i] = _vertices[i] + _blendVerticesSum[i];
                }

                _mesh.vertices = _blendVertices;

                _mesh.RecalculateBounds();
                _mesh.RecalculateNormals();
                _mesh.RecalculateTangents();

                isPropertyChanged = false;
            }
        }
    }
#endif

    #region Override
    public override void Create()
    {
        GetVerticesAndTriangles(_mouseQuality, _mouseFaceQuality, _thicknessQuality, _thickness, out _vertices, out _triangles);

        #region Set mesh
        Mesh mesh = new Mesh();
        mesh.vertices = _vertices;
        mesh.triangles = _triangles;

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        if (_meshFilter == null)
        {
            _meshFilter = GetComponent<MeshFilter>();
            if (_meshFilter == null)
            {
                _meshFilter = gameObject.AddComponent<MeshFilter>();
            }
        }
        _meshFilter.mesh = mesh;

        if (_meshRenderer == null)
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            if (_meshRenderer == null)
            {
                _meshRenderer = gameObject.AddComponent<MeshRenderer>();

                _meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _meshRenderer.receiveShadows = false;
            }
        }
        if (mat_mouse == null)
        {
            mat_mouse = new Material(Shader.Find("Standard"));
        }
        _meshRenderer.material = mat_mouse;

        _mesh = mesh;
        #endregion

        #region Set blend
        if (_blends == null || _blends.Length == 0)
        {
            _blends = new Blend[]
            {
                new Blend("Idle", _vertices, Blend_Idle(_vertices), 0f, 1f),
                new Blend("Smile", _vertices, Blend_Smile(_vertices), 0f, 1.5f),
                new Blend("Angry", _vertices, Blend_Angry(_vertices), 0f, 1.5f),
                new Blend("Sad", _vertices, Blend_Sad(_vertices), 0f, 1.5f),
            };
        }
        else
        {
            _blends = new Blend[]
            {
                new Blend("Idle", _vertices, Blend_Idle(_vertices), _blends.Where(t => t.BlendName == "Idle").FirstOrDefault().BlendStrength, 1f),
                new Blend("Smile", _vertices, Blend_Smile(_vertices), _blends.Where(t => t.BlendName == "Smile").FirstOrDefault().BlendStrength, 1.5f),
                new Blend("Angry", _vertices, Blend_Angry(_vertices), _blends.Where(t => t.BlendName == "Angry").FirstOrDefault().BlendStrength, 1.5f),
                new Blend("Sad", _vertices, Blend_Sad(_vertices), _blends.Where(t => t.BlendName == "Sad").FirstOrDefault().BlendStrength, 1.2f),
            };
        }

        _blendVertices = new Vector3[_vertices.Length];
        _blendVerticesSum = new Vector3[_blendVertices.Length];
        #endregion
    }
    #endregion

    private void GetVerticesAndTriangles(int mouseQuality, int mouseFaceQuality, int thicknessQuality, float thickness, out Vector3[] vertices, out int[] triangles)
    {
        #region Common Properties
        float angle;
        int triIndex;
        #endregion

        #region Front
        Vector3[] frontVerts = new Vector3[mouseQuality + mouseQuality * mouseFaceQuality + 1];
        float frontZ = thickness * 0.5f;
        frontVerts[0] = new Vector3(0, 0, frontZ);

        int[] frontTris;

        if (mouseFaceQuality > 0)
        {
            float offset;
            int vertIndex = 1;
            for (int j = 0; j <= mouseFaceQuality; j++)
            {
                offset = 1f / (mouseFaceQuality + 1) * (j + 1);
                for (int i = 0; i < mouseQuality; i++)
                {
                    angle = i / (float)mouseQuality * Mathf.PI * 2;
                    frontVerts[vertIndex] = new Vector3(Mathf.Cos(angle) * offset, Mathf.Sin(angle) * offset, frontZ);

                    vertIndex++;
                }
            }

            frontTris = new int[mouseQuality * 3 + (mouseQuality * mouseFaceQuality * 2 * 3)];
            triIndex = 0;
            vertIndex = 1;
            for (; vertIndex < mouseQuality; vertIndex++)
            {
                frontTris[triIndex + 0] = 0;
                frontTris[triIndex + 1] = vertIndex;
                frontTris[triIndex + 2] = vertIndex + 1;

                triIndex += 3;
            }
            {
                frontTris[triIndex + 0] = 0;
                frontTris[triIndex + 1] = mouseQuality;
                frontTris[triIndex + 2] = 1;

                triIndex += 3;
            }

            vertIndex = 1;
            for (int j = 0; j < mouseFaceQuality; j++)
            {
                for (int i = 0; i < mouseQuality - 1; i++)
                {
                    frontTris[triIndex + 0] = vertIndex;
                    frontTris[triIndex + 1] = vertIndex + mouseQuality;
                    frontTris[triIndex + 2] = vertIndex + mouseQuality + 1;

                    frontTris[triIndex + 3] = vertIndex;
                    frontTris[triIndex + 4] = vertIndex + mouseQuality + 1;
                    frontTris[triIndex + 5] = vertIndex + 1;

                    vertIndex++;
                    triIndex += 6;
                }
                {
                    frontTris[triIndex + 0] = vertIndex;
                    frontTris[triIndex + 1] = vertIndex + mouseQuality;
                    frontTris[triIndex + 2] = vertIndex + 1;

                    frontTris[triIndex + 3] = vertIndex;
                    frontTris[triIndex + 4] = vertIndex + 1;
                    frontTris[triIndex + 5] = vertIndex - mouseQuality + 1;

                    vertIndex++;
                    triIndex += 6;
                }
            }
        }
        else
        {
            for (int i = 1; i < frontVerts.Length; i++)
            {
                angle = (i - 1) / (float)mouseQuality * Mathf.PI * 2;
                frontVerts[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), frontZ);
            }

            frontTris = new int[mouseQuality * 3];
            triIndex = 0;
            for (int i = 1; i < mouseQuality; i++)
            {
                frontTris[triIndex + 0] = 0;
                frontTris[triIndex + 1] = i;
                frontTris[triIndex + 2] = i + 1;

                triIndex += 3;
            }
            {
                frontTris[triIndex + 0] = 0;
                frontTris[triIndex + 1] = mouseQuality;
                frontTris[triIndex + 2] = 1;
            }
        }
        #endregion

        #region Back
        Vector3[] backVerts = new Vector3[mouseQuality + mouseQuality * mouseFaceQuality + 1];
        float backZ = thickness * -0.5f;
        backVerts[0] = new Vector3(0, 0, backZ);

        int[] backTris;

        if (mouseFaceQuality > 0)
        {
            float offset;
            int vertIndex = 1;
            for (int j = 0; j <= mouseFaceQuality; j++)
            {
                offset = 1f / (mouseFaceQuality + 1) * (j + 1);
                for (int i = 0; i < mouseQuality; i++)
                {
                    angle = i / (float)mouseQuality * Mathf.PI * 2;
                    backVerts[vertIndex] = new Vector3(Mathf.Cos(angle) * offset, Mathf.Sin(angle) * offset, backZ);

                    vertIndex++;
                }
            }

            backTris = new int[mouseQuality * 3 + (mouseQuality * mouseFaceQuality * 2 * 3)];
            triIndex = 0;
            vertIndex = 1;
            for (; vertIndex < mouseQuality; vertIndex++)
            {
                backTris[triIndex + 0] = 0;
                backTris[triIndex + 1] = vertIndex + 1;
                backTris[triIndex + 2] = vertIndex;

                triIndex += 3;
            }
            {
                backTris[triIndex + 0] = 0;
                backTris[triIndex + 1] = 1;
                backTris[triIndex + 2] = mouseQuality;

                triIndex += 3;
            }

            vertIndex = 1;
            for (int j = 0; j < mouseFaceQuality; j++)
            {
                for (int i = 0; i < mouseQuality - 1; i++)
                {
                    backTris[triIndex + 0] = vertIndex;
                    backTris[triIndex + 1] = vertIndex + mouseQuality + 1;
                    backTris[triIndex + 2] = vertIndex + mouseQuality;
                    
                    backTris[triIndex + 3] = vertIndex;
                    backTris[triIndex + 4] = vertIndex + 1;
                    backTris[triIndex + 5] = vertIndex + mouseQuality + 1;

                    vertIndex++;
                    triIndex += 6;
                }
                {
                    backTris[triIndex + 0] = vertIndex;
                    backTris[triIndex + 1] = vertIndex + 1;
                    backTris[triIndex + 2] = vertIndex + mouseQuality;
                    
                    backTris[triIndex + 3] = vertIndex;
                    backTris[triIndex + 4] = vertIndex - mouseQuality + 1;
                    backTris[triIndex + 5] = vertIndex + 1;

                    vertIndex++;
                    triIndex += 6;
                }
            }
        }
        else
        {
            for (int i = 1; i < backVerts.Length; i++)
            {
                angle = (i - 1) / (float)mouseQuality * Mathf.PI * 2;
                backVerts[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), backZ);
            }

            backTris = new int[mouseQuality * 3];
            triIndex = 0;
            for (int i = 1; i < mouseQuality; i++)
            {
                backTris[triIndex + 0] = 0;
                backTris[triIndex + 1] = i + 1;
                backTris[triIndex + 2] = i;

                triIndex += 3;
            }
            {
                backTris[triIndex + 0] = 0;
                backTris[triIndex + 1] = 1;
                backTris[triIndex + 2] = mouseQuality;
            }
        }
        #endregion

        #region Middle

        Vector3[] middleVerts;
        int[] middleTris;

        if (thicknessQuality > 0)
        {
            Vector3[] thickVerts = new Vector3[mouseQuality * thicknessQuality];
            float middleZ;
            int vertIndex = 0;
            for (int i = 0; i < thicknessQuality; i++)
            {
                middleZ = Mathf.Lerp(frontZ, backZ, (i + 1) / (float)(thicknessQuality + 1));
                for (int j = 0; j < mouseQuality; j++)
                {
                    angle = j / (float)mouseQuality * Mathf.PI * 2;
                    thickVerts[vertIndex] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), middleZ);

                    vertIndex++;
                }
            }

            middleVerts = new Vector3[mouseQuality + thickVerts.Length + mouseQuality];
            Array.Copy(frontVerts, mouseQuality * mouseFaceQuality + 1, middleVerts, 0, mouseQuality);
            Array.Copy(thickVerts, 0, middleVerts, mouseQuality, thickVerts.Length);
            Array.Copy(backVerts, mouseQuality * mouseFaceQuality + 1, middleVerts, mouseQuality + thickVerts.Length, mouseQuality);

            middleTris = new int[(thicknessQuality + 1) * mouseQuality * 2 * 3];
            triIndex = 0;
            int offset;
            for (int j = 0; j < thicknessQuality + 1; j++)
            {
                offset = mouseQuality * j;
                for (int i = 0; i < mouseQuality - 1; i++)
                {
                    middleTris[triIndex + 0] = i + offset;
                    middleTris[triIndex + 1] = i + mouseQuality + offset;
                    middleTris[triIndex + 2] = i + mouseQuality + 1 + offset;

                    middleTris[triIndex + 3] = i + offset;
                    middleTris[triIndex + 4] = i + mouseQuality + 1 + offset;
                    middleTris[triIndex + 5] = i + 1 + offset;

                    triIndex += 6;
                }
                {
                    middleTris[triIndex + 0] = mouseQuality - 1 + offset;
                    middleTris[triIndex + 1] = mouseQuality - 1 + mouseQuality + offset;
                    middleTris[triIndex + 2] = mouseQuality + offset;

                    middleTris[triIndex + 3] = mouseQuality - 1 + offset;
                    middleTris[triIndex + 4] = mouseQuality + offset;
                    middleTris[triIndex + 5] = 0 + offset;

                    triIndex += 6;
                }
            }
        }
        else
        {
            middleVerts = new Vector3[mouseQuality + mouseQuality];
            Array.Copy(frontVerts, mouseQuality * mouseFaceQuality + 1, middleVerts, 0, mouseQuality);
            Array.Copy(backVerts, mouseQuality * mouseFaceQuality + 1, middleVerts, mouseQuality, mouseQuality);

            middleTris = new int[mouseQuality * 2 * 3];
            triIndex = 0;
            for (int i = 0; i < mouseQuality - 1; i++)
            {
                middleTris[triIndex + 0] = i;
                middleTris[triIndex + 1] = i + mouseQuality;
                middleTris[triIndex + 2] = i + mouseQuality + 1;

                middleTris[triIndex + 3] = i;
                middleTris[triIndex + 4] = i + mouseQuality + 1;
                middleTris[triIndex + 5] = i + 1;

                triIndex += 6;
            }
            {
                middleTris[triIndex + 0] = mouseQuality - 1;
                middleTris[triIndex + 1] = mouseQuality - 1 + mouseQuality;
                middleTris[triIndex + 2] = mouseQuality;

                middleTris[triIndex + 3] = mouseQuality - 1;
                middleTris[triIndex + 4] = mouseQuality;
                middleTris[triIndex + 5] = 0;
            }
        }
        #endregion

        //front->middle->back
        #region Move Triangle Index
        int moveIndex = frontVerts.Length;
        for (int i = 0; i < middleTris.Length; i++)
        {
            middleTris[i] += moveIndex;
        }

        moveIndex = frontVerts.Length + middleVerts.Length;
        for(int i = 0; i < backTris.Length; i++)
        {
            backTris[i] += moveIndex;
        }
        #endregion

        vertices = new Vector3[frontVerts.Length + middleVerts.Length + backVerts.Length];
        Array.Copy(frontVerts, 0, vertices, 0, frontVerts.Length);
        Array.Copy(middleVerts, 0, vertices, frontVerts.Length, middleVerts.Length);
        Array.Copy(backVerts, 0, vertices, frontVerts.Length + middleVerts.Length, backVerts.Length);

        triangles = new int[frontTris.Length + middleTris.Length + backTris.Length];
        Array.Copy(frontTris, 0, triangles, 0, frontTris.Length);
        Array.Copy(middleTris, 0, triangles, frontTris.Length, middleTris.Length);
        Array.Copy(backTris, 0, triangles, frontTris.Length + middleTris.Length, backTris.Length);
    }

    #region Blend
    private Vector3[] Blend_Idle(Vector3[] vertices)
    {
        Vector3[] blendVerts = new Vector3[vertices.Length];

        Vector3 v;
        for(int i = 0; i < blendVerts.Length; i++)
        {
            v = vertices[i];

            blendVerts[i] = new Vector3(Mathf.Lerp(v.x, 0, 0.2f), Mathf.Lerp(v.y, 0, 0.9f), Mathf.Lerp(v.z, 0, 0.2f));
        }

        return blendVerts;
    }

    private Vector3[] Blend_Smile(Vector3[] vertices)
    {
        Vector3[] blendVerts = new Vector3[vertices.Length];

        Vector3 v;
        for (int i = 0; i < blendVerts.Length; i++)
        {
            v = vertices[i];

            blendVerts[i] = new Vector3(v.x, Mathf.Lerp(v.y, -Mathf.Sqrt(1f - Mathf.Pow(v.x, 2)), 0.7f) + 0.5f, v.z);
        }

        return blendVerts;
    }

    private Vector3[] Blend_Angry(Vector3[] vertices)
    {
        Vector3[] blendVerts = new Vector3[vertices.Length];

        Vector3 v;
        float goalY;
        for (int i = 0; i < blendVerts.Length; i++)
        {
            v = vertices[i];

            if (v.y > 0) goalY = 0.25f;
            else if (v.y < 0) goalY = -0.25f;
            else goalY = 0;

            blendVerts[i] = new Vector3(Mathf.Lerp(v.x, 0, 0.2f), Mathf.Lerp(v.y, goalY, 0.9f) - 0.2f, Mathf.Lerp(v.z, 0, 0.2f));
        }

        return blendVerts;
    }

    private Vector3[] Blend_Sad(Vector3[] vertices)
    {
        Vector3[] blendVerts = new Vector3[vertices.Length];

        Vector3 v;
        for (int i = 0; i < blendVerts.Length; i++)
        {
            v = vertices[i];

            blendVerts[i] = new Vector3(v.x, Mathf.Lerp(v.y, Mathf.Sqrt(1f - Mathf.Pow(v.x, 2)), 0.7f) - 0.7f, v.z);
        }

        return blendVerts;
    }
    #endregion
}
