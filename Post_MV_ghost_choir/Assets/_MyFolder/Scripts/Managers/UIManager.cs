using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private const float CanvasWidth = 1920;
    private const float CanvasHeight = 1080;
    private const float CanvasRatio = CanvasHeight / CanvasWidth;

    [SerializeField] private CanvasGroup cg_video;
    [SerializeField] private RawImage rawimg_video;
    private IEnumerator _videoSetCoroutine;
    private IEnumerator _textLoadingAnimation;
    private string _loadingAnimationText = string.Empty;
    private IEnumerator _videoIntroAnimation;

    [SerializeField] private CanvasGroup cg_loading;
    [SerializeField] private Text txt_message;

    private IEnumerator _climaxStartActionCoroutine = null;
    private IEnumerator _endedActionCoroutine = null;




    private void Awake()
    {
        VideoManager.Instance.NetworkDisconnectAction += NetworkNotConnected;
        MVManager.Instance.ClimaxStarted += ClimaxStartAction;
        VideoManager.Instance.VideoEndedAction += EndingAction;
    }

    private void OnDestroy()
    {
        VideoManager.Instance.NetworkDisconnectAction -= NetworkNotConnected;
        MVManager.Instance.ClimaxStarted -= ClimaxStartAction;
        VideoManager.Instance.VideoEndedAction -= EndingAction;

        if (_climaxStartActionCoroutine != null)
        {
            StopCoroutine(_climaxStartActionCoroutine);
            _climaxStartActionCoroutine = null;
        }
        if (_videoIntroAnimation != null)
        {
            StopCoroutine(_videoIntroAnimation);
            _videoIntroAnimation = null;
        }
        if(_textLoadingAnimation != null)
        {
            StopCoroutine(_textLoadingAnimation);
            _textLoadingAnimation = null;
        }
        if (_videoSetCoroutine != null)
        {
            StopCoroutine(_videoSetCoroutine);
            _videoSetCoroutine = null;
        }
    }

    private void EndingAction()
    {
        if(_endedActionCoroutine == null)
        {
            _endedActionCoroutine = EndingActionCoroutine();
            StartCoroutine(_endedActionCoroutine);
        }
    }

    private IEnumerator EndingActionCoroutine()
    {
        //yield return new WaitForSeconds(2f);
        yield return new WaitForSeconds(1f);

        float time = 5f;
        float t = 0;
        while (t < time)
        {
            t += Time.deltaTime;

            cg_loading.alpha = t / time;

            yield return null;
        }
        cg_loading.alpha = 1f;

        _endedActionCoroutine = null;
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
        float time = 0.5f;
        float t = 0;
        while(t < time)
        {
            t += Time.deltaTime;

            cg_loading.alpha = t / time;

            yield return null;
        }
        cg_loading.alpha = 1;

        yield return new WaitForSeconds(1f);

        const float alphaBefore = 0.02f;
        const float alphaAfter = 1f - alphaBefore;

        time = 1.5f;
        t = 0;
        while(t < time)
        {
            t += Time.deltaTime;

            cg_loading.alpha = 1 - t / time * alphaBefore;

            yield return null;
        }

        time = 1.0f;
        t = 0;
        while(t < time)
        {
            t += Time.deltaTime;

            cg_loading.alpha = 1 - (alphaBefore + t / time * alphaAfter);

            yield return null;
        }
        cg_loading.alpha = 0;

        _climaxStartActionCoroutine = null;
    }

    private void NetworkNotConnected()
    {
        if(_textLoadingAnimation != null)
        {
            StopCoroutine(_textLoadingAnimation);
            _textLoadingAnimation = null;
        }

        txt_message.text = "Please check network";
    }

    private void Start()
    {
        cg_video.alpha = 0;

        cg_loading.alpha = 1;
        txt_message.text = string.Empty;
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Return))
        {
            #region Initialize
            if(_textLoadingAnimation != null)
            {
                StopCoroutine(_textLoadingAnimation);
                _textLoadingAnimation = null;
            }
            if (_videoIntroAnimation != null)
            {
                StopCoroutine(_videoIntroAnimation);
                _videoIntroAnimation = null;
            }
            if(_videoSetCoroutine != null)
            {
                StopCoroutine(_videoSetCoroutine);
                _videoSetCoroutine = null;
            }

            VideoManager.Instance.StopPrepareCoroutine();
            VideoManager.Instance.StopPlayCoroutine();

            Time.timeScale = 1f;
            cg_loading.alpha = 1f;
            #endregion



            VideoManager.Instance.Prepare();

            if (_videoSetCoroutine == null)
            {
                _videoSetCoroutine = VideoSetCoroutine();
                StartCoroutine(_videoSetCoroutine);
            }
        }
        else
