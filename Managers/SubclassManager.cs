using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Interfaces;

using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.NodeDeserializers;
using YamlDotNet.Serialization.TypeInspectors;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization.ObjectGraphVisitors;
using Exiled.API.Enums;
using Microsoft.SqlServer.Server;


// -----------------------------------------------------------------------
// <copyright file="ConfigManager.cs" company="Exiled Team">
// Copyright (c) Exiled Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------


// I took this from Exiled because I need the same thing, but I needed to change some things.

namespace Subclass.Managers
{
    /// <summary>
    /// Used to handle plugin configs.
    /// </summary>
    public static class SubclassManager
    {
        /// <summary>
        /// Gets the config serializer.
        /// </summary>
        public static ISerializer Serializer { get; } = new SerializerBuilder()
            .WithTypeInspector(inner => new CommentGatheringTypeInspector(inner))
            .WithEmissionPhaseObjectGraphVisitor(args => new CommentsObjectGraphVisitor(args.InnerVisitor))
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreFields()
            .Build();

        /// <summary>
        /// Gets the config serializer.
        /// </summary>
        public static IDeserializer Deserializer { get; } = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .WithNodeDeserializer(inner => new ValidatingNodeDeserializer(inner), deserializer => deserializer.InsteadOf<ObjectNodeDeserializer>())
            .IgnoreFields()
            .IgnoreUnmatchedProperties()
            .Build();

