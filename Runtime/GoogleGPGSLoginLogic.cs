#if UNITY_ANDROID && GPGS
#define SUPPORT
#else
#undef SUPPORT
#endif

using System.Threading.Tasks;
using UnityEngine;

#if SUPPORT
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#else

public enum SignInStatus
{
    Success,
}
#endif

namespace UNKO
{
    // https://github.com/playgameservices/play-games-plugin-for-unity
    // 0.10.13v 기준
    public class GoogleGPGSLoginLogic : IPlatformLoginLogic
    {
        const string GPGS = "GPGS";

        public void Init()
        {
#if SUPPORT
            PlayGamesClientConfiguration config = new PlayGamesClientConfiguration.Builder()
            // enables saving game progress.
            // .EnableSavedGames()
            // requests the email address of the player be available.
            // Will bring up a prompt for consent.
            .RequestEmail()
            // requests a server auth code be generated so it can be passed to an
            //  associated back end server application and exchanged for an OAuth token.
            .RequestServerAuthCode(false)
            // requests an ID token be generated.  This OAuth token can be used to
            //  identify the player to other services such as Firebase.
            .RequestIdToken()
            .Build();

            PlayGamesPlatform.InitializeInstance(config);
            PlayGamesPlatform.DebugLogEnabled = true;
            PlayGamesPlatform.Activate();
#endif

            Debug.Log($"{GPGS} Init");
        }

        public async Task<bool> LoginAsync()
        {
            var resultStatus = await GoogleLoginStatusAsync();
            Debug.Log($"{GPGS} login done, result:{resultStatus}");

            return resultStatus == SignInStatus.Success;
        }


        public async Task<SignInStatus> GoogleLoginStatusAsync()
        {
            Debug.Log($"{GPGS} try login..");
            SignInStatus resultStatus = SignInStatus.Success;
#if SUPPORT
            bool isWait = true;
            PlayGamesPlatform.Instance.Authenticate(SignInInteractivity.CanPromptAlways, (status) =>
            {
                resultStatus = status;
                isWait = false;
            });

            while (isWait)
            {
                await Task.Delay(1);
            }
#endif
            await Task.Delay(1); // disable async warning

            return resultStatus;
        }

        public void Logout()
        {
#if SUPPORT
            PlayGamesPlatform.Instance.SignOut();
#endif

            Debug.Log($"{GPGS} log out");
        }

        public string GetToken()
        {
#if SUPPORT
            return ((PlayGamesLocalUser)Social.localUser).GetIdToken();
#else
            return string.Empty;
#endif
        }

        public string GetEmail()
        {
#if SUPPORT
            return ((PlayGamesLocalUser)Social.localUser).Email;
#else
            return string.Empty;
#endif
        }

        public string GetAuthCode()
        {
#if SUPPORT
            return PlayGamesPlatform.Instance.GetServerAuthCode();
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
            return Social.localUser.authenticated;
        }
    }
}