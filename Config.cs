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

        [Description("Makes chance to get classes additive instead of individual.")]
        public bool AdditiveChance { get; set; } = false;

        [Description("Makes chance to get classes weighted instead of individual. additive_chance must be false for this to work.")]
        public bool WeightedChance { get; set; }

		public Dictionary<RoleType, float> BaseWeights { get; set; } = new Dictionary<RoleType, float>
		{
			{
				RoleType.ChaosInsurgency,
				0f
			},
			{
				RoleType.ClassD,
				0f
			},
			{
				RoleType.FacilityGuard,
				0f
			},
			{
				RoleType.NtfCadet,
				0f
			},
			{
				RoleType.NtfCommander,
				0f
			},
			{
				RoleType.NtfLieutenant,
				0f
			},
			{
				RoleType.NtfScientist,
				0f
			},
			{
				RoleType.Scientist,
				0f
			},
			{
				RoleType.Scp049,
				0f
			},
			{
				RoleType.Scp0492,
				0f
			},
			{
				RoleType.Scp079,
				0f
			},
			{
				RoleType.Scp096,
				0f
			},
			{
				RoleType.Scp106,
				0f
			},
			{
				RoleType.Scp173,
				0f
			},
			{
				RoleType.Scp93953,
				0f
			},
			{
				RoleType.Scp93989,
				0f
			},
			{
				RoleType.Tutorial,
				0f
			}
		};

		[Description("The separator for spawn parameter arguments. Set this to (a) character(s) that are unique in class names, team names, etc.")]
        public string SpawnParameterSeparator { get; set; } = "_";

        [Description("The default time the got class broadcast lasts, still can be overridden by specific classes.")]
        public float GlobalBroadcastTime { get; set; } = 5f;

        [Description("The message shown to players when they try to go invisible, but already are.")]
        public string AlreadyInvisibleMessage { get; set; } = "You're already invisible!";

        [Description("The message shown to players when they fail to disguise.")]
        public string DisguiseFailedMessage { get; set; } = "Disguise failed, your team, or SCPs, are the most within range.";

        [Description("The message shown to players when their revive fails because they aren't near a dead body.")]
        public string ReviveFailedNoBodyMessage { get; set; } = "You must be near a dead body.";

        [Description("The message shown to players when their revive fails because the body is not revivable.")]
        public string CantReviveMessage { get; set; } = "This player is not revivable.";

        [Description("The message shown to players when they use the help command and the class provided is not found.")]
        public string HelpNoClassFound { get; set; } = "Class not found!";

        [Description("The message shown to players when they use the help command and the class provided has no help string.")]
        public string HelpNoHelpFound { get; set; } = "Class has no help message!";

        [Description("The message shown to players when they use the help command and provided no arguments.")]
        public string HelpNoArgumentsProvided { get; set; } = "Please provide the class name if you don't have a subclass.";

        [Description("The message shown to players when they use the an ability that requires spectators and there are none.")]
        public string NoAvailableSpectators { get; set; } = "There are no available spectators.";

		[Description("The message shown to players when they don't have enough stamina to do an action.")]
		public string OutOfStaminaMessage { get; set; } = "You do not have enough stamina.";

	}
}
