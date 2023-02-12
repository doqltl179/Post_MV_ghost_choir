using System.IO;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class FieldGenerator : Generator
{
    [Header("Properties")]
    //public Vector2Int _fieldSize = new Vector2Int(255, 255);
    [Range(1, 255)] public int _fieldSize = 255;
    //public Vector2 _fieldSizeOffset = Vector2.one;
    [Range(0.1f, 10f)] public float _fieldSizeOffset = 1f;
    [Range(0f, 25.5f)] public float _fieldHeightOffset = 1f;
    [Range(0, 10)] public int _fieldNormalizeSensitive = 3;
    [Range(0, 10)] public int _fieldColorNormalizeSensitive = 3;

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private MeshCollider _meshCollider;

    [Header("Ingredients")]
    public Material mat_field;
    public Texture2D tex_field;
    private const string TextureDirectoryPath = "_MyFolder/Textures";
    private const string TextureFileName = "FieldTexture.png";

    private Vector3[] _vertices;
    private Vector3[] _verticesOfAppliedSizeOffset;
    private Vector3[] _verticesOfAppliedHeightOffset;
    private Vector3[] _verticesOfAppliedNormalize;
    private int[] _triangles;
    private Vector2[] _uvs;
    private Color[] _colors;
    private Color[] _colorsOfAppliedNormalize;
    private Mesh _mesh;
    private const string FieldMeshDirectoryPath = "_MyFolder/Prefabs/Field";
    private const string FieldMeshFileName = "Field.fbx";

    [Header("Props")]
    public Transform _props;




    //private void OnDestroy()
    //{
    //    if(tex_field != null)
    //    {
    //        DestroyImmediate(tex_field);
    //    }    
    //}

#if UNITY_EDITOR
    [Header("Editor Properties")]
    public bool _changeMesh = false;
    public bool _changeTexture = false;
    public bool _textureAutoSave = false;

    //private Vector2Int _fieldSizeSaver = Vector2Int.zero;
    private int _fieldSizeSaver = 0;
    //private Vector2 _fieldSizeOffsetSaver = Vector2.one;
    private float _fieldSizeOffsetSaver = 1f;
    private float _fieldHeightOffsetSaver = -1f;
    private int _fieldNormalizeSensitiveSaver = 0;
    private int _fieldColorNormalizeSensitiveSaver = 0;

    private void Update()
    {
        if (_changeMesh)
        {
            if (_fieldSize != _fieldSizeSaver)
            {
                //if (_fieldSize.x < 1)
                //{
                //    _fieldSize.x = 1;

                //    return;
                //}
                //else if (_fieldSize.x > 255)
                //{
                //    _fieldSize.x = 255;

                //    return;
                //}

                //if (_fieldSize.y < 1)
                //{
                //    _fieldSize.y = 1;

                //    return;
                //}
                //else if (_fieldSize.y > 255)
                //{
                //    _fieldSize.y = 255;

                //    return;
                //}

                Create();

                _fieldSizeSaver = _fieldSize;
            }

            if (_fieldSizeOffset != _fieldSizeOffsetSaver)
            {
                _verticesOfAppliedSizeOffset = GetVerticesOfAppliedSizeOffset(_vertices, _fieldSizeOffset);
                _verticesOfAppliedNormalize = GetVerticesOfAppliedNormalize(_verticesOfAppliedSizeOffset, _fieldSize, _fieldNormalizeSensitive);
                _verticesOfAppliedHeightOffset = GetVerticesOfAppliedHeightOffset(_verticesOfAppliedNormalize, _fieldHeightOffset);
                _mesh.vertices = _verticesOfAppliedHeightOffset;

                _mesh.RecalculateBounds();
                _mesh.RecalculateNormals();
                _mesh.RecalculateTangents();

                SetHeightProps();

                _meshCollider.sharedMesh = _mesh;

                _fieldSizeOffsetSaver = _fieldSizeOffset;
            }

            if (_fieldNormalizeSensitive != _fieldNormalizeSensitiveSaver)
            {
                _verticesOfAppliedNormalize = GetVerticesOfAppliedNormalize(_verticesOfAppliedSizeOffset, _fieldSize, _fieldNormalizeSensitive);
                _verticesOfAppliedHeightOffset = GetVerticesOfAppliedHeightOffset(_verticesOfAppliedNormalize, _fieldHeightOffset);
                _mesh.vertices = _verticesOfAppliedHeightOffset;

                _mesh.RecalculateBounds();
                _mesh.RecalculateNormals();
                _mesh.RecalculateTangents();

                _meshCollider.sharedMesh = _mesh;

                SetHeightProps();

                _fieldNormalizeSensitiveSaver = _fieldNormalizeSensitive;
            }

            if (_fieldHeightOffset != _fieldHeightOffsetSaver)
            {
                _verticesOfAppliedHeightOffset = GetVerticesOfAppliedHeightOffset(_verticesOfAppliedNormalize, _fieldHeightOffset);
                _mesh.vertices = _verticesOfAppliedHeightOffset;

                _mesh.RecalculateBounds();
                _mesh.RecalculateNormals();
                _mesh.RecalculateTangents();

                SetHeightProps();

                _meshCollider.sharedMesh = _mesh;

                _fieldHeightOffsetSaver = _fieldHeightOffset;
            }

            if (_fieldColorNormalizeSensitive != _fieldColorNormalizeSensitiveSaver)
            {
                _colorsOfAppliedNormalize = GetColorsOfAppliedNormalize(_colors, _fieldSize, _fieldColorNormalizeSensitive);
                _mesh.colors = _colorsOfAppliedNormalize;

                if (_changeTexture)
                {
                    tex_field.SetPixels(_colorsOfAppliedNormalize);
                    tex_field.Apply();

                    if (_textureAutoSave)
                    {
                        SaveTexture(tex_field, TextureDirectoryPath, TextureFileName);
                    }
                }

                _fieldColorNormalizeSensitiveSaver = _fieldColorNormalizeSensitive;
            }
        }
    }
#endif

    #region Override
#if UNITY_EDITOR
    public override void Create()
    {
        _vertices = GetVertices(_fieldSize);
        _verticesOfAppliedSizeOffset = GetVerticesOfAppliedSizeOffset(_vertices, _fieldSizeOffset);
        _verticesOfAppliedNormalize = GetVerticesOfAppliedNormalize(_verticesOfAppliedSizeOffset, _fieldSize, _fieldNormalizeSensitive);
        _verticesOfAppliedHeightOffset = GetVerticesOfAppliedHeightOffset(_verticesOfAppliedNormalize, _fieldHeightOffset);

        _triangles = GetTriangles(_fieldSize);
        _uvs = GetUVs(_fieldSize);

        _colors = GetColors(_fieldSize);
        _colorsOfAppliedNormalize = GetColorsOfAppliedNormalize(_colors, _fieldSize, _fieldColorNormalizeSensitive);

        _mesh = new Mesh();
        //_mesh.vertices = _vertices;
        _mesh.vertices = _verticesOfAppliedHeightOffset;
        _mesh.triangles = _triangles;
        _mesh.uv = _uvs;
        _mesh.colors = _colorsOfAppliedNormalize;

        _mesh.RecalculateBounds();
        _mesh.RecalculateNormals();
        _mesh.RecalculateTangents();

        if (_meshFilter == null)
        {
            _meshFilter = GetComponent<MeshFilter>();
            if(_meshFilter == null)
            {
                _meshFilter = gameObject.AddComponent<MeshFilter>();
            }
        }
        _meshFilter.mesh = _mesh;

        if(_meshRenderer == null)
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            if(_meshRenderer == null)
            {
                _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }
        }
        if(mat_field == null)
        {
            mat_field = new Material(Shader.Find("Standard"));
        }
        _meshRenderer.material = mat_field;

        if(_meshCollider == null)
        {
            _meshCollider = GetComponent<MeshCollider>();
            if(_meshCollider == null)
            {
                _meshCollider = gameObject.AddComponent<MeshCollider>();
            }
        }
        _meshCollider.sharedMesh = _mesh;

        if (tex_field == null)
        {
            tex_field = new Texture2D(_fieldSize, _fieldSize);
            tex_field.SetPixels(_colorsOfAppliedNormalize);
            tex_field.Apply();

            if (_textureAutoSave)
            {
                SaveTexture(tex_field, TextureDirectoryPath, TextureFileName);
            }
        }
        else if (tex_field.width != _fieldSize || tex_field.height != _fieldSize)
        {
            //DestroyImmediate(tex_field);

            if (_changeTexture)
            {
                tex_field = new Texture2D(_fieldSize, _fieldSize);
                tex_field.SetPixels(_colorsOfAppliedNormalize);
                tex_field.Apply();
            }

            if (_textureAutoSave)
            {
                SaveTexture(tex_field, TextureDirectoryPath, TextureFileName);
            }
        }
        mat_field.SetTexture("_MainTex", tex_field);

        SetHeightProps();
    }
