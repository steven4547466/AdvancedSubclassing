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

            server = new Handlers.Server();
            Server.RoundStarted += server.OnRoundStarted;
            Server.RoundEnded += server.OnRoundEnded;

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
            player = null;

            Server.RoundStarted -= server.OnRoundStarted;
            Server.RoundEnded -= server.OnRoundEnded;
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
                    SubClass c = new SubClass(item.Key, item.Value, config.ClassesOptionsStrings[item.Key], config.ClassesOptionsBool[item.Key], config.ClassesOptionsInt[item.Key], config.ClassesOptionsFloat[item.Key], config.ClassesOptionsSpawns[item.Key], config.ClassesOptionsSpawnItems[item.Key], config.ClassesOptionsAmmoOnSpawn[item.Key], config.ClassesOptionsAbilities[item.Key], config.ClassesOptionsAbilityCooldowns[item.Key]);
                    classes.Add(item.Key, c);
                    Log.Debug($"Loaded class {item.Key}", config.Debug);
                }
                catch(KeyNotFoundException e)
                {
                    throw new Exception("An option was not found, even if it's empty, make sure to include it. See the wiki for more info.");
                }
            }
            return classes;
        }
    }

    public class SubClass
    {

        public string Name = "";
        public RoleType AffectsRole = RoleType.None;

        public Dictionary<string, string> StringOptions = new Dictionary<string, string>();

        public Dictionary<string, bool> BoolOptions = new Dictionary<string, bool>();

        public Dictionary<string, int> IntOptions = new Dictionary<string, int>();

        public Dictionary<string, float> FloatOptions = new Dictionary<string, float>();

        public List<RoomType> SpawnLocations = new List<RoomType>();

        public List<ItemType> SpawnItems = new List<ItemType>();

        public Dictionary<AmmoType, int> SpawnAmmo = new Dictionary<AmmoType, int>();

        public List<AbilityType> Abilities = new List<AbilityType>();

        public Dictionary<AbilityType, float> AbilityCooldowns = new Dictionary<AbilityType, float>();

        public SubClass(string name, RoleType role, Dictionary<string, string> strings, Dictionary<string, bool> bools, Dictionary<string, int> ints, Dictionary<string, float> floats, List<RoomType> spawns, List<ItemType> items, Dictionary<AmmoType, int> ammo, List<AbilityType> abilities, Dictionary<AbilityType, float> cooldowns)
        {
            Name = name;
            AffectsRole = role;
            StringOptions = strings;
            BoolOptions = bools;
            IntOptions = ints;
            FloatOptions = floats;
            SpawnLocations = spawns;
            SpawnItems = items;
            SpawnAmmo = ammo;
            Abilities = abilities;
            AbilityCooldowns = cooldowns;
        }
    }

    public enum AbilityType
    {
        PryGates,
        GodMode,
        InvisibleUntilInteract,
        BypassKeycardReaders,
        HealGrenadeFrag,
        HealGrenadeFlash
    }
}
