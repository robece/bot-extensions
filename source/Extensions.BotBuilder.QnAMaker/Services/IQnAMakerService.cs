using Extensions.BotBuilder.QnAMaker.Domain;
using System.Collections.Generic;

namespace Extensions.BotBuilder.QnAMaker.Services
{
    public interface IQnAMakerService
    {
        Dictionary<string, Microsoft.Bot.Builder.AI.QnA.QnAMaker> QnAMakerServices { get; }

        QnAMakerConfig GetConfiguration();
    }
}