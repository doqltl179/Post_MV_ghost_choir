using System;
using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class EyeGenerator : Generator, IEyeAnimation
{
    [Range(3, 73)] public int _quality = 36;
    [Range(0f, 1f)] public float _round = 0f;
    public Material _eyeMaterial;

    private Vector3[] _roundVertices;

    private Vector3[] _blendVertices;
    private Vector3[] _blendVerticesSum;

    private Vector3[] _vertices;
    private int[] _triangles;
    private Mesh _mesh;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;

    public enum EyeType
    {
        LeftEye,
        RightEye
    }
    public EyeType _eyeType;

    enum FaceDirection
    {
        Forward,
        Back,
        Right,
        Left,
        Up,
        Down
    }

    #region For Editor
    private int _qualitySaver = -1;
    private float _roundSaver = -1;
    #endregion

    private void Start()
    {
        Create();

        if (_blends == null || _blends.Length == 0)
        {
            _blends = new Blend[]
            {
                new Blend("Happy", _vertices, GetBlendHappy(_vertices, 0.4f, 0.1f, 0f), 0f, 1.5f),
                new Blend("Angry", _vertices, GetBlendAngry(_vertices, 0.4f, 0.1f, _eyeType == EyeType.LeftEye ? 150f : -150f), 0f, 1.5f),
                new Blend("Boring", _vertices, GetBlendBoring(_vertices, 0.4f, 0.1f, 180f), 0, 1.5f),
            };
        }
        else
        {
            _blends = new Blend[]
            {
                new Blend("Happy", _vertices, GetBlendHappy(_vertices, 0.4f, 0.1f, 0f), _blends.Where(t => t.BlendName == "Happy").FirstOrDefault().BlendStrength, 1.5f),
                new Blend("Angry", _vertices, GetBlendAngry(_vertices, 0.4f, 0.1f, _eyeType == EyeType.LeftEye ? 165f : -165f), _blends.Where(t => t.BlendName == "Angry").FirstOrDefault().BlendStrength, 1.5f),
                new Blend("Boring", _vertices, GetBlendBoring(_vertices, 0.4f, 0.1f, _eyeType == EyeType.LeftEye ? 185f : -185f), _blends.Where(t => t.BlendName == "Boring").FirstOrDefault().BlendStrength, 1.5f),
            };
        }

        _blendVertices = new Vector3[_vertices.Length];
        Array.Copy(_vertices, 0, _blendVertices, 0, _blendVertices.Length);

        _blendVerticesSum = new Vector3[_blendVertices.Length];

        _qualitySaver = _quality;
        _roundSaver = -1;



        _roundVertices = GetVerticesWithRoundOffset(_vertices, _round, 1.5f);

        //_mesh.vertices = _roundVertices;

        Array.Copy(_roundVertices, 0, _blendVertices, 0, _blendVertices.Length);

        Array.Clear(_blendVerticesSum, 0, _blendVerticesSum.Length);
        Blend b;
        for (int i = 0; i < _blends.Length; i++)
        {
            b = _blends[i];

            b.InitializePropertySavers();
            b.StrengthChanged();
        }
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
            _blendVertices[i] = _roundVertices[i] + _blendVerticesSum[i];
        }

        _roundSaver = _round;
    }

    #region Interface
    public void Init()
    {

    }
    #endregion

    bool isPropertiesChanged = false;
    private void Update()
    {
#if UNITY_EDITOR
        if (_quality != _qualitySaver)
        {
            Create();

            if (_blends == null || _blends.Length == 0)
            {
                _blends = new Blend[]
                {
                    new Blend("Happy", _vertices, GetBlendHappy(_vertices, 0.4f, 0.1f, 0f), 0f, 1.5f),
                    new Blend("Angry", _vertices, GetBlendAngry(_vertices, 0.4f, 0.1f, _eyeType == EyeType.LeftEye ? 150f : -150f), 0f, 1.5f),
                    new Blend("Boring", _vertices, GetBlendBoring(_vertices, 0.4f, 0.1f, 180f), 0, 1.5f),
                };
            }
            else
            {
                _blends = new Blend[]
                {
                    new Blend("Happy", _vertices, GetBlendHappy(_vertices, 0.4f, 0.1f, 0f), _blends.Where(t => t.BlendName == "Happy").FirstOrDefault().BlendStrength, 1.5f),
                    new Blend("Angry", _vertices, GetBlendAngry(_vertices, 0.4f, 0.1f, _eyeType == EyeType.LeftEye ? 165f : -165f), _blends.Where(t => t.BlendName == "Angry").FirstOrDefault().BlendStrength, 1.5f),
                    new Blend("Boring", _vertices, GetBlendBoring(_vertices, 0.4f, 0.1f, _eyeType == EyeType.LeftEye ? 185f : -185f), _blends.Where(t => t.BlendName == "Boring").FirstOrDefault().BlendStrength, 1.5f),
                };
            }

            _blendVertices = new Vector3[_vertices.Length];
            Array.Copy(_vertices, 0, _blendVertices, 0, _blendVertices.Length);

            _blendVerticesSum = new Vector3[_blendVertices.Length];

            _qualitySaver = _quality;
            _roundSaver = -1;

            isPropertiesChanged = true;
        }

        if(_round != _roundSaver)
        {
            _roundVertices = GetVerticesWithRoundOffset(_vertices, _round, 1.5f);

            //_mesh.vertices = _roundVertices;

            Array.Copy(_roundVertices, 0, _blendVertices, 0, _blendVertices.Length);

            Array.Clear(_blendVerticesSum, 0, _blendVerticesSum.Length);
            Blend b;
            for (int i = 0; i < _blends.Length; i++)
            {
                b = _blends[i];

                b.InitializePropertySavers();
                b.StrengthChanged();
            }
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
                _blendVertices[i] = _roundVertices[i] + _blendVerticesSum[i];
            }

            _roundSaver = _round;

            isPropertiesChanged = true;
        }
