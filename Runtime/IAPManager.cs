using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

// IStoreListener = UnityEngine.Purchasing.asmdef
// StandardPurchasingModule = UnityEngine.Purchasing.Stores.asmdef

public interface IIAP_Data
{
    string GetIAP_DataKey();
    string GetIAP_ID();
    ProductType GetProductType();
}

public class IAPManager : IStoreListener
{
    private const int CUT_MAX_LENGTH = 300;

    public event System.Action OnInit;
    public delegate IEnumerator CheckPurchaseCoroutine(PurchaseEventArgs arg, System.Action<bool> checkPurchaseResult);

    public bool IsInit { get; private set; }

    Dictionary<string, IIAP_Data> _IAP_Data = new Dictionary<string, IIAP_Data>();
    IStoreController _storeController;
    System.Action<bool> _onResult;

    System.Func<Product, IIAP_Data, bool> _onCheckIsTryPurchaseLogic;
    CheckPurchaseCoroutine _checkPurchaseLogic;
    System.Func<IEnumerator, Coroutine> _OnStartCoroutine;

    public IAPManager(System.Func<IEnumerator, Coroutine> OnStartCoroutine, CheckPurchaseCoroutine checkPurchaseLogic)
    {
        _checkPurchaseLogic = checkPurchaseLogic;
        _onCheckIsTryPurchaseLogic = (product, data) => true;
        _OnStartCoroutine = OnStartCoroutine;
    }

    public IAPManager(System.Func<IEnumerator, Coroutine> OnStartCoroutine, CheckPurchaseCoroutine checkPurchaseLogic, System.Func<Product, IIAP_Data, bool> onCheckIsTryPurchaseLogic)
    {
        _checkPurchaseLogic = checkPurchaseLogic;
        _onCheckIsTryPurchaseLogic = onCheckIsTryPurchaseLogic;
        _OnStartCoroutine = OnStartCoroutine;
    }

    public void Init<T>(IEnumerable<T> data)
        where T : IIAP_Data
    {
        _IAP_Data.Clear();

        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        foreach (var IAP_Data in data.Where(data => string.IsNullOrEmpty(data.GetIAP_ID()) == false))
        {
            string IAPDataKey = IAP_Data.GetIAP_DataKey();
            string productID = IAP_Data.GetIAP_ID();
            Debug.Log($"{nameof(IAPManager)} init, IAPDataKey:{IAPDataKey}, productID:{productID}, data:{IAP_Data}");

            builder.AddProduct(productID, IAP_Data.GetProductType());
            // builder.AddProduct(productID, IAP_Data.GetProductType(), new IDs
            // {
            //     {productID, GooglePlay.Name},
            //     {productID, AppleAppStore.Name},
            // });
            _IAP_Data.Add(IAPDataKey, IAP_Data);
        }
        UnityPurchasing.Initialize(this, builder);

        Debug.Log($"{nameof(IAPManager)} Try UnityPurchasing.Initialize, data.count:{data.Count()}");
    }

    public void Purchase(string IAP_DataKey, System.Action<bool> OnResult)
    {
        if (_storeController == null)
        {
            Debug.LogError($"{nameof(IAPManager)} _storeController == null");
            OnResult(false);
            return;
        }

        if (_IAP_Data.TryGetValue(IAP_DataKey, out var iapData) == false)
        {
            Debug.LogError($"{nameof(IAPManager)} not contain iap data, key:{IAP_DataKey}");
            OnResult(false);
            return;
        }

        Product product = _storeController.products.WithID(iapData.GetIAP_ID());
        if (product == null)
        {
            Debug.LogError($"{nameof(IAPManager)} product is null, key:{IAP_DataKey}");
            OnResult(false);
            return;
        }

        if (_onCheckIsTryPurchaseLogic(product, iapData) == false)
        {
            OnResult(false);
            return;
        }

        if (product.availableToPurchase == false)
        {
            Debug.LogError($"{nameof(IAPManager)} product is not availableToPurchase, key:{IAP_DataKey}");
            OnResult(false);
            return;
        }

        _onResult = OnResult;
        _storeController.InitiatePurchase(product);
    }

    public bool TryGetIAPData(string IAP_DataKey, out IIAP_Data data)
    {
        return _IAP_Data.TryGetValue(IAP_DataKey, out data);
    }

    public bool TryGetIAPDataByProductID(string productID, out IIAP_Data data)
    {
        data = _IAP_Data.Values.FirstOrDefault(data => data.GetIAP_ID().Equals(productID));
        return data != null;
    }

    public bool TryGetProduct(string IAP_DataKey, out Product product)
    {
        product = null;
        if (_storeController == null)
        {
            Debug.LogError($"{nameof(IAPManager)}.TryGetProduct _storeController is null, data Key:{IAP_DataKey}");
            return false;
        }

        if (_IAP_Data.TryGetValue(IAP_DataKey, out var data))
        {
            product = _storeController.products.WithID(data.GetIAP_ID());
            return product != null;
        }

        return false;
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        Debug.Log($"{nameof(IAPManager)} OnInitialized");

        _storeController = controller;
        IsInit = true;
        OnInit?.Invoke();
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.LogError($"{nameof(IAPManager)} OnInitializeFailed, error:{error}");
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.LogError($"{nameof(IAPManager)} OnPurchaseFailed, product:{product}, failureReason:{failureReason}");
        _onResult?.Invoke(false);
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
    {
        if (purchaseEvent == null)
        {
            Debug.LogError("purchaseEvent == null");
            _onResult?.Invoke(false);
            return PurchaseProcessingResult.Pending;
        }

        if (purchaseEvent.purchasedProduct == null)
        {
            Debug.LogError("purchaseEvent.purchasedProduct == null");
            _onResult?.Invoke(false);
            return PurchaseProcessingResult.Pending;
        }

        Product purchasedProduct = purchaseEvent.purchasedProduct;
        string productID = purchasedProduct.definition.id;
        Debug.Log($"Start ProcessPurchase productID:{productID}");

        if (TryGetIAPDataByProductID(productID, out var iapData) == false)
        {
            Debug.LogError("iapData == null");
            _onResult?.Invoke(false);
            return PurchaseProcessingResult.Pending;
        }

        string IAP_ID = iapData.GetIAP_ID();
        if (string.Equals(productID, IAP_ID, System.StringComparison.Ordinal) == false)
        {
            Debug.LogError($"purchasedProduct.definition.id({productID}) != IAP_ID({IAP_ID})");
            _onResult?.Invoke(false);
            return PurchaseProcessingResult.Pending;
        }

        // Debug.Log($"Purchased {productID} start cut receipt");
        // purchasedProduct.receipt.CutString(Debug.Log);
        // Debug.Log($"Purchased {productID} finish cut receipt");

        if (_checkPurchaseLogic == null)
        {
            _onResult?.Invoke(true);
            return PurchaseProcessingResult.Complete;
        }
        else
        {
            _OnStartCoroutine(_checkPurchaseLogic(purchaseEvent, (result) =>
            {
                if (result)
                {
                    _storeController.ConfirmPendingPurchase(purchasedProduct);
                }
                _onResult?.Invoke(result);

            }));
            return PurchaseProcessingResult.Pending;
        }
    }
}
