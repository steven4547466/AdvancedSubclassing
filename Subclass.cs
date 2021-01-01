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
using Subclass.Managers;
using System.IO;
using Exiled.Loader;

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
        public override Version Version { get; } = new Version(1, 2, 11);
        public override Version RequiredExiledVersion { get; } = new Version(2, 1, 22);
        public override string Prefix { get; } = "Subclass";

        public Handlers.Player player { get; set; }
        public Handlers.Server server { get; set; }
        public Handlers.Map map { get; set; }

        public Dictionary<string, SubClass> Classes { get; set; }
        public Dictionary<RoleType, Dictionary<SubClass, float>> ClassesAdditive = null;

        public bool Scp035Enabled = Loader.Plugins.Any(p => p.Name == "scp035" && p.Config.IsEnabled);
        public bool CommonUtilsEnabled = Loader.Plugins.Any(p => p.Name == "Common Utilities" && p.Config.IsEnabled);

        int harmonyPatches = 0;
        private Harmony HarmonyInstance { get; set; }

        public override void OnEnabled()
        {
            if (Subclass.Instance.Config.IsEnabled == false)
            {
                Log.Info("Subclass was disabled, why did this run?");
                return;
            }
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
                TrackingAndMethods.RemoveAndAddRoles(player, true);
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
            //Player.Died += player.OnDied;
            Player.Dying += player.OnDying;
            Player.Shooting += player.OnShooting;
            Player.InteractingLocker += player.OnInteractingLocker;
            Player.UnlockingGenerator += player.OnUnlockingGenerator;
            Player.TriggeringTesla += player.OnTriggeringTesla;
            Player.ChangingRole += player.OnChangingRole;
            Player.Spawning += player.OnSpawning;
            Player.Hurting += player.OnHurting;
            Player.EnteringFemurBreaker += player.OnEnteringFemurBreaker;
            Player.Escaping += player.OnEscaping;
            Player.FailingEscapePocketDimension += player.OnFailingEscapePocketDimension;
            Player.Interacted += player.OnInteracted;
            Player.UsingMedicalItem += player.OnUsingMedicalItem;
            Player.EnteringPocketDimension += player.OnEnteringPocketDimension;
            Player.SpawningRagdoll += player.OnSpawningRagdoll;

            server = new Handlers.Server();
            Server.RoundStarted += server.OnRoundStarted;
            Server.RoundEnded += server.OnRoundEnded;
            Server.RespawningTeam += server.OnRespawningTeam;

            map = new Handlers.Map();
            Map.ExplodingGrenade += map.OnExplodingGrenade;
        }

        public void UnregisterEvents()
        {
            Log.Info("Events unregistered");
            Player.InteractingDoor -= player.OnInteractingDoor;
            //Player.Died -= player.OnDied;
            Player.Dying -= player.OnDying;
            Player.Shooting -= player.OnShooting;
            Player.InteractingLocker -= player.OnInteractingLocker;
            Player.UnlockingGenerator -= player.OnUnlockingGenerator;
            Player.TriggeringTesla -= player.OnTriggeringTesla;
            Player.ChangingRole -= player.OnChangingRole;
            Player.Spawning -= player.OnSpawning;
            Player.Hurting -= player.OnHurting;
            Player.EnteringFemurBreaker -= player.OnEnteringFemurBreaker;
            Player.Escaping -= player.OnEscaping;
            Player.FailingEscapePocketDimension -= player.OnFailingEscapePocketDimension;
            Player.Interacted -= player.OnInteracted;
            Player.UsingMedicalItem -= player.OnUsingMedicalItem;
            Player.EnteringPocketDimension -= player.OnEnteringPocketDimension;
            Player.SpawningRagdoll -= player.OnSpawningRagdoll;
            player = null;

            Server.RoundStarted -= server.OnRoundStarted;
            Server.RoundEnded -= server.OnRoundEnded;
            Server.RespawningTeam -= server.OnRespawningTeam;

            server = null;

            Map.ExplodingGrenade -= map.OnExplodingGrenade;
            map = null;
        }

        public Dictionary<string, SubClass> GetClasses()
        {
            Dictionary<string, SubClass> classes = new Dictionary<string, SubClass>();
            Config config = Instance.Config;
            classes = SubclassManager.LoadClasses();
            if (config.AdditiveChance)
            {
                ClassesAdditive = new Dictionary<RoleType, Dictionary<SubClass, float>>();
                foreach (var item in classes)
                {
                    foreach (RoleType role in item.Value.AffectsRoles)
                    {
                        if (!ClassesAdditive.ContainsKey(role)) ClassesAdditive.Add(role, new Dictionary<SubClass, float>() { { item.Value, item.Value.FloatOptions["ChanceToGet"] } });
                        else ClassesAdditive[role].Add(item.Value, item.Value.FloatOptions["ChanceToGet"]);
                    }

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
            return classes;
        }
    }

    public class SubClass
    {

        public string Name = "";

        public List<RoleType> AffectsRoles = new List<RoleType>(){RoleType.None};
        public Dictionary<string, float> AffectsUsers = new Dictionary<string, float>();
        public Dictionary<string, float> Permissions = new Dictionary<string, float>();
        public Dictionary<string, int> SpawnParameters = new Dictionary<string, int>();

        public Dictionary<string, string> StringOptions = new Dictionary<string, string>();

        public Dictionary<string, bool> BoolOptions = new Dictionary<string, bool>();

        public Dictionary<string, int> IntOptions = new Dictionary<string, int>();

        public Dictionary<string, float> FloatOptions = new Dictionary<string, float>();

        public List<string> SpawnLocations = new List<string>();

        public Dictionary<int, Dictionary<ItemType, float>> SpawnItems = new Dictionary<int, Dictionary<ItemType, float>>();

        public Dictionary<AmmoType, int> SpawnAmmo = new Dictionary<AmmoType, int>();

        public List<AbilityType> Abilities = new List<AbilityType>();

        public Dictionary<AbilityType, float> AbilityCooldowns = new Dictionary<AbilityType, float>();
        public Dictionary<AbilityType, float> InitialAbilityCooldowns = new Dictionary<AbilityType, float>();

        public List<string> AdvancedFFRules = new List<string>();

        public List<string> OnHitEffects = new List<string>();

        public List<string> OnSpawnEffects = new List<string>();

        public Dictionary<string, List<string>> OnDamagedEffects = new Dictionary<string, List<string>>();

        public List<RoleType> RolesThatCantDamage = new List<RoleType>();

        public List<RoleType> CantDamageRoles = new List<RoleType>();

        public string EndsRoundWith = "RIP";

        public RoleType SpawnsAs = RoleType.None;

        public RoleType[] EscapesAs = { RoleType.None, RoleType.None };

        public SubClass(string name, List<RoleType> role, Dictionary<string, string> strings, Dictionary<string, bool> bools,
            Dictionary<string, int> ints, Dictionary<string, float> floats, List<string> spawns, Dictionary<int, Dictionary<ItemType, float>> items,
            Dictionary<AmmoType, int> ammo, List<AbilityType> abilities, Dictionary<AbilityType, float> cooldowns,
            List<string> ffRules = null, List<string> onHitEffects = null, List<string> spawnEffects = null, List<RoleType> cantDamage = null,
            string endsRoundWith = "RIP", RoleType spawnsAs = RoleType.None, RoleType[] escapesAs = null, Dictionary<string, List<string>> onTakeDamage = null, List<RoleType> cantDamageRoles = null,
            Dictionary<string, float> affectsUsers = null, Dictionary<string, float> permissions = null, Dictionary<AbilityType, float> initialAbilityCooldowns = null,
            Dictionary<string, int> spawnParameters = null)
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
            if (cantDamage != null) RolesThatCantDamage = cantDamage;
            if (endsRoundWith != "RIP") EndsRoundWith = endsRoundWith;
            if (spawnsAs != RoleType.None) SpawnsAs = spawnsAs;
            if (escapesAs != null) EscapesAs = escapesAs;
            if (onTakeDamage != null) OnDamagedEffects = onTakeDamage;
            if (cantDamageRoles != null) CantDamageRoles = cantDamageRoles;
            if (affectsUsers != null) AffectsUsers = affectsUsers;
            if (permissions != null) Permissions = permissions;
            if (initialAbilityCooldowns != null) InitialAbilityCooldowns = initialAbilityCooldowns;
            if (spawnParameters != null) SpawnParameters = spawnParameters;
        }

        public SubClass(SubClass subClass)
        {
            Name = subClass.Name;
            AffectsRoles = new List<RoleType>(subClass.AffectsRoles);
            StringOptions = new Dictionary<string, string>(subClass.StringOptions);
            BoolOptions = new Dictionary<string, bool>(subClass.BoolOptions);
            IntOptions = new Dictionary<string, int>(subClass.IntOptions);
            FloatOptions = new Dictionary<string, float>(subClass.FloatOptions);
            SpawnLocations = new List<string>(subClass.SpawnLocations);
            SpawnItems = new Dictionary<int, Dictionary<ItemType, float>>(subClass.SpawnItems);
            SpawnAmmo = new Dictionary<AmmoType, int>(subClass.SpawnAmmo);
            Abilities = new List<AbilityType>(subClass.Abilities);
            AbilityCooldowns = new Dictionary<AbilityType, float>(subClass.AbilityCooldowns);
            AdvancedFFRules = new List<string>(subClass.AdvancedFFRules);
            OnHitEffects = new List<string>(subClass.OnHitEffects);
            OnSpawnEffects = new List<string>(subClass.OnSpawnEffects);
            RolesThatCantDamage = new List<RoleType>(subClass.RolesThatCantDamage);
            EndsRoundWith = subClass.EndsRoundWith;
            SpawnsAs = subClass.SpawnsAs;
            EscapesAs = subClass.EscapesAs;
            OnDamagedEffects = subClass.OnDamagedEffects;
            CantDamageRoles = subClass.CantDamageRoles;
            AffectsUsers = subClass.AffectsUsers;
            InitialAbilityCooldowns = subClass.InitialAbilityCooldowns;
            Permissions = subClass.Permissions;
            SpawnParameters = subClass.SpawnParameters;
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
        LifeSteal,
        Zombie106,
        FlashOnCommand,
        GrenadeOnCommand,
        ExplodeOnDeath,
        InvisibleOnCommand,
        Disguise,
        CantHeal,
        HealAura,
        DamageAura,
        Regeneration,
        Infect,
        BackupCommand,
        Vent,
        PowerSurge,
        Summon
    }
}