        /// <summary>
        /// Loads all plugin configs.
        /// </summary>
        /// <param name="rawConfigs">The raw configs to be loaded.</param>
        /// <returns>Returns a dictionary of loaded configs.</returns>
        public static Dictionary<string, SubClass> LoadClasses()
        {
            try
            {
                Log.Info("Loading classes...");

                if (!Directory.Exists(Path.Combine(Paths.Configs, "Subclasses")))
                {
                    Log.Info("Subclasses directory not found, creating.");
                    Directory.CreateDirectory(Path.Combine(Paths.Configs, "Subclasses"));
                    Directory.CreateDirectory(Path.Combine(Paths.Configs, Path.Combine("Subclasses", "global")));
                    Directory.CreateDirectory(Path.Combine(Paths.Configs, Path.Combine("Subclasses", Server.Port.ToString())));
                    return null;
                }

                if (!Directory.Exists(Path.Combine(Paths.Configs, Path.Combine("Subclasses", "global"))))
                {
                    Log.Info("Subclasses global directory not found, creating.");
                    Directory.CreateDirectory(Path.Combine(Paths.Configs, Path.Combine("Subclasses", "global")));
                    Directory.CreateDirectory(Path.Combine(Paths.Configs, Path.Combine("Subclasses", Server.Port.ToString())));
                    return null;
                }

                if (!Directory.Exists(Path.Combine(Paths.Configs, Path.Combine("Subclasses", Server.Port.ToString()))))
                {
                    Log.Info("Subclasses directory for this port not found, creating.");
                    Directory.CreateDirectory(Path.Combine(Paths.Configs, Path.Combine("Subclasses", Server.Port.ToString())));
                    return null;
                }

                List<string> classes = new List<string>();
                classes.AddRange(Directory.GetFiles(Path.Combine(Paths.Configs, Path.Combine("Subclasses", "global"))));
                classes.AddRange(Directory.GetFiles(Path.Combine(Paths.Configs, Path.Combine("Subclasses", Server.Port.ToString()))));

                Dictionary<string, SubClass> subClasses = new Dictionary<string, SubClass>();

                foreach (string path in classes)
                {
                    string file = Read(path);
                    Dictionary<string, object> rawClass = Deserializer.Deserialize<Dictionary<string, object>>(file) ?? new Dictionary<string, object>();
                    try
                    {
                        Dictionary<string, object> obj = (Dictionary<string, object>) Deserializer.Deserialize(Serializer.Serialize(rawClass), typeof(Dictionary<string, object>));
                        Log.Debug($"Attempting to load class: {(string)obj["name"]}", Subclass.Instance.Config.Debug);

                        Dictionary<object, object> boolOptionsTemp = (Dictionary<object, object>)obj["boolean_options"];
                        Dictionary<string, bool> boolOptions = new Dictionary<string, bool>();
                        foreach (var item in boolOptionsTemp)
                        {
                            boolOptions.Add((string)item.Key, bool.Parse((string)item.Value));
                        }
                        if (!boolOptions["Enabled"]) continue;

                        List<string> ffRules = obj.ContainsKey("advanced_ff_rules") ? ((IEnumerable<object>)obj["advanced_ff_rules"]).Cast<string>().ToList() : null;
                        List<string> onHitEffects = obj.ContainsKey("on_hit_effects") ? ((IEnumerable<object>)obj["on_hit_effects"]).Cast<string>().ToList() : null;
                        List<string> onSpawnEffects = obj.ContainsKey("on_spawn_effects") ? ((IEnumerable<object>)obj["on_spawn_effects"]).Cast<string>().ToList() : null;
                        Team endsRoundWith = obj.ContainsKey("ends_round_with") ? (Team)Enum.Parse(typeof(Team), (string)obj["ends_round_with"]) : Team.RIP;

                        List<string> cantDamageTemp = obj.ContainsKey("roles_that_cant_damage") ? ((IEnumerable<object>)obj["roles_that_cant_damage"]).Cast<string>().ToList() : null;
                        List<RoleType> cantDamage = new List<RoleType>();
                        if (cantDamageTemp != null)
                        {
                            foreach (string role in cantDamageTemp) cantDamage.Add((RoleType)Enum.Parse(typeof(RoleType), role));
                        }

                        List<string> affectsRolesTemp = ((IEnumerable<object>)obj["affects_roles"]).Cast<string>().ToList();
                        List<RoleType> affectsRoles = new List<RoleType>();
                        foreach (string role in affectsRolesTemp) affectsRoles.Add((RoleType)Enum.Parse(typeof(RoleType), role));

                        Dictionary<object, object> stringOptionsTemp = (Dictionary<object, object>)obj["string_options"];
                        Dictionary<string, string> stringOptions = new Dictionary<string, string>();
                        foreach (var item in stringOptionsTemp)
                        {
                            stringOptions.Add((string)item.Key, (string)item.Value);
                        }

                        Dictionary<object, object> intOptionsTemp = (Dictionary<object, object>)obj["integer_options"];
                        Dictionary<string, int> intOptions = new Dictionary<string, int>();
                        foreach (var item in intOptionsTemp)
                        {
                            intOptions.Add((string)item.Key, int.Parse((string) item.Value));
                        }

                        Dictionary<object, object> floatOptionsTemp = (Dictionary<object, object>)obj["float_options"];
                        Dictionary<string, float> floatOptions = new Dictionary<string, float>();
                        foreach (var item in floatOptionsTemp)
                        {
                            floatOptions.Add((string)item.Key, float.Parse((string)item.Value));
                        }

                        List<string> spawnsTemp = ((IEnumerable<object>)obj["spawn_locations"]).Cast<string>().ToList();
                        List<RoomType> spawns = new List<RoomType>();
                        foreach (var item in spawnsTemp) spawns.Add((RoomType)Enum.Parse(typeof(RoomType), item));

                        Dictionary<object, object> spawnItemsTemp = (Dictionary<object, object>)obj["spawn_items"];
                        Dictionary<int, Dictionary<ItemType, float>> spawnItems = new Dictionary<int, Dictionary<ItemType, float>>();

                        foreach (var item in spawnItemsTemp)
                        {
                            spawnItems.Add(int.Parse((string)item.Key), new Dictionary<ItemType, float>());
                            foreach (var item2 in (Dictionary<object, object>) spawnItemsTemp[item.Key])
                                spawnItems[int.Parse((string)item.Key)].Add((ItemType)Enum.Parse(typeof(ItemType), (string)item2.Key), float.Parse((string)item2.Value));
                        }

                        Dictionary<object, object> ammoTemp = (Dictionary<object, object>)obj["spawn_ammo"];
                        Dictionary<AmmoType, int> ammo = new Dictionary<AmmoType, int>();
                        foreach (var item in ammoTemp)
                        {
                            ammo.Add((AmmoType)Enum.Parse(typeof(AmmoType), (string)item.Key), int.Parse((string)item.Value));
                        }

                        List<string> abilitiesTemp = ((IEnumerable<object>)obj["abilities"]).Cast<string>().ToList();
                        List<AbilityType> abilities = new List<AbilityType>();
                        foreach (string ability in abilitiesTemp) abilities.Add((AbilityType)Enum.Parse(typeof(AbilityType), ability));


                        Dictionary<object, object> abilityCooldownsTemp = (Dictionary<object, object>)obj["ability_cooldowns"];
                        Dictionary<AbilityType, float> abilityCooldowns = new Dictionary<AbilityType, float>();
                        foreach (var item in abilityCooldownsTemp)
                        {
                            abilityCooldowns.Add((AbilityType)Enum.Parse(typeof(AbilityType), (string)item.Key), float.Parse((string)item.Value));
                        }

                        subClasses.Add((string)obj["name"], new SubClass((string) obj["name"], affectsRoles, stringOptions, boolOptions, intOptions, floatOptions, 
                            spawns, spawnItems, ammo, abilities, abilityCooldowns, ffRules, onHitEffects, onSpawnEffects, cantDamage, endsRoundWith));
                        Log.Debug($"Successfully loaded class: {(string)obj["name"]}", Subclass.Instance.Config.Debug);
                    }
                    catch (YamlException yamlException)
                    {
                        Log.Error($"Class with path: {path} could not be loaded Skipping. {yamlException}");
                    }
                    catch (FormatException e)
                    {
                        Log.Error($"Class with path: {path} could not be loaded due to a format exception. {e}");
                    }
                }

                Log.Info("Classes loaded successfully!");

                return subClasses;
            }
            catch (Exception exception)
            {
                Log.Error($"An error has occurred while loading subclasses! {exception}");

                return null;
            }
        }

