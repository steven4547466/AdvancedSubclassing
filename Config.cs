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
    }
}
