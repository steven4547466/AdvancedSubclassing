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
// Well... I basically changed everything but the serializer and deserializer stuff.

namespace Subclass.Managers
{
    public static class SubclassManager
    {
        public static ISerializer Serializer { get; } = new SerializerBuilder()
            .WithTypeInspector(inner => new CommentGatheringTypeInspector(inner))
            .WithEmissionPhaseObjectGraphVisitor(args => new CommentsObjectGraphVisitor(args.InnerVisitor))
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreFields()
            .Build();

        public static IDeserializer Deserializer { get; } = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .WithNodeDeserializer(inner => new ValidatingNodeDeserializer(inner), deserializer => deserializer.InsteadOf<ObjectNodeDeserializer>())
            .IgnoreFields()
            .IgnoreUnmatchedProperties()
            .Build();

        public static Dictionary<string, SubClass> LoadClasses()
        {
            try
            {
                Log.Info("Loading classes...");

                if (!Directory.Exists(Path.Combine(Paths.Configs, "Subclasses")))
                {
                    Log.Info("Subclasses directory not found, creating.");
                    Directory.CreateDirectory(Path.Combine(Paths.Configs, "Subclasses"));
                    Directory.CreateDirectory(Path.Combine(Paths.Configs, "Subclasses", "global"));
                    Directory.CreateDirectory(Path.Combine(Paths.Configs, "Subclasses", Server.Port.ToString()));
                    return new Dictionary<string, SubClass>();
                }

                if (!Directory.Exists(Path.Combine(Paths.Configs,"Subclasses", "global")))
                {
                    Log.Info("Subclasses global directory not found, creating.");
                    Directory.CreateDirectory(Path.Combine(Paths.Configs, "Subclasses", "global"));
                    Directory.CreateDirectory(Path.Combine(Paths.Configs, "Subclasses", Server.Port.ToString()));
                    return new Dictionary<string, SubClass>();
                }

                if (!Directory.Exists(Path.Combine(Paths.Configs, "Subclasses", Server.Port.ToString())))
                {
                    Log.Info("Subclasses directory for this port not found, creating.");
                    Directory.CreateDirectory(Path.Combine(Paths.Configs, "Subclasses", Server.Port.ToString()));
                    return new Dictionary<string, SubClass>();
                }

                List<string> classes = new List<string>();
                classes.AddRange(Directory.GetFiles(Path.Combine(Paths.Configs, "Subclasses", "global")));
                foreach (string directory in Directory.GetDirectories(Path.Combine(Paths.Configs, "Subclasses", "global")))
                {
                    classes.AddRange(Directory.GetFiles(Path.Combine(Paths.Configs, "Subclasses", "global", directory)));
                }

                classes.AddRange(Directory.GetFiles(Path.Combine(Paths.Configs, "Subclasses", Server.Port.ToString())));
                foreach(string directory in Directory.GetDirectories(Path.Combine(Paths.Configs, "Subclasses", Server.Port.ToString())))
                {
                    classes.AddRange(Directory.GetFiles(Path.Combine(Paths.Configs, "Subclasses", Server.Port.ToString(), directory)));
                }

                Dictionary<string, SubClass> subClasses = new Dictionary<string, SubClass>();

                foreach (string path in classes.Where(f => f.EndsWith("yml")))
                {
                    string file = Read(path);
                    Dictionary<string, object> rawClass = Deserializer.Deserialize<Dictionary<string, object>>(file) ?? new Dictionary<string, object>();
                    try
                    {
                        Dictionary<string, object> obj = (Dictionary<string, object>) Deserializer.Deserialize(Serializer.Serialize(rawClass), typeof(Dictionary<string, object>));
                        Log.Debug($"Attempting to load class: {(string)obj["name"]}", Subclass.Instance.Config.Debug);


                        Log.Debug($"Attempting to load bool options for class: {(string)obj["name"]}", Subclass.Instance.Config.Debug);
                        Dictionary<object, object> boolOptionsTemp = (Dictionary<object, object>)obj["boolean_options"];
                        Dictionary<string, bool> boolOptions = new Dictionary<string, bool>();
                        foreach (var item in boolOptionsTemp)
                        {
                            boolOptions.Add((string)item.Key, bool.Parse((string)item.Value));
                        }
                        if (!boolOptions["Enabled"]) continue;

                        Log.Debug($"Attempting to load ff rules for class: {(string)obj["name"]}", Subclass.Instance.Config.Debug);
                        List<string> ffRules = obj.ContainsKey("advanced_ff_rules") ? ((IEnumerable<object>)obj["advanced_ff_rules"]).Cast<string>().ToList() : null;

                        Log.Debug($"Attempting to load on hit effects for class: {(string)obj["name"]}", Subclass.Instance.Config.Debug);
                        List<string> onHitEffects = obj.ContainsKey("on_hit_effects") ? ((IEnumerable<object>)obj["on_hit_effects"]).Cast<string>().ToList() : null;

                        Log.Debug($"Attempting to load on spawn effects for class: {(string)obj["name"]}", Subclass.Instance.Config.Debug);
                        List<string> onSpawnEffects = obj.ContainsKey("on_spawn_effects") ? ((IEnumerable<object>)obj["on_spawn_effects"]).Cast<string>().ToList() : null;

                        Log.Debug($"Attempting to load on damaged effects for class: {(string)obj["name"]}", Subclass.Instance.Config.Debug);
                        Dictionary<string, List<string>> onTakeDamage = new Dictionary<string, List<string>>();
                        if (obj.ContainsKey("on_damaged_effects"))
                        {
                            Dictionary<object, object> onTakeDamageTemp = (Dictionary<object, object>)obj["on_damaged_effects"];
                            foreach (var item in onTakeDamageTemp)
                            {
                                onTakeDamage.Add(((string)item.Key).ToUpper(), ((IEnumerable<object>)item.Value).Cast<string>().ToList());
                            }
                        }

                        Log.Debug($"Attempting to load ends round with for class: {(string)obj["name"]}", Subclass.Instance.Config.Debug);
                        Team endsRoundWith = obj.ContainsKey("ends_round_with") ? (Team)Enum.Parse(typeof(Team), (string)obj["ends_round_with"]) : Team.RIP;

                        Log.Debug($"Attempting to load roles that cant damage for class: {(string)obj["name"]}", Subclass.Instance.Config.Debug);
                        List<string> cantDamageTemp = obj.ContainsKey("roles_that_cant_damage") ? ((IEnumerable<object>)obj["roles_that_cant_damage"]).Cast<string>().ToList() : null;
                        List<RoleType> cantDamage = new List<RoleType>();
                        if (cantDamageTemp != null)
                        {
                            foreach (string role in cantDamageTemp) cantDamage.Add((RoleType)Enum.Parse(typeof(RoleType), role));
                        }

                        Log.Debug($"Attempting to load affects roles for class: {(string)obj["name"]}", Subclass.Instance.Config.Debug);
                        List<string> affectsRolesTemp = ((IEnumerable<object>)obj["affects_roles"]).Cast<string>().ToList();
                        List<RoleType> affectsRoles = new List<RoleType>();
                        foreach (string role in affectsRolesTemp) affectsRoles.Add((RoleType)Enum.Parse(typeof(RoleType), role));

                        Log.Debug($"Attempting to load string options for class: {(string)obj["name"]}", Subclass.Instance.Config.Debug);
                        Dictionary<object, object> stringOptionsTemp = (Dictionary<object, object>)obj["string_options"];
                        Dictionary<string, string> stringOptions = new Dictionary<string, string>();
                        foreach (var item in stringOptionsTemp)
                        {
                            stringOptions.Add((string)item.Key, (string)item.Value);
                        }

                        Log.Debug($"Attempting to load int options for class: {(string)obj["name"]}", Subclass.Instance.Config.Debug);
                        Dictionary<object, object> intOptionsTemp = (Dictionary<object, object>)obj["integer_options"];
                        Dictionary<string, int> intOptions = new Dictionary<string, int>();
                        foreach (var item in intOptionsTemp)
                        {
                            intOptions.Add((string)item.Key, int.Parse((string) item.Value));
                        }

                        Log.Debug($"Attempting to load float options for class: {(string)obj["name"]}", Subclass.Instance.Config.Debug);
                        Dictionary<object, object> floatOptionsTemp = (Dictionary<object, object>)obj["float_options"];
                        Dictionary<string, float> floatOptions = new Dictionary<string, float>();
                        foreach (var item in floatOptionsTemp)
                        {
                            floatOptions.Add((string)item.Key, float.Parse((string)item.Value));
                        }

                        Log.Debug($"Attempting to load spawns for class: {(string)obj["name"]}", Subclass.Instance.Config.Debug);
                        List<string> spawnsTemp = ((IEnumerable<object>)obj["spawn_locations"]).Cast<string>().ToList();
                        List<RoomType> spawns = new List<RoomType>();
                        foreach (var item in spawnsTemp) spawns.Add((RoomType)Enum.Parse(typeof(RoomType), item));

                        Log.Debug($"Attempting to load spawn items for class: {(string)obj["name"]}", Subclass.Instance.Config.Debug);
                        Dictionary<object, object> spawnItemsTemp = (Dictionary<object, object>)obj["spawn_items"];
                        Dictionary<int, Dictionary<ItemType, float>> spawnItems = new Dictionary<int, Dictionary<ItemType, float>>();
                        foreach (var item in spawnItemsTemp)
                        {
                            spawnItems.Add(int.Parse((string)item.Key), new Dictionary<ItemType, float>());
                            foreach (var item2 in (Dictionary<object, object>) spawnItemsTemp[item.Key])
                                spawnItems[int.Parse((string)item.Key)].Add((ItemType)Enum.Parse(typeof(ItemType), (string)item2.Key), float.Parse((string)item2.Value));
                        }

                        Log.Debug($"Attempting to load spawn ammo for class: {(string)obj["name"]}", Subclass.Instance.Config.Debug);
                        Dictionary<object, object> ammoTemp = (Dictionary<object, object>)obj["spawn_ammo"];
                        Dictionary<AmmoType, int> ammo = new Dictionary<AmmoType, int>();
                        foreach (var item in ammoTemp)
                        {
                            ammo.Add((AmmoType)Enum.Parse(typeof(AmmoType), (string)item.Key), int.Parse((string)item.Value));
                        }


                        Log.Debug($"Attempting to load abilities for class: {(string)obj["name"]}", Subclass.Instance.Config.Debug);
                        List<string> abilitiesTemp = ((IEnumerable<object>)obj["abilities"]).Cast<string>().ToList();
                        List<AbilityType> abilities = new List<AbilityType>();
                        foreach (string ability in abilitiesTemp) abilities.Add((AbilityType)Enum.Parse(typeof(AbilityType), ability));

                        Log.Debug($"Attempting to load ability cooldowns for class: {(string)obj["name"]}", Subclass.Instance.Config.Debug);
                        Dictionary<object, object> abilityCooldownsTemp = (Dictionary<object, object>)obj["ability_cooldowns"];
                        Dictionary<AbilityType, float> abilityCooldowns = new Dictionary<AbilityType, float>();
                        foreach (var item in abilityCooldownsTemp)
                        {
                            abilityCooldowns.Add((AbilityType)Enum.Parse(typeof(AbilityType), (string)item.Key), float.Parse((string)item.Value));
                        }

                        Log.Debug($"Attempting to load spawns as for class: {(string)obj["name"]}", Subclass.Instance.Config.Debug);
                        RoleType spawnsAs = obj.ContainsKey("spawns_as") ? (RoleType)Enum.Parse(typeof(RoleType), (string)obj["spawns_as"]) : RoleType.None;

                        Log.Debug($"Attempting to load escapes as for class: {(string)obj["name"]}", Subclass.Instance.Config.Debug);
                        RoleType[] escapesAs = { RoleType.None, RoleType.None };
                        if (obj.ContainsKey("escapes_as"))
                        {
                            Dictionary<object, object> escapesAsTemp = (Dictionary<object, object>)obj["escapes_as"];
                            if (escapesAsTemp.ContainsKey("not_cuffed"))
                                escapesAs[0] = (RoleType)Enum.Parse(typeof(RoleType), (string)escapesAsTemp["not_cuffed"]);
                            else
                                escapesAs[0] = RoleType.None;
                            if (escapesAsTemp.ContainsKey("cuffed"))
                                escapesAs[1] = (RoleType)Enum.Parse(typeof(RoleType), (string)escapesAsTemp["cuffed"]);
                            else
                                escapesAs[1] = RoleType.None;
                        }

                        subClasses.Add((string)obj["name"], new SubClass((string) obj["name"], affectsRoles, stringOptions, boolOptions, intOptions, floatOptions, 
                            spawns, spawnItems, ammo, abilities, abilityCooldowns, ffRules, onHitEffects, onSpawnEffects, cantDamage, endsRoundWith, spawnsAs, escapesAs, onTakeDamage));
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
    /// Source: https://dotnetfiddle.net/8M6iIE.
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