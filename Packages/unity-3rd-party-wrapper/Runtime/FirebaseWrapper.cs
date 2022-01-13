#if (UNITY_ANDROID || UNITY_IOS) && FIREBASE
#define SUPPORT
#else
#undef SUPPORT
#endif

using System;
using System.Threading.Tasks;
using UnityEngine;

#if SUPPORT
using Firebase;
using Firebase.Auth;
#else
using Firebase.Auth;
namespace Firebase.Auth
{
    public class FirebaseUser
    {

    }
}
#endif

public static class FirebaseWrapper
{
#if SUPPORT
    static FirebaseAuth _auth;
#endif

    public static void Init()
    {
#if SUPPORT
        // Firebase 클라우드 메시징 초기화
        // https://firebase.google.com/docs/cloud-messaging/unity/client?hl=ko#initialize
        // google-services.json 의 앱 ID를 이용하여 앱을 초기화한다.
        Firebase.Messaging.FirebaseMessaging.TokenReceived += OnTokenReceived;
        Firebase.Messaging.FirebaseMessaging.MessageReceived += OnMessageReceived;

        // https://firebase.google.com/docs/cloud-messaging/unity/topic-messaging?hl=ko
        // 이거 안하면 메세지 수신 안하기 때문에 해야함
        Firebase.Messaging.FirebaseMessaging.SubscribeAsync("default");

        // 수동으로 FCM을 다시 설정
        // Firebase.Messaging.FirebaseMessaging.TokenRegistrationOnInitEnabled = true;

        // GooglePlay 서비스 버전 요구사항 확인
        // https://firebase.google.com/docs/cloud-messaging/unity/client?hl=ko#confirm_google_play_version 
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                Debug.Log("firebase dependencyStatus is available");

                // https://stackoverflow.com/questions/55442088/when-i-use-firebase-unity-plugin-to-implement-an-facebook-auth-function-in-andro
                _auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
            }
            else
            {
                Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
            }
        });
#endif
    }

    public static async void InitAsync()
    {
#if SUPPORT
        // Firebase 클라우드 메시징 초기화
        // https://firebase.google.com/docs/cloud-messaging/unity/client?hl=ko#initialize
        // google-services.json 의 앱 ID를 이용하여 앱을 초기화한다.
        Firebase.Messaging.FirebaseMessaging.TokenReceived += OnTokenReceived;
        Firebase.Messaging.FirebaseMessaging.MessageReceived += OnMessageReceived;

        // https://firebase.google.com/docs/cloud-messaging/unity/topic-messaging?hl=ko
        // 이거 안하면 메세지 수신 안하기 때문에 해야함
        await Firebase.Messaging.FirebaseMessaging.SubscribeAsync("default");

        // 수동으로 FCM을 다시 설정
        // Firebase.Messaging.FirebaseMessaging.TokenRegistrationOnInitEnabled = true;

        // GooglePlay 서비스 버전 요구사항 확인
        // https://firebase.google.com/docs/cloud-messaging/unity/client?hl=ko#confirm_google_play_version 
        var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (dependencyStatus == DependencyStatus.Available)
        {
            Debug.Log("firebase dependencyStatus is available");
        }
        else
        {
            Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
        }
#endif
    }

    public static async Task<FirebaseUser> FirebaseGoogleSignInLoginAsync(string idToken)
    {
#if SUPPORT
        Debug.Log($"try {nameof(FirebaseGoogleSignInLoginAsync)} login..");

        try
        {
            Credential credential = GoogleAuthProvider.GetCredential(idToken, null);
            FirebaseUser newUser = await _auth.SignInWithCredentialAsync(credential);
            Debug.Log($"{nameof(FirebaseGoogleSignInLoginAsync)} User signed in successfully: {newUser.DisplayName} ({newUser.UserId})");

            return newUser;
        }
        catch (Exception error)
        {
            Debug.LogError($"{nameof(FirebaseGoogleSignInLoginAsync)} error {error}");
        }
#else
        Debug.Log($"{nameof(FirebaseGoogleSignInLoginAsync)} login dummy");
#endif

        return null;
    }

    public static async Task<FirebaseUser> FirebaseGoogleGPGSLoginAsync(string authCode)
    {
#if SUPPORT
        Debug.Log($"try {nameof(FirebaseGoogleGPGSLoginAsync)} login..");

        try
        {
            Credential credential = PlayGamesAuthProvider.GetCredential(authCode);
            FirebaseUser newUser = await _auth.SignInWithCredentialAsync(credential);
            Debug.Log($"{nameof(FirebaseGoogleGPGSLoginAsync)} User signed in successfully: {newUser.DisplayName} ({newUser.UserId})");

            return newUser;
        }
        catch (Exception error)
        {
            Debug.LogError($"{nameof(FirebaseGoogleGPGSLoginAsync)} error {error}");
        }
#else
        Debug.Log($"try {nameof(FirebaseGoogleGPGSLoginAsync)} login dummy");
#endif

        return null;
    }

    public static async Task<FirebaseUser> FirebaseAppleLoginAsync(string idToken, string rawNonce, string authCode)
    {
#if SUPPORT
        try
        {
            // Firebase는 Google이나 Apple GameCenter와 달리 AppleLoginProvider를 제공하지 않고 있다. 그래서 OAuthProvider를 사용해야한다.
            // RawNonce는 SHA256으로 변환하기 전 문자열을 의미한다.
            // 출처: http://milennium9.godohosting.com/wordpress/?p=300
            var credential = OAuthProvider.GetCredential("apple.com", idToken, rawNonce, authCode);

            var newUser = await _auth.SignInWithCredentialAsync(credential);
            Debug.LogFormat($"{nameof(FirebaseAppleLoginAsync)} User signed in successfully: {newUser.DisplayName} ({newUser.UserId})");

            return newUser;
        }
        catch (Exception error)
        {
            Debug.LogError($"{nameof(FirebaseAppleLoginAsync)} error {error}");
        }
#else
        Debug.Log($"try {nameof(FirebaseAppleLoginAsync)} login dummy");
#endif

        return null;

    }

    static void OnTokenReceived(object sender, Firebase.Messaging.TokenReceivedEventArgs token)
    {
        Debug.Log($"Firebase Received Token, token:{token.Token}");
    }

    static void OnMessageReceived(object sender, Firebase.Messaging.MessageReceivedEventArgs e)
    {
        Firebase.Messaging.FirebaseNotification notification = e.Message.Notification;
        Debug.Log($"Firebase Received Message, title:{notification.Title}, {notification.Body}");
    }
}