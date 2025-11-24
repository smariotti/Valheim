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

namespace TrophyHuntMod
{
    public partial class TrophyHuntMod : BaseUnityPlugin
    {
        public static bool __m_charmTimerStarted = false;

        static List<CharmedCharacter> __m_allCharmedCharacters = new List<CharmedCharacter>();

        const float TROPHY_PACIFIST_CHARM_DURATION = 60; // seconds

        public const float cFollowDistance = 3.0f;
        public const float cRadiusScale = 2f;

        public const float PACIFIST_THRALL_PLAYER_TARGET_DISTANCE = 50.0f;

        public class GandrArrowData
        {
            public GandrArrowData(string arrowName, string ingredient, string name, string description, Type statusEffectType)
            {
                m_arrowName = arrowName;
                m_ingredient = ingredient;
                m_name = name;
                m_description = description;
                m_statusEffectType = statusEffectType;                
            }

            public string m_arrowName = "";
            public string m_ingredient = "";
            public string m_name = "";
            public string m_description = "";
            public Type m_statusEffectType;
        }

        public static GandrArrowData[] __m_gandrArrowData = new GandrArrowData[]
        {
            new GandrArrowData("ArrowWood", "Wood", "Wooden Gandr", "Thrall becomes stronger.", typeof(SE_GandrWood)),
            new GandrArrowData("ArrowFlint", "Flint", "Flint Gandr", "Thrall hards like stone.", typeof(SE_GandrFlint)),
            new GandrArrowData("ArrowFire", "Resin", "Fire Gandr", "Thrall burns its enemies.", typeof(SE_GandrFire)),
            new GandrArrowData("ArrowBronze", "Bronze", "Bronze Gandr", "Thrall bristles with new strength.", typeof(SE_GandrBronze)),
            new GandrArrowData("ArrowPoison", "Ooze", "Poison Gandr", "", typeof(SE_GandrPoison)),
            new GandrArrowData("ArrowIron", "Iron", "Iron Gandr", "", typeof(SE_GandrIron)),
            new GandrArrowData("ArrowFrost", "FreezeGland", "Frost Gandr", "", typeof(SE_GandrFrost)),
            new GandrArrowData("ArrowObsidian", "Obsidian", "Glass Gandr", "", typeof(SE_GandrObsidian)),
            new GandrArrowData("ArrowSilver", "Silver", "Silver Gandr", "", typeof(SE_GandrSilver)),
            new GandrArrowData("ArrowNeedle", "Needle", "Needle Gandr", "", typeof(SE_GandrNeedle)),
            new GandrArrowData("ArrowCarapace", "Carapace", "Bug Gandr", "", typeof(SE_GandrCarapace)),
            new GandrArrowData("ArrowCharred", "Blackwood", "Charred Gandr", "", typeof(SE_GandrCharred)),
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

                CharmedCharacter guy = __m_allCharmedCharacters.Find(c => c.m_zdoid == __instance.GetZDOID());
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

                CharmedCharacter guy = __m_allCharmedCharacters.Find(c => c.m_zdoid == __instance.GetZDOID());
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

                //var nview = __instance.m_nview;
                //if (nview == null || !nview.IsValid())
                //    return;

                //// Record original faction
                //if (!nview.GetZDO().GetBool("charmed"))
                //{
                //    return;
                //}


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

                    //__instance.SetTarget(null);
                    //__instance.SetTargetInfo(ZDOID.None);
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

                //if (go == Player.m_localPlayer.gameObject)
                //{
                //    float velmag = Player.m_localPlayer.GetVelocity().magnitude;
                //    distanceReduction = velmag * 0.5f;
                //}
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

        [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
        public static class Character_RPC_Damage_Patch
        {
            public static bool Prefix(Character __instance, long sender, HitData hit)
            {
                if (!IsPacifist())
                {
                    return true;
                }

                if (__instance == null)
                    return true;

                if (hit.GetAttacker() == Player.m_localPlayer)
                {
                    return false;
                }

                // Debug
                // List the status effects on the attacker
                if (hit.GetAttacker() != null)
                {
                    Debug.LogWarning($"[RPC_Damage] {__instance?.name} hit by {hit.GetAttacker()?.name} : {hit?.m_damage.ToString()}");
                    List<StatusEffect> statusFX = hit.GetAttacker().m_seman.GetStatusEffects();
                    foreach (var se in statusFX)
                    {
                        Debug.LogWarning($"  Status Effect: {se.m_name} {se.GetType()} TTL: {se.m_ttl} {se.GetIconText()}");
                    }
                }

                return true;
            }
        }

        public static bool IsThrallArrow(string ammoName)
        {
            Debug.LogWarning($"[IsThrallArrow] Checking ammo name: {ammoName}");

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
            public CharmedCharacter() { m_zdoid = ZDOID.None; m_pin = null; m_charmExpireTime = 0; m_originalFaction = Character.Faction.TrainingDummy; m_swimSpeed = 2f; }
            public CharmedCharacter(ZDOID zdoid) { m_zdoid = zdoid; }

            // Data we store for the charmed guy
            public ZDOID m_zdoid = ZDOID.None;
            public Minimap.PinData m_pin = null;
            public long m_charmExpireTime = 0;
            public Character.Faction m_originalFaction = Character.Faction.TrainingDummy;
            public float m_swimSpeed = 2f;
        }

        public static float GetCharmDuration()
        {
            System.Random randomizer = new System.Random();
            //            float duration = TROPHY_PACIFIST_CHARM_DURATION + (float)randomizer.NextDouble() * TROPHY_PACIFIST_CHARM_DURATION / 4 - TROPHY_PACIFIST_CHARM_DURATION / 8;
            
            float duration = TROPHY_PACIFIST_CHARM_DURATION;

            //            Debug.LogWarning($"[GetCharmDuration] {duration} seconds.");

            return duration;
        }

        public static ZDOID GetZDOIDFromCharacter(Character character)
        {
            if (character == null)
            {
                return ZDOID.None;
            }

            var nview = character.m_nview;
            if (nview == null || !nview.IsValid())
            {
                return ZDOID.None;
            }

            return character.GetZDOID();
        }

        public static Character GetCharacterFromZDOID(ZDOID zdoid)
        {
            List<Character> allCharacters = Character.GetAllCharacters();
            foreach (Character character in allCharacters)
            {
                if (character.GetZDOID() == zdoid)
                {
                    return character;
                }
            }
            return null;
        }

        public static void RecharmAllCharmedEnemies()
        {
            foreach (var cc in __m_allCharmedCharacters)
            {
                SetCharmedState(cc);
            }
        }

        public static bool IsCharmed(Character character)
        {
            CharmedCharacter guy = __m_allCharmedCharacters.Find(c => c.m_zdoid == character.GetZDOID());

            bool result = guy != null;

            return result;
        }

        public static CharmedCharacter GetCharmedCharacter(Character character)
        {
            CharmedCharacter guy = __m_allCharmedCharacters.Find(c => c.m_zdoid == character.GetZDOID());

            return guy;
        }

        public static void AddToCharmedList(Character enemy, long duration)
        {
            // Create a data structure to track the guy we charmed
            CharmedCharacter cc = new CharmedCharacter();

            ZDOID zdoid = enemy.GetZDOID();
            cc.m_zdoid = zdoid;
            cc.m_pin = Minimap.instance.AddPin(enemy.transform.position, Minimap.PinType.Icon3, "", false, false);
            cc.m_originalFaction = enemy.m_faction;
            cc.m_charmExpireTime = __m_charmTimerSeconds + duration;
            cc.m_swimSpeed = enemy.m_swimSpeed;

            __m_allCharmedCharacters.Add(cc);

            SetCharmedState(cc);

            if (__m_allCharmedCharacters.Count > 0 && Player.m_localPlayer.m_maxAdrenaline == 0)
            {
                if (Player.m_localPlayer != null)
                {
                    Player.m_localPlayer.m_maxAdrenaline = 30;
                    Player.m_localPlayer.m_adrenaline = 0;
                }
            }
        }

        public static void RemoveFromCharmedList(Character enemy)
        {
            CharmedCharacter cc = __m_allCharmedCharacters.Find(c => c.m_zdoid == enemy.GetZDOID());
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

        public static void SetCharmedState(CharmedCharacter cc)
        {
            CharmedCharacter guy = __m_allCharmedCharacters.Find(c => c.m_zdoid == cc.m_zdoid);
            if (guy != null)
            {
                Character enemy = GetCharacterFromZDOID(guy.m_zdoid);
                if (enemy == null)
                {
                    Debug.LogError($"Unable to SetCharmedState for ZDOID {guy.m_zdoid} - character not found!");
                    return;
                }

                //            enemy.SetTamed(true);

                // Change faction to player
                enemy.m_faction = Character.Faction.Players;

                enemy.m_swimSpeed *= 10;

                // Optional: give a color tint or particle effect
                AddCharmEffect(enemy);

                //                Debug.LogError($"SetCharmedState for {enemy.name} - success");

                var monsterAI = enemy.GetComponent<MonsterAI>();
                if (monsterAI)
                {
                    //Tameable tameable = enemy.GetComponent<Tameable>();
                    //if (tameable)
                    //{
                    //    tameable.m_commandable = true;
                    //}

                    if (Player.m_localPlayer != null && Player.m_localPlayer.gameObject != null)
                    {
                        monsterAI.SetFollowTarget(Player.m_localPlayer.gameObject);
                    }

                    monsterAI.m_attackPlayerObjects = false;
                    //                monsterAI.ResetRandomMovement();
                    //                monsterAI.ResetPatrolPoint();
                    //                monsterAI.SetAlerted(alert: false);
                    monsterAI.m_fleeIfNotAlerted = false;
                    monsterAI.m_fleeIfLowHealth = 0;
                    //                monsterAI.m_afraidOfFire = false;
                    //                monsterAI.m_fleeIfHurtWhenTargetCantBeReached = false;
                    //                monsterAI.m_circleTargetInterval = 0;
                    monsterAI.m_character.m_group = "";
                    monsterAI.SetHuntPlayer(false);
                    monsterAI.SetTarget(null);
                    monsterAI.SetTargetInfo(ZDOID.None);
                }
            }
        }

        public static void SetUncharmedState(CharmedCharacter cc)
        {
            Character enemy = GetCharacterFromZDOID(cc.m_zdoid);
            if (!enemy)
            {
                Debug.LogError($"Unable to SetUncharmedState for ZDOID {cc.m_zdoid} - character not found!");
                return;
            }

            //            enemy.SetTamed(false);
            RemoveCharmEffect(enemy);
            //            Debug.LogError($"SetUncharmedState for {enemy.name} - success");
            enemy.m_faction = cc.m_originalFaction;
            enemy.m_swimSpeed = cc.m_swimSpeed;

            var monsterAI = enemy.GetComponent<MonsterAI>();
            if (monsterAI)
            {
                monsterAI.SetFollowTarget(null);
                //                monsterAI.ResetRandomMovement();
                //                monsterAI.ResetPatrolPoint();
                //                monsterAI.SetAlerted(alert: false);
                monsterAI.SetTarget(null);
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.Awake))]
        public static class Character_Awake_Patch
        {
            public static void Postfix(Character __instance)
            {
                if (!IsPacifist())
                {
                    return;
                }

                if (__instance == null)
                    return;

                CharmedCharacter guy = __m_allCharmedCharacters.Find(c => c.m_zdoid == __instance.GetZDOID());
                if (guy != null)
                {
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

        public static long __m_charmTimerSeconds = 0;

        static IEnumerator CharmTimerUpdate()
        {
            while (__m_charmTimerStarted)
            {
                __m_charmTimerSeconds++;

                Debug.LogWarning($"Charm Data");
                Debug.LogWarning($"Charm Seconds: {__m_charmTimerSeconds}");
                Debug.LogWarning($"Charm List: {__m_allCharmedCharacters.Count}");
                for (var i = 0; i < __m_allCharmedCharacters.Count; i++)
                {
                    var cc = __m_allCharmedCharacters[i];
                    Debug.LogWarning($"  Charm {i}: ZDOID {cc.m_zdoid} ExpireTime: {cc.m_charmExpireTime} Orig Faction: {cc.m_originalFaction}");
                }

                // For all charmed characters
                CharmedCharacter toRemove = null;
                foreach (var cc in __m_allCharmedCharacters)
                {
                    Minimap.instance.RemovePin(cc.m_pin);

                    if (__m_charmTimerSeconds >= cc.m_charmExpireTime)
                    {
                        toRemove = cc;
                        break;
                    }

                    Character target = GetCharacterFromZDOID(cc.m_zdoid);
                    if (target)
                    {
                        cc.m_pin = Minimap.instance.AddPin(target.transform.position, Minimap.PinType.Icon3, "", false, false);
                    }
                }

                if (toRemove != null)
                {
                    SetUncharmedState(toRemove);
                    Minimap.instance.RemovePin(toRemove.m_pin);

                    Character target = GetCharacterFromZDOID(toRemove.m_zdoid);
                    if (target)
                    {
                        Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"{target?.GetHoverName()} is no longer yours.");
                    }
                    else
                    {
                        Debug.LogWarning($"Target to uncharm not found for ZDOID {toRemove.m_zdoid}");
                    }

                    __m_allCharmedCharacters.Remove(toRemove);
                }

                yield return new WaitForSeconds(1f);
            }
        }

        private static void AddCharmEffect(Character target)
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
        public static class Projectile_OnHit_WoodArrowCharm
        {
            public static bool Prefix(Projectile __instance, Collider collider)
            {
                if (!IsPacifist())
                {
                    return true;
                }
                // Ensure it's a projectile owned by a player
                if (__instance == null || __instance.m_owner == null || __instance.m_owner.IsPlayer())
                    return true;

                // Get the item that spawned the projectile
                var item = __instance.m_ammo;
                if (item == null || item.m_shared == null)
                    return true;

                //                Debug.LogWarning($"[Projectile_OnHit_WoodArrowCharm] {__instance.m_ammo.m_shared.m_name} {collider.gameObject.name}");

                // Only apply to wood arrows
                if (!IsThrallArrow(item.m_shared.m_name))
                    return true;

                //                Debug.LogWarning($"[Projectile_OnHit_WoodArrowCharm] damage rejected for {collider.gameObject.name}");

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

                    // Skip if already charmed
                    if (IsCharmed(hitChar))
                    {
                        hitChar.m_seman.RemoveAllStatusEffects();
                        RemoveFromCharmedList(hitChar);
                    }

                    AddToCharmedList(hitChar, (long)GetCharmDuration());
                    ApplyGandrEffect(arrowName, hitChar);
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
                    Debug.LogWarning($"name: {vanillaRecipe.name} item: {vanillaRecipe?.m_item?.name} amount: {vanillaRecipe.m_amount} enabled: {vanillaRecipe.m_enabled} craftingStation = {vanillaRecipe.m_craftingStation?.m_name} repairStation = {vanillaRecipe.m_repairStation?.m_name}");
                }
                else
                {
                    Debug.LogError($"Could not find Recipe_{prefabName}");
                }

                vanillaRecipe.m_item = itemDrop;
                vanillaRecipe.m_amount = 5;
                vanillaRecipe.m_enabled = true;
                vanillaRecipe.m_craftingStation = null;
                //                vanillaRecipe.m_repairStation = null;
                vanillaRecipe.m_minStationLevel = 0;
                vanillaRecipe.m_resources = new Piece.Requirement[] {
                        new Piece.Requirement()
                        {
                            m_resItem = db.GetItemPrefab(ingredient).GetComponent<ItemDrop>(),
                            m_amount = 5,
                            m_amountPerLevel = 0
                        }
                    };
            }
            static void Postfix(ObjectDB __instance)
            {
                Debug.LogWarning($"ObjectDB.Awake() called");
                if (!IsPacifist())
                    return;

                // See if the Database is initialized
                if (ObjectDB.instance != null &&
                    ObjectDB.instance.m_items.Count != 0
                    && ObjectDB.instance.GetItemPrefab("Amber") != null)
                {
                    Debug.LogWarning($"ObjectDB available");

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

            Debug.LogWarning($"ApplyGandrEffect: {arrowName} {hitChar.m_name}");

            //foreach (var an in __m_gandrArrowData)
            //{
            //    Debug.LogWarning($"{an.m_arrowName} {an.m_ingredient} {an.m_name} {an.m_description}");
            //}

            GandrArrowData arrowData = __m_gandrArrowData.First(a => arrowName.ToLower() == a.m_name.ToLower());
            if (arrowData == null)
            {
                Debug.LogError($"No GandrArrowData found for arrow {arrowName}");
                return;
            }

            StatusEffect se = ScriptableObject.CreateInstance(arrowData.m_statusEffectType) as StatusEffect;

            se = hitChar.m_seman.AddStatusEffect(se);

            if (se != null)
            {
                Debug.LogWarning($"STATUS EFFECT APPLIED: {se.m_name} {se.m_ttl} {hitChar?.GetHoverName()} {arrowData.m_description}");
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"{se.m_name} {se.m_ttl} {hitChar?.GetHoverName()} {arrowData.m_description}.");
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
                            Character c = GetCharacterFromZDOID(cc.m_zdoid);
                            if (c)
                            {
                                c.Heal(c.GetMaxHealth() - c.GetHealth(), showText: true);
                                Debug.LogWarning($"Healed charmed enemy {c.name} to full health due to adrenaline overflow.");
                                if (trinketPopped)
                                {
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
                                    Hud.instance.AdrenalineBarFlash();
                                    player.m_adrenaline = 0;
                                }
                            }
                        }
                    }
                }

                return true;
            }
        }

        public class SE_GandrEffect : SE_Stats
        {
            float m_adrenalineScalar = 0.0f;
            float m_adrenalineScaleMagnitude = 1.0f;

            public override void Setup(Character character)
            {
                base.Setup(character);

                // SE lasts for this many seconds
                m_ttl = 30.0f;
                m_damageModifier = 2.0f;
                m_skillLevelModifier = 10.0f;
                m_skillLevel = SkillType.All;
                //                m_skillLevelModifier2 = 10.0f;
                m_name = "Base Gandr Effect";
            }
            public override void UpdateStatusEffect(float dt)
            {
                base.UpdateStatusEffect(dt);

                if (Player.m_localPlayer.GetMaxAdrenaline() > 0)
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
                HitData.DamageTypes dt = m_percentigeDamageModifiers;
                dt.Modify(1.0f + m_adrenalineScalar * m_adrenalineScaleMagnitude);
                hitData.m_damage.Add(dt);

                base.ModifyAttack(skill, ref hitData);
            }

            public override void OnDamaged(HitData hit, Character attacker)
            {
                base.OnDamaged(hit, attacker);

                hit.m_damage.Modify(1/(1+m_adrenalineScalar*m_adrenalineScaleMagnitude));
            }

            public override void Stop()
            {
                base.Stop();
            }

        }

/*
 * 	"Hit Damage Armor
(base health multiplier)"	"Damage Output
(base damage multiplier)"	Bonus Damage Type	"Bonus Damage Output
(base damage multiplier)"
ArrowWood	    2.0	2.0	Default	
ArrowFlint	    2.5	2.5	Pierce	    1.5
ArrowFire	    3.0	3.0	Fire	    1.5
ArrowBronze	    3.5	3.5	Blunt	    2.0
ArrowPoison	    4.0	4.0	Poison	    2.5
ArrowIron	    4.5	4.5	Slash	    3.0
ArrowFrost	    5.0	5.0	Frost	    3.5
ArrowObsidian	5.5	5.5	Slash	    4.0
ArrowSilver	    6.0	6.0	Spirit	    4.5
ArrowNeedle	    7.0	7.0	Pierce	    5.0
ArrowCarapace	8.0	8.0	Blunt	    6.0
ArrowCharred	9.0	9.0	Lightning	7.0
 */

        public class SE_GandrWood : SE_GandrEffect
        {
            public override void Setup(Character character)
            {
                base.Setup(character);
                m_percentigeDamageModifiers.m_fire = 2;
                m_percentigeDamageModifiers.m_frost = 2;
                m_percentigeDamageModifiers.m_lightning = 2;
                m_percentigeDamageModifiers.m_poison = 2;
                m_percentigeDamageModifiers.m_spirit = 2;

                m_name = "Wood Gandr Effect";
            }
        }
        public class SE_GandrFlint : SE_GandrEffect
        {
            public override void Setup(Character character)
            {
                base.Setup(character);
                m_damageModifier = 2.5f;
                m_addArmor = 0;
                m_armorMultiplier = 2.5f;
                m_percentigeDamageModifiers.m_pierce = 1.5f;
                m_name = "Flint Gandr Effect";

            }
        }
        public class SE_GandrFire : SE_GandrEffect
        {
            public override void Setup(Character character)
            {
                base.Setup(character);
                m_damageModifier = 3f;
                m_addArmor = 0;
                m_armorMultiplier = 3f;
                m_percentigeDamageModifiers.m_fire = 1.5f;
                m_name = "Fire Gandr Effect";

                Debug.LogWarning($"SE_GandrFire Setup called { m_percentigeDamageModifiers.ToString()}");
            }
        }

        public class SE_GandrBronze : SE_GandrEffect
        {
            public override void Setup(Character character)
            {
                base.Setup(character);
                m_damageModifier = 3.5f;
                m_addArmor = 0;
                m_armorMultiplier = 3.5f;
                m_percentigeDamageModifiers.m_blunt = 2f;
                m_name = "Bronze Gandr Effect";

            }
        }

        public class SE_GandrPoison : SE_GandrEffect
        {
            public override void Setup(Character character)
            {
                base.Setup(character);
                m_damageModifier = 4f;
                m_addArmor = 0;
                m_armorMultiplier = 4f;
                m_percentigeDamageModifiers.m_poison = 2f;
                m_name = "Poison Gandr Effect";
            }
        }
        public class SE_GandrIron : SE_GandrEffect
        {
            public override void Setup(Character character)
            {
                base.Setup(character);
                m_damageModifier = 4.5f;
                m_addArmor = 0;
                m_armorMultiplier = 4.5f;
                m_percentigeDamageModifiers.m_slash = 2.5f;
                m_name = "Iron Gandr Effect";
            }
        }
        public class SE_GandrFrost : SE_GandrEffect
        {
            public override void Setup(Character character)
            {
                base.Setup(character);
                m_damageModifier = 5f;
                m_addArmor = 0;
                m_armorMultiplier = 5f;
                m_percentigeDamageModifiers.m_frost = 3f;
                m_name = "Frost Gandr Effect";

            }
        }
        public class SE_GandrObsidian : SE_GandrEffect
        {
            public override void Setup(Character character)
            {
                base.Setup(character);
                m_damageModifier = 5.5f;
                m_addArmor = 0;
                m_armorMultiplier = 5.5f;
                m_percentigeDamageModifiers.m_slash = 3f;
                m_name = "Obsidian Gandr Effect";

            }
        }
        public class SE_GandrSilver : SE_GandrEffect
        {
            public override void Setup(Character character)
            {
                base.Setup(character);
                m_damageModifier = 6f;
                m_addArmor = 0;
                m_armorMultiplier = 6f;
                m_percentigeDamageModifiers.m_spirit = 3.5f;
                m_name = "Silver Gandr Effect";

            }
        }
        public class SE_GandrNeedle : SE_GandrEffect
        {
            public override void Setup(Character character)
            {
                base.Setup(character);
                m_damageModifier = 7f;
                m_addArmor = 0;
                m_armorMultiplier = 7f;
                m_percentigeDamageModifiers.m_pierce = 4f;
                m_name = "Needle Gandr Effect";

            }
        }
        public class SE_GandrCarapace : SE_GandrEffect
        {
            public override void Setup(Character character)
            {
                base.Setup(character);
                m_damageModifier = 8f;
                m_addArmor = 0;
                m_armorMultiplier = 7f;
                m_percentigeDamageModifiers.m_blunt = 5f;
                m_name = "Carapace Gandr Effect";

            }
        }
        public class SE_GandrCharred : SE_GandrEffect
        {
            public override void Setup(Character character)
            {
                base.Setup(character);
                m_damageModifier = 9f;
                m_addArmor = 0;
                m_armorMultiplier = 7f;
                m_percentigeDamageModifiers.m_lightning = 6f;
                m_name = "Charred Gandr Effect";
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

                    EnemyHud.HudData data = hudData.Value;

                    Transform charmTransform = data.m_gui.transform.Find("Charm");

                    CharmedCharacter cc = GetCharmedCharacter(c);
                    if (cc == null)
                    {
                        if (charmTransform && charmTransform.gameObject.activeSelf)
                        {
                            charmTransform.gameObject.SetActive(false);
                        }
                        continue;
                    }
                    
                    if (IsCharmed(c))
                    {
                        float remainingTime = cc.m_charmExpireTime - __m_charmTimerSeconds;

                        if (charmTransform && !charmTransform.gameObject.activeSelf)
                        {
                            charmTransform.gameObject.SetActive(true);
                        }

                        GuiBar charmBar = data.m_gui.transform.Find("Charm/health_fast_friendly").GetComponent<GuiBar>();
                        charmBar.SetValue(remainingTime / GetCharmDuration());
                        charmBar.SetColor(new Color((float)0xF3 / 255f, (float)0x87 / 255f, (float)0xC5 / 255f));
                    }
                }
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

                Transform newTransform = newBarObject.transform;
                newTransform.localPosition += new Vector3(0, -10, 0);

                Transform healthFastTransform = newTransform.Find("health_fast");
                if (healthFastTransform == null)
                {
                    Debug.LogError("Could not find health_fast transform!");
                }
                if (healthFastTransform?.gameObject == null)
                {
                    Debug.LogError("Could not find health_fast gameobject!");
                }
                healthFastTransform?.gameObject?.SetActive(false);
                Debug.LogError("healthFastTransform set to Active?");

                Transform healthSlowTransform = newTransform.Find("health_slow");
                healthSlowTransform?.gameObject?.SetActive(false);

                Transform healthFastFriendlyTransform = newTransform.Find("health_fast_friendly");
                healthFastFriendlyTransform?.gameObject?.SetActive(true);

                newBarObject.SetActive(false);
            }
        }
    }
}
