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
    }
}