        /// <summary>
        /// Read all plugin configs.
        /// </summary>
        /// <returns>Returns the read configs.</returns>
        public static string Read(string path)
        {
            try
            {
                if (File.Exists(Path.Combine(Path.Combine(Paths.Configs, Path.Combine("Subclasses", path)))))
                    return File.ReadAllText(Path.Combine(Paths.Configs, Path.Combine("Subclasses", path)));
            }
            catch (Exception exception)
            {
                Log.Error($"An error has occurred while reading class from {Paths.Configs} path: {exception}");
            }

            return string.Empty;
        }

    }
}


// -----------------------------------------------------------------------
// <copyright file="CommentGatheringTypeInspector.cs" company="Exiled Team">
// Copyright (c) Exiled Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------
namespace Subclass.Managers
{
    /// <summary>
    /// Spurce: https://dotnetfiddle.net/8M6iIE.
    /// </summary>
    internal sealed class CommentGatheringTypeInspector : TypeInspectorSkeleton
    {
        private readonly ITypeInspector innerTypeDescriptor;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommentGatheringTypeInspector"/> class.
        /// </summary>
        /// <param name="innerTypeDescriptor">The inner type description instance.</param>
        public CommentGatheringTypeInspector(ITypeInspector innerTypeDescriptor)
        {
            this.innerTypeDescriptor = innerTypeDescriptor ?? throw new ArgumentNullException("innerTypeDescriptor");
        }

        /// <inheritdoc/>
        public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object container)
        {
            return innerTypeDescriptor
                .GetProperties(type, container)
                .Select(descriptor => new CommentsPropertyDescriptor(descriptor));
        }
    }
}

// -----------------------------------------------------------------------
// <copyright file="CommentsPropertyDescriptor.cs" company="Exiled Team">
// Copyright (c) Exiled Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Subclass.Managers
{


    /// <summary>
    /// Source: https://dotnetfiddle.net/8M6iIE.
    /// </summary>
    internal sealed class CommentsPropertyDescriptor : IPropertyDescriptor
    {
        private readonly IPropertyDescriptor baseDescriptor;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommentsPropertyDescriptor"/> class.
        /// </summary>
        /// <param name="baseDescriptor">The base descriptor instance.</param>
        public CommentsPropertyDescriptor(IPropertyDescriptor baseDescriptor)
        {
            this.baseDescriptor = baseDescriptor;
            Name = baseDescriptor.Name;
        }

        /// <inheritdoc/>
        public string Name { get; set; }

        /// <inheritdoc/>
        public Type Type => baseDescriptor.Type;

        /// <inheritdoc/>
        public Type TypeOverride
        {
            get => baseDescriptor.TypeOverride;
            set => baseDescriptor.TypeOverride = value;
        }

        /// <inheritdoc/>
        public int Order { get; set; }

        /// <inheritdoc/>
        public ScalarStyle ScalarStyle
        {
            get => baseDescriptor.ScalarStyle;
            set => baseDescriptor.ScalarStyle = value;
        }

        /// <inheritdoc/>
        public bool CanWrite => baseDescriptor.CanWrite;

        /// <inheritdoc/>
        public void Write(object target, object value)
        {
            baseDescriptor.Write(target, value);
        }

        /// <inheritdoc/>
        public T GetCustomAttribute<T>()
            where T : Attribute
        {
            return baseDescriptor.GetCustomAttribute<T>();
        }

        /// <inheritdoc/>
        public IObjectDescriptor Read(object target)
        {
            var description = baseDescriptor.GetCustomAttribute<DescriptionAttribute>();
            return description != null
                ? new CommentsObjectDescriptor(baseDescriptor.Read(target), description.Description)
                : baseDescriptor.Read(target);
        }
    }
}

