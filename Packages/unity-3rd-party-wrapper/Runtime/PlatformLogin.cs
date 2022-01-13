using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace UNKO
{
    public enum Platform
    {
        GoogleSignIn, // ios && android
        GoogleGPGS, // android only
        AppleAuth, // iOS only
    }

    public interface IPlatformLoginLogic
    {
        void Init();
        Task<bool> LoginAsync();
        void Update();
        void Logout();
        string GetToken();
        string GetEmail();
        string GetAuthCode();
        bool IsSupportCurrentPlatform();
        bool CheckIsLogin();
    }

    public static class PlatformLogin
    {
        static Dictionary<Platform, IPlatformLoginLogic> _loginLogicByPlatform = new Dictionary<Platform, IPlatformLoginLogic>()
        {
            { Platform.GoogleSignIn, new GoogleSignInLoginLogic() },
            { Platform.GoogleGPGS, new GoogleGPGSLoginLogic() },
            { Platform.AppleAuth, new AppleLoginLogic() },
        };

        static List<IPlatformLoginLogic> _initLogics = new List<IPlatformLoginLogic>();

        public static void Init()
        {
            var platforms = System.Enum.GetValues(typeof(Platform));
            foreach (var platformObject in platforms)
            {
                Platform platform = (Platform)platformObject;
                IPlatformLoginLogic platformLoginLogic = GetLoginLogic(platform);
                bool isSupport = platformLoginLogic.IsSupportCurrentPlatform();
                Debug.Log($"Platform Login {platform} is Support?{isSupport}");
                if (isSupport)
                {
                    platformLoginLogic.Init();
                    _initLogics.Add(platformLoginLogic);
                }
            }
        }

        public static void Init(Platform platform)
            => GetLoginLogic(platform).Init();

        public static async Task<bool> LoginAsync(Platform platform)
            => await GetLoginLogic(platform).LoginAsync();

        public static void Update()
        {
            foreach (var logic in _initLogics)
            {
                logic.Update();
            }
        }

        public static void Logout(Platform platform)
            => GetLoginLogic(platform).Logout();

        public static string GetToken(Platform platform)
            => GetLoginLogic(platform).GetToken();

        public static string GetEmail(Platform platform)
            => GetLoginLogic(platform).GetEmail();

        public static string GetAuthCode(Platform platform)
            => GetLoginLogic(platform).GetAuthCode();

        public static bool CheckIsLogin(Platform platform)
            => GetLoginLogic(platform).CheckIsLogin();

        public static IPlatformLoginLogic GetLoginLogic(Platform platform)
        {
            _loginLogicByPlatform.TryGetValue(platform, out var loginLogic);
            return loginLogic;
        }
    }
    // #endif
}