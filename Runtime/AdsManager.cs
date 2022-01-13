using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Advertisements;
using UnityEngine.EventSystems;

public enum AdPlatform
{
    Unknown,
    Editor,
    iOS,
    Android
}

public enum AdState
{
    ERROR = -1,
    SKIPPED = 0,
    COMPLETED = 1,
    UNKNOWN = 2,

    /// <summary>
    /// 유니티에서 광고를 안틀어 줄 때
    /// </summary>
    NOT_READY,
    START,
    FAIL,
    ADS_SHOW_CLICK,
}

public static class AdStateEx
{
    static HashSet<AdState> _isFinish = new HashSet<AdState>()
    {
        AdState.NOT_READY, AdState.ERROR, AdState.SKIPPED, AdState.COMPLETED, AdState.FAIL
    };

    public static bool IsFinish(this AdState state)
        => _isFinish.Contains(state);
}

public enum AdsType
{
    /// <summary>
    /// 스킵 못하는 동영상 광고
    /// </summary>
    Rewarded,

    /// <summary>
    /// 스킵 가능한 동영상 광고
    /// </summary>
    Interstitial
}

public interface IAdsData
{
    string GetAdsKey();
    string GetADPlacementID(AdPlatform currentPlatform);
    AdsType GetAdsType();
}

public class AdsManager : IUnityAdsInitializationListener, IUnityAdsListener, IUnityAdsLoadListener, IUnityAdsShowListener
{
    public class AdsDataWrapper
    {
        public IAdsData Data { get; private set; }
        public bool IsLoad { get; private set; }

        public AdsDataWrapper(IAdsData data)
        {
            this.Data = data;
            IsLoad = false;
        }

        public void SetIsLoad(bool isLoad)
        {
            this.IsLoad = isLoad;
        }
    }

    public event Action OnInit;

    public bool IsInit { get; private set; } = false;

    string _androidGameId;
    string _iOSGameId;

    Dictionary<string, IAdsData> _adsData = new Dictionary<string, IAdsData>();
    Dictionary<string, AdsDataWrapper> _adsDataWrapperByPlacementID = new Dictionary<string, AdsDataWrapper>();

    string _gameId;
    AdPlatform _currentPlatform = AdPlatform.Unknown;
    Action<AdState, string> _onUpdateStateAds;

    Func<IAdsData, bool> _isSkipAdsLogic;
    Func<IEnumerator, Coroutine> _onStartCoroutine;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="androidGameId"></param>
    /// <param name="iOSGameId"></param>
    /// <param name="testMode"></param>
    /// <param name="enablePerPlacementLoad">https://forum.unity.com/threads/enableperplacementload-question.1179919/</param>
    /// <param name="isSkipAdsLogic"></param>
    public AdsManager(string androidGameId, string iOSGameId, bool testMode, bool enablePerPlacementLoad, Func<IEnumerator, Coroutine> onStartCoroutine, Func<IAdsData, bool> isSkipAdsLogic = null)
    {
        _androidGameId = androidGameId;
        _iOSGameId = iOSGameId;
        _isSkipAdsLogic = isSkipAdsLogic;
        _onStartCoroutine = onStartCoroutine;

        DefineCurrentPlatform();
        InitializeAds(testMode, enablePerPlacementLoad);
    }

    public void AddAds(IAdsData[] data)
    {
        _adsData = data.ToDictionary(data => data.GetAdsKey());
        InitData();
    }

    public void AddAds<T>(IEnumerable<T> data) where T : IAdsData
    {
        _adsData = data.OfType<IAdsData>().ToDictionary(data => data.GetAdsKey());
        InitData();
    }

    private void InitData()
    {
        foreach (var data in _adsData.Values)
        {
            string placementId = data.GetADPlacementID(_currentPlatform);
            AdsDataWrapper dataWrapper = GetOrCreateDataWrapper(placementId);
            TryLoadAds(placementId, dataWrapper);
        }
    }

    public bool CheckIsReady(string adDataKey)
    {
        if (_adsData.TryGetValue(adDataKey, out IAdsData data) == false)
        {
            Debug.LogError($"Not found data, key:{adDataKey}");
            return false;
        }

        return CheckIsReady(data);
    }

    public void ShowAd(string adDataKey, Action OnUpdateStateAds)
        => ShowAd(adDataKey, (state, msg) =>
        {
            if (state.IsFinish())
            {
                OnUpdateStateAds();
            }
        });


    public void ShowAd(string adDataKey, Action<AdState> OnUpdateStateAds)
        => ShowAd(adDataKey, (state, msg) => OnUpdateStateAds(state));

    public void ShowAd(string adDataKey, Action<AdState, string> OnUpdateStateAds)
    {
        if (_adsData.TryGetValue(adDataKey, out IAdsData data) == false)
        {
            OnUpdateStateAds(AdState.ERROR, $"Not found data, key:{adDataKey}");
            return;
        }

        if (_isSkipAdsLogic != null && _isSkipAdsLogic(data))
        {
            OnUpdateStateAds(AdState.COMPLETED, $"ads is skip, key:{adDataKey}");
            return;
        }

        string placementId = data.GetADPlacementID(_currentPlatform);
        if (CheckIsReady(data) == false)
        {
            OnUpdateStateAds(AdState.NOT_READY, $"ads is not ready, key:{adDataKey}, placementId:{placementId}");
            return;
        }

        _onUpdateStateAds = OnUpdateStateAds;
        Advertisement.Show(placementId, this);
        Debug.Log($"Unity Ads Show, placementId:{placementId}");
    }

