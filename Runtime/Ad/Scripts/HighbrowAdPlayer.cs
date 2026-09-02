using System;
using System.Collections;
using System.Reflection;
using Highbrow.Core.Utils;
using Highbrow.Log;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Highbrow
{
    [Serializable]
    public class InfoPanel
    {
        public GameObject Root;
        public Button StoreBtn;
        public GameObject PlayStoreIcon;
        public GameObject AppStoreIcon;
        public Image AppIcon;
        public TMPro.TextMeshProUGUI TextTitle;
        public TMPro.TextMeshProUGUI TextDesc;
        public Highbrow.UI.HbrwUISlider Slider;
    }

    /// <summary>
    /// Highbrow in-house cross-promotion video advertisement player.
    /// Supports automatic screen ratio detection, dynamic store routing, countdown timer, and Resources prefab loading.
    /// </summary>
    public class HighbrowAdPlayer : MonoBehaviour
    {
        [Header("Ad Player")]
        public VideoPlayer AdsVideoPlayer = null;
        public RawImage VideoScreen;

        [Header("Ad Data")]
        [Tooltip("광고 시작 후 닫기 버튼 노출 시까지의 대기 시간(초)")]
        [FormerlySerializedAs("WaitAdSec")]
        public float AdSkipSec = 7.0f;

        [Header("Highbrow Promotion DataSet")]
        public HighbrowGamesInfo HbrwGamesInfo;

        [Header("Panels")]
        public InfoPanel SideInfoPanel;
        public InfoPanel BottomInfoPanel;

        [Header("UI - Player")]
        public Button SkipBtn;
        public Button PauseBtn;
        public Button SoundBtn;
        public TMPro.TextMeshProUGUI SkipText;

        // -------------------------------------------------
        private Action onAdEndCallback = null;
        private GameMarketInfo _currentGameInfo = null;
        private InfoPanel _currentInfoPanel = null;
        private static int _totalPlayCount = 0;
        private bool isCompleted = false;

        private const float STD_RESOLUTION_RATIO = 19f / 9f;
        private const float ADD_SEC_PER_PLAY = 10.0f;
        private const float MAX_SEC_SKIP = 30.0f;

        private const string PREFAB_RESOURCE_PATH = "Highbrow/UI_HighbrowAd";
        private const string GAMES_INFO_RESOURCE_PATH = "Highbrow/HighbrowGamesInfo";

        void Awake()
        {
            if (AdsVideoPlayer == null)
                AdsVideoPlayer = GetComponentInChildren<VideoPlayer>();
            if (VideoScreen == null)
                VideoScreen = GetComponentInChildren<RawImage>();

            if (HbrwGamesInfo == null)
            {
                HbrwGamesInfo = Resources.Load<HighbrowGamesInfo>(GAMES_INFO_RESOURCE_PATH);
            }

            if (!ValidateAssignedFields())
            {
                HighbrowLogger.LogError("[HighbrowAdPlayer] Critical fields missing. Destroying player instance.");
                Destroy(gameObject);
                return;
            }
        }

        void Start()
        {
            BeforePrepareVideo();
        }

        void OnDestroy()
        {
            try
            {
                onAdEndCallback?.Invoke();
            }
            catch (Exception ex)
            {
                HighbrowLogger.LogError($"[HighbrowAdPlayer] Error invoking onAdEndCallback: {ex.Message}");
            }
        }

        #region Public APIs

        /// <summary>
        /// Starts playing the cross-promotion ad.
        /// Automatically tracks Ad impression start log via HighbrowLog.
        /// </summary>
        public void PlayAd()
        {
            AdSkipSec = Math.Min(AdSkipSec + _totalPlayCount * ADD_SEC_PER_PLAY, MAX_SEC_SKIP);
            if (SkipText != null)
                SkipText.text = $"{AdSkipSec:0}s";
            _totalPlayCount++;

            // Track Ad start log
            HighbrowLog.TrackAd(AdType.CrossPromotion);

            StartCoroutine(PrepareVideo());
            StartCoroutine(WaitForAd());
        }

        /// <summary>
        /// Registers callback invoked when ad presentation finishes (either complete or skipped).
        /// </summary>
        public void SetOnAdEnd(Action callback)
        {
            onAdEndCallback = callback;
        }

        /// <summary>
        /// Configures initial skip wait duration in seconds.
        /// </summary>
        public void SetSkipSec(float sec)
        {
            AdSkipSec = sec;
        }

        /// <summary>
        /// Resets cumulative consecutive play count.
        /// </summary>
        public void ClearPlayCount()
        {
            _totalPlayCount = 0;
        }

        #endregion

        #region Static Show Helper (Resources based)

        /// <summary>
        /// Dynamically instantiates and displays the Highbrow cross-promotion ad popup.
        /// No Addressables required (loads directly from Resources).
        /// </summary>
        /// <param name="onCompleted">Callback when ad ends or is closed.</param>
        /// <param name="onFailed">Callback when prefab cannot be loaded or shown.</param>
        /// <param name="parentCanvas">Optional target canvas. If null, finds or creates one.</param>
        /// <returns>Spawned HighbrowAdPlayer component, or null on failure.</returns>
        public static HighbrowAdPlayer Show(Action onCompleted = null, Action onFailed = null, Canvas parentCanvas = null)
        {
            GameObject prefab = Resources.Load<GameObject>(PREFAB_RESOURCE_PATH);
            if (prefab == null)
            {
                HighbrowLogger.LogError($"[HighbrowAdPlayer] Failed to load prefab from Resources at: {PREFAB_RESOURCE_PATH}");
                onFailed?.Invoke();
                return null;
            }

            Canvas canvas = parentCanvas;
            if (canvas == null)
            {
#if UNITY_2023_1_OR_NEWER
                canvas = FindFirstObjectByType<Canvas>();
#else
                canvas = FindObjectOfType<Canvas>();
#endif
            }

            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("HighbrowAdCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            GameObject instance = Instantiate(prefab, canvas.transform);
            HighbrowAdPlayer player = instance.GetComponent<HighbrowAdPlayer>();
            if (player == null)
            {
                HighbrowLogger.LogError("[HighbrowAdPlayer] Instantiated prefab is missing HighbrowAdPlayer component.");
                Destroy(instance);
                onFailed?.Invoke();
                return null;
            }

            player.SetOnAdEnd(onCompleted);
            player.PlayAd();
            return player;
        }

        #endregion

        #region Internal Video Preparation

        private void BeforePrepareVideo()
        {
            if (HbrwGamesInfo != null)
            {
                HbrwGamesInfo.Initialize();
            }

            bool isOverStdRatio = CheckResolutionIfOverStdRatio();
            ApplyProperScreenPos(isOverStdRatio);
            SetSkipButton();

            if (AdsVideoPlayer != null)
            {
                AdsVideoPlayer.loopPointReached += (player) =>
                {
                    StartCoroutine(PrepareVideo());
                };
            }
        }

        private bool CheckResolutionIfOverStdRatio()
        {
            int screenWidth = Screen.width;
            int screenHeight = Screen.height;

            if (screenWidth < screenHeight)
                (screenHeight, screenWidth) = (screenWidth, screenHeight);

            float aspectRatio = (float)screenWidth / screenHeight;
            return aspectRatio >= STD_RESOLUTION_RATIO;
        }

        private void ApplyProperScreenPos(bool isOverStdRatio)
        {
            if (VideoScreen != null)
            {
                var screenRectTransform = VideoScreen.rectTransform;
                if (isOverStdRatio)
                {
                    screenRectTransform.anchorMin = new Vector2(0, 0.5f);
                    screenRectTransform.anchorMax = new Vector2(0, 0.5f);
                    screenRectTransform.pivot = new Vector2(0, 0.5f);
                    screenRectTransform.anchoredPosition = new Vector2(0, 0);
                }
                else
                {
                    screenRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    screenRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    screenRectTransform.pivot = new Vector2(0.5f, 0.5f);
                    screenRectTransform.anchoredPosition = new Vector2(0, 0);
                }
            }

            if (SideInfoPanel?.Root != null)
                SideInfoPanel.Root.SetActive(isOverStdRatio);
            if (BottomInfoPanel?.Root != null)
                BottomInfoPanel.Root.SetActive(!isOverStdRatio);

            _currentInfoPanel = isOverStdRatio ? SideInfoPanel : BottomInfoPanel;
        }

        private void SetSkipButton()
        {
            if (SkipBtn != null)
            {
                SkipBtn.onClick.RemoveAllListeners();
                SkipBtn.onClick.AddListener(OnCloseClicked);
                SkipBtn.enabled = false;
            }
        }

        private void OnCloseClicked()
        {
            if (!isCompleted)
            {
                isCompleted = true;
                HighbrowLog.TrackAd(AdType.CrossPromotion);
            }
            Destroy(gameObject);
        }

        private IEnumerator PrepareVideo()
        {
            yield return new WaitForEndOfFrame();

            if (HbrwGamesInfo != null)
            {
                _currentGameInfo = HbrwGamesInfo.GetRandomByWeight();
            }

            if (_currentGameInfo == null)
            {
                HighbrowLogger.LogWarning("[HighbrowAdPlayer] No active GameMarketInfo available. Closing ad.");
                Destroy(gameObject);
                yield break;
            }

            ApplyGameInfo(_currentGameInfo);

            if (AdsVideoPlayer != null)
            {
                if (AdsVideoPlayer.clip == null && string.IsNullOrEmpty(AdsVideoPlayer.url))
                {
                    HighbrowLogger.LogWarning("[HighbrowAdPlayer] No video clip or URL assigned for ad. Closing.");
                    Destroy(gameObject);
                    yield break;
                }

                AdsVideoPlayer.Prepare();
                while (!AdsVideoPlayer.isPrepared)
                {
                    yield return new WaitForSeconds(0.2f);
                }

                PlayVideo();
            }

            if (_currentInfoPanel?.Slider != null)
            {
                _currentInfoPanel.Slider.ResetPosition(true);
                _currentInfoPanel.Slider.Move();
            }
        }

        private void ApplyGameInfo(GameMarketInfo gameInfo)
        {
            if (gameInfo == null || _currentInfoPanel == null) return;

            var lang = Application.systemLanguage;

            if (_currentInfoPanel.TextTitle != null && gameInfo.Name != null)
                _currentInfoPanel.TextTitle.text = gameInfo.Name.GetText(lang);

            if (_currentInfoPanel.TextDesc != null && gameInfo.Desc != null)
                _currentInfoPanel.TextDesc.text = gameInfo.Desc.GetText(lang);

            if (_currentInfoPanel.AppIcon != null && gameInfo.AppIcon != null)
                _currentInfoPanel.AppIcon.sprite = gameInfo.AppIcon;

            bool isAndroid = Application.platform == RuntimePlatform.Android;
            if (_currentInfoPanel.PlayStoreIcon != null)
                _currentInfoPanel.PlayStoreIcon.SetActive(isAndroid);
            if (_currentInfoPanel.AppStoreIcon != null)
                _currentInfoPanel.AppStoreIcon.SetActive(!isAndroid);

            if (_currentInfoPanel.StoreBtn != null)
            {
                _currentInfoPanel.StoreBtn.onClick.RemoveAllListeners();
                _currentInfoPanel.StoreBtn.onClick.AddListener(() =>
                {
                    string url = gameInfo.GetStoreLink();
                    if (!string.IsNullOrEmpty(url))
                    {
                        Application.OpenURL(url);
                    }
                });
            }

            if (AdsVideoPlayer != null)
            {
                AdsVideoPlayer.clip = null;
                AdsVideoPlayer.url = null;

                if (!string.IsNullOrEmpty(gameInfo.VideoUrl))
                {
                    AdsVideoPlayer.url = gameInfo.VideoUrl;
                }
                else if (gameInfo.Video != null)
                {
                    AdsVideoPlayer.clip = gameInfo.Video;
                }
            }
        }

        #endregion

        #region Video Controls

        public void PlayVideo()
        {
            if (AdsVideoPlayer != null && AdsVideoPlayer.isPrepared)
                AdsVideoPlayer.Play();
        }

        public void StopVideo()
        {
            if (AdsVideoPlayer != null && AdsVideoPlayer.isPrepared)
                AdsVideoPlayer.Stop();
        }

        public void PauseVideo()
        {
            if (AdsVideoPlayer != null && AdsVideoPlayer.isPrepared)
                AdsVideoPlayer.Pause();
        }

        public void SetLoop(bool loop)
        {
            if (AdsVideoPlayer != null)
                AdsVideoPlayer.isLooping = loop;
        }

        public void SetMute(bool mute)
        {
            if (AdsVideoPlayer != null && AdsVideoPlayer.canSetDirectAudioVolume)
                AdsVideoPlayer.SetDirectAudioMute(0, mute);
        }

        #endregion

        #region Timer Routine

        private IEnumerator WaitForAd()
        {
            if (SkipBtn != null)
                SkipBtn.enabled = false;

            int remainingTime = Mathf.CeilToInt(AdSkipSec);

            while (remainingTime > 0)
            {
                if (SkipText != null)
                    SkipText.text = $"{remainingTime}s";

                yield return new WaitForSeconds(1f);
                remainingTime--;
            }

            if (SkipText != null)
            {
                SkipText.text = "✕";
                SkipText.fontStyle = TMPro.FontStyles.Bold;
            }

            if (SkipBtn != null)
                SkipBtn.enabled = true;
        }

        private bool ValidateAssignedFields()
        {
            FieldInfo[] fields = GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (FieldInfo field in fields)
            {
                if (field.IsDefined(typeof(SerializeField), true))
                {
                    object val = field.GetValue(this);
                    if (val == null)
                    {
                        HighbrowLogger.LogWarning($"[HighbrowAdPlayer] Field '{field.Name}' is not assigned in inspector.");
                    }
                }
            }
            return true;
        }

        #endregion
    }
}

namespace Highbrow.Ad
{
    /// <summary>
    /// Static facade providing one-line access to show Highbrow house ads.
    /// </summary>
    public static class HighbrowAd
    {
        /// <summary>
        /// Shows the Highbrow cross-promotion ad popup.
        /// </summary>
        /// <param name="onCompleted">Invoked when ad playback ends or is skipped.</param>
        /// <param name="onFailed">Invoked if popup cannot be instantiated.</param>
        /// <param name="canvas">Optional canvas override.</param>
        public static HighbrowAdPlayer Show(Action onCompleted = null, Action onFailed = null, Canvas canvas = null)
        {
            return HighbrowAdPlayer.Show(onCompleted, onFailed, canvas);
        }
    }
}