#endif

    private Vector3[] GetVertices(int size)
    {
        int w = size + 1;
        int h = size + 1;
        Vector3 offset = new Vector3(w * 0.5f, 0, h * 0.5f);

        Vector3[] vertices = new Vector3[w * h];
        int vertIndex = 0;
        for(int z = 0; z < h; z++)
        {
            for(int x = 0; x < w; x++)
            {
                vertices[vertIndex] = new Vector3(x, Random.Range(-1f, 1f), z) - offset;

                vertIndex++;
            }
        }

        return vertices;
    }

    private int[] GetTriangles(int size)
    {
        int w = size + 1;
        int h = size + 1;

        int[] triangles = new int[size * size * 2 * 3];
        int triIndex = 0;
        int vertIndex = 0;
        for(int z = 0; z < size; z++)
        {
            for(int x = 0; x < size; x++)
            {
                triangles[triIndex + 0] = vertIndex;
                triangles[triIndex + 1] = vertIndex + w;
                triangles[triIndex + 2] = vertIndex + w + 1;

                triangles[triIndex + 3] = vertIndex;
                triangles[triIndex + 4] = vertIndex + w + 1;
                triangles[triIndex + 5] = vertIndex + 1;

                triIndex += 6;

                vertIndex++;
            }

            vertIndex++;
        }

        return triangles;
    }

    private Vector2[] GetUVs(int size)
    {
        int w = size + 1;
        int h = size + 1;

        Vector2[] uvs = new Vector2[w * h];
        int uvIndex = 0;
        float uv_x, uv_y;
        for(int z = 0; z < h; z++)
        {
            uv_y = (float)z / size;
            for(int x = 0; x < w; x++)
            {
                uv_x = (float)x / size;

                uvs[uvIndex] = new Vector2(uv_x, uv_y);

                uvIndex++;
            }
        }

        return uvs;
    }

    private Color[] GetColors(int size)
    {
        int w = size + 1;
        int h = size + 1;

        Color[] colors = new Color[w * h];
        int colIndex = 0;
        int r;
        for(int z = 0; z < h; z++)
        {
            for(int x = 0; x < w; x++)
            {
                r = Random.Range(0, 7);
                if(r == 0)
                {
                    colors[colIndex] = Color.gray;
                }
                else if(r == 6 || r == 5)
                {
                    colors[colIndex] = Color.black;
                }
                else
                {
                    colors[colIndex] = new Color(Random.Range(0.1f, 0.4f), Random.Range(0.1f, 0.36f), Random.Range(0f, 0.1f));
                }

                colIndex++;
            }
        }

        return colors;
    }
    #endregion

    private void SetHeightProps()
    {
        if(_props != null)
        {
            Transform child;
            Ray ray;
            RaycastHit hit;
            for(int i = 0; i < _props.childCount; i++)
            {
                child = _props.GetChild(i);

                ray = new Ray(child.position + Vector3.up * 100, Vector3.down);
                if(Physics.Raycast(ray, out hit))
                {
                    child.position = hit.point + Vector3.down * 0.06f;

                    float angleY = child.eulerAngles.y;
                    child.up = hit.normal;
                    child.eulerAngles = new Vector3(child.eulerAngles.x, angleY, child.eulerAngles.z);
                }
            }
        }
    }

    private Vector3[] GetVerticesOfAppliedHeightOffset(Vector3[] vertices, float height)
    {
        Vector3[] verts = new Vector3[vertices.Length];
        Vector3 vert;
        for(int i = 0; i < verts.Length; i++)
        {
            vert = vertices[i];
            verts[i] = new Vector3(vert.x, vert.y * height, vert.z);
        }

        return verts;
    }

    private Vector3[] GetVerticesOfAppliedSizeOffset(Vector3[] vertices, float sizeOffset)
    {
        Vector3[] verts = new Vector3[vertices.Length];

        Vector3 vert;
        for(int i = 0; i < verts.Length; i++)
        {
            vert = vertices[i];

            verts[i] = new Vector3(vert.x * sizeOffset, vert.y, vert.z * sizeOffset);
        }

        return verts;
    }

    private Vector3[] GetVerticesOfAppliedNormalize(Vector3[] vertices, int size, int normalizeSensitive)
    {
        int w = size + 1;
        int h = size + 1;

        Vector3[] verts = new Vector3[vertices.Length];
        System.Array.Copy(vertices, 0, verts, 0, verts.Length);

        Vector3 vert;
        Vector3 arrayValue = Vector3.zero;
        float arrayValueSum;
        int sumCount;
        for(int i = 0; i < normalizeSensitive; i++)
        {
            for(int v = 0; v < verts.Length; v++)
            {
                vert = verts[v];

                arrayValueSum = 0;
                sumCount = 0;

                if (TryGetArrayValue(verts, v - h - 1, ref arrayValue))
                {
                    arrayValueSum += arrayValue.y;
                    sumCount++;
                }
                if (TryGetArrayValue(verts, v - h, ref arrayValue))
                {
                    arrayValueSum += arrayValue.y;
                    sumCount++;
                }
                if (TryGetArrayValue(verts, v - h + 1, ref arrayValue))
                {
                    arrayValueSum += arrayValue.y;
                    sumCount++;
                }

                if (TryGetArrayValue(verts, v - 1, ref arrayValue))
                {
                    arrayValueSum += arrayValue.y;
                    sumCount++;
                }
                if (TryGetArrayValue(verts, v, ref arrayValue))
                {
                    arrayValueSum += arrayValue.y;
                    sumCount++;
                }
                if (TryGetArrayValue(verts, v + 1, ref arrayValue))
                {
                    arrayValueSum += arrayValue.y;
                    sumCount++;
                }

                if (TryGetArrayValue(verts, v + h - 1, ref arrayValue))
                {
                    arrayValueSum += arrayValue.y;
                    sumCount++;
                }
                if (TryGetArrayValue(verts, v + h, ref arrayValue))
                {
                    arrayValueSum += arrayValue.y;
                    sumCount++;
                }
                if (TryGetArrayValue(verts, v + h + 1, ref arrayValue))
                {
                    arrayValueSum += arrayValue.y;
                    sumCount++;
                }

                verts[v] = new Vector3(vert.x, arrayValueSum / sumCount, vert.z);
            }
        }

        return verts;
    }

    private Color[] GetColorsOfAppliedNormalize(Color[] colors, int size, int normalizeSensitive)
    {
        int w = size + 1;
        int h = size + 1;

        Color[] cols = new Color[colors.Length];
        System.Array.Copy(colors, 0, cols, 0, cols.Length);

        Color col;
        Color arrayValue = Color.black;
        Color arrayValueSum;
        int sumCount;
        for (int i = 0; i < normalizeSensitive; i++)
        {
            for (int v = 0; v < cols.Length; v++)
            {
                col = cols[v];

                arrayValueSum = Color.black;
                sumCount = 0;

                if (TryGetArrayValue(cols, v - h - 1, ref arrayValue))
                {
                    arrayValueSum += arrayValue;
                    sumCount++;
                }
                if (TryGetArrayValue(cols, v - h, ref arrayValue))
                {
                    arrayValueSum += arrayValue;
                    sumCount++;
                }
                if (TryGetArrayValue(cols, v - h + 1, ref arrayValue))
                {
                    arrayValueSum += arrayValue;
                    sumCount++;
                }

                if (TryGetArrayValue(cols, v - 1, ref arrayValue))
                {
                    arrayValueSum += arrayValue;
                    sumCount++;
                }
                if (TryGetArrayValue(cols, v, ref arrayValue))
                {
                    arrayValueSum += arrayValue;
                    sumCount++;
                }
                if (TryGetArrayValue(cols, v + 1, ref arrayValue))
                {
                    arrayValueSum += arrayValue;
                    sumCount++;
                }

                if (TryGetArrayValue(cols, v + h - 1, ref arrayValue))
                {
                    arrayValueSum += arrayValue;
                    sumCount++;
                }
                if (TryGetArrayValue(cols, v + h, ref arrayValue))
                {
                    arrayValueSum += arrayValue;
                    sumCount++;
                }
                if (TryGetArrayValue(cols, v + h + 1, ref arrayValue))
                {
                    arrayValueSum += arrayValue;
                    sumCount++;
                }

                cols[v] = arrayValueSum / sumCount;
            }
        }

        return cols;
    }

    private bool TryGetArrayValue(Vector3[] array, int index, ref Vector3 value)
    {
        try
        {
            value = array[index];

            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool TryGetArrayValue(Color[] array, int index, ref Color value)
    {
        try
        {
            value = array[index];

            return true;
        }
        catch
        {
            return false;
        }
    }

    private void SaveTexture(Texture2D texture, string directoryPath, string fileName)
    {
        if(string.IsNullOrEmpty(directoryPath) || string.IsNullOrEmpty(fileName))
        {
            Debug.LogError("Not used correctly directory path or file name");

            return;
        }

        if(!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        try
        {
            byte[] textureBuffer = texture.EncodeToPNG();

            string path = Path.Combine(Application.dataPath, directoryPath, fileName);
            File.WriteAllBytes(path, textureBuffer);
        }
        catch
        {
            Debug.Log("Save Failed");
        }
    }

#if UNITY_EDITOR
    #region Custom Inspector
    public void LoadFieldMesh()
    {
        string loadPath = Path.Combine("Assets", FieldMeshDirectoryPath, FieldMeshFileName);
        Mesh loadMesh = AssetDatabase.LoadAssetAtPath<Mesh>(loadPath);
        if(loadMesh != null)
        {
            _mesh = loadMesh;

            if (_meshFilter == null)
            {
                _meshFilter = GetComponent<MeshFilter>();
                if (_meshFilter == null)
                    _meshFilter = gameObject.AddComponent<MeshFilter>();
            }
            _meshFilter.mesh = _mesh;

            if(_meshCollider == null)
            {
                _meshCollider = GetComponent<MeshCollider>();
                if (_meshCollider == null)
                    _meshCollider = gameObject.AddComponent<MeshCollider>();
            }
            _meshCollider.sharedMesh = _mesh;

            SetHeightProps();
        }
        else
        {
            Debug.Log(string.Format("Mesh not exist. path : {0}", loadPath));
        }
    }

    public void LoadFieldTexture()
    {
        string loadPath = Path.Combine("Assets", TextureDirectoryPath, TextureFileName);
        Texture2D loadTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(loadPath);
        if (loadTexture != null)
        {
            tex_field = loadTexture;
            mat_field.SetTexture("_MainTex", tex_field);
        }
        else
        {
            Debug.Log(string.Format("Texture not exist. path : {0}", loadPath));
        }
    }

    public void ApplyTexture()
    {
        SaveTexture(tex_field, TextureDirectoryPath, TextureFileName);

        LoadFieldTexture();
    }
    #endregion
#endif
}
