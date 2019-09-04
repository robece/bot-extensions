using System.Collections.Generic;

namespace Extensions.BotBuilder.Luis.Domain
{
    public class LuisConfig
    {
        public string LuisRouterUrl { get; set; }
        public string BingSpellCheckSubscriptionKey { get; set; }
        public IEnumerable<LuisApp> LuisApplications { get; set; }
    }
}