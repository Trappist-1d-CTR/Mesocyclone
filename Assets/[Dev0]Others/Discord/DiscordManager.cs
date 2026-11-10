using UnityEngine;
using UnityEngine.UI;
using Discord.Sdk;

namespace Mesocyclone.Discord
{
    /// <summary>
    /// Manages and handles the discord social SDK for discord integration
    /// - particularly in RPC (Rich Presence)
    /// </summary>
    public sealed class DiscordManager : MonoBehaviour
    {
        public static DiscordManager instance { get; private set; }
        

        [field: SerializeField]
        public ulong clientId { get; private set; }

        // fuckass C# 9
        [SerializeField]
        private string _status;
        public string status
        {
            get { return _status; }

            private set
            {
                _status = value;
                statusText.text = _status;
            }
        }


        [SerializeField]
        private Button loginButton;

        [SerializeField]
        private Text statusText;


        private Client client;
        private string codeVerifier;


        private void Start()
        {
            client = new Client();

            client.AddLogCallback(OnLog, LoggingSeverity.Error);
            client.SetStatusChangedCallback(OnStatusChanged);

            if (loginButton != null)
                loginButton.onClick.AddListener(StartOAuthFlow);
            else
                Debug.LogError("Login button reference is missing!");
        }

        private void OnEnable()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnDisable()
        {
            if (instance != null)
                instance = null;
        }

        private void OnLog(string message, LoggingSeverity severity)
        {
            Debug.Log($"Log: {severity} - {message}");
        }

        private void OnStatusChanged(Client.Status status, Client.Error error, int code)
        {
            Debug.Log($"Status changed: {status}");

            this.status = status.ToString();

            if (error != Client.Error.None)
                Debug.LogError($"Error: {error}\nCode: {code}");
        }

        // authenticate your discord account
        private void StartOAuthFlow()
        {
            AuthorizationCodeVerifier authVerifier = client.CreateAuthorizationCodeVerifier();

            codeVerifier = authVerifier.Verifier();

            AuthorizationArgs args = new AuthorizationArgs();
            args.SetClientId(clientId);
            args.SetScopes(Client.GetDefaultPresenceScopes());
            args.SetCodeChallenge(authVerifier.Challenge());

            client.Authorize(args, OnAuthorizeResult);
        }

        private void OnAuthorizeResult(ClientResult result, string code, string redirect)
        {
            Debug.Log($"Authorization result: [{result.Error()}] [{code}] [{redirect}]");

            if (!result.Successful())
                return;

            GetTokenFromCode(code, redirect);
        }

        private void GetTokenFromCode(string code, string redirect)
        {
            client.GetToken(
                clientId,
                code,
                this.codeVerifier,
                redirect,
                (result, token, refreshToken, tokenType, expiresIn, scope)
                => { }
            );
        }
    }
}