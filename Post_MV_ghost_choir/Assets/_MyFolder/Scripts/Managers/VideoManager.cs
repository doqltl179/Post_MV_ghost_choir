using Sound;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;

public class VideoManager : MonoBehaviour
{
    private static VideoManager _instance;
    public static VideoManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var temp = FindObjectsOfType<VideoManager>();
                if (temp != null && temp.Length > 0)
                {
                    foreach (var t in temp)
                        Destroy(t.gameObject);
                }

                GameObject go = new GameObject(nameof(VideoManager));

                VideoManager vm = go.AddComponent<VideoManager>();
                vm.Init();

                _instance = vm;
            }

            return _instance;
        }
    }

    private const string VideoURL = "https://youtu.be/kXF3VYYa5TI";

    public Action NetworkDisconnectAction;
    /// <summary>
    /// This work when video start play.
    /// </summary>
    public Action VideoPlayAction;
    public Action VideoPreparedAction;
    public Action VideoEndedAction;

    public enum VideoState
    {
        Playing,
        Preparing,
        Prepared,
        Paused,
        Ended,
        NetworkConnecting,
        NetworkConnected,
        NetworkDisconnected
    }
    public VideoState State { get; private set; } = VideoState.NetworkDisconnected;
    private VideoState _stateSaver = VideoState.NetworkDisconnected;

    private VideoPlayer _videoPlayer;
    private YoutubePlayer.YoutubePlayer _youtubePlayer;
    private const string MV_URL = "https://youtu.be/kXF3VYYa5TI";
    public float NormalizeTime
    {
        get
        {
            if (_videoPlayer != null && (_videoPlayer.isPlaying || _videoPlayer.isPaused))
            {
                return (float)(_videoPlayer.time / _videoPlayer.length);
            }
            else
            {
                return -1;
            }
        }
    }
    public RenderTexture VideoRenderTexture { get; private set; }

    private AudioSource _audioSource;
    public const int AudioSampleCount = 13;
    public float[] _audioSamples { get; private set; } = new float[(int)Mathf.Pow(2, AudioSampleCount)];
    //public float[] _audioSamplesForDebug = new float[(int)Mathf.Pow(2, AudioSampleCount)];
    public float[] _audioNormalizedSamples = new float[AudioSampleCount];
    public float[] _audioNormalizedSamplesMax = new float[AudioSampleCount];
    public float[] _octaveSampleMax;
    public int[] _octaveSampleCount;

    public int CurrentOctave;
    public float CurrentOctaveSample;

    public float Frequency { get; private set; }
    public float NormalizedFrequency { get; private set; }
    private float _maxFrequency;

    private IEnumerator _videoPlayCoroutine;
    private IEnumerator _videoPrepareCoroutine;

    public void Init()
    {
        if (_videoPlayer == null)
        {
            VideoPlayer v = gameObject.AddComponent<VideoPlayer>();

            v.source = VideoSource.Url;
            //v.url = MV_URL;

            v.playOnAwake = false;

            v.audioOutputMode = VideoAudioOutputMode.AudioSource;

            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            v.SetTargetAudioSource(0, audioSource);

            _audioSource = audioSource;

            v.loopPointReached += delegate { 
                State = VideoState.Ended;
                Debug.Log("Video State to 'Ended'");
            };

            _videoPlayer = v;
        }

        if(_youtubePlayer == null)
        {
            var youtubePlayer = gameObject.AddComponent<YoutubePlayer.YoutubePlayer>();
            youtubePlayer.youtubeUrl = MV_URL;
            youtubePlayer.is360Video = false;
            youtubePlayer.cli = YoutubePlayer.YoutubePlayer.Cli.YtDlp;

            _youtubePlayer = youtubePlayer;
        }

        SoundFrequency.SaveFrequency();

        _octaveSampleMax = new float[SoundFrequency.OctaveLength];
        _octaveSampleCount = new int[SoundFrequency.OctaveLength];
    }

    private void OnDestroy()
    {
        if(_videoPlayCoroutine != null)
        {
            StopCoroutine(_videoPlayCoroutine);
            _videoPlayCoroutine = null;
        }
        if(_videoPrepareCoroutine != null)
        {
            StopCoroutine(_videoPrepareCoroutine);
            _videoPrepareCoroutine = null;
        }

        if(VideoRenderTexture != null)
        {
            Destroy(VideoRenderTexture);
        }
    }

    float sum;
    int sumCount;
    float sampleNormal;
    private void Update()
    {
        if (_videoPlayer.isPlaying)
        {
            _audioSource.GetSpectrumData(_audioSamples, 0, FFTWindow.Blackman);

            Frequency = SoundFrequency.GetFrequency(_audioSamples, _maxFrequency, out CurrentOctaveSample);
            CurrentOctave = SoundFrequency.GetOctave(Frequency);
            NormalizedFrequency = SoundFrequency.GetNormalizedFrequency(CurrentOctave, Frequency);
            if (CurrentOctave >= 0)
            {
                if(_octaveSampleMax[CurrentOctave] < CurrentOctaveSample)
                    _octaveSampleMax[CurrentOctave] = CurrentOctaveSample;

                _octaveSampleCount[CurrentOctave]++;
            }

            for (int i = 0; i < AudioSampleCount; i++)
            {
                sum = 0;
                sumCount = 0;
                for(int j = 0; j < Mathf.Pow(2, i) * 2; j++)
                {
                    sum += _audioSamples[j] * (sumCount + 1);
                    sumCount++;
                }

                sampleNormal = sum / sumCount;

                if (_audioNormalizedSamplesMax[i] < sampleNormal)
                    _audioNormalizedSamplesMax[i] = sampleNormal;

                _audioNormalizedSamples[i] = sampleNormal;
            }
        }

#if UNITY_EDITOR
        if(Input.GetKeyDown(KeyCode.Space))
        {
            if (State == VideoState.Playing)
            {
                _videoPlayer.Pause();
                Time.timeScale = 0f;

                State = VideoState.Paused;
            }
            else if (State == VideoState.Paused)
            {
                _videoPlayer.Play();
                Time.timeScale = 1f;

                State = VideoState.Playing;
            }
        }
#endif
    }

    private void LateUpdate()
    {
        CheckState();
    }

    private void CheckState()
    {
        if(State != _stateSaver)
        {
            StateChangedAction(State);

            _stateSaver = State;
        }
    }

    private void StateChangedAction(VideoState state)
    {
        switch(state)
        {
            case VideoState.NetworkConnected:
                {

                }
                break;

            case VideoState.NetworkConnecting:
                {

                }
                break;

            case VideoState.NetworkDisconnected:
                {
                    NetworkDisconnectAction?.Invoke();
                }
                break;

            case VideoState.Playing:
                {
                    VideoPlayAction?.Invoke();
                }
                break;

            case VideoState.Prepared:
                {
                    VideoPreparedAction?.Invoke();
                }
                break;

            case VideoState.Preparing:
                {

                }
                break;

            case VideoState.Ended:
                {
                    VideoEndedAction?.Invoke();
                }
                break;
        }
    }

    private IEnumerator VideoPlayCoroutine(float delay = 0f)
    {
        if (State != VideoState.Prepared)
        {
            if (_videoPrepareCoroutine == null)
            {
                _videoPrepareCoroutine = VideoPrepareCoroutine();
                StartCoroutine(_videoPrepareCoroutine);
            }

            while(_videoPrepareCoroutine != null)
            {
                yield return null;
            }

            if(State == VideoState.NetworkDisconnected)
            {
                //NetworkDisconnectAction?.Invoke();

                _videoPlayCoroutine = null;

                yield break;
            }
        }

        State = VideoState.Playing;
        Debug.Log("Video State to 'Playing'");

        //Skip one frame for set video RenderTexture on UI
        yield return null;

        yield return new WaitForSeconds(delay);

        _videoPlayer.Play();

        _videoPlayCoroutine = null;
    }

    private IEnumerator VideoPrepareCoroutine()
    {
        yield return StartCoroutine(CheckInternet());
        if(State == VideoState.NetworkDisconnected)
        {
            Debug.Log("Please check internet");

            _videoPrepareCoroutine = null;

            yield break;
        }



        State = VideoState.Preparing;
        Debug.Log("Video State to 'Preparing'");
        _youtubePlayer.PrepareVideoAsync(VideoURL);

        yield return null;
        while (!_videoPlayer.isPrepared)
        {
            yield return null;
        }
        State = VideoState.Prepared;
        Debug.Log("Video State to 'Prepared'");

        _maxFrequency = _videoPlayer.GetAudioSampleRate(0) * 0.5f;

        if (VideoRenderTexture == null)
        {
            RenderTexture rt = new RenderTexture((int)_videoPlayer.width, (int)_videoPlayer.height, 24);
            _videoPlayer.targetTexture = rt;

            VideoRenderTexture = rt;
        }
        else
        {
            if (VideoRenderTexture.width != (int)_videoPlayer.width || VideoRenderTexture.height != (int)_videoPlayer.height)
            {
                Destroy(VideoRenderTexture);

                RenderTexture rt = new RenderTexture((int)_videoPlayer.width, (int)_videoPlayer.height, 24);
                _videoPlayer.targetTexture = rt;

                VideoRenderTexture = rt;
            }
        }

        Debug.Log(string.Format("Audio Track Count : {0}, Audio Channel Count : {1}", _videoPlayer.audioTrackCount, _videoPlayer.GetAudioChannelCount(_videoPlayer.audioTrackCount)));

        _videoPrepareCoroutine = null;
    }

    private IEnumerator CheckInternet()
    {
        UnityWebRequest request = new UnityWebRequest("http://google.com");

        State = VideoState.NetworkConnecting;
        Debug.Log("Video State to 'NetworkConnecting'");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            State = VideoState.NetworkConnected;
            Debug.Log("Video State to 'NetworkConnected'");
        }
        else
        {
            State = VideoState.NetworkDisconnected;
            Debug.Log("Video State to 'NetworkDisconnected'");
        }
    }

    #region Utility
    public float GetNormalizedFrequency(float frequency)
    {
        return SoundFrequency.GetNormalizedFrequency(frequency);
    }

    public float GetNormalizedFrequency(int octave, float frequency)
    {
        return SoundFrequency.GetNormalizedFrequency(octave, frequency);
    }

    public float GetFrequency(Vector2Int samplesIndexRange, out float usedSample)
    {
        if (samplesIndexRange.x < 0 || samplesIndexRange.y > _audioSamples.Length || samplesIndexRange.y - samplesIndexRange.x < 1)
        {
            usedSample = 0;

            return 0;
        }

        return SoundFrequency.GetFrequency(_audioSamples, samplesIndexRange, _maxFrequency, out usedSample);
    }

    public int GetOctave(float frequency)
    {
        return SoundFrequency.GetOctave(frequency);
    }

    public int[] GetInverseOvtaves(int[] octaves)
    {
        List<int> inverseOctaveList = new List<int>();
        for (int i = 0; i < SoundFrequency.OctaveLength; i++)
        {
            if (!UnityUtilityMethod.IsInclude<int>(octaves, i))
                inverseOctaveList.Add(i);
        }

        return inverseOctaveList.ToArray();
    }

    public float GetOctaveMaxSample(int octave)
    {
        return SoundFrequency.GetOctaveMaxSample(octave);
    }

    public void Prepare()
    {
        if (/*State != VideoState.Playing &&*/ _videoPrepareCoroutine == null)
        {
            _videoPrepareCoroutine = VideoPrepareCoroutine();
            StartCoroutine(_videoPrepareCoroutine);
        }
    }

    public void Play(float delay = 0f)
    {
        if (_videoPlayCoroutine == null && (State != VideoState.Playing || State != VideoState.Paused))
        {
            _videoPlayCoroutine = VideoPlayCoroutine(delay);
            StartCoroutine(_videoPlayCoroutine);
        }
    }

    public void StopPrepareCoroutine()
    {
        if(_videoPrepareCoroutine != null)
        {
            StopCoroutine(_videoPrepareCoroutine);
            _videoPrepareCoroutine = null;
        }
    }

    public void StopPlayCoroutine()
    {
        if(_videoPlayCoroutine != null)
        {
            StopCoroutine(_videoPlayCoroutine);
            _videoPlayCoroutine = null;
        }
    }

    public void JumpToBackAFewSeconds(float seconds, out float normalizeTime)
    {
        normalizeTime = ((float)_videoPlayer.time + seconds) / (float)_videoPlayer.length;
        normalizeTime = Mathf.Clamp01(normalizeTime);

        _videoPlayer.time += seconds;
    }

    public void JumpToFrontAFewSeconds(float seconds, out float normalizeTime)
    {
        normalizeTime = ((float)_videoPlayer.time - seconds) / (float)_videoPlayer.length;
        normalizeTime = Mathf.Clamp01(normalizeTime);

        _videoPlayer.time -= seconds;
    }
    #endregion
}
