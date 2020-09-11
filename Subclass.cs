using Exiled.API.Enums;
using Exiled.API.Features;
using HarmonyLib;
using MEC;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Player = Exiled.Events.Handlers.Player;
using Server = Exiled.Events.Handlers.Server;
using Map = Exiled.Events.Handlers.Map;
using System.Reflection;
using CustomPlayerEffects;
using Exiled.Permissions.Commands.Permissions;

namespace Subclass
{
    public class Subclass : Plugin<Config>
    {

        private static readonly Lazy<Subclass> LazyInstance = new Lazy<Subclass>(() => new Subclass());
        public static Subclass Instance => LazyInstance.Value;

        private Subclass() { }

        public override PluginPriority Priority { get; } = PluginPriority.Last;
        public override string Name { get; } = "Subclass";
        public override string Author { get; } = "Steven4547466";
        public override Version Version { get; } = new Version(1, 0, 0);
        public override Version RequiredExiledVersion { get; } = new Version(2, 1, 3);
        public override string Prefix { get; } = "Subclass";

        public Handlers.Player player { get; set; }
        public Handlers.Server server { get; set; }
        public Handlers.Map map { get; set; }

        public Dictionary<string, SubClass> Classes { get; set; }
        public Dictionary<RoleType, Dictionary<SubClass, float>> ClassesAdditive = new Dictionary<RoleType, Dictionary<SubClass, float>>();

        int harmonyPatches = 0;
        public Harmony HarmonyInstance { get; private set; }

        public override void OnEnabled()
        {
            if (Subclass.Instance.Config.IsEnabled == false) return;
            base.OnEnabled();
            Log.Info("Subclass enabled.");
            RegisterEvents();
            Classes = GetClasses();

            HarmonyInstance = new Harmony($"steven4547466.subclass-{++harmonyPatches}");
            HarmonyInstance.PatchAll();
        }

        public override void OnDisabled()
        {
            base.OnDisabled();
            Log.Info("Subclass disabled.");
            UnregisterEvents();
            HarmonyInstance.UnpatchAll();
            foreach (Exiled.API.Features.Player player in Exiled.API.Features.Player.List)
            {
                Tracking.RemoveAndAddRoles(player, true);
            }
        }

        public override void OnReloaded()
        {
            base.OnReloaded();
            Log.Info("Subclass reloading.");
        }

        public void RegisterEvents()
        {
            player = new Handlers.Player();
            Player.InteractingDoor += player.OnInteractingDoor;
            Player.Died += player.OnDied;
            Player.Shooting += player.OnShooting;
            Player.InteractingLocker += player.OnInteractingLocker;
            Player.UnlockingGenerator += player.OnUnlockingGenerator;
            Player.TriggeringTesla += player.OnTriggeringTesla;
            Player.ChangingRole += player.OnChangingRole;
            Player.Spawning += player.OnSpawning;
            Player.Hurting += player.OnHurting;
            Player.EnteringFemurBreaker += player.OnEnteringFemurBreaker;

            server = new Handlers.Server();
            Server.RoundStarted += server.OnRoundStarted;
            Server.RoundEnded += server.OnRoundEnded;
            Server.SendingConsoleCommand += server.OnSendingConsoleCommand;
            Server.RespawningTeam += server.OnRespawningTeam;

            map = new Handlers.Map();
            Map.ExplodingGrenade += map.OnExplodingGrenade;
        }

        public void UnregisterEvents()
        {
            Log.Info("Events unregistered");
            Player.InteractingDoor -= player.OnInteractingDoor;
            Player.Died -= player.OnDied;
            Player.Shooting -= player.OnShooting;
            Player.InteractingLocker -= player.OnInteractingLocker;
            Player.UnlockingGenerator -= player.OnUnlockingGenerator;
            Player.TriggeringTesla -= player.OnTriggeringTesla;
            Player.ChangingRole -= player.OnChangingRole;
            Player.Spawning -= player.OnSpawning;
            Player.Hurting -= player.OnHurting;
            Player.EnteringFemurBreaker -= player.OnEnteringFemurBreaker;
            player = null;

            Server.RoundStarted -= server.OnRoundStarted;
            Server.RoundEnded -= server.OnRoundEnded;
            Server.SendingConsoleCommand -= server.OnSendingConsoleCommand;
            Server.RespawningTeam -= server.OnRespawningTeam;
            server = null;

            Map.ExplodingGrenade -= map.OnExplodingGrenade;
            map = null;
        }

