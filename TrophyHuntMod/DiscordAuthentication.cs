using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json;
//using System.IO;

using TrophyHuntMod;
using System.Runtime.CompilerServices;
using UnityEngine;
//using static UnityEngine.GUI;
//using static PrivilegeManager;

public class DiscordOAuthFlow
{
    private const string TokenEndpoint = "https://discord.com/api/oauth2/token";
    private const string UserEndpoint = "https://discord.com/api/users/@me";
    private HttpListener httpListener;

    string m_clientId = string.Empty;
    string m_redirectUri = string.Empty;
    int m_authPort = 5000;
    string m_code = string.Empty;
    DiscordUserResponse m_userInfo = null;

    bool VERBOSE = false;

    public DiscordUserResponse GetUserResponse() { return m_userInfo; }
    public void ClearUserResponse() { m_userInfo = null; }

    public delegate void StatusCallback();

    StatusCallback m_statusCallback = null;

    public void StartOAuthFlow(string clientId, string redirectUri, int port, StatusCallback callback)
    {
        m_clientId = clientId;
        m_redirectUri = redirectUri;
        m_statusCallback= callback;
        m_authPort = port;

        if (VERBOSE) System.Diagnostics.Debug.WriteLine("Starting OAuth flow...");
        StartServer(redirectUri);
        OpenDiscordAuthorization(clientId, redirectUri);
    }

