using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.API.Interfaces;
using System.Collections.Generic;
using System.ComponentModel;

namespace Subclass
{
    public sealed class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;

        [Description("Enable debug logs?")]
        public bool Debug { get; set; } = false;

        [Description("The list of subclasses. Please see the github wiki for more info.")]
        public Dictionary<string, RoleType> Classes { get; set; } = new Dictionary<string, RoleType>()
        {
            { "ExampleClass", RoleType.ClassD }
        };

        [Description("The list of subclass string options. Please see the github wiki for more info.")]
        public Dictionary<string, Dictionary<string, string>> ClassesOptionsStrings { get; set; } = new Dictionary<string, Dictionary<string, string>>()
        {
            { "ExampleClass", new Dictionary<string, string>()
                {
                    { "GotClassMessage", "You are an ExampleClass, this is an example." },
                    { "AbilityCooldownMessage", "Your {ability} ability is on cooldown for {seconds} seconds!" }
                }
            }
        };

        [Description("The list of subclass true/false options. Please see the github wiki for more info.")]
        public Dictionary<string, Dictionary<string, bool>> ClassesOptionsBool { get; set; } = new Dictionary<string, Dictionary<string, bool>>()
        {
            { "ExampleClass", new Dictionary<string, bool>()
                {
                    { "Enabled", false },
                    { "DisregardHasFF", false },
                    { "HasFriendlyFire", false },
                    { "DisregardTakesFF", false },
                    { "TakesFriendlyFire", false },
                } 
            }
        };

        [Description("The list of subclass integer options. Please see the github wiki for more info.")]
        public Dictionary<string, Dictionary<string, int>> ClassesOptionsInt { get; set; } = new Dictionary<string, Dictionary<string, int>>()
        {
            { "ExampleClass", new Dictionary<string, int>()
                {
                    { "MaxHealth", 100 },
                    { "HealthOnSpawn", 100 },
                    { "MaxArmor", 100 },
                    { "ArmorOnSpawn", 100 }
                }
            }
        };

        [Description("The list of subclass float options. Please see the github wiki for more info.")]
        public Dictionary<string, Dictionary<string, float>> ClassesOptionsFloat { get; set; } = new Dictionary<string, Dictionary<string, float>>()
        {
            { "ExampleClass", new Dictionary<string, float>()
                {
                    { "ChanceToGet", 2f }
                }
            }
        };

        [Description("The list of subclass spawn locations. Please see the github wiki for more info.")]
        public Dictionary<string, List<RoomType>> ClassesOptionsSpawns { get; set; } = new Dictionary<string, List<RoomType>>()
        {
            { "ExampleClass", new List<RoomType>()
                {
                    RoomType.Unknown
                }
            }
        };

        [Description("The list of subclass spawn items. Please see the github wiki for more info.")]
        public Dictionary<string, List<ItemType>> ClassesOptionsSpawnItems { get; set; } = new Dictionary<string, List<ItemType>>()
        {
                { "ExampleClass", new List<ItemType>()
                    {
                        ItemType.GunCOM15,
                    }
                }
        };

        [Description("The list of subclass ammo on spawn. Please see the github wiki for more info.")]
        public Dictionary<string, Dictionary<AmmoType, int>> ClassesOptionsAmmoOnSpawn { get; set; } = new Dictionary<string, Dictionary<AmmoType, int>>()
        {
                { "ExampleClass", new Dictionary<AmmoType, int>()
                    {
                        { AmmoType.Nato556, -1},
                        { AmmoType.Nato762, -1},
                        { AmmoType.Nato9, -1}
                    }
                }
        };

        [Description("The list of subclass abilities. Please see the github wiki for more info.")]
        public Dictionary<string, List<AbilityType>> ClassesOptionsAbilities { get; set; } = new Dictionary<string, List<AbilityType>>()
        {
            { "ExampleClass", new List<AbilityType>()
                {
                    AbilityType.PryGates,
                    AbilityType.GodMode,
                    AbilityType.InvisibleUntilInteract
                }
            }
        };

        [Description("The list of subclass ability cooldowns. Please see the github wiki for more info.")]
        public Dictionary<string, Dictionary<AbilityType, float>> ClassesOptionsAbilityCooldowns { get; set; } = new Dictionary<string, Dictionary<AbilityType, float>>()
        {
            { "ExampleClass", new Dictionary<AbilityType, float>()
                {
                    { AbilityType.PryGates, 5f },
                    { AbilityType.InvisibleUntilInteract, 5f }
                }
            }
        };

        [Description("The list of subclasses that this subclass can fire upon regardless of other friendly fire rules.")]
        public Dictionary<string, List<string>> AdvancedFfRules { get; set; } = new Dictionary<string, List<string>>()
        {
            { "ExampleClass", new List<string>()}
        };

        [Description("The list of effects this player could apply when they deal damage to another player. Please see the wiki for more info.")]
        public Dictionary<string, List<string>> OnHitEffects { get; set; } = new Dictionary<string, List<string>>()
        {
            { "ExampleClass", new List<string>()}
        };

        [Description("The list of effects this player has on spawn. Please see the wiki for more info.")]
        public Dictionary<string, List<string>> OnSpawnEffects { get; set; } = new Dictionary<string, List<string>>()
        {
            { "ExampleClass", new List<string>()}
        };
    }
}
