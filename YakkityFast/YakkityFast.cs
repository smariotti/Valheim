using BepInEx;
using HarmonyLib;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using BepInEx.Configuration;
using static Terminal;
using Splatform;
using System.Runtime.InteropServices.WindowsRuntime;

namespace YakkityFast
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class YakkityFast : BaseUnityPlugin
    {
        public const string PluginGUID = "com.oathorse.Yakkity";
        public const string PluginName = "Yakkity Fast";
        public const string PluginVersion = "0.1.1";
        private readonly Harmony harmony = new Harmony(PluginGUID);

        private ConfigEntry<float> m_minSpeed;
        private ConfigEntry<float> m_maxSeconds;
        private ConfigEntry<float> m_fadeDelay;
        private ConfigEntry<float> m_fadeRate;
        private ConfigEntry<float> m_startGracePeriod;
        private ConfigEntry<float> m_stopGracePeriod;

        private AudioSource m_audioSource;
        private AudioClip m_sound;
        private bool m_isPlaying = false;

        private float m_elapsedSeconds = 0.0f;
        private float m_gracePeriodTimer = 0.0f;
        private bool m_disabled = false;

        IEnumerator LoadAudio(string filePath)
        {
            string url = "file://" + filePath;

            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.OGGVORBIS))
            {
                DebugLog("Loading: '" + www.url + "'");
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError("Error loading OGG file: result: " + www.result + " error: "+ www.error);
                }
                else
                {
                    m_sound = DownloadHandlerAudioClip.GetContent(www);
                    m_audioSource = gameObject.AddComponent<AudioSource>();
                    m_audioSource.clip = m_sound;
                }
            }
        }

        const float DEFAULT_FADE_DELAY = 60.0f;
        const float DEFAULT_MAX_SECONDS = 120.0f;

        void DebugLog(string message) 
        {
            //Debug.LogWarning(message);
        }
        void AddConsoleCommands()
        {
            {
                ConsoleCommand trophyHuntCommand = new ConsoleCommand("yakkity", "Controls Yakkity Fast", delegate (ConsoleEventArgs args)
                {
                    if (!Game.instance)
                    {
                        return true;
                    }

                    if (args.Length > 1)
                    {
                        string arg = args[1];
                        if (arg == "unleashed")
                        {
                            m_fadeDelay.Value = 0.0f;
                            m_maxSeconds.Value = 0.0f;
                            m_disabled = false;
                            Config.Save();
                        }
                        else if (arg == "default")
                        {
                            m_fadeDelay.Value = DEFAULT_FADE_DELAY;
                            m_maxSeconds.Value = DEFAULT_MAX_SECONDS;
                            m_disabled = false;
                            Config.Save();
                        }
                        else if (arg == "off")
                        {
                            m_disabled = true;
                        }
                        else if (arg == "on")
                        {
                            m_disabled = false;
                        }

                    }
                    return true;
                });
            }
        }
                
        void Awake()
        {
            string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            string filePath = assemblyFolder + @"\" + "YakkitySax.ogg";
            StartCoroutine(LoadAudio(filePath));
            harmony.PatchAll();

            AddConsoleCommands();

            m_minSpeed = Config.Bind("General", "Minimum Speed", 10.5f, "The minimum velocity where the audio will start playing");
            m_maxSeconds = Config.Bind("General", "Maximum Seconds", 0.0f, "The maximum number of seconds of audio that will ever play, 0 for no limit");
            m_fadeDelay = Config.Bind("General", "Fade Delay", 0.0f, "The number of seconds before audio fades");
            m_fadeRate = Config.Bind("General", "Fade Rate", 0.1f, "The rate at which audio will fade once playing");
            m_startGracePeriod = Config.Bind("General", "Start Grace Period", 1.0f, "Number of seconds velocity must be below Minimum Speed for music to Start");
            m_stopGracePeriod = Config.Bind("General", "Stop Grace Period", 3.0f, "Number of seconds velocity must be below Minimum Speed for music to Stop");
        }

        void Update()
        {
            var player = Player.m_localPlayer; // Get the local player
            if (player == null) return;

            if (!m_audioSource)
            {
                return;
            }

            if (m_disabled)
            {
                if (m_isPlaying)
                {
                    m_audioSource.Stop();
                    m_isPlaying = false;
                }
                return;
            }

            Vector3 velocity = player.GetVelocity();
            velocity.y = 0.0f; // Ignore vertical velocity
            bool isMoving = (velocity.magnitude > m_minSpeed.Value);

            //            Debug.LogWarning($"Player {player.name} {isMoving} {velocity}");

            if (isMoving)
            {
                if (!m_isPlaying)
                {
                    m_gracePeriodTimer -= Time.deltaTime;
                    if (m_gracePeriodTimer <= 0.0f)
                    {
                        Debug.LogWarning("YF: Starting music");
                        m_audioSource.Play();
                        m_isPlaying = true;
                        m_elapsedSeconds = 0.0f;
                        m_gracePeriodTimer = m_stopGracePeriod.Value;
                        m_audioSource.volume = 1.0f;
                    }
                    else
                    {
                        DebugLog($"YF: Start Audio m_gracePeriodTimer: {m_gracePeriodTimer}");
                    }
                }
                else
                {
                    m_elapsedSeconds += Time.deltaTime;
                    if (m_fadeDelay.Value > 0.0f && m_elapsedSeconds > m_fadeDelay.Value)
                    {
                        DebugLog("YF: Fading, volume =" + m_audioSource.volume);
                        float fadeAmount = m_fadeRate.Value * Time.deltaTime;
                        m_audioSource.volume -= fadeAmount;
                        m_audioSource.volume = Mathf.Clamp(m_audioSource.volume, 0.0f, 1.0f);
                    }

                    if (m_maxSeconds.Value > 0.0f && m_elapsedSeconds > m_maxSeconds.Value)
                    {
                        DebugLog("YF: MaxSeconds Elapsed, Stopping");
                        m_audioSource.Stop();
                        m_isPlaying = false;
                    }

                    m_gracePeriodTimer = m_stopGracePeriod.Value;
                }
            }
            else
            {
                if (m_isPlaying)
                {
                    m_gracePeriodTimer -= Time.deltaTime;
                    if (m_gracePeriodTimer <= 0.0f)
                    {
                        DebugLog("YF: Grace Period elapsed, Stopping");
                        m_audioSource.Stop();
                        m_isPlaying = false;
                        m_gracePeriodTimer = m_startGracePeriod.Value;
                    }
                    else
                    {
                        DebugLog($"YF: Stop Audio m_gracePeriodTimer: {m_gracePeriodTimer}");
                        m_audioSource.volume = m_gracePeriodTimer / m_stopGracePeriod.Value;
                    }
                }
                else
                {
                    m_gracePeriodTimer = m_startGracePeriod.Value;
                }
            }
        }
    }
}