    private void OpenDiscordAuthorization(string clientId, string redirectUri)
    {
        if (VERBOSE) System.Diagnostics.Debug.WriteLine("Opening Discord authorization URL...");
        string scope = "identify";

        if (VERBOSE) System.Diagnostics.Debug.WriteLine("[INFO] Opening browser for Discord authentication...");

        string authUrl = $"https://discord.com/oauth2/authorize" +
                         $"?client_id={clientId}" +
                         $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                         $"&response_type=token" +
                         $"&scope={scope}";

        if (VERBOSE) System.Diagnostics.Debug.WriteLine($"authURL={authUrl}");

        // Opens the authorization URL in the default web browser
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = authUrl,
            UseShellExecute = true
        });
    }

    private void StartServer(string redirectUri)
    {
        if (VERBOSE) System.Diagnostics.Debug.WriteLine("Starting local HTTP server to listen for authorization code...");
        if (httpListener != null)
        {
            httpListener.Stop();
        }
        else
        { 
            httpListener = new HttpListener();
            httpListener.Prefixes.Add($"http://localhost:{m_authPort}/");
        }

        httpListener.Start();

        Task.Run(() => ImplicitGrantWaitForCallback());
    }

    private void StopServer()
    {
        if (VERBOSE) System.Diagnostics.Debug.WriteLine("Stopping local HTTP server...");
        httpListener.Stop();
    }

    private async Task ImplicitGrantWaitForCallback()
    {
        if (VERBOSE) System.Diagnostics.Debug.WriteLine("[INFO] Waiting for Discord authentication...");

        while (true)
        {
            HttpListenerContext context = await httpListener.GetContextAsync();
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            if (request.Url.AbsolutePath == "/callback" && string.IsNullOrEmpty(request.QueryString["token"]))
            {
                // Serve the JavaScript callback page
                string htmlContent = GetCallbackHtml();
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(htmlContent);
                response.ContentLength64 = buffer.Length;
                response.ContentType = "text/html";
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            else if (request.QueryString["token"] != null)
            {
                // Process the token
                string accessToken = request.QueryString["token"];
                if (VERBOSE) System.Diagnostics.Debug.WriteLine($"[SUCCESS] Received access token: {accessToken}");

                // Send response to browser
                string successResponse = "<html>\r\n\r\n<body style=\"background-color:#202020;\\\">\r\n    <center>\r\n        <figure class=\"image image-style-align-left\\\"><img style=\"aspect-ratio:256/256;\" src=\"https://gcdn.thunderstore.io/live/repository/icons/oathorse-TrophyHuntMod-0.8.8.png.256x256_q95_crop.jpg\" width=\"256\\\" height=\"256\\\"></figure>\r\n        <p>&nbsp;</p>\r\n        <p><span style=\"color:#f0e080;font-size:22px;\"><strong>Congratulations! You've connected Discord to the TrophyHuntMod and have enabled online reporting!</p></strong></span>\r\n        \r\n        <p><span style=\"color:#e0e0e0;font-size:20px;\"><strong>Data reported by the mod can now be used in official Trophy Hunt Tournaments.</strong></span></p>\r\n        <p>&nbsp;</p>\r\n        <p><span style=\"color:#e0e0e0;font-size:22px;\"><strong>Only your Discord id and username are used, and not for anything but Trophy Hunt event leaderboards.</strong></span></p>\r\n        <p><span style=\"color:#e04040;font-size:20px;\"><strong>They will not be shared with anyone else.</strong></span></p>\r\n        <p>&nbsp;</p>\r\n        <p>&nbsp;</p>\r\n        <p><span style=\"color:#e0e0e0;font-size:24px;\\\"><strong>You can now close this window.</strong></span></p>\r\n    </center>\r\n</body>\r\n\r\n</html>";
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(successResponse);
                response.ContentLength64 = buffer.Length;
                response.ContentType = "text/html";
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.OutputStream.Close();

                StopServer();

                // Fetch user info
                await ImplicitGrantGetDiscordUserInfo(accessToken);

                m_statusCallback();

                break;
            }
        }
    }

    private async Task ImplicitGrantGetDiscordUserInfo(string accessToken)
    {
        using HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

        HttpResponseMessage response = await client.GetAsync("https://discord.com/api/users/@me");
        string responseBody = await response.Content.ReadAsStringAsync();

        if (VERBOSE) System.Diagnostics.Debug.WriteLine($"ResponseBody: {responseBody}");

        if (response.IsSuccessStatusCode)
        {
            m_userInfo = JsonConvert.DeserializeObject<DiscordUserResponse>(responseBody);
        }
        else
        {
            if (VERBOSE) System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to get user info: {responseBody}");
        }
    }

    private string GetCallbackHtml()
    {
        return $@"<!DOCTYPE html>
<html>
<head>
    <title>Discord Auth</title>
    <script>
        window.onload = function() {{
            const params = new URLSearchParams(window.location.hash.substr(1));
            const accessToken = params.get('access_token');
            if (accessToken) {{
                window.location.href = 'http://localhost:{m_authPort}/callback?token=' + encodeURIComponent(accessToken);
            }} else {{
                document.body.innerHTML = '<h2>Error: No access token found.</h2>';
            }}
        }};
    </script>
</head>
<body>
    <h2>Authenticating...</h2>
</body>
</html>";
    }
}

public class DiscordUserResponse
{
    public string id { get; set; }
    public string username { get; set; }
    public string discriminator { get; set; }
    public string avatar { get; set; }
    public string global_name { get; set; }

    public Sprite avatarSprite = null;
}

public class MainThreadDispatcher : MonoBehaviour
{
    private static MainThreadDispatcher _instance;
    private readonly Queue<Action> _actions = new Queue<Action>();

    public static MainThreadDispatcher Instance
    {
        get
        {
            if (_instance == null)
            {
                var obj = new GameObject("MainThreadDispatcher");
                _instance = obj.AddComponent<MainThreadDispatcher>();
                DontDestroyOnLoad(obj);
            }
            return _instance;
        }
    }

    private void Update()
    {
        lock (_actions)
        {
            while (_actions.Count > 0)
            {
                _actions.Dequeue()?.Invoke();
            }
        }
    }

    public void Enqueue(Action action)
    {
        if (action == null) return;
        lock (_actions)
        {
            _actions.Enqueue(action);
        }
    }
}