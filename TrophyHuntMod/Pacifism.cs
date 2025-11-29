using BepInEx;
using static Terminal;
using UnityEngine;
using System.Collections.Generic;
using HarmonyLib;
using static Skills;
using System.Collections;
using static TrophyHuntMod.TrophyHuntMod;
using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using TMPro;
using static EnemyHud;
using UnityEngine.U2D;
using System.Text;
using System.IO;
using static Player;
using System.Security.Cryptography;
using static HitData;

namespace TrophyHuntMod
{
    public partial class TrophyHuntMod : BaseUnityPlugin
    {
        public static bool __m_charmTimerStarted = false;

        static List<CharmedCharacter> __m_allCharmedCharacters = new List<CharmedCharacter>();

        const float TROPHY_PACIFIST_CHARM_DURATION = 300; // seconds

        public const float cFollowDistance = 3.0f;
        public const float cRadiusScale = 2f;

        public const float PACIFIST_THRALL_PLAYER_TARGET_DISTANCE = 50.0f;

        public class GandrArrowData
        {
            public GandrArrowData(string arrowName, string ingredient, string name, string description, Type statusEffectType, GandrTypeIndex spriteIndex)
            {
                m_arrowName = arrowName;
                m_ingredient = ingredient;
                m_name = name;
                m_description = description;
                m_statusEffectType = statusEffectType;
                m_spriteIndex = spriteIndex;
            }

            public string m_arrowName = "";
            public string m_ingredient = "";
            public string m_name = "";
            public string m_description = "";
            public Type m_statusEffectType;
            public GandrTypeIndex m_spriteIndex;
        }

        public static GandrArrowData[] __m_gandrArrowData = new GandrArrowData[]
        {
            new GandrArrowData("ArrowWood", "Wood", "Wooden Gandr", "Enemy comes under your thrall.", null, GandrTypeIndex.Wood),        // Null SE means no Status Effect!
            new GandrArrowData("ArrowFlint", "Flint", "Flint Gandr", "Thrall becomes piercing.", typeof(SE_GandrFlint), GandrTypeIndex.Flint),
            new GandrArrowData("ArrowFire", "Resin", "Fire Gandr", "Thrall burns its enemies.", typeof(SE_GandrFire), GandrTypeIndex.Fire),
            new GandrArrowData("ArrowBronze", "Bronze", "Bronze Gandr", "Thrall becomes blunt.", typeof(SE_GandrBronze), GandrTypeIndex.Bronze),
            new GandrArrowData("ArrowPoison", "Ooze", "Poison Gandr", "Thrall becomes poisonous.", typeof(SE_GandrPoison), GandrTypeIndex.Poison),
            new GandrArrowData("ArrowIron", "Iron", "Iron Gandr", "Thrall becomes sharp.", typeof(SE_GandrIron), GandrTypeIndex.Iron),
            new GandrArrowData("ArrowFrost", "FreezeGland", "Frost Gandr", "Thrall becomes frosty.", typeof(SE_GandrFrost), GandrTypeIndex.Frost),
            new GandrArrowData("ArrowObsidian", "Obsidian", "Glass Gandr", "Thrall becomes very sharp.", typeof(SE_GandrObsidian), GandrTypeIndex.Obsidian),
            new GandrArrowData("ArrowSilver", "Silver", "Silver Gandr", "Thrall becomes spiritual.", typeof(SE_GandrSilver), GandrTypeIndex.Silver),
            new GandrArrowData("ArrowNeedle", "Needle", "Needle Gandr", "Thrall becomes very piercing.", typeof(SE_GandrNeedle), GandrTypeIndex.Needle),
            new GandrArrowData("ArrowCarapace", "Carapace", "Bug Gandr", "Thrall becomes very blunt.", typeof(SE_GandrCarapace), GandrTypeIndex.Carapace),
            new GandrArrowData("ArrowCharred", "Blackwood", "Charred Gandr", "Thrall charges with lightning.", typeof(SE_GandrCharred), GandrTypeIndex.Charred),
        };

        

        [HarmonyPatch(typeof(Character), nameof(Character.GetJogSpeedFactor))]
        public static class Character_GetJogSpeedFactor_Patch
        {
            public static void Postfix(Character __instance, ref float __result)
            {
                if (!IsPacifist())
                {
                    return;
                }
                Guid cGUID = GetGUIDFromCharacter(__instance);
                CharmedCharacter guy = __m_allCharmedCharacters.Find(c => c.m_charmGUID == cGUID);
                if (guy != null)
                {
                    __result = CHARMED_ENEMY_SPEED_MULTIPLIER; // Normal speed
                }
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.GetRunSpeedFactor))]
        public static class Character_GetRunSpeedFactor_Patch
        {
            public static void Postfix(Character __instance, ref float __result)
            {
                if (!IsPacifist())
                {
                    return;
                }

                Guid cGUID = GetGUIDFromCharacter(__instance);
                CharmedCharacter guy = __m_allCharmedCharacters.Find(c => c.m_charmGUID == cGUID);
                if (guy != null)
                {
                    __result = CHARMED_ENEMY_SPEED_MULTIPLIER; // Normal speed
                }
            }
        }