        public Dictionary<string, SubClass> GetClasses()
        {
            Dictionary<string, SubClass> classes = new Dictionary<string, SubClass>();
            Config config = Instance.Config;
            foreach (var item in Instance.Config.Classes)
            {
                try
                {
                    List<string> ffRules = config.AdvancedFfRules.ContainsKey(item.Key) ? config.AdvancedFfRules[item.Key] : null;
                    List<string> onHitEffects = config.OnHitEffects.ContainsKey(item.Key) ? config.OnHitEffects[item.Key] : null;
                    List<string> onSpawnEffects = config.OnSpawnEffects.ContainsKey(item.Key) ? config.OnSpawnEffects[item.Key] : null;


                    SubClass c = new SubClass(item.Key, item.Value, config.ClassesOptionsStrings[item.Key], config.ClassesOptionsBool[item.Key], config.ClassesOptionsInt[item.Key], config.ClassesOptionsFloat[item.Key], config.ClassesOptionsSpawns[item.Key], config.ClassesOptionsSpawnItems[item.Key], config.ClassesOptionsAmmoOnSpawn[item.Key], config.ClassesOptionsAbilities[item.Key], config.ClassesOptionsAbilityCooldowns[item.Key], ffRules, onHitEffects, onSpawnEffects);
                    classes.Add(item.Key, c);
                    Log.Debug($"Loaded class {item.Key}", config.Debug);
                    if (config.AdditiveChance)
                    {
                        foreach (RoleType role in item.Value)
                        {
                            if (!ClassesAdditive.ContainsKey(role)) ClassesAdditive.Add(role, new Dictionary<SubClass, float>() { { c, c.FloatOptions["ChanceToGet"] } });
                            else ClassesAdditive[role].Add(c, c.FloatOptions["ChanceToGet"]);
                        }
                        //var additiveClasses = ClassesAdditive.ToList();
                        Dictionary<RoleType, Dictionary<SubClass, float>> classesAdditiveCopy = new Dictionary<RoleType, Dictionary<SubClass, float>>();
                        foreach (RoleType role in ClassesAdditive.Keys)
                        {
                            var additiveClasses = ClassesAdditive[role].ToList();
                            additiveClasses.Sort((x, y) => y.Value.CompareTo(x.Value));
                            if (!classesAdditiveCopy.ContainsKey(role)) classesAdditiveCopy.Add(role, new Dictionary<SubClass, float>());
                            classesAdditiveCopy[role] = additiveClasses.ToDictionary(x => x.Key, x => x.Value);
                        }
                        ClassesAdditive.Clear();
                        Dictionary<RoleType, float> sums = new Dictionary<RoleType, float>();
                        foreach (var roles in classesAdditiveCopy)
                        {
                            foreach (SubClass sclass in classesAdditiveCopy[roles.Key].Keys)
                            {
                                if (!sums.ContainsKey(roles.Key)) sums.Add(roles.Key, 0);
                                sums[roles.Key] += sclass.FloatOptions["ChanceToGet"];
                                if (!ClassesAdditive.ContainsKey(roles.Key)) ClassesAdditive.Add(roles.Key, new Dictionary<SubClass, float>() { { sclass, sclass.FloatOptions["ChanceToGet"] } });
                                else ClassesAdditive[roles.Key].Add(sclass, sums[roles.Key]);
                            }
                        }
                    }
                    else
                        ClassesAdditive = null;
                }
                catch(KeyNotFoundException e)
                {
                    throw new Exception($"An option was not found, even if it's empty, make sure to include it. See the wiki for more info. Class: {item.Key}");
                }catch (NullReferenceException e)
                {
                    throw new Exception($"Class: {item.Key} was not present in one of the options dictionaries. even if it's empty, make sure to include it. See the wiki for more info.");
                }
            }
            return classes;
        }
    }

    public class SubClass
    {

        public string Name = "";
        public List<RoleType> AffectsRoles = new List<RoleType>(){RoleType.None};

        public Dictionary<string, string> StringOptions = new Dictionary<string, string>();

        public Dictionary<string, bool> BoolOptions = new Dictionary<string, bool>();

        public Dictionary<string, int> IntOptions = new Dictionary<string, int>();

        public Dictionary<string, float> FloatOptions = new Dictionary<string, float>();

        public List<RoomType> SpawnLocations = new List<RoomType>();

        public Dictionary<int, Dictionary<ItemType, float>> SpawnItems = new Dictionary<int, Dictionary<ItemType, float>>();

        public Dictionary<AmmoType, int> SpawnAmmo = new Dictionary<AmmoType, int>();

        public List<AbilityType> Abilities = new List<AbilityType>();

        public Dictionary<AbilityType, float> AbilityCooldowns = new Dictionary<AbilityType, float>();

        public List<string> AdvancedFFRules = new List<string>();

        public List<string> OnHitEffects = new List<string>();

        public List<string> OnSpawnEffects = new List<string>();

        public SubClass(string name, List<RoleType> role, Dictionary<string, string> strings, Dictionary<string, bool> bools, 
            Dictionary<string, int> ints, Dictionary<string, float> floats, List<RoomType> spawns, Dictionary<int, Dictionary<ItemType, float>> items, 
            Dictionary<AmmoType, int> ammo, List<AbilityType> abilities, Dictionary<AbilityType, float> cooldowns,
            List<string> ffRules = null, List<string> onHitEffects = null, List<string> spawnEffects = null)
        {
            Name = name;
            AffectsRoles = role;
            StringOptions = strings;
            BoolOptions = bools;
            IntOptions = ints;
            FloatOptions = floats;
            SpawnLocations = spawns;
            SpawnItems = items;
            SpawnAmmo = ammo;
            Abilities = abilities;
            AbilityCooldowns = cooldowns;
            if (ffRules != null) AdvancedFFRules = ffRules;
            if (onHitEffects != null) OnHitEffects = onHitEffects;
            if (spawnEffects != null) OnSpawnEffects = spawnEffects;
        }
    }

    public enum AbilityType
    {
        PryGates,
        GodMode,
        InvisibleUntilInteract,
        BypassKeycardReaders,
        HealGrenadeFrag,
        HealGrenadeFlash,
        BypassTeslaGates,
        InfiniteSprint,
        Disable096Trigger,
        Disable173Stop,
        Revive,
        Echolocate,
        Scp939Vision,
        NoArmorDecay,
        NoClip,
        NoSCP207Damage,
        NoSCPDamage,
        NoHumanDamage,
        InfiniteAmmo,
        Nimble,
        Necromancy,
        FlashImmune,
        GrenadeImmune,
        CantBeSacraficed,
        CantActivateFemurBreaker,
        LifeSteal
    }
}
