using UnityEngine;
using UnityEngine.Advertisements;

public class AdsButton : MonoBehaviour, IUnityAdsListener
{
    [SerializeField]
    private string _adsDataKey = string.Empty;

    AdsManager _adsManager = null;
    bool _managerIsInit;

    public void Init(AdsManager manager)
    {
        _adsManager = manager;
        BindInit();
    }

    public void ShowAd(System.Action onPurchaseResult)
        => ShowAd((state, msg) =>
        {
            if (state.IsFinish())
                onPurchaseResult();
        });


    public void ShowAd(System.Action<AdState> onPurchaseResult)
        => ShowAd((state, msg) => onPurchaseResult(state));

    public void ShowAd(System.Action<AdState, string> onPurchaseResult)
    {
        if (_adsManager == null)
        {
            Debug.LogError($"{name}{nameof(AdsButton)} ShowAd fail, _adsManager == null");
            return;
        }

        _adsManager.ShowAd(_adsDataKey, onPurchaseResult);
    }

    public void SetAdsDataKey(string dataKey)
    {
        _adsDataKey = dataKey;
        if (_managerIsInit)
        {
            Init();
        }
    }

    private void FirstInit()
    {
        _managerIsInit = true;
        _adsManager.OnInit -= FirstInit;

        Init();
    }

    private void Init()
    {
        _managerIsInit = true;
        Advertisement.AddListener(this);
    }

    private void OnDestroy()
    {
        Advertisement.RemoveListener(this);
    }

    public void OnUnityAdsReady(string placementId)
    {
    }

    public void OnUnityAdsDidError(string message)
    {
        Debug.LogError($"{nameof(AdsButton)} OnUnityAdsDidError, dataKey:{_adsDataKey}, message:{message}");
    }

    public void OnUnityAdsDidStart(string placementId)
    {
    }

    public void OnUnityAdsDidFinish(string placementId, ShowResult showResult)
    {
    }

    private void BindInit()
    {
        if (_adsManager.IsInit)
        {
            FirstInit();
        }
        else
        {
            _adsManager.OnInit += FirstInit;
        }
    }
}