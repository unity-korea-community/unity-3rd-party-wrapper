#if UNITY_IOS && APPLE_AUTH
#define SUPPORT
#else
#undef SUPPORT
#endif

using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

#if SUPPORT
using AppleAuth;
using AppleAuth.Enums;
using AppleAuth.Extensions;
using AppleAuth.Interfaces;
using AppleAuth.Native;
#endif

namespace UNKO
{
    // 참고 http://milennium9.godohosting.com/wordpress/?p=300
    // https://github.com/lupidan/apple-signin-unity
    // 1.4.2v 기준
    public class AppleLoginLogic : IPlatformLoginLogic
    {
        private const string USER_SAVE_KEY = "UNKO.AppleLoginLogic.User";
        const string APPLE_AUTH = "AppleAuth";

        [System.Serializable]
        public class User
        {
            public string userId;
            public string email;
            public string fullName;
            public string identityToken;
            public string authorizationCode;

            public User(string userId, string email, string fullName, string identityToken, string authorizationCode)
            {
                this.userId = userId;
                this.email = email;
                this.fullName = fullName;
                this.identityToken = identityToken;
                this.authorizationCode = authorizationCode;

                Debug.Log($"{nameof(AppleLoginLogic)} userId:{userId}, email:{email}, fullName:{fullName}, token:{identityToken}, autoCode:{authorizationCode}");
            }
        }

        public static string RawNonce { get; private set; }
        public static string Nonce { get; private set; }
        public static User LoginUser { get; private set; }

#if SUPPORT
        private IAppleAuthManager _appleAuthManager;
#endif

        public void Init()
        {
#if SUPPORT
            // Creates a default JSON deserializer, to transform JSON Native responses to C# instances
            var deserializer = new PayloadDeserializer();
            // Creates an Apple Authentication manager with the deserializer
            this._appleAuthManager = new AppleAuthManager(deserializer);

            string json = PlayerPrefs.GetString(USER_SAVE_KEY);
            UpdateUser(json);
#endif

            Debug.Log($"{APPLE_AUTH} Init");
        }

        public async Task<bool> LoginAsync()
        {
#if SUPPORT
            Debug.Log($"{APPLE_AUTH} LoginAsync Try");

            LoginUser = null;
            // Nonce 초기화
            // Nonce는 Apple로그인 시 접속 세션마다 새로 생성
            RawNonce = System.Guid.NewGuid().ToString();
            Nonce = GenerateNonce(RawNonce);

            var loginArgs = new AppleAuthLoginArgs(LoginOptions.IncludeEmail | LoginOptions.IncludeFullName, Nonce);
            bool isWait = true;
            bool isSuccess = false;
            _appleAuthManager.LoginWithAppleId(
                loginArgs,
                credential =>
                {
                    Debug.Log($"{APPLE_AUTH} LoginAsync Try");

                    // Obtained credential, cast it to IAppleIDCredential
                    if (credential is IAppleIDCredential appleIdCredential)
                    {
                        // Apple User ID
                        // You should save the user ID somewhere in the device
                        var userId = appleIdCredential.User;

                        // Email (Received ONLY in the first login)
                        var email = appleIdCredential.Email;

                        // Full name (Received ONLY in the first login)
                        string fullName = string.Empty;
                        if(appleIdCredential.FullName == null)
                        {
                            Debug.LogError("appleIdCredential.FullName == null");
                        }
                        else
                        {
                            fullName = appleIdCredential.FullName.ToString();
                        }

                        // Identity token
                        var identityToken = Encoding.UTF8.GetString(
                        appleIdCredential.IdentityToken,
                        0,
                        appleIdCredential.IdentityToken.Length);

                        // Authorization code
                        var authorizationCode = Encoding.UTF8.GetString(
                        appleIdCredential.AuthorizationCode,
                        0,
                        appleIdCredential.AuthorizationCode.Length);

                        // And now you have all the information to create/login a user in your system
                        LoginUser = new User(userId, email, fullName, identityToken, authorizationCode);
                        string json = JsonUtility.ToJson(LoginUser);
                        UpdateUser(json);
                        isSuccess = true;
                    }

                    isWait = false;
                },
                error =>
                {
                    // Something went wrong
                    var authorizationErrorCode = error.GetAuthorizationErrorCode();
                    Debug.LogError($"{APPLE_AUTH} error, code:{authorizationErrorCode}");
                });

            while (isWait)
            {
                await Task.Delay(1);
            }

            Debug.Log($"{APPLE_AUTH} success?:{isSuccess}");
            return isSuccess;
#else
            Debug.Log($"{APPLE_AUTH} LoginAsync, but is not support");
            return false;
#endif
        }

        public void Update()
        {
#if SUPPORT
            _appleAuthManager.Update();
#endif
        }

        public void Logout()
        {
            LoginUser = null;
            // 기능 지원을 안함
            // https://github.com/lupidan/apple-signin-unity#how-can-i-logout-does-the-plugin-provide-any-logout-option
        }

        public string GetToken()
                => LoginUser?.identityToken;

        public string GetEmail()
                => LoginUser?.email;

        public string GetAuthCode()
                => LoginUser?.authorizationCode;

        public bool IsSupportCurrentPlatform()
        {
#if SUPPORT
            return AppleAuthManager.IsCurrentPlatformSupported;
#else
            return false;
#endif
        }

        public bool CheckIsLogin()
        {
            return LoginUser != null;
        }

        private void UpdateUser(string json)
        {
            LoginUser = string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<User>(json);
        }

        // Nonce는 SHA256으로 만들어서 전달해야함
        // 출처: http://milennium9.godohosting.com/wordpress/?p=300
        private static string GenerateNonce(string _rawNonce)
        {
            SHA256 sha = new SHA256Managed();
            var sb = new StringBuilder();
            // Encoding은 반드시 ASCII여야 함
            byte[] hash = sha.ComputeHash(Encoding.ASCII.GetBytes(_rawNonce));
            // ToString에서 "x2"로 소문자 변환해야 함. 대문자면 실패함. ㅠㅠ
            foreach (var b in hash)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}