#endif

        if (_blends != null)
        {
            Blend b;
            bool blendStrengthChanged = false;
            for (int i = 0; i < _blends.Length; i++)
            {
                b = _blends[i];

                b.StrengthChanged(ref blendStrengthChanged);
            }

            if (blendStrengthChanged)
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
                    _blendVertices[i] = _roundVertices[i] + _blendVerticesSum[i];
                }

                isPropertiesChanged = true;
            }
        }

        if (isPropertiesChanged)
        {
            _mesh.vertices = _blendVertices;

            _mesh.RecalculateBounds();
            _mesh.RecalculateNormals();
            _mesh.RecalculateTangents();

            isPropertiesChanged = false;
        }
    }

    #region Override
    public override void Create()
    {
        _mesh = GetMesh(_quality);

        if (_meshFilter == null)
        {
            _meshFilter = GetComponent<MeshFilter>();
            if (_meshFilter == null)
                _meshFilter = gameObject.AddComponent<MeshFilter>();
        }
        _meshFilter.mesh = _mesh;

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

        if (_eyeMaterial == null)
        {
            _eyeMaterial = new Material(Shader.Find("Standard"));
        }

        _meshRenderer.material = _eyeMaterial;
    }
    #endregion

    private Mesh GetMesh(int meshQuality)
    {
        GetVerticesAndTriangles(meshQuality, out _vertices, out _triangles);

        Mesh mesh = new Mesh();
        mesh.vertices = _vertices;
        mesh.triangles = _triangles;

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        return mesh;
    }

    private Vector3[] GetVerticesWithRoundOffset(Vector3[] vertices, float round, float size = 1f)
    {
        Vector3[] roundVerts = new Vector3[vertices.Length];
        for(int i = 0; i < roundVerts.Length; i++)
        {
            roundVerts[i] = Vector3.Lerp(vertices[i], vertices[i].normalized * 0.5f * size, round);
        }

        return roundVerts;
    }

    private void GetVerticesAndTriangles(int meshQuality, out Vector3[] vertices, out int[] triangles)
    {
        Vector3[] forwardVertices = GetFaceVertices(meshQuality, FaceDirection.Forward);
        Vector3[] backVertices = GetFaceVertices(meshQuality, FaceDirection.Back);
        Vector3[] rightVertices = GetFaceVertices(meshQuality, FaceDirection.Right);
        Vector3[] leftVertices = GetFaceVertices(meshQuality, FaceDirection.Left);
        Vector3[] upVertices = GetFaceVertices(meshQuality, FaceDirection.Up);
        Vector3[] downVertices = GetFaceVertices(meshQuality, FaceDirection.Down);

        vertices = new Vector3[forwardVertices.Length + backVertices.Length + rightVertices.Length + leftVertices.Length + upVertices.Length + downVertices.Length];

        Array.Copy(forwardVertices, 0, vertices, 0, forwardVertices.Length);
        Array.Copy(backVertices, 0, vertices, forwardVertices.Length, backVertices.Length);
        Array.Copy(rightVertices, 0, vertices, forwardVertices.Length + backVertices.Length, rightVertices.Length);
        Array.Copy(leftVertices, 0, vertices, forwardVertices.Length + backVertices.Length + rightVertices.Length, leftVertices.Length);
        Array.Copy(upVertices, 0, vertices, forwardVertices.Length + backVertices.Length + rightVertices.Length + leftVertices.Length, upVertices.Length);
        Array.Copy(downVertices, 0, vertices, forwardVertices.Length + backVertices.Length + rightVertices.Length + leftVertices.Length + upVertices.Length, downVertices.Length);

        int usedLength = meshQuality - 1;
        int faceCount = 6;
        triangles = new int[usedLength * 2 * 3 * usedLength * faceCount];
        int vertIndex;
        int vertIndexOffset;
        int triIndex = 0;
        for (int t = 0; t < faceCount; t++)
        {
            vertIndexOffset = t * meshQuality * meshQuality;

            for (int y = 0; y < usedLength; y++)
            {
                for (int x = 0; x < usedLength; x++)
                {
                    vertIndex = y * meshQuality + x + vertIndexOffset;

                    triangles[triIndex + 0] = vertIndex + 1;
                    triangles[triIndex + 1] = vertIndex;
                    triangles[triIndex + 2] = vertIndex + meshQuality;

                    triangles[triIndex + 3] = vertIndex + 1;
                    triangles[triIndex + 4] = vertIndex + meshQuality;
                    triangles[triIndex + 5] = vertIndex + meshQuality + 1;

                    triIndex += 6;
                }
            }
        }
    }

    private Vector3[] GetFaceVertices(int meshQuality, FaceDirection direction)
    {
        Vector3[] vertices = new Vector3[meshQuality * meshQuality];

        Vector3 startPoint = Vector3.zero;
        Vector3 endPoint = Vector3.zero;
        switch (direction)
        {
            case FaceDirection.Forward:
                {
                    startPoint = new Vector3(0.5f, -0.5f, 0.5f);
                    endPoint = new Vector3(-0.5f, 0.5f, 0.5f);

                    float x, y;
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        y = i / meshQuality;
                        x = i % meshQuality;
                        vertices[i] = new Vector3(Mathf.Lerp(startPoint.x, endPoint.x, x / (meshQuality - 1)), Mathf.Lerp(startPoint.y, endPoint.y, y / (meshQuality - 1)), 0.5f);
                    }
                }
                break;

            case FaceDirection.Back:
                {
                    startPoint = new Vector3(-0.5f, -0.5f, -0.5f);
                    endPoint = new Vector3(0.5f, 0.5f, -0.5f);

                    float x, y;
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        y = i / meshQuality;
                        x = i % meshQuality;
                        vertices[i] = new Vector3(Mathf.Lerp(startPoint.x, endPoint.x, x / (meshQuality - 1)), Mathf.Lerp(startPoint.y, endPoint.y, y / (meshQuality - 1)), -0.5f);
                    }
                }
                break;

            case FaceDirection.Right:
                {
                    startPoint = new Vector3(0.5f, -0.5f, -0.5f);
                    endPoint = new Vector3(0.5f, 0.5f, 0.5f);

                    float y, z;
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        y = i / meshQuality;
                        z = i % meshQuality;
                        vertices[i] = new Vector3(0.5f, Mathf.Lerp(startPoint.y, endPoint.y, y / (meshQuality - 1)), Mathf.Lerp(startPoint.z, endPoint.z, z / (meshQuality - 1)));
                    }
                }
                break;

            case FaceDirection.Left:
                {
                    startPoint = new Vector3(-0.5f, -0.5f, 0.5f);
                    endPoint = new Vector3(-0.5f, 0.5f, -0.5f);

                    float y, z;
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        y = i / meshQuality;
                        z = i % meshQuality;
                        vertices[i] = new Vector3(-0.5f, Mathf.Lerp(startPoint.y, endPoint.y, y / (meshQuality - 1)), Mathf.Lerp(startPoint.z, endPoint.z, z / (meshQuality - 1)));
                    }
                }
                break;

            case FaceDirection.Up:
                {
                    startPoint = new Vector3(-0.5f, 0.5f, -0.5f);
                    endPoint = new Vector3(0.5f, 0.5f, 0.5f);

                    float x, z;
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        z = i / meshQuality;
                        x = i % meshQuality;
                        vertices[i] = new Vector3(Mathf.Lerp(startPoint.x, endPoint.x, x / (meshQuality - 1)), 0.5f, Mathf.Lerp(startPoint.z, endPoint.z, z / (meshQuality - 1)));
                    }
                }
                break;

            case FaceDirection.Down:
                {
                    startPoint = new Vector3(-0.5f, -0.5f, 0.5f);
                    endPoint = new Vector3(0.5f, -0.5f, -0.5f);

                    float x, z;
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        z = i / meshQuality;
                        x = i % meshQuality;
                        vertices[i] = new Vector3(Mathf.Lerp(startPoint.x, endPoint.x, x / (meshQuality - 1)), -0.5f, Mathf.Lerp(startPoint.z, endPoint.z, z / (meshQuality - 1)));
                    }
                }
                break;
        }

        return vertices;
    }

    #region Blend
    private Vector3[] GetBlendBoring(Vector3[] vertices, float roundRadius, float roundTailRadius, float rotateAngleZ)
    {
        Vector3[] verts = Blend_Round(vertices, 1f, roundRadius, roundTailRadius);
        for (int i = 0; i < verts.Length; i++)
        {
            verts[i] = RotateAround(verts[i], Vector3.forward, rotateAngleZ);
        }

        return verts;
    }

    private Vector3[] GetBlendAngry(Vector3[] vertices, float roundRadius, float roundTailRadius, float rotateAngleZ)
    {
        Vector3[] verts = Blend_Round(vertices, 1f, roundRadius, roundTailRadius);
        for (int i = 0; i < verts.Length; i++)
        {
            verts[i] = RotateAround(verts[i], Vector3.forward, rotateAngleZ);
        }

        return verts;
    }

    private Vector3[] GetBlendHappy(Vector3[] vertices, float roundRadius, float roundTailRadius, float rotateAngleZ)
    {
        Vector3[] verts = Blend_Round(vertices, 1f, roundRadius, roundTailRadius);
        for (int i = 0; i < verts.Length; i++)
        {
            verts[i] = RotateAround(verts[i], Vector3.forward, rotateAngleZ);
        }

        return verts;
    }

    private Vector3[] Blend_Round(Vector3[] vertices, float blend, float roundRadius, float roundTailRadius)
    {
        Vector3[] roundVerts = new Vector3[vertices.Length];

        Vector3 vertex;
        float angle;
        Vector3 roundPos;
        Vector3 goalPos;
        for(int i = 0; i < roundVerts.Length; i++)
        {
            vertex = vertices[i];

            angle = Mathf.Atan2(vertex.y, vertex.x);
            if (angle > 0)
            {
                roundPos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * roundRadius;
            }
            else
            {
                roundPos = (angle < -Mathf.PI * 0.5f ? Vector3.left : Vector3.right) * roundRadius;
            }

            goalPos = roundPos + (vertex - roundPos).normalized * roundTailRadius;

            roundVerts[i] = Vector3.Lerp(vertex, goalPos, blend);
        }

        return roundVerts;
    }
    #endregion

    private Vector3 RotateAround(Vector3 current, Vector3 axis, float angle)
    {
        return Quaternion.Euler(axis * angle) * current;
    }

    private Vector3[] LerpArray(Vector3[] from, Vector3[] to, float t)
    {
        Vector3[] lerpVerts = new Vector3[from.Length];
        for(int i = 0; i < lerpVerts.Length; i++)
        {
            lerpVerts[i] = Vector3.Slerp(from[i], to[i], t);
        }

        return lerpVerts;
    }

    private float Pythagoras(float mostLongLength, float useLineLength)
    {
        return Mathf.Sqrt(Mathf.Pow(mostLongLength, 2) - Mathf.Pow(useLineLength, 2));
    }
}
