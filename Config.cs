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
	}
}