#endif
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if(VideoManager.Instance.State != VideoManager.VideoState.Prepared && 
                VideoManager.Instance.State != VideoManager.VideoState.Playing &&
                VideoManager.Instance.State != VideoManager.VideoState.Paused)
            {
                VideoManager.Instance.Prepare();

                if (_videoSetCoroutine == null)
                {
                    _videoSetCoroutine = VideoSetCoroutine();
                    StartCoroutine(_videoSetCoroutine);
                }
            }
            else if(VideoManager.Instance.State == VideoManager.VideoState.Prepared)
            {
                VideoManager.Instance.Play(3f);

                if (_videoIntroAnimation == null)
                {
                    _videoIntroAnimation = VideoIntroAnimation();
                    StartCoroutine(_videoIntroAnimation);
                }
            }
        }
    }

    private IEnumerator VideoIntroAnimation()
    {
        cg_loading.alpha = 1f;
        cg_video.alpha = 0;

        float fadeTime = 10f;
        float t = fadeTime;
        while(0 < t)
        {
            t -= Time.deltaTime;

            cg_loading.alpha = Mathf.Sin(Mathf.Lerp(0, Mathf.PI * 0.5f, t / fadeTime));

            yield return null;
        }
        cg_loading.alpha = 0f;

        _videoIntroAnimation = null;
    }

    private IEnumerator VideoSetCoroutine()
    {
        if (_textLoadingAnimation == null)
        {
            _textLoadingAnimation = TextLoadingAnimation();
            StartCoroutine(_textLoadingAnimation);
        }
        _loadingAnimationText = "Start set music vidio";
        yield return null;


        _loadingAnimationText = "Network checking";
        yield return null;

        while (VideoManager.Instance.State == VideoManager.VideoState.NetworkConnecting)
        {
            yield return null;
        }

        if (VideoManager.Instance.State == VideoManager.VideoState.NetworkDisconnected)
        {
            _loadingAnimationText = "Please check network";

            if (_textLoadingAnimation != null)
            {
                StopCoroutine(_textLoadingAnimation);
                _textLoadingAnimation = null;
            }

            _videoSetCoroutine = null;

            yield break;
        }


        _loadingAnimationText = "Video loading";
        yield return null;

        while(VideoManager.Instance.State == VideoManager.VideoState.Preparing)
        {
            yield return null;
        }

        while(VideoManager.Instance.VideoRenderTexture == null)
        {
            yield return null;
        }

        rawimg_video.texture = VideoManager.Instance.VideoRenderTexture;
        rawimg_video.rectTransform.sizeDelta = Resize(rawimg_video.texture.width, rawimg_video.texture.height);

        if(_textLoadingAnimation != null)
        {
            StopCoroutine(_textLoadingAnimation);
            _textLoadingAnimation = null;
        }
        txt_message.text = string.Empty;

        _videoSetCoroutine = null;
    }

    private IEnumerator TextLoadingAnimation()
    {
        string textTemp = _loadingAnimationText;
        StringBuilder message = new StringBuilder();

        const int loadingMaxCount = 3;
        int currentCount;
        int countSaver = -1;

        float animationTime = 0.5f;
        float t = 0;
        while(true)
        {
            if (textTemp != _loadingAnimationText)
            {
                textTemp = _loadingAnimationText;

                t = 0;
                countSaver = -1;
            }

            t += Time.deltaTime;
            currentCount = (int)(t / animationTime);
            if (currentCount != countSaver)
            {
                message.Clear();

                switch (currentCount % loadingMaxCount)
                {
                    case 0:
                        message.Append(textTemp).Append(".");
                        break;

                    case 1:
                        message.Append(textTemp).Append(".").Append(".");
                        break;

                    case 2:
                        message.Append(textTemp).Append(".").Append(".").Append(".");
                        break;
                }

                txt_message.text = message.ToString();

                countSaver = currentCount;
            }

            yield return null;
        }

        //_textLoadingAnimation = null;
    }

    private Vector2 Resize(float width, float height)
    {
        float r = height / width;
        float w, h;

        if(CanvasRatio < r)
        {
            h = CanvasHeight;
            w = h * width / height;
        }
        else
        {
            w = CanvasWidth;
            h = w * height / width;
        }

        return new Vector2(w, h);
    }
}
