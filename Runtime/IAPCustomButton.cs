using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class IAPCustomButton : MonoBehaviour
{
    [SerializeField]
    private string _IAP_DataKey = string.Empty;
    [SerializeField]
    private Text _priceText = null;

    [Inject]
    IAPManager _iapManager = null;

    string _productID;
    bool _managerIsInit;

    void Awake()
    {
        if (_iapManager != null)
        {
            OnAwake();
        }
    }

    public void SetManager(IAPManager manager)
    {
        _iapManager = manager;
        OnAwake();
    }

    public void Purchase(System.Action<bool> onPurchaseResult)
    {
        _iapManager.Purchase(_IAP_DataKey, onPurchaseResult);
    }

    public void SetIAP_DataKey(string dataKey)
    {
        _IAP_DataKey = dataKey;
        if (_managerIsInit)
        {
            Init();
        }
    }

    public void SetPriceText(Text priceText)
    {
        _priceText = priceText;
        if (_managerIsInit)
        {
            Init();
        }
    }

    private void OnAwake()
    {
        if (_iapManager.IsInit)
        {
            FirstInit();
        }
        else
        {
            _iapManager.OnInit += FirstInit;
        }
    }

    private void FirstInit()
    {
        _managerIsInit = true;
        _iapManager.OnInit -= FirstInit;

        Init();
    }

    private void Init()
    {
        _managerIsInit = true;

        if (string.IsNullOrEmpty(_IAP_DataKey))
        {
            return;
        }

        if (_iapManager.TryGetIAPData(_IAP_DataKey, out var data) == false)
        {
            Debug.LogError($"{name} TryGetIAPData, IAP data key:{_IAP_DataKey}", this);
            return;
        }

        if (_iapManager.TryGetProduct(_IAP_DataKey, out var productData) == false)
        {
            Debug.LogError($"{name} TryGetProduct fail, IAP data key:{_IAP_DataKey}", this);
            return;
        }

        _productID = data.GetIAP_ID();
        if (_priceText != null)
        {
            _priceText.text = productData.metadata.localizedPriceString;
        }
    }
}