// -----------------------------------------------------------------------
// <copyright file="CommentsObjectDescriptor.cs" company="Exiled Team">
// Copyright (c) Exiled Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Subclass.Managers
{
    /// <summary>
    /// Source: https://dotnetfiddle.net/8M6iIE.
    /// </summary>
    internal sealed class CommentsObjectDescriptor : IObjectDescriptor
    {
        private readonly IObjectDescriptor innerDescriptor;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommentsObjectDescriptor"/> class.
        /// </summary>
        /// <param name="innerDescriptor">The inner descriptor instance.</param>
        /// <param name="comment">The comment to be written.</param>
        public CommentsObjectDescriptor(IObjectDescriptor innerDescriptor, string comment)
        {
            this.innerDescriptor = innerDescriptor;
            Comment = comment;
        }

        /// <summary>
        /// Gets the comment to be written.
        /// </summary>
        public string Comment { get; private set; }

        /// <inheritdoc/>
        public object Value => innerDescriptor.Value;

        /// <inheritdoc/>
        public Type Type => innerDescriptor.Type;

        /// <inheritdoc/>
        public Type StaticType => innerDescriptor.StaticType;

        /// <inheritdoc/>
        public ScalarStyle ScalarStyle => innerDescriptor.ScalarStyle;
    }
}

// -----------------------------------------------------------------------
// <copyright file="CommentsObjectGraphVisitor.cs" company="Exiled Team">
// Copyright (c) Exiled Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Subclass.Managers
{

    /// <summary>
    /// Source: https://dotnetfiddle.net/8M6iIE.
    /// </summary>
    internal sealed class CommentsObjectGraphVisitor : ChainedObjectGraphVisitor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommentsObjectGraphVisitor"/> class.
        /// </summary>
        /// <param name="nextVisitor">The next visitor instance.</param>
        public CommentsObjectGraphVisitor(IObjectGraphVisitor<IEmitter> nextVisitor)
            : base(nextVisitor)
        {
        }

        /// <inheritdoc/>
        public override bool EnterMapping(IPropertyDescriptor key, IObjectDescriptor value, IEmitter context)
        {
            if (value is CommentsObjectDescriptor commentsDescriptor && commentsDescriptor.Comment != null)
            {
                context.Emit(new Comment(commentsDescriptor.Comment, false));
            }

            return base.EnterMapping(key, value, context);
        }
    }
}

// -----------------------------------------------------------------------
// <copyright file="ValidatingNodeDeserializer.cs" company="Exiled Team">
// Copyright (c) Exiled Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Subclass.Managers
{
    /// <summary>
    /// Basic configs validation.
    /// </summary>
    internal sealed class ValidatingNodeDeserializer : INodeDeserializer
    {
        private readonly INodeDeserializer nodeDeserializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidatingNodeDeserializer"/> class.
        /// </summary>
        /// <param name="nodeDeserializer">The node deserializer instance.</param>
        public ValidatingNodeDeserializer(INodeDeserializer nodeDeserializer) => this.nodeDeserializer = nodeDeserializer;

        /// <inheritdoc/>
        public bool Deserialize(IParser parser, Type expectedType, Func<IParser, Type, object> nestedObjectDeserializer, out object value)
        {
            if (nodeDeserializer.Deserialize(parser, expectedType, nestedObjectDeserializer, out value))
            {
                Validator.ValidateObject(value, new ValidationContext(value, null, null), true);

                return true;
            }

            return false;
        }
    }
}