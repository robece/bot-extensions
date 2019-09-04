using Extensions.BotBuilder.Luis.Domain;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Extensions.BotBuilder.Luis.Services
{
    public interface ILuisService
    {
        Dictionary<string, LuisRecognizer> LuisServices { get; }

        Task<IEnumerable<LuisAppDetail>> RecognizeInMultipleAppsAsync(ITurnContext context, double score);

        LuisConfig GetConfiguration();
    }
}