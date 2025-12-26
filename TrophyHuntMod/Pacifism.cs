using BepInEx;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using static Skills;

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

        public static bool __m_showCharmList = false;

        public static Color __m_pinkColor = new Color(0.95f, 0.53f, 0.77f);

        public const int MAX_NUM_THRALLS = 5;

        public const bool LOG_DAMAGE = false;
        public static void LogDamage(string logEntry)
        {
            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DamageLog.csv");

            // Append the message to the file
            File.AppendAllText(logFilePath, logEntry+"\n");
        }

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
            new GandrArrowData("ArrowWood", "Wood", "Wooden Gandr", " is under your thrall.", null, GandrTypeIndex.Wood),        // Null SE means no Status Effect!
            new GandrArrowData("ArrowFlint", "Flint", "Flint Gandr", " thrall is piercing.", typeof(SE_GandrFlint), GandrTypeIndex.Flint),
            new GandrArrowData("ArrowFire", "Resin", "Fire Gandr", " thrall channels fire.", typeof(SE_GandrFire), GandrTypeIndex.Fire),
            new GandrArrowData("ArrowBronze", "Bronze", "Bronze Gandr", " thrall is blunt.", typeof(SE_GandrBronze), GandrTypeIndex.Bronze),
            new GandrArrowData("ArrowPoison", "Ooze", "Poison Gandr", " thrall channels poison.", typeof(SE_GandrPoison), GandrTypeIndex.Poison),
            new GandrArrowData("ArrowIron", "Iron", "Iron Gandr", " thrall is sharp.", typeof(SE_GandrIron), GandrTypeIndex.Iron),
            new GandrArrowData("ArrowFrost", "FreezeGland", "Frost Gandr", " thrall channels frost.", typeof(SE_GandrFrost), GandrTypeIndex.Frost),
            new GandrArrowData("ArrowObsidian", "Obsidian", "Glass Gandr", " thrall is very sharp.", typeof(SE_GandrObsidian), GandrTypeIndex.Obsidian),
            new GandrArrowData("ArrowSilver", "Silver", "Silver Gandr", " thrall channels spirit.", typeof(SE_GandrSilver), GandrTypeIndex.Silver),
            new GandrArrowData("ArrowNeedle", "Needle", "Needle Gandr", " thrall is very piercing.", typeof(SE_GandrNeedle), GandrTypeIndex.Needle),
            new GandrArrowData("ArrowCarapace", "Carapace", "Bug Gandr", " thrall is very blunt.", typeof(SE_GandrCarapace), GandrTypeIndex.Carapace),
            new GandrArrowData("ArrowCharred", "Blackwood", "Charred Gandr", " thrall channels lightning.", typeof(SE_GandrCharred), GandrTypeIndex.Charred),
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
                    //                    Debug.LogError("MonsterAI_UpdateAI_Patch: __instance.m_character is null!");
                    return;
                }

                if (Player.m_localPlayer == null)
                {
                    //                    Debug.LogError("MonsterAI_UpdateAI_Patch: Player.m_localPlayer is null!");
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

                // Temporary bump up sense ranges for this enemy
                float oldHearRange = me.m_hearRange;
                float oldSightRange = me.m_viewRange;

                me.m_hearRange *= 1.25f;
                me.m_viewRange *= 1.25f;

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

                me.m_hearRange = oldHearRange;
                me.m_viewRange = oldSightRange;

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

                if (closestEnemy == null)
                {
                    MonsterAI monster = __instance as MonsterAI;

                    if (monster)
                    {
                        monster.m_targetStatic = monster.FindClosestStaticPriorityTarget();
                        if (monster.m_targetStatic == null)
                        {
                            monster.m_targetStatic = monster.FindRandomStaticTarget(PACIFIST_THRALL_PLAYER_TARGET_DISTANCE);
                        }
                    }
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
            if (ammoName.ToLower().Contains("Gandr".ToLower()))
            {
                return true;
            }

            return false;
        }

        [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
        public static class Character_RPC_Damage_Patch
        {
            public static bool Prefix(Character __instance, long sender, HitData hit)
            {
                if (!IsPacifist())
                {
                    return true;
                }

                if (hit.GetAttacker() == Player.m_localPlayer)
                {
                    return false;
                }

                Character attacker = hit.GetAttacker();
                if (attacker == null)
                    return true;
                
                CharmedCharacter cc = GetCharmedCharacter(attacker);
                if (cc == null)
                    return true;



                LogDamage($"{hit.GetAttacker().GetHoverName()}, {cc.m_charmLevel}, {__instance.GetHoverName()}, {hit.GetTotalDamage()}, {hit.GetTotalBlockableDamage()}, {hit.GetTotalElementalDamage()}, {hit.GetTotalPhysicalDamage()}, {hit.m_damage.ToString()}");

                return true;
            }
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
                                weapon.m_shared.m_skillType == SkillType.WoodCutting)
                            //                                weapon.m_shared.m_skillType == SkillType.Unarmed)
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
        public static bool HasThrall()
        {
            if (!IsPacifist())
                return false;

            return (__m_allCharmedCharacters.Count > 0);
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

        public static void SetNonTrinketAdrenaline()
        {
            if (Player.m_localPlayer?.m_trinketItem != null)
            {
                //                Debug.LogError($"Has Trinket {Player.m_localPlayer.m_trinketItem?.m_shared?.m_name}");
            }
            else
            {

                if (__m_allCharmedCharacters != null && __m_allCharmedCharacters.Count > 0 && Player.m_localPlayer.m_maxAdrenaline == 0)
                {
                    Player.m_localPlayer.m_maxAdrenaline = 30;
                    Player.m_localPlayer.m_adrenaline = 0;
                }
            }
        }


        public static void DoPacifistPostPlayerSpawnTasks()
        {
            if (!IsPacifist())
                return;

            if (!Player.m_localPlayer)
                return;

            foreach (var cc in __m_allCharmedCharacters)
            {
                if (cc != null)
                {
                    SetCharmedState(cc, false);
                }
            }

            SetNonTrinketAdrenaline();

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

            UpdateModUI(Player.m_localPlayer);
        }

        public static void RemoveFromCharmedList(CharmedCharacter cc)
        {
            if (cc == null) return;

            RemoveGUIDFromCharmedList(cc.m_charmGUID);

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
            UpdateModUI(Player.m_localPlayer);
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

                    //                    enemy.m_canSwim = true;
                    // enemy.m_swimDepth = 10f;

                    monsterAI.m_attackPlayerObjects = false;

                    monsterAI.m_fleeIfNotAlerted = false;
                    monsterAI.m_fleeIfLowHealth = 0;

                    monsterAI.m_fleeIfHurtWhenTargetCantBeReached = false;

                    monsterAI.m_afraidOfFire = false;
                    monsterAI.m_avoidFire = false;
                    monsterAI.m_avoidLand = false;
                    monsterAI.m_avoidWater = true;

                    monsterAI.m_circulateWhileCharging = false;
                    monsterAI.m_circleTargetInterval = 0;
                    monsterAI.m_passiveAggresive = true;

                    monsterAI.m_character.m_group = "";
                    monsterAI.SetHuntPlayer(false);
                    monsterAI.SetTarget(monsterAI.FindEnemy());
                    monsterAI.SetTargetInfo(ZDOID.None);
                    monsterAI.SetAlerted(true);

                    monsterAI.SetDespawnInDay(false);

                    //                    Debug.Log($"Swim ({enemy.m_canSwim}) Depth: {enemy.m_swimDepth} Water level: {enemy.m_waterLevel}");
                }
            }

            SetNonTrinketAdrenaline();

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

                if (__m_showCharmList)
                {
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
                }

                if (__m_allCharmedCharacters.Count > 0)
                {
                    ShowThrallsWindow(__m_thrallsWindowObject);
                }
                else
                {
                    HideThrallsWindow();
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

                        DarkenThrall(target);
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

                    RemoveFromCharmedList(toRemove);
                    //                    __m_allCharmedCharacters.Remove(toRemove);
                }

                yield return new WaitForSeconds(1f);
            }
        }

        public static void DarkenThrall(Character target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                foreach (var mat in r.materials)
                {
                    if (mat.HasProperty("_Color"))
                    {
                        mat.color = new Color(0.0f, 0f, 0f, 1f);
                    }
                }
            }
        }

        public static void LightenThrall(Character target)
        {
            // Reset color
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                foreach (var mat in r.materials)
                {
                    if (mat.HasProperty("_Color"))
                    {
                        mat.color = Color.white;
                    }
                }
            }
        }

        public static void AddCharmEffect(Character target, bool particleEffect = true)
        {
            DarkenThrall(target);

            if (particleEffect)
            {
                // Use a built-in pink-ish particle, such as vfx_surtling_death or vfx_pickaxe_sparks
                // (replace with vfx_heart style prefab if your version has it)
                GameObject heartVFX = ZNetScene.instance.GetPrefab("fx_hen_love");
                if (heartVFX != null)
                {
                    //                Debug.LogWarning($"[heartVFX] {heartVFX.name}");

                    GameObject fx = UnityEngine.Object.Instantiate(heartVFX, target.transform.position, Quaternion.identity);
                    var ps = fx.GetComponentInChildren<ParticleSystem>();
                    if (ps != null)
                    {
                        ps.Stop();
                        ParticleSystem.MainModule main = ps.main;
                        main.startColor = new ParticleSystem.MinMaxGradient(__m_pinkColor);
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
            LightenThrall(target);
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
                        //                        Debug.LogWarning($"Invalid hit char.");

                        return;
                    }

                    if (hitChar.m_faction == Character.Faction.Boss)
                    {
                        Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"Bosses cannot be charmed.");

                        return;
                    }
                    //                    Debug.LogWarning($"Hit char {hitChar.name} of faction {hitChar.GetFaction()} and group {hitChar.m_group} nview {hitChar.m_nview}");

                    if (!IsCharmed(hitChar) && __m_allCharmedCharacters.Count >= MAX_NUM_THRALLS)
                    {
                        Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"You already have {MAX_NUM_THRALLS} thralls.");

                        return;
                    }

                    if (__instance.m_owner != null && hitChar)
                    {
                        __instance.m_owner.RaiseSkill(__instance.m_skill, __instance.m_raiseSkillAmount);
                        __instance.m_owner.AddAdrenaline(__instance.m_adrenaline);
                    }

                    bool wasCharmed = false;
                    if (IsCharmed(hitChar))
                    {
                        CharmedCharacter cc = GetCharmedCharacter(hitChar);
                        if (cc != null)
                        {
                            if (arrowName.Contains("Wood"))
                            {
                                Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"Thrall {hitChar.GetHoverName()} released from bondage.");

                                SetUncharmedState(cc);
                                RemoveGUIDFromCharmedList(cc.m_charmGUID);
                                ZNetScene.instance.Destroy(__instance.gameObject);
                                return;
                            }
                            else
                            {
                                cc.m_charmExpireTime = __m_charmTimerSeconds + (long)GetCharmDuration();
                                wasCharmed = true;
                            }
                        }
                    }
                    else
                    {
                        AddToCharmedList(hitChar, (long)GetCharmDuration());
                    }

                    ApplyGandrEffect(arrowName, hitChar, wasCharmed);


                    ZNetScene.instance.Destroy(__instance.gameObject);
                }

                catch (System.Exception ex)
                {
                    Debug.LogError($"[WoodArrowCharm] Error: {ex}");
                }
            }
        }

        public class SavedItemData
        {
            public string m_name;
            public string m_description;
        }

        static public Dictionary<string, SavedItemData> __m_originalArrows = new Dictionary<string, SavedItemData>();
        static public Dictionary<string, Recipe> __m_originalArrowRecipes = new Dictionary<string, Recipe>();

        public static void ChangeArrow(ObjectDB db, string prefabName, string ingredient, string newName, string newDescription)
        {
            GameObject arrowPrefab = db.GetItemPrefab(prefabName);
            if (!arrowPrefab)
            {
                Debug.LogError($"Could not find {prefabName}");
                return;
            }

            // ItemDrop
            //

            ItemDrop itemDrop = arrowPrefab.GetComponent<ItemDrop>();
            if (!itemDrop)
            {
                Debug.LogError($"Could not find ItemDrop on {prefabName}");
                return;
            }

            SavedItemData savedItemData = new SavedItemData();
            savedItemData.m_name = itemDrop.m_itemData.m_shared.m_name;
            savedItemData.m_description = itemDrop.m_itemData.m_shared.m_description;

            if (!__m_originalArrows.ContainsKey(prefabName))
            {
                __m_originalArrows[prefabName] = savedItemData;
            }

            itemDrop.m_itemData.m_shared.m_description = newDescription;
            itemDrop.m_itemData.m_shared.m_name = newName;

            // Recipe
            //

            Recipe vanillaRecipe = db.m_recipes.Find(r => r.name == $"Recipe_{prefabName}");
            if (vanillaRecipe)
            {
                __m_originalArrowRecipes[prefabName] = vanillaRecipe;
            }
            else
            {
                Debug.LogError($"Could not find Recipe_{prefabName}");
                return;
            }

            db.m_recipes.Remove(vanillaRecipe);

            Recipe vanillaRecipeCopy = Instantiate(vanillaRecipe);

            // TODO: Keep recipes the same, but create bigger stacks
            // make station type and level requirements stay the same as vanilla
            //
            vanillaRecipeCopy.m_item = itemDrop;
            vanillaRecipeCopy.m_amount = 20;
            vanillaRecipeCopy.m_enabled = true;
            if (prefabName == "ArrowWood")
            {
                vanillaRecipeCopy.m_craftingStation = null;
                vanillaRecipeCopy.m_minStationLevel = 0;
            }
            vanillaRecipeCopy.m_resources = new Piece.Requirement[] {
                        new Piece.Requirement()
                        {
                            m_resItem = db.GetItemPrefab(ingredient).GetComponent<ItemDrop>(),
                            m_amount = 1,
                            m_amountPerLevel = 0
                        }
                    };

            db.m_recipes.Add(vanillaRecipeCopy);
        }

        static public void RestoreArrow(ObjectDB db, string prefabName)
        {
            if (__m_originalArrows.ContainsKey(prefabName))
            {
                GameObject obj = db.GetItemPrefab(prefabName);
                if (obj)
                {
                    ItemDrop itemDrop = obj.GetComponent<ItemDrop>();
                    itemDrop.m_itemData.m_shared.m_name = __m_originalArrows[prefabName].m_name;
                    itemDrop.m_itemData.m_shared.m_description = __m_originalArrows[prefabName].m_description;
                }
            }

            if (__m_originalArrowRecipes.ContainsKey(prefabName))
            {
                Recipe originalRecipe = __m_originalArrowRecipes[prefabName];

                if (originalRecipe)
                {
                    Recipe recipe = db.m_recipes.Find(r => r.name == $"Recipe_{prefabName}");
                    if (recipe)
                    {
                        db.m_recipes.Remove(recipe);
                    }
                    db.m_recipes.Add(originalRecipe);
                }
            }
        }

        public static void ChangeArrowsAndRecipes(ObjectDB db)
        {
            __m_originalArrows.Clear();
            __m_originalArrowRecipes.Clear();

            foreach (var arrowData in __m_gandrArrowData)
            {
                ChangeArrow(db, arrowData.m_arrowName, arrowData.m_ingredient, arrowData.m_name, arrowData.m_description);
            }

            db.UpdateRegisters();
        }

        public static void RestoreArrowsAndRecipes(ObjectDB db)
        {
            foreach (var arrowData in __m_gandrArrowData)
            {
                RestoreArrow(db, arrowData.m_arrowName);
            }
            __m_originalArrows.Clear();
            __m_originalArrowRecipes.Clear();

            db.UpdateRegisters();
        }

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        public static class ObjectDB_Awake_Patch
        {
            static void Postfix(ObjectDB __instance)
            {
                //                Debug.LogWarning($"ObjectDB.Awake() called");
                if (!IsPacifist())
                {
                    return;
                }

                // See if the Database is initialized
                if (ObjectDB.instance != null &&
                    ObjectDB.instance.m_items.Count != 0
                    && ObjectDB.instance.GetItemPrefab("Amber") != null)
                {
                    //                    Debug.LogWarning($"ObjectDB available");
                    ChangeArrowsAndRecipes(__instance);
                }
            }
        }

        public static void ApplyGandrEffect(string arrowName, Character hitChar, bool wasCharmed = false)
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
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"{hitChar.GetHoverName()}{arrowData.m_description}");
                }
                else
                {
                    Debug.LogError($"ApplyGandrEffect: Failed to add status effect {arrowData.m_statusEffectType} to {hitChar.name}");
                }
            }
            else
            {
                if (wasCharmed)
                {
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"{hitChar.GetHoverName()} continues to serve you!");
                }
                else
                {
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"{hitChar.GetHoverName()} is under your thrall!");
                }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.AddAdrenaline))]
        public static class Player_AddAdrenaline_Patch
        {
            public static bool Prefix(Player __instance, ref float v)
            {
                if (!IsPacifist())
                {
                    return true;
                }

                //                Debug.LogWarning($"AddAdrenaline: {__instance.GetAdrenaline()}/{__instance.GetMaxAdrenaline()} + {v}");

                if (HasThrall())
                {
                    if (v > 0)
                    {
                        v *= 2.0f;
                    }
                    else
                    {
                        v *= 0.5f;
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
                    // Adrenaline increases much faster
                    use *= 2;

                    //                    Debug.LogError($"ADRENALINE: {player.m_adrenaline}/{player.m_maxAdrenaline} + {use}");

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
                                //                                Debug.LogError($"ADRENALINE: Healed charmed enemy {c.name} to full health due to adrenaline overflow.");
                                if (trinketPopped)
                                {
                                    //                                    Debug.LogError($"TRINKET POPPED: {trinketStatusEffect.m_name}");

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

                                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, $"Thrall {c.GetHoverName()} reached Charm Level {cc.m_charmLevel}!");
                            }
                        }

                        Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"Your thralls grow stronger!");
                    }
                }

                return true;
            }
        }

        public static void ScaleThrallIncomingDamage(Character me, ref HitData hit, float charmLevel = 1f, float adrenalineScalar = 0.5f, float damageReceivedModifier = 1.0f)
        {
            //                            Debug.Log($"{me.GetHoverName()} Incoming Pre Damage: {hit.m_damage}");
            hit.m_damage.Modify((1.0f / Math.Max(1, charmLevel / 2 + adrenalineScalar)) * damageReceivedModifier);
            //                            Debug.Log($"{me.GetHoverName()} Incoming Post Damage :{hit.m_damage} (CharmLevel: {charmLevel} Adrenaline Scalar: {adrenalineScalar} recievedModifier: {damageReceivedModifier}");
        }

        public static void ScaleThrallOutgoingDamage(ref HitData hit, float charmLevel = 1f, float adrenalineScalar = 0.5f, float damageInflictedModifier = 1.0f)
        {
            //                           Debug.Log($"{hit.GetAttacker().GetHoverName()} Attack Pre Damage: {hit.m_damage}");
            hit.m_damage.Modify((Math.Max(1, charmLevel / 2 + adrenalineScalar)) * damageInflictedModifier);
            //                           Debug.Log($"{hit.GetAttacker().GetHoverName()} Attack Post Damage: {hit.m_damage} (CharmLevel: {charmLevel} Adrenaline Scalar: {adrenalineScalar} inflictedModifier: {damageInflictedModifier}");

        }

        [HarmonyPatch(typeof(Attack), nameof(Attack.ModifyDamage))]
        public class Attack_ModifyDamage_Patch
        {
            public static bool Prefix(Attack __instance, HitData hitData, float damageFactor)
            {
                Character me = __instance.m_character;
                if (IsPacifist())
                {
                    if (IsCharmed(me) && me?.m_seman.GetStatusEffects().Count == 0)
                    {
                        CharmedCharacter cc = GetCharmedCharacter(me);
                        if (cc != null)
                        {
                            //                            Debug.Log($"Unbuffed thrall {me.GetHoverName()}");
                            float adrenaline = Player.m_localPlayer.GetAdrenaline() / Player.m_localPlayer.GetMaxAdrenaline();
                            ScaleThrallOutgoingDamage(ref hitData, cc.m_charmLevel, adrenaline, DEFAULT_THRALL_OUTGOING_DAMAGE_SCALAR);
                        }
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
        public class Character_Damage_Patch
        {
            public static bool Prefix(Character __instance, HitData hit)
            {
                if (IsPacifist())
                {
                    if (IsCharmed(__instance) && __instance?.m_seman.GetStatusEffects().Count == 0)
                    {
                        CharmedCharacter cc = GetCharmedCharacter(__instance);
                        if (cc != null)
                        {
                            //                            Debug.Log($"Unbuffed thrall {__instance.GetHoverName()}");
                            float adrenaline = Player.m_localPlayer.GetAdrenaline() / Player.m_localPlayer.GetMaxAdrenaline();
                            ScaleThrallIncomingDamage(__instance, ref hit, cc.m_charmLevel, adrenaline, DEFAULT_THRALL_INCOMING_DAMAGE_SCALAR);
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

        public const float DEFAULT_THRALL_INCOMING_DAMAGE_SCALAR = 0.7f;
        public const float DEFAULT_THRALL_OUTGOING_DAMAGE_SCALAR = 1.1f;

        public static GandrSEData[] __m_gandrSEData = new GandrSEData[]
        {
            //                                   inflicted   received  typedDamage   name
            new GandrSEData(typeof(SE_GandrFlint),      1.10f,  0.70f,   0.50f,    "Flint Gandr" ),  // Flint
            new GandrSEData(typeof(SE_GandrFire),       1.20f,  0.65f,   0.60f,    "Fire Gandr" ),  // Fire
            new GandrSEData(typeof(SE_GandrBronze),     1.30f,  0.60f,   0.70f,    "Bronze Gandr"  ),    // Bronze
            new GandrSEData(typeof(SE_GandrPoison),     1.40f,  0.55f,   0.80f,    "Poison Gandr"  ),    // Poison
            new GandrSEData(typeof(SE_GandrIron),       1.50f,  0.50f,   0.90f,    "Iron Gandr"  ),  // Iron
            new GandrSEData(typeof(SE_GandrFrost),      1.60f,  0.45f,   1.00f,    "Frost Gandr"  ),    // Frost
            new GandrSEData(typeof(SE_GandrObsidian),   1.70f,  0.40f,   1.10f,    "Glass Gandr"  ),   // Obsidian
            new GandrSEData(typeof(SE_GandrSilver),     1.80f,  0.35f,   1.20f,    "Silver Gandr"  ), // Silver
            new GandrSEData(typeof(SE_GandrNeedle),     1.90f,  0.30f,   1.30f,    "Needle Gandr"  ),   // Needle
            new GandrSEData(typeof(SE_GandrCarapace),   2.00f,  0.25f,   1.40f,    "Bug Gandr"  ),   // Carapace
            new GandrSEData(typeof(SE_GandrCharred),    2.50f,  0.20f,   1.50f,    "Charred Gandr"  ),   // Charred
        };


        public class SE_GandrEffect : SE_Stats
        {
            public float m_adrenalineScalar = 0.0f;
            public float m_baseDamageInflictedModifier = 1.0f;
            public float m_baseDamageReceivedModifier = 1.2f;
            public float m_baseTypedDamageModifier = 0.5f;
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
                    m_baseTypedDamageModifier = seData.m_typedDamage;

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
            }

            public override void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
            {
                HitData.DamageTypes dt = m_percentigeDamageModifiers;

                // Add damage typed damage
                float largestDamage = 0.0f;
                hitData.m_damage.GetMajorityDamageType(out largestDamage);
                dt.Modify(largestDamage * m_baseTypedDamageModifier);
                hitData.m_damage.Add(dt);

                ScaleThrallOutgoingDamage(ref hitData, m_charmLevel, m_adrenalineModifier, m_baseDamageInflictedModifier);

                base.ModifyAttack(skill, ref hitData);
            }

            public override void OnDamaged(HitData hit, Character attacker)
            {
                base.OnDamaged(hit, attacker);

                ScaleThrallIncomingDamage(m_character, ref hit, m_charmLevel, m_adrenalineScalar, m_baseDamageReceivedModifier);
            }

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
                            if (se.m_icon == null)
                                continue;

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
                            rectTransform.anchoredPosition = new Vector2(-leftJustify + iconOffset, -iconSize / 2 - 1);

                            iconElementIndex++;
                        }

                        // Update the pink Charm bar 
                        GuiBar charmBar = data.m_gui.transform.Find("Charm/CharmBar").GetComponent<GuiBar>();
                        float remainingTime = cc.m_charmExpireTime - __m_charmTimerSeconds;
                        //                        Debug.Log($"Bar {c.name} {remainingTime}/{GetCharmDuration()} {charmBar.m_value}/{charmBar.m_maxValue}\n  wid:{charmBar.m_width} smoothVal:{charmBar.m_smoothValue} smoothSpeed:{charmBar.m_smoothSpeed}");
                        charmBar.SetWidth(100.0f);
                        charmBar.SetValue(remainingTime / GetCharmDuration());
                        charmBar.SetColor(__m_pinkColor);

                        GameObject textElement = data.m_gui.transform.Find($"Charm/CharmLevelText").gameObject;
                        TextMeshProUGUI tm = textElement.GetComponent<TextMeshProUGUI>();
                        tm.text = $"{cc.m_charmLevel}";
                    }
                }
            }
        }

        public static int MAX_NUM_CHARM_ICONS = 16;

        public static void CreateCharmIconsInMasterHud(Transform parentTransform)
        {
            for (int i = 0; i < MAX_NUM_CHARM_ICONS; i++)
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

        public static void ReleaseAllThralls()
        {
            for (int i = __m_allCharmedCharacters.Count - 1; i >= 0; i--)
            {
                CharmedCharacter cc = __m_allCharmedCharacters[i];
                SetUncharmedState(cc);
                RemoveGUIDFromCharmedList(cc.m_charmGUID);
            }
        }

        public static void SetCharmLevel(int charmLevel)
        {
            for (int i = __m_allCharmedCharacters.Count - 1; i >= 0; i--)
            {
                CharmedCharacter cc = __m_allCharmedCharacters[i];
                cc.m_charmLevel = charmLevel;
            }
        }


        // Spawn Density
        //

        [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.SetupLocations))]
        public static class ZoneSystem_SetupLocations_Patch
        {
            public static void Postfix(ZoneSystem __instance)
            {
                if (!IsPacifist())
                    return;

                // ensure ZNetScene is present
                if (ZNetScene.instance == null)
                {
                    Debug.LogWarning("[MyMod] ZNetScene not loaded when SetupLocations patched.");
                    return;
                }

                List<ZoneSystem.ZoneLocation> locations = __instance.m_locations;
                if (locations == null) return;

                foreach (var loc in locations)
                {
                    //                    Debug.LogWarning($"location {loc.m_name} {loc.m_prefabName} Bio={loc.m_biome} Qty={loc.m_quantity} minDistSim={loc.m_minDistanceFromSimilar} maxDistSim={loc.m_maxDistanceFromSimilar} minDist={loc.m_minDistance} maxDist={loc.m_maxDistance} enable={loc.m_enable}");

                    if (loc == null) continue;
                    float locMultiplier = 1.5f;

                    if (loc.m_biome.HasFlag(Heightmap.Biome.Meadows))
                    {
                        if (loc.m_prefabName.Contains("Runestone_Boars"))
                        {
                            locMultiplier = 10.0f;
                        }
                    }
                    if (loc.m_biome.HasFlag(Heightmap.Biome.BlackForest))
                    {

                    }
                    if (loc.m_biome.HasFlag(Heightmap.Biome.Swamp))
                    {

                    }
                    if (loc.m_biome.HasFlag(Heightmap.Biome.Mountain))
                    {

                    }
                    if (loc.m_biome.HasFlag(Heightmap.Biome.Plains))
                    {

                    }
                    if (loc.m_biome.HasFlag(Heightmap.Biome.Mistlands))
                    {

                    }
                    if (loc.m_biome.HasFlag(Heightmap.Biome.AshLands))
                    {

                    }
                    if (loc.m_biome.HasFlag(Heightmap.Biome.DeepNorth))
                    {

                    }
                    // Double the requested quantity
                    if (loc.m_quantity > 1)
                    {
                        loc.m_quantity = Mathf.RoundToInt(loc.m_quantity * locMultiplier);
                    }

                    if (loc.m_minDistance > 0 && loc.m_maxDistance > 0)
                    {
                        loc.m_minDistance = Mathf.Max(10f, loc.m_minDistance * (1 / locMultiplier));
                        loc.m_maxDistance = Mathf.Max(10f, loc.m_maxDistance * (1 / locMultiplier));
                    }

                    if (loc.m_minDistanceFromSimilar > 0)
                    {
                        loc.m_minDistanceFromSimilar /= locMultiplier;
                    }
                }
            }
        }

        /*
        [HarmonyPatch(typeof(DungeonGenerator), "Generate")]
        public class DungeonGenerator_Generate_Patch
        {
            public static void Postfix(DungeonGenerator __instance)
            {
                var root = __instance.m_rootInstance;
                if (!root) return;

                // Existing spawners
                foreach (var spawn in root.GetComponentsInChildren<CharacterSpawn>())
                {
                    spawn.m_maxSpawned *= 2;   // Double enemy density
                    spawn.m_spawnInterval *= 0.5f; // More frequent respawns
                }

                // Add new spawners to rooms (advanced)
                foreach (Transform t in root.GetComponentsInChildren<Transform>())
                {
                    if (UnityEngine.Random.value < 0.1f)
                    {
                        var spawner = Object.Instantiate(ZNetScene.instance.GetPrefab("Spawner"), t);
                        var cs = spawner.GetComponent<CharacterSpawn>();
                        cs.m_charPrefab = ZNetScene.instance.GetPrefab("Draugr");
                        cs.m_maxSpawned = 3;
                    }
                }
            }
        }
        */

        public class BackupSpawnData
        {
            public float m_spawnChance;
            public int m_maxSpawned;
            public float m_spawnInterval;
            public float m_spawnRadiusMin;
            public float m_spawnRadiusMax;
            public float m_spawnDistance;
        }

        public class SpawnModifierData
        {
            public float m_chance = 1.1f;
            public float m_max = 2.0f;
            public float m_interval = 0.9f;
            public float m_minRadius = 0.0f;
            public float m_maxRadius = 0.0f;
            public float m_distance = 0.85f;
        }

        //public class SpawnModifierData
        //{
        //    public float m_chance = 10.0f;
        //    public float m_max = 10.0f;
        //    public float m_interval = 0.1f;
        //    public float m_minRadius = 1.0f;
        //    public float m_maxRadius = 50.0f;
        //    public float m_distance = 0.1f;
        //}

        public static Dictionary<string, SpawnModifierData> __m_spawnMultipliers = new Dictionary<string, SpawnModifierData>()
        {
            // Creature Name, Spawn Chance Multiplier, Max Spawned Multiplier, Spawn Interval Multiplier
            {"Abomination",         new SpawnModifierData() {  } },
            {"Asksvin",             new SpawnModifierData() {  } },
            {"Bjorn",               new SpawnModifierData() {  } },
            {"Blob",                new SpawnModifierData() {  } },
            {"BlobElite",           new SpawnModifierData() {  } },
            {"BlobLava",            new SpawnModifierData() {  } },
            {"Boar",                new SpawnModifierData() { m_chance = 4.0f, m_max = 8.0f, m_interval = 0.25f, m_distance = 0.1f } },
            {"BonemawSerpent",      new SpawnModifierData() {  } },
            {"Charred_Archer",      new SpawnModifierData() {  } },

            {"Charred_Melee",       new SpawnModifierData() {  } },

            {"Charred_Twitcher",    new SpawnModifierData() {  } },

            {"CinderSky",           new SpawnModifierData() { m_chance = 1.0f, m_max = 1.0f, m_interval = 1.0f, m_distance = 1.0f } },
            {"CinderStorm",         new SpawnModifierData() { m_chance = 1.0f, m_max = 1.0f, m_interval = 1.0f, m_distance = 1.0f } },
            {"Deathsquito",         new SpawnModifierData() {  } },
            {"Deer",                new SpawnModifierData() { m_chance = 1.2f, m_max = 3.0f, m_interval = 1f, m_distance = 0.75f } }, // m_chance = 2.0f, m_max = 4.0f, m_interval = 0.5f, m_distance = 0.5f
            {"Draugr",              new SpawnModifierData() {  } },

            {"Draugr_Elite",        new SpawnModifierData() {  } },
            {"Dverger",             new SpawnModifierData() {  } },
            {"DvergerAshlands",     new SpawnModifierData() {  } },
            {"FallenValkyrie",      new SpawnModifierData() {  } },
            {"Fenring",             new SpawnModifierData() {  } },
            {"FireFlies",           new SpawnModifierData() {  } },
            {"Fish1",               new SpawnModifierData() { m_chance = 1.0f, m_max = 1.0f, m_interval = 1.0f, m_distance = 1.0f } },
            {"Fish10",              new SpawnModifierData() { m_chance = 1.0f, m_max = 1.0f, m_interval = 1.0f, m_distance = 1.0f } },
            {"Fish11",              new SpawnModifierData() { m_chance = 1.0f, m_max = 1.0f, m_interval = 1.0f, m_distance = 1.0f } },
            {"Fish12",              new SpawnModifierData() { m_chance = 1.0f, m_max = 1.0f, m_interval = 1.0f, m_distance = 1.0f } },
            {"Fish2",               new SpawnModifierData() { m_chance = 1.0f, m_max = 1.0f, m_interval = 1.0f, m_distance = 1.0f } },
            {"Fish3",               new SpawnModifierData() { m_chance = 1.0f, m_max = 1.0f, m_interval = 1.0f, m_distance = 1.0f } },
            {"Fish5",               new SpawnModifierData() { m_chance = 1.0f, m_max = 1.0f, m_interval = 1.0f, m_distance = 1.0f } },
            {"Fish6",               new SpawnModifierData() { m_chance = 1.0f, m_max = 1.0f, m_interval = 1.0f, m_distance = 1.0f } },
            {"Fish7",               new SpawnModifierData() { m_chance = 1.0f, m_max = 1.0f, m_interval = 1.0f, m_distance = 1.0f } },
            {"Fish8",               new SpawnModifierData() { m_chance = 1.0f, m_max = 1.0f, m_interval = 1.0f, m_distance = 1.0f } },
            {"Fish9",               new SpawnModifierData() { m_chance = 1.0f, m_max = 1.0f, m_interval = 1.0f, m_distance = 1.0f } },
            {"Gjall",               new SpawnModifierData() {  } },
            {"Goblin",              new SpawnModifierData() {  } },


            {"GoblinBrute",         new SpawnModifierData() {  } },
            {"Greydwarf",           new SpawnModifierData() {  } },



            {"Greydwarf_Elite",      new SpawnModifierData() {  } },
            {"Greydwarf_Shaman",    new SpawnModifierData() {  } },

            {"Greyling",         new SpawnModifierData() { m_chance = 3.0f, m_max = 4.0f, m_interval = 0.5f, m_distance = 0.3f } },
            {"Hare",                new SpawnModifierData() {  } },
            {"Hatchling",           new SpawnModifierData() {  } },
            {"LavaRock",            new SpawnModifierData() { m_chance = 1.0f, m_max = 1.0f, m_interval = 1.0f, m_distance = 1.0f } },
            {"Leech",               new SpawnModifierData() {  } },
            {"Lox",                 new SpawnModifierData() {  } },
            {"Morgen_NonSleeping",  new SpawnModifierData() {  } },
            {"Neck",                new SpawnModifierData() { m_chance = 2.0f, m_max = 4.0f, m_interval = 0.5f, m_distance = 0.5f } },

            {"Seagal",               new SpawnModifierData() { m_chance = 1.0f, m_max = 1.0f, m_interval = 1.0f, m_distance = 1.0f } },
            {"Seeker",              new SpawnModifierData() {  } },


            {"SeekerBrood",          new SpawnModifierData() {  } },
            {"SeekerBrute",         new SpawnModifierData() {  } },
            {"Serpent",             new SpawnModifierData() {  } },
            {"Skeleton",            new SpawnModifierData() {  } },

            {"StoneGolem",           new SpawnModifierData() {  } },
            {"Surtling",            new SpawnModifierData() {  } },

            {"Tick",             new SpawnModifierData() {  } },
            {"Troll",               new SpawnModifierData() {  } },
            {"Unbjorn",             new SpawnModifierData() {  } },
            {"Volture",             new SpawnModifierData() {  } },
            {"Wolf",                new SpawnModifierData() {  } },

            {"Wraith",               new SpawnModifierData() {  } },
            {"odin",                new SpawnModifierData() { m_chance = 1.0f, m_max = 1.0f, m_interval = 1.0f, m_distance = 1.0f } },
            {"projectile_ashlandmeteor",new SpawnModifierData() { m_chance = 1.0f, m_max = 1.0f, m_interval = 1.0f, m_distance = 1.0f } }

        };

        public static void PrintSpawnSystem(List<SpawnSystemList> ssls, string name)
        {

            Debug.Log($" SpawnLists '{name}' Count: {ssls.Count} {ssls.GetHashCode()}");

            using (StreamWriter writer = new StreamWriter($"SpawnDataDump2{name}.txt", false))
            {
                foreach (SpawnSystemList ssl in ssls)
                {
                    writer.WriteLine($"SpawnList: {ssl?.name} {ssl?.GetHashCode().ToString()} {ssl.m_spawners?.Count}");
                    foreach (SpawnSystem.SpawnData sp in ssl?.m_spawners)
                    {
                        writer.WriteLine($" {sp.m_name} {sp.m_spawnChance}% Max: {sp.m_maxSpawned} Interval:{sp.m_spawnInterval} RadiusMin:{sp.m_spawnRadiusMin} RadiusMax:{sp.m_spawnRadiusMax} SpawnDistance: {sp.m_spawnDistance}");
                    }
                }
            }
        }

        public static int __m_spawnListsPatched = 0;

        public static Dictionary<int, Dictionary<int, BackupSpawnData>> __m_originalSpawnData = new Dictionary<int, Dictionary<int, BackupSpawnData>>();
        public static void DoBackupSpawnData(List<SpawnSystemList> spawnLists, ref Dictionary<int, Dictionary<int, BackupSpawnData>> outDict)
        {
            outDict = new Dictionary<int, Dictionary<int, BackupSpawnData>>();
            foreach (SpawnSystemList ssl in spawnLists)
            {
                int listHashCode = ssl.GetHashCode();
                Dictionary<int, BackupSpawnData> newDict = new Dictionary<int, BackupSpawnData>();
                outDict.Add(listHashCode, newDict);
                foreach (SpawnSystem.SpawnData data in ssl.m_spawners)
                {
                    int dataHash = data.GetHashCode();
                    BackupSpawnData newData = new BackupSpawnData();
                    newData.m_spawnInterval = data.m_spawnInterval;
                    newData.m_spawnChance = data.m_spawnChance;
                    newData.m_maxSpawned = data.m_maxSpawned;
                    newData.m_spawnRadiusMin = data.m_spawnRadiusMin;
                    newData.m_spawnRadiusMax = data.m_spawnRadiusMax;
                    newData.m_spawnDistance = data.m_spawnDistance;
                    newDict.Add(dataHash, newData);
                }
            }
        }

        public static void DoRestoreSpawnData(Dictionary<int, Dictionary<int, BackupSpawnData>> backupData, ref List<SpawnSystemList> spawnLists)
        {
            foreach (SpawnSystemList ssl in spawnLists)
            {
                Dictionary<int, BackupSpawnData> listDict = backupData[ssl.GetHashCode()];
                foreach (SpawnSystem.SpawnData data in ssl.m_spawners)
                {
                    BackupSpawnData bd = listDict[data.GetHashCode()];
                    data.m_spawnInterval = bd.m_spawnInterval;
                    data.m_spawnChance = bd.m_spawnChance;
                    data.m_maxSpawned = bd.m_maxSpawned;
                    data.m_spawnRadiusMin = bd.m_spawnRadiusMin;
                    data.m_spawnRadiusMax = bd.m_spawnRadiusMax;
                    data.m_spawnDistance = bd.m_spawnDistance;
                }
            }
        }

        [HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.OnDestroy))]
        public static class SpawnSystem_OnDestroy_Patch
        {
            public static void Postfix(SpawnSystem __instance)
            {
                if (IsPacifist())
                {
                    if (--__m_spawnListsPatched < 1)
                    {
                        //                        Debug.Log($"SpawnSystem.OnDestroy: Num Spawn Lists: {__m_spawnListsPatched}");
                        // restore original spawn lists
                        DoRestoreSpawnData(__m_originalSpawnData, ref __instance.m_spawnLists);

                        //                        PrintSpawnSystem(__instance.m_spawnLists, "Restored");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.Awake))]
        public static class SpawnSystem_Awake_Patch
        {
            public static void Postfix(SpawnSystem __instance)
            {
                if (IsPacifist())
                {
                    if (__instance == null)
                        return;

                    //                    Debug.Log($"SpawnSystem.Awake: Num Spawn Lists: {__m_spawnListsPatched}");
                    if (__m_spawnListsPatched++ == 0)
                    {
                        // Save original spawn lists
                        //                        PrintSpawnSystem(__instance.m_spawnLists, "Original");

                        DoBackupSpawnData(__instance.m_spawnLists, ref __m_originalSpawnData);

                        // Adjust spawn parameters for pacifist mode to increase density and frequency
                        foreach (SpawnSystemList ssl in __instance.m_spawnLists)
                        {
                            // Only process a given SpawnSystemList ONCE

                            foreach (SpawnSystem.SpawnData sp in ssl.m_spawners)
                            {
                                if (sp == null)
                                    continue;

                                if (__m_spawnMultipliers.TryGetValue(sp.m_prefab.name, out SpawnModifierData smd))
                                {
                                    //                                    Debug.Log($"SpawnSystem.Awake buffed {sp.m_prefab.name}");

                                    //                            sp.m_groupSizeMin = (int)((float)sp.m_groupSizeMin * smd.m_maxSpawnedModifier);
                                    //                            sp.m_groupSizeMax = (int)((float)sp.m_groupSizeMax * smd.m_maxSpawnedModifier);
                                    sp.m_spawnChance = Math.Min(sp.m_spawnChance * smd.m_chance, 100f);                        // Percentage chance to spawn at each timer interval
                                    sp.m_maxSpawned = (int)Math.Max((float)sp.m_maxSpawned * smd.m_max, 1.0f);               // Maximum number spawned at a time
                                    sp.m_spawnInterval = Math.Max(1.0f, sp.m_spawnInterval * smd.m_interval);                  // Spawn interval timer (default is 120 to 1000)
                                    sp.m_spawnRadiusMin = smd.m_minRadius;   // Minimum radius from player (0 is SpawnSystem default)
                                    sp.m_spawnRadiusMax = smd.m_maxRadius;   // Maximum radius from player (0 is SpawnSystem default)
                                    sp.m_spawnDistance = Math.Max(1.0f, sp.m_spawnDistance * smd.m_distance);                  // Minimum distance to another one (10 to 64)

                                }
                                else
                                {
                                    Debug.LogWarning($"Did not find spawn modifier for {sp.m_prefab.name}");
                                }
                            }
                        }
                        //                        Debug.Log($"SpawnSystem.Awake: Spawn List Patched");

                        //                        PrintSpawnSystem(__instance.m_spawnLists, "Modified");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.UpdateSpawning))]
        public static class SpawnSystem_UpdateSpawning_Patch
        {
            public static bool Prefix(SpawnSystem __instance)
            {
                if (!IsPacifist())
                    return true;


                if (__instance == null)
                    return true;

                if (Player.m_localPlayer == null)
                    return true;

                foreach (SpawnSystemList ssl in __instance.m_spawnLists)
                {
                    if (ssl == null)
                        continue;

                    foreach (SpawnSystem.SpawnData sp in ssl.m_spawners)
                    {
                        if (sp == null)
                            continue;

                        Heightmap.Biome playerBiome = Player.m_localPlayer.GetCurrentBiome();
                        if (playerBiome == sp.m_biome)
                        {
                            sp.m_maxSpawned = Math.Max(2, (__m_allCharmedCharacters.Count + 1));
                        }
                    }
                }
                return true;
            }
        }


        static GameObject __m_thrallsWindowObject = null;
        static GameObject __m_thrallsWindowBackground = null;
        static TextMeshProUGUI __m_thrallsWindowText = null;
        static Vector2 __m_thrallsTooltipWindowSize = new Vector2(410, 170);
        static Vector2 __m_thrallsTooltipTextOffset = new Vector2(5, 2);

        public static void CreateThrallsWindow(Transform parentTransform)
        {
            // Tooltip Background
            __m_thrallsWindowBackground = new GameObject("Thrall Window Background");

            // Set %the parent to the HUD
            __m_thrallsWindowBackground.transform.SetParent(parentTransform, false);

            Vector2 windowPos = new Vector2(-90, 360);

            RectTransform bgTransform = __m_thrallsWindowBackground.AddComponent<RectTransform>();
            bgTransform.sizeDelta = __m_thrallsTooltipWindowSize;
            bgTransform.anchoredPosition = windowPos;
            bgTransform.pivot = new Vector2(0, 0);

            // Add an Image component for the background
            UnityEngine.UI.Image backgroundImage = __m_thrallsWindowBackground.AddComponent<UnityEngine.UI.Image>();
            backgroundImage.color = new Color(0, 0, 0, 0.90f); // Semi-transparent black background
            __m_thrallsWindowBackground.SetActive(false);

            // Create a new GameObject for the tooltip
            __m_thrallsWindowObject = new GameObject("Thrall Window Text");
            __m_thrallsWindowObject.transform.SetParent(parentTransform, false);

            // Add a RectTransform component for positioning
            RectTransform rectTransform = __m_thrallsWindowObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(__m_thrallsTooltipWindowSize.x - __m_thrallsTooltipTextOffset.x, __m_thrallsTooltipWindowSize.y - __m_thrallsTooltipTextOffset.y);
            rectTransform.anchoredPosition = windowPos + new Vector2(5, 0);
            rectTransform.pivot = new Vector2(0, 0);

            // Add a TextMeshProUGUI component for displaying the tooltip text
            __m_thrallsWindowText = AddTextMeshProComponent(__m_thrallsWindowObject);
            __m_thrallsWindowText.fontSize = 14;
            __m_thrallsWindowText.alignment = TextAlignmentOptions.TopLeft;
            __m_thrallsWindowText.color = Color.yellow;

            // Initially hide the tooltip
            __m_thrallsWindowObject.SetActive(true);
        }

        public static string BuildThrallsWindowText(ref int lines)
        {
            string text =
                $"<size=20><b><color=#FFB75B>Thralls</color><b></size>\n";

            text += $"\n<size=16><pos=0%><color=white><u>Friend</u></color><pos=35%><u><color=yellow>(Level)</color></u><pos=50%><color=red><u>Health</u></color><pos=78%><color=orange><u>Remain</u></color>\n";

            int lineCount = 0;
            foreach (var cc in __m_allCharmedCharacters)
            {
                Character c = GetCharacterFromGUID(cc.m_charmGUID);
                if (c == null)
                    continue;
                float remainingTime = cc.m_charmExpireTime - __m_charmTimerSeconds;
                DateTime remainTime = DateTime.MinValue.AddSeconds(remainingTime);
                string timeStr = remainTime.ToString("m'm 's's'");

                text += $"<color=yellow>{lineCount + 1}:</color> <pos=5%><color=white>{c.GetHoverName()}<pos=40%><color=yellow>({cc.m_charmLevel})</color><pos=50%><color=red>{(int)(c.GetHealthPercentage() * 100)}%</color><pos=78%></color><color=orange>{timeStr}</color></size>\n";
                lineCount++;
            }
            for (int i = MAX_NUM_THRALLS - lineCount; i > 0; i--)
            {
                text += $"<color=yellow>{lineCount + 1}:</color> <pos=5%><color=#505050> -- Unused -- <pos=40%><color=#505050>--<pos=50%><color=#505050>---<pos=78%><color=#505050>---</color></size>\n";
                lineCount++;
            }

            lines = lineCount;
            return text;
        }

        public static void ShowThrallsWindow(GameObject uiObject)
        {
            if (uiObject == null)
            {
                Debug.LogError("ShowThrallsWindow: uiObject is null!");

                return;
            }

            int lineCount = 0;

            string text = BuildThrallsWindowText(ref lineCount);

            __m_thrallsWindowText.text = text;
            __m_thrallsWindowText.ForceMeshUpdate(true, true);

            __m_thrallsWindowBackground.SetActive(true);
            __m_thrallsWindowObject.SetActive(true);

            __m_thrallsWindowText.ForceMeshUpdate(true, true);
        }

        public static void HideThrallsWindow()
        {
            __m_thrallsWindowBackground.SetActive(false);
            __m_thrallsWindowObject.SetActive(false);
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