    public void InitializeAds(bool testMode, bool enablePerPlacementLoad)
    {
        _gameId = (Application.platform == RuntimePlatform.IPhonePlayer) ? _iOSGameId : _androidGameId;

        // 어째서인지 모르겠는데, 씬에 이미 EventSystem이 있음에도 불구하고
        // Advertisement.Initialize를 호출하면 EventSystem이 또 생겨서 총 2개가 생김
        // 근데 빌드하면 안생김
        // 그래서 Scene에 있는 EventSystem을 제거하고, AdsManager에서 Editor가 아닐 때 동적으로 생성하게끔 변경
        if (Application.isEditor == false)
        {
            new GameObject(nameof(EventSystem), typeof(EventSystem), typeof(StandaloneInputModule));
        }
        Advertisement.Initialize(_gameId, testMode, enablePerPlacementLoad, this);
        Advertisement.AddListener(this);

        IsInit = true;
        OnInit?.Invoke();
    }

    public void OnInitializationComplete()
    {
        Debug.Log("Unity Ads initialization complete.");
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.LogError($"Unity Ads Initialization Failed: {error}, {message}");
    }

    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
        UpdateAdsState(AdState.ERROR, message);
    }

    public void OnUnityAdsShowStart(string placementId)
    {
        UpdateAdsState(AdState.START, string.Empty);
    }

    public void OnUnityAdsShowClick(string placementId)
    {
        UpdateAdsState(AdState.ADS_SHOW_CLICK, string.Empty);
    }

    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
    {
        UpdateAdsState((AdState)showCompletionState, string.Empty);
    }

    public void OnUnityAdsAdLoaded(string placementId)
    {
        Debug.Log($"Unity Ads OnUnityAdsAdLoaded, placementId:{placementId}");

        AdsDataWrapper dataWrapper = GetOrCreateDataWrapper(placementId);
        dataWrapper?.SetIsLoad(Advertisement.IsReady(placementId));
    }

    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        Debug.LogError($"Unity Ads OnUnityAdsFailedToLoad Failed, placementId:{placementId}, error:{error}, message:{message}");
        AdsDataWrapper dataWrapper = GetOrCreateDataWrapper(placementId);
        TryLoadAds(placementId, dataWrapper);
    }

    public void OnUnityAdsReady(string placementId)
    {
        var wrapper = GetOrCreateDataWrapper(placementId);
        wrapper?.SetIsLoad(true);

        Debug.Log($"Unity Ads OnUnityAdsReady, placementId:{placementId}");
    }

    public void OnUnityAdsDidError(string message)
    {
        Debug.LogError($"Unity Ads OnUnityAdsDidError, message:{message}");
    }

    private void TryLoadAds(string placementId, AdsDataWrapper adsDataWrapper)
    {
        bool isLoad = Advertisement.IsReady(placementId);
        adsDataWrapper?.SetIsLoad(isLoad);
        Debug.LogWarning($"Unity Ads TryLoadAds placementId:{placementId}");
        if (isLoad == false)
        {
            _onStartCoroutine(DelayInvoke(() => Advertisement.Load(placementId, this), UnityEngine.Random.Range(1f, 10f)));
        }
    }

    public void OnUnityAdsDidStart(string placementId)
    {
        Debug.Log($"Unity Ads OnUnityAdsDidStart, placementId:{placementId}");
    }

    public void OnUnityAdsDidFinish(string placementId, ShowResult showResult)
    {
        Debug.Log($"Unity Ads OnUnityAdsDidFinish, placementId:{placementId}, showResult:{showResult}");

        switch (showResult)
        {
            case ShowResult.Failed:
                UpdateAdsState(AdState.FAIL, string.Empty);
                break;

            case ShowResult.Skipped:
                UpdateAdsState(AdState.SKIPPED, string.Empty);
                break;

            case ShowResult.Finished:
                UpdateAdsState(AdState.COMPLETED, string.Empty);
                break;
        }
    }

    public bool CheckIsReady(IAdsData data)
    {
        string placementId = data.GetADPlacementID(_currentPlatform);
        var dataWrapper = GetOrCreateDataWrapper(placementId);
        if (dataWrapper == null)
        {
            return false;
        }

        if (dataWrapper.IsLoad == false)
        {
            return false;
        }

        return Advertisement.IsReady(placementId);
    }

    private void UpdateAdsState(AdState state, string message)
    {
        _onUpdateStateAds?.Invoke(state, message);
    }

    private AdsDataWrapper GetOrCreateDataWrapper(string placementId)
    {
        AdsDataWrapper dataWrapper = null;
        var adsData = _adsData.Values.FirstOrDefault(data => data.GetADPlacementID(_currentPlatform) == placementId);
        if (adsData != null)
        {
            if (_adsDataWrapperByPlacementID.TryGetValue(placementId, out dataWrapper) == false)
            {
                dataWrapper = new AdsDataWrapper(adsData);
                _adsDataWrapperByPlacementID.Add(placementId, dataWrapper);
            }
        }

        return dataWrapper;
    }

    private void DefineCurrentPlatform()
    {
        if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            _currentPlatform = AdPlatform.iOS;
        }
        else if (Application.platform == RuntimePlatform.Android)
        {
            _currentPlatform = AdPlatform.Android;
        }
        else
        {
            _currentPlatform = AdPlatform.Editor;
        }
    }

    IEnumerator DelayInvoke(System.Action action, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        action?.Invoke();
    }
}