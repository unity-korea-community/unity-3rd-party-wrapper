#if (UNITY_ANDROID || UNITY_IOS) && GSI && !UNITY_EDITOR
#define SUPPORT
#else
#undef SUPPORT
#endif

using System;
using System.Threading.Tasks;
using UnityEngine;

#if SUPPORT
using Google;
#else

#endif

namespace UNKO
{
    // https://github.com/googlesamples/google-signin-unity
    // 이것도 최신 받아야 함, https://github.com/googlesamples/unity-jar-resolve
    // 1.0.4v 기준
    // development 빌드는 안됨
    public class GoogleSignInLoginLogic : IPlatformLoginLogic
    {
        const string GSI_STRING = "GoogleSignIn";

        public static string WebClientID { get; private set; }

#if SUPPORT
        public static GoogleSignInUser LoginUser { get; private set; }
        private GoogleSignInConfiguration _configuration;
#endif

        public static void SetWebClientID(string webClientID)
        {
            WebClientID = webClientID;
        }

        public void Init()
        {
#if SUPPORT
            if (string.IsNullOrEmpty(WebClientID))
            {
                Debug.LogError($"{nameof(GSI_STRING)} Init 전에 {nameof(SetWebClientID)}를 호출하기 바랍니다");
                return;
            }
            // Defer the configuration creation until Awake so the web Client ID
            // Can be set via the property inspector in the Editor.
            _configuration = new GoogleSignInConfiguration
            {
                WebClientId = WebClientID,
                RequestAuthCode = true,
                RequestIdToken = true,
                RequestProfile = true,
                RequestEmail = true,
                UseGameSignIn = false // For iOS Support
            };
#endif

            Debug.Log($"{GSI_STRING} Init");
        }

        public async Task<bool> LoginAsync()
        {
            bool isSuccess = false;
#if SUPPORT
            Debug.Log($"{GSI_STRING} Try Login..");

            GoogleSignIn.Configuration = _configuration;
            try
            {
                LoginUser = await GoogleSignIn.DefaultInstance.SignIn();
                isSuccess = LoginUser != null;
                if (isSuccess)
                {
                    Debug.Log($"{GSI_STRING} login success, DisplayName:{LoginUser.DisplayName}, Email:{LoginUser.Email}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"{GSI_STRING} Login error, {e}");
            }
#endif

            Debug.Log($"{GSI_STRING} Login Finish, isSuccess?:{isSuccess}");

            return isSuccess;
        }

        public void Logout()
        {
#if SUPPORT
            GoogleSignIn.DefaultInstance.SignOut();
            LoginUser = null;
#endif
            Debug.Log($"{GSI_STRING} google log out");
        }

        public string GetToken()
        {
#if SUPPORT
            return LoginUser.IdToken;
#else
            return string.Empty;
#endif
        }

        public string GetEmail()
        {
#if SUPPORT
            return LoginUser.Email;
#else
            return string.Empty;
#endif
        }

        public string GetAuthCode()
        {
#if SUPPORT
            return LoginUser.AuthCode;
#else
            return string.Empty;
#endif
        }

        public void Update()
        {
        }

        public bool IsSupportCurrentPlatform()
        {
#if SUPPORT
            return true;
#else
            return false;
#endif
        }

        public bool CheckIsLogin()
        {
#if SUPPORT
            return LoginUser != null;
#else
            return false;
#endif
        }
    }
}