        [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
        public static class MonsterAI_UpdateAI_Patch
        {
            public static void Postfix(MonsterAI __instance, float dt)
            {
                if (!IsPacifist())
                {
                    return;
                }

                if (__instance == null || __instance.m_character == null)
                    return;

                if (!IsCharmed(__instance.m_character))
                {
                    return;
                }

                if (__instance.m_character == null)
                {
                    Debug.LogError("MonsterAI_UpdateAI_Patch: __instance.m_character is null!");
                    return;
                }

                if (Player.m_localPlayer == null)
                {
                    Debug.LogError("MonsterAI_UpdateAI_Patch: Player.m_localPlayer is null!");
                    return;
                }

                float playerDist = Vector3.Distance(Player.m_localPlayer.transform.position, __instance.m_character.transform.position);
                Character target = __instance.GetTargetCreature();
                if (target)
                {
                    float targetDist = Vector3.Distance(target.transform.position, __instance.m_character.transform.position);

                    //                    Debug.LogWarning($"MonsterAI.UpdateAI() {__instance.name} T: {target.name} distToTarget {targetDist} F: {__instance.GetFollowTarget().name} distToPlayer: {playerDist} ");

                    __instance.SetFollowTarget(Player.m_localPlayer.gameObject);
                }
            }
        }


        [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.Follow))]
        public static class BaseAI_Follow_Patch
        {
            public static bool Prefix(BaseAI __instance, GameObject go, float dt)
            {
                float distanceReduction = 0.0f;

                float num = Vector3.Distance(go.transform.position, __instance.m_character.transform.position);
                bool run = num > 10f;
                if (num < cFollowDistance + __instance.m_character.GetRadius() * cRadiusScale - distanceReduction)
                {
                    __instance.StopMoving();
                }
                else
                {
                    __instance.MoveTo(dt, go.transform.position, 0f, run);
                }

                return false;
            }
        }


        [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.FindEnemy))]
        public static class BaseAI_FindEnemy_Patch
        {
            public static bool Prefix(BaseAI __instance, ref Character __result)
            {
                if (!IsPacifist())
                {
                    return true;
                }

                if (!IsCharmed(__instance.m_character))
                {
                    return true;
                }
                Character localPlayer = Player.m_localPlayer;
                if (localPlayer == null)
                {
                    return true;
                }

                BaseAI me = __instance;

                List<Character> allCharacters = Character.GetAllCharacters();
                Character closestEnemy = null;
                float closestDist = 99999f;
                foreach (Character enemy in allCharacters)
                {
                    if (!BaseAI.IsEnemy(me.m_character, enemy) || enemy.IsDead() || enemy.m_aiSkipTarget)
                    {
                        continue;
                    }

                    BaseAI enemyAI = enemy.GetBaseAI();
                    if ((enemyAI == null || !enemyAI.IsSleeping()) && me.CanSenseTarget(enemy))
                    {
                        float meToEnemyDist = Vector3.Distance(enemy.transform.position, localPlayer.transform.position);
                        if (meToEnemyDist < closestDist || closestEnemy == null)
                        {
                            closestEnemy = enemy;
                            closestDist = meToEnemyDist;
                        }
                    }
                }
                if (closestEnemy == null && me.HuntPlayer())
                {
                    Player closestPlayer = Player.GetClosestPlayer(me.transform.position, 200f);
                    if (closestPlayer != null && (closestPlayer.InDebugFlyMode() || closestPlayer.InGhostMode()))
                    {
                        __result = null;
                        return false;
                    }

                    __result = closestPlayer;
                    return false;
                }

                if (closestDist > PACIFIST_THRALL_PLAYER_TARGET_DISTANCE)
                {
                    //                    Debug.LogWarning($"Thrall {__instance.m_character.name} wants to discard {closestEnemy?.name} at distance {closestDist}");
                    MonsterAI monster = __instance as MonsterAI;
                    if (monster)
                    {
                        monster.m_timeSinceSensedTargetCreature = 0;
                        monster.m_targetCreature = null;
                        monster.m_targetStatic = null;
                        monster.SetTargetInfo(ZDOID.None);
                    }
                    __result = null;
                    return false;
                }

                __result = closestEnemy;

                return false;
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.GiveDefaultItems))]
        public static class Humanoid_GiveDefaultItems_Patch
        {
            public static void Postfix(Humanoid __instance)
            {
                //                Debug.LogWarning($"Player_GiveDefaultItems_Patch Postfix called. Type: {__instance.GetType()}");

                if (!IsPacifist())
                {
                    return;
                }

                if (__instance == null)
                    return;

                if (__instance.GetType() == typeof(Player))
                {
                    Inventory inv = __instance.GetInventory();
                    if (inv != null)
                    {
                        if (!inv.HaveItem("$item_bow"))
                        {
                            GameObject prefab = ObjectDB.instance.GetItemPrefab("Bow");
                            if (prefab != null)
                            {
                                //                               Debug.LogWarning($"Giving Player a Bow");
                                __instance.GiveDefaultItem(prefab);
                            }
                            else
                            {
                                Debug.LogWarning($"Could not find Bow prefab in ObjectDB");
                            }
                        }
                    }
                }
            }
        }

        public static bool IsThrallArrow(string ammoName)
        {
//            Debug.LogWarning($"[IsThrallArrow] Checking ammo name: {ammoName}");

            if (ammoName.ToLower().Contains("Gandr".ToLower()))
            {
                return true;
            }

            return false;
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
        public static class Humanoid_StartAttack_Patch
        {
            public static bool Prefix(Humanoid __instance, Character target, bool secondaryAttack, bool __result)
            {
                if (!IsPacifist())
                {
                    return true;
                }

                if (__instance != null)
                {
                    if (__instance == Player.m_localPlayer)
                    {
                        //                        Debug.LogWarning($"[Humanoid_StartAttack_Patch] Target: {target?.name}");

                        // If we're using a bow and wood arrows, we allow this
                        ItemDrop.ItemData weapon = __instance.GetCurrentWeapon();
                        if (weapon != null && weapon.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow)
                        {
                            ItemDrop.ItemData ammo = __instance.GetAmmoItem();
                            if (ammo != null && IsThrallArrow(ammo.m_shared.m_name))
                            {
                                return true;
                            }
                        }
                        else
                        {
                            //                            Debug.LogWarning($"[Humanoid_StartAttack_Patch] Weapon: {weapon.m_shared.m_name} {weapon.m_shared.m_itemType} { weapon.m_shared.m_skillType }");

                            if (weapon.m_shared.m_skillType == SkillType.Axes ||
                                weapon.m_shared.m_skillType == SkillType.Pickaxes ||
                                weapon.m_shared.m_skillType == SkillType.WoodCutting ||
                                weapon.m_shared.m_skillType == SkillType.Unarmed)
                            {
                                return true;
                            }
                        }

                        __result = false;
                        return false;
                    }
                }
                return true;
            }
        }

        public class CharmedCharacter
        {
            // Data we store for the charmed guy
            public Guid m_charmGUID = Guid.Empty;
            public Minimap.PinData m_pin = null;
            public long m_charmExpireTime = 0;
            public Character.Faction m_originalFaction = Character.Faction.TrainingDummy;
            public float m_swimSpeed = 2f;
            public int m_charmLevel = 1;
        }

        public static float GetCharmDuration()
        {
            System.Random randomizer = new System.Random();
            //            float duration = TROPHY_PACIFIST_CHARM_DURATION + (float)randomizer.NextDouble() * TROPHY_PACIFIST_CHARM_DURATION / 4 - TROPHY_PACIFIST_CHARM_DURATION / 8;
            
            float duration = TROPHY_PACIFIST_CHARM_DURATION;

            //            Debug.LogWarning($"[GetCharmDuration] {duration} seconds.");

            return duration;
        }

        public static Guid GetGUIDFromCharacter(Character character)
        {
            if (character == null) return Guid.Empty;

            ZDO zdo = character.GetComponent<ZNetView>()?.GetZDO();
            if (zdo == null)
            {
                return Guid.Empty;
            }
            string guidString = zdo.GetString("CharmGUID");
            Guid cGUID = Guid.Empty;
            Guid.TryParse(guidString, out cGUID);

            return cGUID;
        }

        public static Character GetCharacterFromGUID(Guid guid)
        {
            List<Character> allCharacters = Character.GetAllCharacters();
            foreach (Character character in allCharacters)
            {
                ZDO zdo = character.GetComponent<ZNetView>()?.GetZDO();
                if (zdo == null)
                {
                    continue;
                }
                string guidString = zdo.GetString("CharmGUID");
                Guid cGUID = Guid.Empty;
                Guid.TryParse(guidString, out cGUID);

                if (cGUID == guid)
                {
                    return character;
                }
            }
            return null;
        }

        //[HarmonyPatch(typeof(ZNetView), nameof(ZNetView.Awake))]
        //public static class ZNetView_Awake_Patch
        //{
        //    public static void Postfix(ZNetView __instance)
        //    {
        //        if (!IsPacifist())
        //        {
        //            return;
        //        }

        //        Character character = __instance.GetComponent<Character>();
        //        if (character == null)
        //        {
        //            return;
        //        }

        //        Guid cGUID = GetGUIDFromCharacter(character);
        //        CharmedCharacter guy = __m_allCharmedCharacters.Find(c => c.m_charmGUID == cGUID);
        //        if (guy != null)
        //        {
        //            if (guy != null)
        //            {
        //                Debug.LogWarning($"ZNetView.Awake(): re-charming {__instance.GetComponent<Character>().name} {guy.m_charmGUID}");
        //                SetCharmedState(guy);
        //            }
        //            else
        //            {
        //                Debug.LogError($"ZNetView.Awake(): Unable to re-charm {__instance.GetComponent<Character>().name} {cGUID}");
        //            }
        //        }
        //    }
        //}

        public static void DoPacifistPostPlayerSpawnTasks()
        {
            if (!IsPacifist())
                return;

            if (!Player.m_localPlayer)
                return;

            if (__m_allCharmedCharacters.Count > 0 && Player.m_localPlayer.m_maxAdrenaline == 0)
            {
                Player.m_localPlayer.m_maxAdrenaline = 30;
                Player.m_localPlayer.m_adrenaline = 0;
            }

            foreach (var cc in __m_allCharmedCharacters)
            {
                if (cc != null)
                {
                    SetCharmedState(cc, false);
                }
            }
        }

        public static bool IsCharmed(Character character)
        {
            Guid cGUID = GetGUIDFromCharacter(character);
            CharmedCharacter guy = __m_allCharmedCharacters.Find(c => c.m_charmGUID == cGUID);

            bool result = guy != null;

            return result;
        }

        public static CharmedCharacter GetCharmedCharacter(Character character)
        {
            Guid cGUID = GetGUIDFromCharacter(character);
            CharmedCharacter guy = __m_allCharmedCharacters.Find(c => c.m_charmGUID == cGUID);

            return guy;
        }

        public static void AddToCharmedList(Character enemy, long duration)
        {
            // Create a data structure to track the guy we charmed
            CharmedCharacter cc = new CharmedCharacter();

            ZDO zdo = enemy.GetComponent<ZNetView>().GetZDO();
            Guid cGUID = System.Guid.NewGuid();
            zdo.Set("CharmGUID", cGUID.ToString());
            
            // Force ZDO to persist
            zdo.Persistent = true;

            cc.m_charmGUID = cGUID;
            cc.m_pin = Minimap.instance.AddPin(enemy.transform.position, Minimap.PinType.Icon3, "", false, false);
            cc.m_originalFaction = enemy.m_faction;
            cc.m_charmExpireTime = __m_charmTimerSeconds + duration;
            cc.m_swimSpeed = enemy.m_swimSpeed;

            __m_allCharmedCharacters.Add(cc);

            SetCharmedState(cc);
        }

        public static void RemoveFromCharmedList(Character enemy)
        {
            if (enemy == null)
            {
                return;
            }

            ZDO zdo = enemy.GetComponent<ZNetView>().GetZDO();
            string guidString = zdo.GetString("CharmGUID");
            Guid cGUID = Guid.Parse(guidString);
            
            RemoveGUIDFromCharmedList(cGUID);
        }

        public static void RemoveGUIDFromCharmedList(Guid guid)
        {
            CharmedCharacter cc = __m_allCharmedCharacters.Find(c => c.m_charmGUID == guid);
            if (cc != null)
            {
                Minimap.instance.RemovePin(cc.m_pin);
                __m_allCharmedCharacters.Remove(cc);
            }

            if (__m_allCharmedCharacters.Count < 1)
            {
                if (Player.m_localPlayer != null)
                {
                    Player.m_localPlayer.m_maxAdrenaline = 0;
                    Player.m_localPlayer.m_adrenaline = 0;
                }
            }
        }

        public static bool SetCharmedState(CharmedCharacter cc, bool playParticleEffect = true)
        {
            CharmedCharacter guy = __m_allCharmedCharacters.Find(c => c.m_charmGUID == cc.m_charmGUID);
            if (guy != null)
            {
                Character enemy = GetCharacterFromGUID(guy.m_charmGUID);
                if (enemy == null)
                {
                    Debug.LogWarning($"Unable to SetCharmedState for GUID {guy.m_charmGUID.ToString()} - character not found!");

                    return false;
                }

                // Change faction to player
                enemy.m_faction = Character.Faction.Players;

                enemy.m_swimSpeed *= 10;

                // Color enemy and/or play particles
                AddCharmEffect(enemy, playParticleEffect);

                var monsterAI = enemy.GetComponent<MonsterAI>();
                if (monsterAI)
                {
                    if (Player.m_localPlayer != null && Player.m_localPlayer.gameObject != null)
                    {
                        monsterAI.SetFollowTarget(Player.m_localPlayer.gameObject);
                    }

                    monsterAI.m_attackPlayerObjects = false;
                    monsterAI.m_fleeIfNotAlerted = false;
                    monsterAI.m_fleeIfLowHealth = 0;
                    monsterAI.m_afraidOfFire = false;
                    monsterAI.m_fleeIfHurtWhenTargetCantBeReached = false;
                    monsterAI.m_circleTargetInterval = 0;
                    monsterAI.m_character.m_group = "";
                    monsterAI.SetHuntPlayer(false);
                    monsterAI.SetTarget(null);
                    monsterAI.SetTargetInfo(ZDOID.None);
                }
            }

            return true;
        }

        public static void SetUncharmedState(CharmedCharacter cc)
        {
            Character enemy = GetCharacterFromGUID(cc.m_charmGUID);
            if (!enemy)
            {
                Debug.LogError($"Unable to SetUncharmedState for GUID {cc.m_charmGUID.ToString()} - character not found!");
                return;
            }

            RemoveCharmEffect(enemy);
            enemy.m_faction = cc.m_originalFaction;
            enemy.m_swimSpeed = cc.m_swimSpeed;

            var monsterAI = enemy.GetComponent<MonsterAI>();
            if (monsterAI)
            {
                monsterAI.SetFollowTarget(null);
                monsterAI.SetTarget(null);
            }
        }

        [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.Awake))]
        public static class MonsterAI_Awake_Patch
        {
            public static void Postfix(MonsterAI __instance)
            {
                if (!IsPacifist())
                {
                    return;
                }

                if (__instance == null || __instance.m_character == null)
                    return;

                //foreach (var cc in __m_allCharmedCharacters)
                //{
                //    Debug.Log("allCharmedCharacters: " + cc.m_charmGUID.ToString());
                //}

                Guid cGUID = GetGUIDFromCharacter(__instance.m_character);
                CharmedCharacter guy = __m_allCharmedCharacters.Find(c => c.m_charmGUID == cGUID);
                if (guy != null)
                {
//                    Debug.LogWarning($"MonsterAI.Awake(): re-charming {__instance.m_character.name} {guy.m_charmGUID}");
                    SetCharmedState(guy);
                }
                else
                {
//                    Debug.LogError($"MonsterAI.Awake(): Unable to re-charm {__instance.m_character.name} {cGUID}");

                }
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.OnDestroy))]
        public static class Character_OnDestroy_Patch
        {
            public static void Prefix(Character __instance)
            {
                if (!IsPacifist())
                {
                    return;
                }

                //               Debug.LogWarning($"OnDestroy: {__instance.name}");
            }
        }

        public static void StartCharmTimer()
        {
            if (__m_charmTimerStarted)
            {
                return;
            }

            __m_charmTimerStarted = true;

            __m_trophyHuntMod.StartCoroutine(CharmTimerUpdate());

            //            Debug.LogWarning("[StartCharmTimer] Started Charm Timer.");
        }

        public static void StopCharmTimer()
        {
            __m_charmTimerStarted = false;
        }


        public static long __m_charmTimerSeconds = 0;

        static IEnumerator CharmTimerUpdate()
        {
            while (__m_charmTimerStarted)
            {
                __m_charmTimerSeconds++;

                Debug.LogWarning($"Charm List: {__m_allCharmedCharacters.Count} Timer: {__m_charmTimerSeconds}");
                for (var i = 0; i < __m_allCharmedCharacters.Count; i++)
                {
                    var cc = __m_allCharmedCharacters[i];
                    Debug.LogWarning($"  Charm {i}: GUID {cc.m_charmGUID.ToString()} ExpireTime: {cc.m_charmExpireTime} Orig Faction: {cc.m_originalFaction}");

                    Character character = GetCharacterFromGUID(cc.m_charmGUID);
                    if (character != null)
                    {
                        foreach (var statusEffect in character.m_seman.GetStatusEffects())
                        {
                            Debug.LogWarning($"    Status Effect: {statusEffect.m_name} TTL: {statusEffect.m_ttl}");
                        }
                    }
                }

                // For all charmed characters
                CharmedCharacter toRemove = null;
                foreach (var cc in __m_allCharmedCharacters)
                {
                    if (Minimap.instance != null)
                    {
                        Minimap.instance.RemovePin(cc.m_pin);
                    }

                    if (__m_charmTimerSeconds >= cc.m_charmExpireTime)
                    {
                        toRemove = cc;
                        break;
                    }

                    Character target = GetCharacterFromGUID(cc.m_charmGUID);
                    if (target)
                    {
                        cc.m_pin = Minimap.instance.AddPin(target.transform.position, Minimap.PinType.Icon3, "", false, false);
                    }
                }

                if (toRemove != null)
                {
                    SetUncharmedState(toRemove);
                    if (Minimap.instance != null)
                    {
                        Minimap.instance.RemovePin(toRemove.m_pin);
                    }

                    Character target = GetCharacterFromGUID(toRemove.m_charmGUID);
                    if (target)
                    {
                        Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"{target?.GetHoverName()} is no longer yours.");
                    }
                    else
                    {
                        Debug.LogWarning($"Target to uncharm not found for Guid {toRemove.m_charmGUID.ToString()}");
                    }

                    RemoveFromCharmedList(target);
//                    __m_allCharmedCharacters.Remove(toRemove);
                }

                yield return new WaitForSeconds(1f);
            }
        }

        private static void AddCharmEffect(Character target, bool particleEffect = true)
        {
            // Simple visual cue — give the charmed enemy a blue tint glow
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                foreach (var mat in r.materials)
                {
                    if (mat.HasProperty("_Color"))
                        mat.color = new Color(0.0f, 0f, 0f, 1f);
                }
            }

            if (particleEffect)
            {
                // Use a built-in pink-ish particle, such as vfx_surtling_death or vfx_pickaxe_sparks
                // (replace with vfx_heart style prefab if your version has it)
                GameObject heartVFX = ZNetScene.instance.GetPrefab("fx_hen_love");
                if (heartVFX != null)
                {
                    //                Debug.LogWarning($"[heartVFX] {heartVFX.name}");

                    var fx = UnityEngine.Object.Instantiate(heartVFX, target.transform.position, Quaternion.identity);
                    var ps = fx.GetComponentInChildren<ParticleSystem>();
                    if (ps != null)
                    {
                        var main = ps.main;
                        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.5f, 0.7f, 1f));
                        ps.Play();
                    }
                }
                else
                {
                    Debug.LogWarning($"[heartVFX] not found.");
                }
            }
        }

        private static void RemoveCharmEffect(Character target)
        {
            // Reset color
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                foreach (var mat in r.materials)
                {
                    if (mat.HasProperty("_Color"))
                        mat.color = Color.white;
                }
            }

            //            Debug.LogWarning($"{target?.name} is no longer doing your bidding.");

        }

        [HarmonyPatch(typeof(Projectile), nameof(Projectile.OnHit))]
        public static class Projectile_OnHit_Patch
        {
            public static bool Prefix(Projectile __instance, Collider collider)
            {
                if (!IsPacifist())
                {
                    return true;
                }

                // Ensure it's a projectile owned by a player
                if (__instance == null)
                    return true;

                // Get the item that spawned the projectile
                var item = __instance.m_ammo;
                if (item == null || item.m_shared == null)
                    return true;

                // Only apply to wood arrows
                if (!IsThrallArrow(item.m_shared.m_name))
                    return true;

                // no damage when charming
                __instance.m_damage = new HitData.DamageTypes();

                return false;
            }

            public static void Postfix(Projectile __instance, Collider collider)
            {
                if (!IsPacifist())
                {
                    return;
                }

                try
                {
                    // Ensure it's a projectile owned by a player
                    if (__instance == null || __instance.m_owner == null)
                        return;

                    // Get the item that spawned the projectile
                    var item = __instance.m_ammo;
                    if (item == null || item.m_shared == null)
                        return;

                    string arrowName = item.m_shared.m_name;

                    //                    Debug.LogWarning($"[Projectile_OnHit_WoodArrowCharm] {__instance.m_ammo.m_shared.m_name} {collider.gameObject.name}");

                    // Only apply to wood arrows
                    if (!IsThrallArrow(arrowName))
                        return;
                    //                    Debug.LogWarning($"[Wood arrow!] Hit detected on {collider.gameObject.name}");

                    // Ensure we hit a Character
                    GameObject obj = Projectile.FindHitObject(collider);
                    if (obj == null)
                    {
                        Debug.LogWarning($"Unable find hit object for Projectile with collilder {collider.name}");
                        return;
                    }
                    Character hitChar = obj.GetComponent<Character>();
                    if (hitChar == null || hitChar.IsPlayer() || hitChar.IsDead())
                    {
                        Debug.LogWarning($"Invalid hit char.");

                        return;
                    }

                    if (hitChar.m_faction == Character.Faction.Boss)
                    {
                        Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"Bosses cannot be charmed.");

                        return;
                    }
                    //                    Debug.LogWarning($"Hit char {hitChar.name} of faction {hitChar.GetFaction()} and group {hitChar.m_group} nview {hitChar.m_nview}");

                    if (IsCharmed(hitChar))
                    {
                        CharmedCharacter cc = GetCharmedCharacter(hitChar);
                        if (cc != null)
                        {
                            cc.m_charmExpireTime = __m_charmTimerSeconds + (long)GetCharmDuration();
//                            Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"{hitChar.GetHoverName()} continues to serve.");
                        }
                        
                        //hitChar.m_seman.RemoveAllStatusEffects();
                        //RemoveFromCharmedList(hitChar);
                    }
                    else
                    {
                        AddToCharmedList(hitChar, (long)GetCharmDuration());
                    }

                    ApplyGandrEffect(arrowName, hitChar);


                    ZNetScene.instance.Destroy(__instance.gameObject);
                }

                catch (System.Exception ex)
                {
                    Debug.LogError($"[WoodArrowCharm] Error: {ex}");
                }
            }
        }

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        public static class ObjectDB_Awake_Patch
        {
            static public void ChangeArrow(ObjectDB db, string prefabName, string ingredient, string newName, string newDescription)
            {
                GameObject arrowPrefab = db.GetItemPrefab(prefabName);
                if (!arrowPrefab)
                {
                    Debug.LogError($"Could not find {prefabName}");
                    return;
                }

                ItemDrop itemDrop = arrowPrefab.GetComponent<ItemDrop>();
                if (!itemDrop)
                {
                    Debug.LogError($"Could not find ItemDrop on {prefabName}");
                    return;
                }

                itemDrop.m_itemData.m_shared.m_description = newDescription;
                itemDrop.m_itemData.m_shared.m_name = newName;

                Recipe vanillaRecipe = db.m_recipes.Find(r => r.name == $"Recipe_{prefabName}");
                if (vanillaRecipe)
                {
//                    Debug.LogWarning($"name: {vanillaRecipe.name} item: {vanillaRecipe?.m_item?.name} amount: {vanillaRecipe.m_amount} enabled: {vanillaRecipe.m_enabled} craftingStation = {vanillaRecipe.m_craftingStation?.m_name} repairStation = {vanillaRecipe.m_repairStation?.m_name}");
                }
                else
                {
                    Debug.LogError($"Could not find Recipe_{prefabName}");
                    return;
                }

                // TODO: Keep recipes the same, but create bigger stacks
                // make station type and level requirements stay the same as vanilla
                //
                vanillaRecipe.m_item = itemDrop;
                vanillaRecipe.m_amount = 100;
                vanillaRecipe.m_enabled = true;
                vanillaRecipe.m_craftingStation = null;
                vanillaRecipe.m_minStationLevel = 0;
                vanillaRecipe.m_resources = new Piece.Requirement[] {
                        new Piece.Requirement()
                        {
                            m_resItem = db.GetItemPrefab(ingredient).GetComponent<ItemDrop>(),
                            m_amount = 1,
                            m_amountPerLevel = 0
                        }
                    };
            }
            static void Postfix(ObjectDB __instance)
            {
//                Debug.LogWarning($"ObjectDB.Awake() called");
                if (!IsPacifist())
                    return;

                // See if the Database is initialized
                if (ObjectDB.instance != null &&
                    ObjectDB.instance.m_items.Count != 0
                    && ObjectDB.instance.GetItemPrefab("Amber") != null)
                {
//                    Debug.LogWarning($"ObjectDB available");

                    foreach (var arrowData in __m_gandrArrowData)
                    {
                        ChangeArrow(__instance, arrowData.m_arrowName, arrowData.m_ingredient, arrowData.m_name, arrowData.m_description);
                    }
                }
            }
        }

        public static void ApplyGandrEffect(string arrowName, Character hitChar)
        {
            if (hitChar == null)
                return;

            GandrArrowData arrowData = __m_gandrArrowData.First(a => arrowName.ToLower() == a.m_name.ToLower());
            if (arrowData == null)
            {
                Debug.LogError($"No GandrArrowData found for arrow {arrowName}");
                return;
            }

            // No status effect buff means just Charm
            if (arrowData.m_statusEffectType != null)
            {
                List<StatusEffect> statusEffects = hitChar.m_seman.GetStatusEffects();
                foreach (StatusEffect statusEffect in statusEffects)
                {
                    if (statusEffect.GetType() == arrowData.m_statusEffectType)
                    {
                        statusEffects.Remove(statusEffect);
                        break;
                    }
                }

                StatusEffect se = ScriptableObject.CreateInstance(arrowData.m_statusEffectType) as StatusEffect;

                se = hitChar.m_seman.AddStatusEffect(se, true);
                if (se != null)
                {
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"{arrowData.m_description}");
                }
                else
                {
                    Debug.LogError($"ApplyGandrEffect: Failed to add status effect {arrowData.m_statusEffectType} to {hitChar.name}");
                }
            }
            else
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"{hitChar.GetHoverName()} is under your thrall!");
            }
        }

        // Patching Charmed but unbuffed thralls to do slightly more and receive slightly less damage
        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyAttack))]
        public class SEMan_ModifyAttack_Patch
        {
            public static bool Prefix(SEMan __instance, SkillType skill, ref HitData hitData)
            {
                if (IsPacifist())
                {
                    if (IsCharmed(__instance.m_character) && __instance.GetStatusEffects().Count == 0)
                    {
                        CharmedCharacter cc = GetCharmedCharacter(__instance.m_character);
                        
                        hitData.ApplyModifier(1.0f + (cc.m_charmLevel / 10.0f));
                    }
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ApplyDamageMods))]
        public class SEMan_ApplyDamageMods_Patch
        {
            public static bool Prefix(SEMan __instance, ref DamageModifiers mods)
            {
                if (IsPacifist())
                {
                    if (IsCharmed(__instance.m_character) && __instance.GetStatusEffects().Count == 0)
                    {
                        CharmedCharacter cc = GetCharmedCharacter(__instance.m_character);
                        DamageModifier modifier = DamageModifier.Normal;
                        if (cc.m_charmLevel > 2) modifier = DamageModifier.SlightlyResistant;
                        if (cc.m_charmLevel > 5) modifier = DamageModifier.Resistant;
                        if (cc.m_charmLevel > 8) modifier = DamageModifier.VeryResistant;

                        List<DamageModPair> modifiers = new List<DamageModPair>();
                        modifiers.Add(new DamageModPair() { m_type = DamageType.Damage, m_modifier = modifier });  
                        mods.Apply(modifiers);
                    }
                }

                return true;
            }
        }



        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyAdrenaline))]
        public static class SEMan_ModifyAdrenaline_Patch
        {
            public static bool Prefix(SEMan __instance, float baseValue, ref float use)
            {
                if (!IsPacifist())
                {
                    return true;
                }

                if (__instance.m_character != Player.m_localPlayer)
                {
                    return true;
                }

                Player player = Player.m_localPlayer;

                if (use > 0)
                {
                    Debug.LogError($"ADRENALINE: {player.m_adrenaline}/{player.m_maxAdrenaline} + {use}");

                    if (player.m_adrenaline + use > player.m_maxAdrenaline)
                    {
                        bool trinketPopped = false;
                        StatusEffect trinketStatusEffect = null;
                        foreach (ItemDrop.ItemData item in player.GetInventory().GetAllItems())
                        {
                            if (item.m_equipped && item.m_shared.m_fullAdrenalineSE != null)
                            {
                                trinketPopped = true;
                                trinketStatusEffect = item.m_shared.m_fullAdrenalineSE;
                                break;
                            }
                        }

                        foreach (var cc in __m_allCharmedCharacters)
                        {
                            Character c = GetCharacterFromGUID(cc.m_charmGUID);
                            if (c)
                            {
                                c.Heal(c.GetMaxHealth() - c.GetHealth(), showText: true);
                                Debug.LogError($"ADRENALINE: Healed charmed enemy {c.name} to full health due to adrenaline overflow.");
                                if (trinketPopped)
                                {
                                    Debug.LogError($"TRINKET POPPED: {trinketStatusEffect.m_name}");

                                    player.m_adrenalinePopEffects.Create(c.transform.position, Quaternion.identity);

                                    StatusEffect activeSE = c.m_seman.GetStatusEffect(trinketStatusEffect.m_nameHash);
                                    if (activeSE)
                                    {
                                        activeSE.ResetTime();
                                    }
                                    else
                                    {
                                        c.m_seman.AddStatusEffect(trinketStatusEffect);
                                    }
                                }
                                else
                                {
                                    player.m_adrenaline = 0;
                                }

                                cc.m_charmLevel++;
                                Hud.instance.AdrenalineBarFlash();
                            }
                        }
                    }
                }

                return true;
            }
        }

        public class GandrSEData
        {
            public GandrSEData(Type type, float inflited, float received, float typedDamage, string name)
            {
                m_type = type;
                m_inflicted = inflited;
                m_received = received;
                m_typedDamage = typedDamage;
                m_name = name;
            }

            public Type m_type;
            public float m_inflicted;
            public float m_received;
            public float m_typedDamage;
            public string m_name;
        }

        public static GandrSEData[] __m_gandrSEData = new GandrSEData[]
        {
            //                                   inflicted   received  typedDamage   name
            new GandrSEData(typeof(SE_GandrFlint),      1.10f,  1.10f,   0.50f,    "Flint Gandr" ),  // Flint
            new GandrSEData(typeof(SE_GandrFire),       1.20f,  1.20f,   0.60f,    "Fire Gandr" ),  // Fire
            new GandrSEData(typeof(SE_GandrBronze),     1.30f,  1.30f,   0.70f,    "Bronze Gandr"  ),    // Bronze
            new GandrSEData(typeof(SE_GandrPoison),     1.40f,  1.40f,   0.80f,    "Poison Gandr"  ),    // Poison
            new GandrSEData(typeof(SE_GandrIron),       1.50f,  1.50f,   0.90f,    "Iron Gandr"  ),  // Iron
            new GandrSEData(typeof(SE_GandrFrost),      1.60f,  1.60f,   1.00f,    "Frost Gandr"  ),    // Frost
            new GandrSEData(typeof(SE_GandrObsidian),   1.70f,  1.70f,   1.10f,    "Glass Gandr"  ),   // Obsidian
            new GandrSEData(typeof(SE_GandrSilver),     1.80f,  1.80f,   1.20f,    "Silver Gandr"  ), // Silver
            new GandrSEData(typeof(SE_GandrNeedle),     1.90f,  1.90f,   1.30f,    "Needle Gandr"  ),   // Needle
            new GandrSEData(typeof(SE_GandrCarapace),   2.00f,  2.00f,   1.40f,    "Bug Gandr"  ),   // Carapace
            new GandrSEData(typeof(SE_GandrCharred),    2.50f,  2.50f,   1.50f,    "Charred Gandr"  ),   // Charred
        };


        public class SE_GandrEffect : SE_Stats
        {
            public float m_adrenalineScalar = 0.0f;
            public float m_baseDamageInflictedModifier = 1.0f;
            public float m_baseDamageReceivedModifier = 1.2f;
            public float m_charmLevel = 1.0f;
            public GandrSEData m_seData = null;

            public override void Setup(Character character)
            {
                base.Setup(character);

                // SE lasts for this many seconds
                m_ttl = 120.0f;
                m_name = "Base Gandr Effect";
                GandrArrowData data = __m_gandrArrowData.First(a => a.m_statusEffectType == this.GetType());
                if (data != null)
                {
                    m_icon = __m_cachedPacifistSprites[(int)data.m_spriteIndex].m_sprite;
                }

                GandrSEData seData = __m_gandrSEData.First(s => s.m_type == this.GetType());
                if (seData != null)
                {
                    m_baseDamageInflictedModifier = seData.m_inflicted;
                    m_baseDamageReceivedModifier = seData.m_received;

                    m_name = seData.m_name;
                    m_nameHash = m_name.GetStableHashCode();

                    m_seData = seData;
                }
            }
            public override void UpdateStatusEffect(float dt)
            { 
                base.UpdateStatusEffect(dt);

                CharmedCharacter cc = GetCharmedCharacter(m_character);
                if (cc != null)
                {
                    m_charmLevel = cc.m_charmLevel;
                }

                if (Player.m_localPlayer?.GetMaxAdrenaline() > 0)
                {
                    m_adrenalineScalar = Player.m_localPlayer.GetAdrenaline() / Player.m_localPlayer.GetMaxAdrenaline();
                }
                else 
                {
                    m_adrenalineScalar = 0.0f;
                }

//                Debug.LogWarning($"GandrEffect UpdateStatusEffect called. AdrenalineScalar: {m_adrenalineScalar}");
            }

            public override void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
            {
                Debug.Log($"{m_character.GetHoverName()} Modifiers: {m_percentigeDamageModifiers}");
                Debug.Log($"{m_character.GetHoverName()} Attack Pre Damage: {hitData.m_damage}");
                HitData.DamageTypes dt = m_percentigeDamageModifiers;

                float largestDamage = 0.0f;
                hitData.m_damage.GetMajorityDamageType(out largestDamage);
                dt.Modify(largestDamage);
                Debug.Log($"{m_character.GetHoverName()} Scaled Modifiers: {m_percentigeDamageModifiers}");
                hitData.m_damage.Add(dt);

                hitData.m_damage.Modify((Math.Max(1, m_charmLevel/2 + m_adrenalineScalar)) * m_baseDamageInflictedModifier);

                Debug.Log($"{m_character.GetHoverName()} Attack Post Damage: {hitData.m_damage} (CharmLevel: {m_charmLevel} Adrenaline Scalar: {m_adrenalineScalar} inflictedModifier: {m_baseDamageInflictedModifier}");

                base.ModifyAttack(skill, ref hitData);
            }

            public override void OnDamaged(HitData hit, Character attacker)
            {
                base.OnDamaged(hit, attacker);

                Debug.Log($"{m_character.GetHoverName()} Incoming Pre Damage: {hit.m_damage}");
                hit.m_damage.Modify(1 / ((Math.Max(1, m_charmLevel/2 + m_adrenalineScalar)) * m_baseDamageReceivedModifier));
                Debug.Log($"{m_character.GetHoverName()} Incoming Post Damage :{hit.m_damage} (CharmLevel: {m_charmLevel} Adrenaline Scalar: {m_adrenalineScalar} recievedModifier: {m_baseDamageReceivedModifier}");
            }

            //public override void ModifyAdrenaline(float baseValue, ref float use)
            //{
            //    use = baseValue * 100.0f;

            //    base.ModifyAdrenaline(baseValue, ref use);
            //}

            public override void Stop()
            {
                base.Stop();

                // Remove icon
            }
        }


        //public class SE_GandrWood : SE_GandrEffect
        //{
        //    public override void Setup(Character character)
        //    {
        //        base.Setup(character);
        //        m_baseDamageInflictedModifier = 2.0f;
        //        m_baseDamageReceivedModifier = 3.0f;
        //        m_percentigeDamageModifiers.m_damage = 2f;
        //        m_name = "Wood Gandr";
        //    }
        //}
        public class SE_GandrFlint : SE_GandrEffect
        {
            public override void Setup(Character character)
            {
                base.Setup(character);
                m_percentigeDamageModifiers.m_pierce = m_seData.m_typedDamage;
                
            }
        }
        public class SE_GandrFire : SE_GandrEffect
        {
            public override void Setup(Character character)
            {
                base.Setup(character);
                m_percentigeDamageModifiers.m_fire = m_seData.m_typedDamage;
            }
        }

        public class SE_GandrBronze : SE_GandrEffect
        {
            public override void Setup(Character character)
            {
                base.Setup(character);
                m_percentigeDamageModifiers.m_blunt = m_seData.m_typedDamage;
            }
        }

        public class SE_GandrPoison : SE_GandrEffect
        {
            public override void Setup(Character character)
            {
                base.Setup(character);
                m_percentigeDamageModifiers.m_poison = m_seData.m_typedDamage;
            }
        }
        public class SE_GandrIron : SE_GandrEffect
        {
            public override void Setup(Character character)
            {
                base.Setup(character);
                m_percentigeDamageModifiers.m_slash = m_seData.m_typedDamage;
            }
        }
        public class SE_GandrFrost : SE_GandrEffect
        {
            public override void Setup(Character character)
            {
                base.Setup(character);
                m_percentigeDamageModifiers.m_frost = m_seData.m_typedDamage;
            }
        }
        public class SE_GandrObsidian : SE_GandrEffect
        {
            public override void Setup(Character character)
            {
                base.Setup(character);
                m_percentigeDamageModifiers.m_slash = m_seData.m_typedDamage;
            }
        }
        public class SE_GandrSilver : SE_GandrEffect
        {
            public override void Setup(Character character)
            {
                base.Setup(character);
                m_percentigeDamageModifiers.m_spirit = m_seData.m_typedDamage;
            }
        }
        public class SE_GandrNeedle : SE_GandrEffect
        {
            public override void Setup(Character character)
            {
                base.Setup(character);
                m_percentigeDamageModifiers.m_pierce = m_seData.m_typedDamage;
            }
        }
        public class SE_GandrCarapace : SE_GandrEffect
        {
            public override void Setup(Character character)
            {
                base.Setup(character);
                m_percentigeDamageModifiers.m_blunt = m_seData.m_typedDamage;
            }
        }
        public class SE_GandrCharred : SE_GandrEffect
        {
            public override void Setup(Character character)
            {
                base.Setup(character);
                m_percentigeDamageModifiers.m_lightning = m_seData.m_typedDamage;
            }
        }

        [HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.UpdateHuds))]
        public class EnemyHud_UpdateHud_Patch
        {
            public static void Postfix(EnemyHud __instance, Player player, Sadle sadle, float dt)
            {
                if (!IsPacifist())
                {
                    return;
                }

                foreach (KeyValuePair<Character, EnemyHud.HudData> hudData in __instance.m_huds)
                {
                    Character c = hudData.Key;
                    if (c == null)
                        continue;

                    // Get the root hud element for the Charm HUD
                    EnemyHud.HudData data = hudData.Value;
                    Transform charmHudRootTransform = data.m_gui.transform.Find("Charm");

                    // Get the hud elements for charm icons we've got to use
                    List<GameObject> enemyHudElements = new List<GameObject>();
                    for (int i = 0; i < MAX_NUM_CHARM_ICONS; i++)
                    {
                        GameObject hudElement = data?.m_gui?.transform.Find($"Charm/CharmIcon{i}")?.gameObject;
                        if (hudElement)
                        {
                            hudElement.SetActive(false);
                            enemyHudElements.Add(hudElement);
                        }
                    }

                    // Hide the Charm hud
                    CharmedCharacter cc = GetCharmedCharacter(c);
                    if (cc == null)
                    {
                        // Hide the icons
                        if (charmHudRootTransform && charmHudRootTransform.gameObject.activeSelf)
                        {
                            charmHudRootTransform.gameObject.SetActive(false);
                        }
                        continue;
                    }
                    
                    // Unless they're charmed, then show them
                    if (IsCharmed(c))
                    {
                        if (charmHudRootTransform && !charmHudRootTransform.gameObject.activeSelf)
                        {
                            // Show the Charm hud
                            charmHudRootTransform.gameObject.SetActive(true);
                        }

                        // Get the UI objects to display the sprite icons
                        int iconElementIndex = 0;

                        List<StatusEffect> statusEffectList = c.m_seman.m_statusEffects;

                        // For each sprite in the sprite list, set up a hud icon and position it
                        foreach (var se in statusEffectList)
                        {
                            GameObject iconElement = enemyHudElements[iconElementIndex];

                            iconElement.gameObject.SetActive(true);

                            UnityEngine.UI.Image image = iconElement.GetComponent<UnityEngine.UI.Image>();
                            image.sprite = se.m_icon;

                            RectTransform rectTransform = iconElement.GetComponent<RectTransform>();
                            int iconSize = 28;
                            rectTransform.sizeDelta = new Vector2(iconSize, iconSize);
                            rectTransform.localScale = Vector3.one;
                            float iconPadding = 1;
                            float leftJustify = ((iconSize + iconPadding) * (statusEffectList.Count - 1)) / 2;
                            float iconOffset = (iconSize + iconPadding) * iconElementIndex;
                            rectTransform.anchoredPosition = new Vector2( -leftJustify + iconOffset, -iconSize/2 - 1);

                            iconElementIndex++;
                        }

                        // Update the pink Charm bar 
                        GuiBar charmBar = data.m_gui.transform.Find("Charm/CharmBar").GetComponent<GuiBar>();
                        float remainingTime = cc.m_charmExpireTime - __m_charmTimerSeconds;
                        //                        Debug.Log($"Bar {c.name} {remainingTime}/{GetCharmDuration()} {charmBar.m_value}/{charmBar.m_maxValue}\n  wid:{charmBar.m_width} smoothVal:{charmBar.m_smoothValue} smoothSpeed:{charmBar.m_smoothSpeed}");
                        charmBar.SetWidth(100.0f);
                        charmBar.SetValue(remainingTime / GetCharmDuration());
                        charmBar.SetColor(new Color((float)0xF3 / 255f, (float)0x87 / 255f, (float)0xC5 / 255f));

                        GameObject textElement = data.m_gui.transform.Find($"Charm/CharmLevelText").gameObject;
                        TextMeshProUGUI tm = textElement.GetComponent<TextMeshProUGUI>();
                        tm.text = $"{cc.m_charmLevel}";
                    }
                }
            }
        }

        public static int MAX_NUM_CHARM_ICONS = 13;

        public static void CreateCharmIconsInMasterHud(Transform parentTransform)
        {
            for (int i=0; i< MAX_NUM_CHARM_ICONS; i++)
            {

                GameObject charmIconElement = new GameObject($"CharmIcon{i}");
                charmIconElement.transform.SetParent(parentTransform);

                // Add RectTransform component for positioning
                RectTransform rectTransform = charmIconElement.AddComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(32, 32);
                rectTransform.anchoredPosition = new Vector2(0, 0); // Set position
                rectTransform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

                // Add an Image component
                UnityEngine.UI.Image image = charmIconElement.AddComponent<UnityEngine.UI.Image>();
                image.color = Color.white;
                image.raycastTarget = false;
            }
        }

        [HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.Awake))]
        public class EnemyHud_Awake_Patch
        {
            public static void Postfix(EnemyHud __instance)
            {
                Transform healthTransform = __instance.m_baseHud.transform.Find("Health");
                GameObject healthObject = healthTransform.gameObject;

                GameObject newBarObject = UnityEngine.Object.Instantiate(healthObject, healthTransform.parent);
                newBarObject.name = "Charm";
                newBarObject.SetActive(false);

                Transform newTransform = newBarObject.transform;
                newTransform.localPosition += new Vector3(0, -10, 0);

                Transform healthFastTransform = newTransform.Find("health_fast");
                if (healthFastTransform != null)
                {
                    GameObject.Destroy(healthFastTransform.gameObject);
                }
                Transform healthSlowTransform = newTransform.Find("health_slow");
                if (healthSlowTransform != null)
                {
                    GameObject.Destroy(healthSlowTransform.gameObject);
                }

                Transform healthFastFriendlyTransform = newTransform.Find("health_fast_friendly");
                healthFastFriendlyTransform.name = "CharmBar";
                GuiBar bar = healthFastFriendlyTransform?.gameObject?.GetComponent<GuiBar>();

                CreateCharmIconsInMasterHud(newBarObject.transform);

                GameObject charmLevelTextElement = new GameObject("CharmLevelText");
                charmLevelTextElement.transform.SetParent(newBarObject.transform);
                RectTransform rectTransform = charmLevelTextElement.AddComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(20, 20);
                rectTransform.localScale = Vector3.one;
                rectTransform.localPosition = Vector2.zero;

                TMPro.TextMeshProUGUI tmText = AddTextMeshProComponent(charmLevelTextElement);
                tmText.text = "X";
                tmText.fontSize = 13;
                tmText.fontStyle = FontStyles.Bold;
                tmText.color = Color.white;
                tmText.raycastTarget = false;
                tmText.fontMaterial.EnableKeyword("OUTLINE_ON");
                tmText.outlineColor = Color.black;
                tmText.outlineWidth = 0.125f; // Adjust the thickness
                tmText.verticalAlignment = VerticalAlignmentOptions.Middle;
                tmText.horizontalAlignment = HorizontalAlignmentOptions.Center;



                //                Transform previous = charmIconElement2.transform;
                //                foreach (var tsid in __m_thrallStatusIcons)
                //                {
                //                    GameObject icon = new GameObject("asdfasdf");
                //                    icon.transform.SetParent(previous);

                //                    // Add RectTransform component for positioning
                //                    rectTransform = icon.AddComponent<RectTransform>();
                //                    rectTransform.sizeDelta = new Vector2(20, 20);
                //                    rectTransform.anchoredPosition = new Vector2(9, -16); // Set position
                //                    rectTransform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

                //                    // Add an Image component
                //                    image = icon.AddComponent<UnityEngine.UI.Image>();
                //                    image.sprite = GetTrophySprite(tsid.m_prefabName);
                ////                    image.color = Color.white;
                //                    image.raycastTarget = false;

                //                    previous = icon.transform;
                //                }
            }
        }

        public class CachedSprite
        {
            public CachedSprite(GandrTypeIndex index, string name, Sprite sprite)
            {
                m_index = index;
                m_name = name;
                m_sprite = sprite;
            }
            
            public GandrTypeIndex m_index;
            public string m_name;
            public Sprite m_sprite;
        }

        public enum GandrTypeIndex
        {
            Wood,
            Flint,
            Fire,
            Bronze,
            Poison,
            Iron,
            Frost,
            Obsidian,
            Silver,
            Needle,
            Carapace,
            Charred,
        }

        static CachedSprite[] __m_cachedPacifistSprites = new CachedSprite[]
        {
            new CachedSprite(GandrTypeIndex.Wood, "T_emote_flex", null),
            new CachedSprite(GandrTypeIndex.Flint, "SpearFlint", null),
            new CachedSprite(GandrTypeIndex.Fire, "Burning", null),
            new CachedSprite(GandrTypeIndex.Bronze, "MaceBronze", null),
            new CachedSprite(GandrTypeIndex.Poison, "Poison", null),
            new CachedSprite(GandrTypeIndex.Iron, "SwordIron", null),
            new CachedSprite(GandrTypeIndex.Frost, "Frost", null),
            new CachedSprite(GandrTypeIndex.Obsidian, "ArrowObsidian", null),
            new CachedSprite(GandrTypeIndex.Silver, "SwordMistwalker", null),
            new CachedSprite(GandrTypeIndex.Needle, "needle", null),
            new CachedSprite(GandrTypeIndex.Carapace, "MaceIron", null),
            new CachedSprite(GandrTypeIndex.Charred, "Lightning", null),
        };

        public static void CacheSprites()
        {
            foreach (CachedSprite cs in __m_cachedPacifistSprites)
            {
                if (cs.m_sprite == null)
                {
                    cs.m_sprite = GetSpriteFromAtlas(cs.m_name, "IconAtlas");
                }
            }
        }

        public static Sprite LoadSpriteByName(string spriteName)
        {
            Sprite sprite = null;

            var sprites = Resources.FindObjectsOfTypeAll<Sprite>();

            sprite = sprites.FirstOrDefault(s => s.name == spriteName);

            return sprite;
        }
  
        public static Sprite GetSpriteFromAtlas(string spriteName, string atlasName)
        {
            var atlases = Resources.FindObjectsOfTypeAll<SpriteAtlas>();
            foreach (var atlas in atlases)
            {
                if (atlas.name != atlasName)
                    continue;

                if (atlas.GetSprite(spriteName) is Sprite s)
                {
//                    Debug.LogWarning($"Found {spriteName} in atlas {atlasName}.");
                    return s;
                }
            }
            return null;
        }

        /*

        public static void DumpSprites()
        {
            DumpAtlas("IconAtlas");
        }
        
        private static SpriteAtlas FindAtlas(string name)
        {
            foreach (var atlas in Resources.FindObjectsOfTypeAll<SpriteAtlas>())
            {
                if (atlas.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                    return atlas;
            }
            return null;
        }

        private static void DumpAtlas(string atlasName)
        {
            SpriteAtlas atlas = FindAtlas(atlasName);
            if (atlas == null)
            {
                Debug.LogError($"SpriteAtlas '{atlasName}' not found.");
                return;
            }

            int spriteCount = atlas.spriteCount;
            Sprite[] sprites = new Sprite[spriteCount];
            atlas.GetSprites(sprites);

            string outDir = Path.Combine("C:\\dev\\", "SpriteDump", atlasName);
            Directory.CreateDirectory(outDir);

            Debug.LogWarning($"Dumping {spriteCount} sprites from atlas '{atlasName}' → {outDir}");

            int exported = 0;

            // Cache one RT per source texture to avoid reallocating for every sprite
            var rtCache = new Dictionary<Texture2D, RenderTexture>();

            foreach (var sprite in sprites)
            {
                if (sprite == null)
                    continue;

                try
                {
                    // create or reuse RT for this sprite's source texture
                    Texture2D sourceTex = sprite.texture;

                    if (sourceTex == null)
                    {
                        Debug.LogWarning($"Sprite {sprite.name} has no texture, skipping.");
                        continue;
                    }

                    if (!rtCache.ContainsKey(sourceTex))
                    {
                        // Create an RT that exactly matches the source texture size
                        RenderTexture rt = RenderTexture.GetTemporary(
                            sourceTex.width,
                            sourceTex.height,
                            0,
                            RenderTextureFormat.ARGB32,
                            RenderTextureReadWrite.sRGB);

                        rt.wrapMode = TextureWrapMode.Clamp;
                        rt.filterMode = FilterMode.Point;

                        // Blit the source texture into the RT once
                        // Note: Graphics.Blit accepts a Texture, not just RenderTexture
                        Graphics.Blit(sourceTex, rt);

                        rtCache[sourceTex] = rt;
                    }

                    RenderTexture cachedRT = rtCache[sourceTex];
                    // Extract only the sprite rectangle from the cached RT
                    DumpSpriteFromCachedRT(sprite, cachedRT, outDir);
                    exported++;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Error dumping {sprite.name}: {ex}");
                }
            }

            // Release cached RTs
            foreach (var kv in rtCache)
            {
                RenderTexture.ReleaseTemporary(kv.Value);
            }

            Debug.LogWarning($"Finished dumping atlas '{atlasName}'. {exported}/{spriteCount} sprites exported.");
        }

        private static void DumpSpriteFromCachedRT(Sprite sprite, RenderTexture atlasRT, string folder)
        {
            Rect r = sprite.textureRect;
            int x = Mathf.RoundToInt(r.x);
            int y = Mathf.RoundToInt(r.y);
            int w = Mathf.RoundToInt(r.width);
            int h = Mathf.RoundToInt(r.height);

            if (w == 0 || h == 0)
            {
                Debug.LogWarning($"Sprite {sprite.name} has zero size, skipping.");
                return;
            }

            // Activate the atlas RT and read the sprite rect out of it
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = atlasRT;

            // Create a Texture2D to hold the sprite pixels
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            // ReadPixels uses bottom-left origin, and sprite.textureRect uses pixel coordinates in the same space,
            // so this rect should be correct.
            tex.ReadPixels(new Rect(x, y, w, h), 0, 0, false);
            tex.Apply();

            RenderTexture.active = prev;

            // Encode and write
            byte[] png = tex.EncodeToPNG();
            UnityEngine.Object.Destroy(tex);

            string filename = SafeFilename(sprite.name) + ".png";
            string path = Path.Combine(folder, filename);
            File.WriteAllBytes(path, png);
        }

        private static string SafeFilename(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
        */
    }
}
