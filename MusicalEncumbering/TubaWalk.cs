using BepInEx;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace TubaWalk
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class EncumberedWalkSound : BaseUnityPlugin
    {
        public const string PluginGUID = "com.oathorse.Tuba";
        public const string PluginName = "Tuba Walk";
        public const string PluginVersion = "0.1.3";
        private readonly Harmony harmony = new Harmony(PluginGUID);

        private AudioSource audioSource;
        private AudioClip walkSound;
        private bool isPlaying;

        IEnumerator LoadAudio(string filePath)
        {
            string url = "file://" + filePath;

//            Debug.LogError("Loading OGG file: " + url);

            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.OGGVORBIS))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError("Error loading OGG file: " + www.error);
                }
                else
                {
                    walkSound = DownloadHandlerAudioClip.GetContent(www);
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.clip = walkSound;
                }
            }
        }

        void Awake()
        {
            string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            string filePath = assemblyFolder + @"\" + "fat-guy-tuba-song.ogg";
            StartCoroutine(LoadAudio(filePath));
            harmony.PatchAll();
        }

        void Update()
        {
            var player = Player.m_localPlayer; // Get the local player
            if (player == null) return;

            if (!audioSource)
            {
                return;
            }

            bool isEncumbered = player.IsEncumbered();
            Vector3 velocity = player.GetVelocity();
            Vector3 moveDir = player.GetMoveDir();

            bool isMoving = (velocity.magnitude > 0.1f && moveDir.magnitude > 0.1f);

//            Debug.LogWarning($"Player {player.name} {isMoving} {isEncumbered} {velocity} {moveDir}");

            if (isMoving && isEncumbered)
            {
                if (!isPlaying)
                {
//                    Debug.LogWarning("ME: Starting Encumbered Walking");
                    audioSource.Play();
                    isPlaying = true;
                }
            }
            else
            {
                if (isPlaying)
                {
//                    Debug.LogWarning("ME: Stopping Encumbered Walking");
                    audioSource.Stop();
                    isPlaying = false;
                }
            }
        }

        //[HarmonyPatch(typeof(SEMan), nameof(SEMan.AddStatusEffect), new[] { typeof(StatusEffect), typeof(bool), typeof(int), typeof(float) })]
        //public class SEMan_AddStatusEffect_Patch
        //{
        //    static void Postfix(SEMan __instance, StatusEffect statusEffect, bool resetTime, int itemLevel, float skillLevel)
        //    {
        //        Debug.LogWarning($"ME: SEMan.AddStatusEffect() {statusEffect.name}");
        //    }
        //}

        //[HarmonyPatch(typeof(SEMan), nameof(SEMan.RemoveStatusEffect), new[] { typeof(StatusEffect), typeof(bool)} )]
        //public class SEMan_RemoveStatusEffect_Patch
        //{
        //    static void Postfix(SEMan __instance, StatusEffect se, bool quiet)
        //    {
        //        Debug.LogWarning($"ME: SEMan.RemoveStatusEffect() {se.name}");
        //    }
        //}
    }
}
