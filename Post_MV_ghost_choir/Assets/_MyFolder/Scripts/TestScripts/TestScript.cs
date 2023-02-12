#if UNITY_EDITOR
using UnityEditor;
using System;
using UnityEngine;
using UnityEngine.UI;
using GD.MinMaxSlider;

[ExecuteInEditMode]
public class TestScript : MonoBehaviour
{
    [Header("Dot")]
    public bool _calculateDot;

    public Dot[] _dotProperties;
    private int _dotProperticesIndex;

    [Header("UI")]
    public RectTransform _canvas;

    [Header("Video")]
    public bool _showVideoProperties;
    public Font _textFont;
    private Image _videoNormalizedTimeImage;
    private const int VideoNormalizedTimeImageHeight = 25;
    private Text _videoNormalizedTimeText;
    private float _videoNormalizedTime = 0;

    [Header("Audio Visualizer")]
    public bool _showAudioVisualizer;
    public AudioVisualizer _audioVisualizerProperties;
    private Image[] _visualImages;
    private const int VisualImageHeightMin = 25;
    private Image[] _visualSampleCountCheckImages;
    private const int SampleCountCheckInterval = 500;

    [Header("Test Character")]
    public CharacterGenerator _testCharacter;



    private void Awake()
    {
        if (Application.isPlaying)
        {
            VideoManager.Instance.VideoPreparedAction += CreateVisualImages;
            VideoManager.Instance.VideoPreparedAction += CreateVideoProperties;

            _testCharacter.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (Application.isPlaying)
        {
            VideoManager.Instance.VideoPreparedAction -= CreateVisualImages;
            VideoManager.Instance.VideoPreparedAction -= CreateVideoProperties;
        }
    }

    private void FixedUpdate()
    {
        if (VideoManager.Instance.State == VideoManager.VideoState.Playing || VideoManager.Instance.State == VideoManager.VideoState.Paused)
        {
            if (_showVideoProperties)
            {
                _videoNormalizedTime = VideoManager.Instance.NormalizeTime;

                Vector2 imageScale = _videoNormalizedTimeImage.rectTransform.localScale;
                imageScale.x = _videoNormalizedTime;
                _videoNormalizedTimeImage.rectTransform.localScale = imageScale;

                _videoNormalizedTimeText.text = string.Format("{0:F4}", _videoNormalizedTime);
            }

            if (_showAudioVisualizer)
            {
                if (_visualImages != null && _visualImages.Length > 0)
                {
                    Vector2 size_temp;
                    float sampleValue;
                    float sizeHeight;
                    for (int i = _audioVisualizerProperties.AudioSamplesRange.x; i < _audioVisualizerProperties.AudioSamplesRange.y; i++)
                    {
                        size_temp = _visualImages[i].rectTransform.sizeDelta;
                        sampleValue = VideoManager.Instance._audioSamples[i] * _audioVisualizerProperties.AudioSampleOffset;
                        sizeHeight = sampleValue * Screen.height * _audioVisualizerProperties.VisualizerHeightOffset + VisualImageHeightMin;
                        if (size_temp.y < sizeHeight)
                        {
                            size_temp.y = sizeHeight;
                        }
                        else
                        {
                            size_temp.y -= _audioVisualizerProperties.VisualizerDownSpeed * Time.deltaTime;
                            if (size_temp.y < VisualImageHeightMin)
                                size_temp.y = VisualImageHeightMin;
                        }

                        _visualImages[i].rectTransform.sizeDelta = size_temp;
                    }
                }
            }
        }
    }

    private void CreateVideoProperties()
    {
        if(_showVideoProperties)
        {
            if (_videoNormalizedTimeImage != null)
                Destroy(_videoNormalizedTimeImage.gameObject);
            if (_videoNormalizedTimeText != null)
                Destroy(_videoNormalizedTimeText.gameObject);



            RectTransform rectTransform;
            Image image;
            Text text;



            GameObject normalizedTimeImage = new GameObject("NormalizedTimeImage",
                    new Type[]{
                    typeof(RectTransform),
                    typeof(Image)
                    });
            normalizedTimeImage.transform.SetParent(_canvas);

            rectTransform = normalizedTimeImage.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.up;
            rectTransform.anchorMax = Vector2.up;
            rectTransform.pivot = Vector2.up;
            rectTransform.anchoredPosition = Vector3.zero;
            rectTransform.sizeDelta = new Vector2(_canvas.sizeDelta.x, VideoNormalizedTimeImageHeight * 0.5f);

            image = normalizedTimeImage.GetComponent<Image>();
            image.color = Color.yellow;

            _videoNormalizedTimeImage = image;



            GameObject normalizedTimeText = new GameObject("NormalizedTimeText",
                    new Type[]{
                    typeof(RectTransform),
                    typeof(Text)
                    });
            normalizedTimeText.transform.SetParent(_canvas);

            rectTransform = normalizedTimeText.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.up;
            rectTransform.anchorMax = Vector2.up;
            rectTransform.pivot = Vector2.up;
            rectTransform.anchoredPosition = Vector2.up * -VideoNormalizedTimeImageHeight;
            rectTransform.sizeDelta = new Vector2(_canvas.sizeDelta.x, VideoNormalizedTimeImageHeight * 2);

            text = normalizedTimeText.GetComponent<Text>();
            text.resizeTextForBestFit = true;
            text.resizeTextMaxSize = 300;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.font = _textFont;

            _videoNormalizedTimeText = text;
        }
    }

    private void CreateVisualImages()
    {
        if (_showAudioVisualizer)
        {
            bool deleteAllImages = _visualImages != null;
            if (deleteAllImages)
            {
                for (int i = 0; i < _visualImages.Length; i++)
                {
                    Destroy(_visualImages[i].gameObject);
                }

                for(int i = 0; i < _visualSampleCountCheckImages.Length; i++)
                {
                    Destroy(_visualSampleCountCheckImages[i].gameObject);
                }
            }



            int visualImageCount = VideoManager.Instance._audioSamples.Length;

            float w = _canvas.sizeDelta.x;
            float image_w = w / visualImageCount;

            _visualImages = new Image[visualImageCount];
            RectTransform rectTransform;
            Image image;
            for (int i = 0; i < visualImageCount; i++)
            {
                GameObject go = new GameObject(string.Format("Sample_{0}", i.ToString().PadLeft(4, '0')),
                    new Type[]{
                    typeof(RectTransform),
                    typeof(Image)
                    });
                go.transform.SetParent(_canvas);

                rectTransform = go.GetComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.zero;
                rectTransform.pivot = Vector2.zero;
                rectTransform.anchoredPosition = Vector2.right * i / visualImageCount * w;
                rectTransform.sizeDelta = new Vector2(image_w, VisualImageHeightMin);

                image = go.GetComponent<Image>();
                image.color = Color.yellow;

                _visualImages[i] = image;
            }



            int visualImageCheckCount = visualImageCount / SampleCountCheckInterval + 1;
            _visualSampleCountCheckImages = new Image[visualImageCheckCount];

            RectTransform currentRect;

            for(int i = 0; i < visualImageCheckCount; i++)
            {
                GameObject go = new GameObject(string.Format("SampleChecker_{0}", i.ToString().PadLeft(4, '0')),
                    new Type[]{
                    typeof(RectTransform),
                    typeof(Image)
                    });
                go.transform.SetParent(_canvas);

                currentRect = _visualImages[SampleCountCheckInterval * i].rectTransform;

                rectTransform = go.GetComponent<RectTransform>();
                rectTransform.anchorMin = currentRect.anchorMin;
                rectTransform.anchorMax = currentRect.anchorMax;
                rectTransform.pivot = currentRect.pivot;
                rectTransform.anchoredPosition = currentRect.anchoredPosition;
                rectTransform.sizeDelta = new Vector2(currentRect.sizeDelta.x * SampleCountCheckInterval * 0.5f, currentRect.sizeDelta.y * 0.5f);

                image = go.GetComponent<Image>();
                image.color = Color.black;

                _visualSampleCountCheckImages[i] = image;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (_calculateDot)
        {
            _dotProperticesIndex = 0;

            foreach (Dot d in _dotProperties)
            {
                if (d.CurrentBone != null && d.DotTestPoint != null)
                {
                    d.Cal();

                    DrawDot(d.CurrentBone.BoneStartPos, d.CurrentBone.BoneEndPos, d.DotTestPoint.position, d.GizmoColor);
                    DrawDotInfo(d.CurrentBone.name, d.StartDot, d.EndDot, d.Lerp, d.GizmoColor,
                        d.DotTestPoint.position + Vector3.right * 0.01f + Vector3.down * 0.01f * (_dotProperticesIndex + 1));

                    _dotProperticesIndex++;
                }
            }
        }
    }

    private void DrawDotInfo(string boneName, float startDot, float endDot, float lerp, Color color, Vector3 textPos)
    {
        Gizmos.color = color;

        GUIStyle textStyle = new GUIStyle();
        textStyle.fontSize = 10;
        textStyle.fontStyle = FontStyle.Bold;
        textStyle.alignment = TextAnchor.MiddleLeft;
        textStyle.normal.textColor = color;

        Handles.Label(textPos, 
            string.Format("{0} -> Start Dot : {1:F2}, End Dot : {2:F2}, Sum : {3:F2}, Lerp : {4:F2}", boneName, startDot, endDot, startDot + endDot, lerp), textStyle);
    }

    private void DrawDot(Vector3 dotStartPos, Vector3 dotEndPos, Vector3 dotTestPos, Color color)
    {
        Gizmos.color = color;

        Gizmos.DrawLine(dotStartPos, dotEndPos);
        Gizmos.DrawLine(dotStartPos, dotTestPos);
        Gizmos.DrawLine(dotEndPos, dotTestPos);
    }
}

[Serializable]
public class AudioVisualizer
{
    public RectTransform VisualizerParent;
    [MinMaxSlider(0, 8192)] public Vector2Int AudioSamplesRange;

    [Range(1f, 1000f)] public float VisualizerDownSpeed = 85f;
    [Range(0.1f, 100f)] public float VisualizerHeightOffset = 2f;
    [Range(0.1f, 10f)] public float AudioSampleOffset = 1f;
}

[Serializable]
public class Dot
{
    public BoneInfo CurrentBone;
    public Transform DotTestPoint;
    public Color GizmoColor;

    public float StartDot, EndDot;
    /// <summary>
    /// StartPos -> EndPos, 0f -> 1f
    /// </summary>
    public float Lerp;



    public void Cal()
    {
        CurrentBone.GetDot(DotTestPoint.position, out StartDot, out EndDot);

        Lerp = CurrentBone.GetLerp(DotTestPoint.position);
    }
}
#endif