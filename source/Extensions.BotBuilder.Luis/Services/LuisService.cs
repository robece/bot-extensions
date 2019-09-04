using Extensions.BotBuilder.Luis.Domain;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Extensions.BotBuilder.Luis.Services
{
    public class LuisService : ILuisService
    {
        private readonly LuisConfig config = null;
        private readonly HttpClientHandler httpClientHandler = null;
        private readonly IBotTelemetryClient botTelemetryClient = null;

        public Dictionary<string, LuisRecognizer> LuisServices { get; }

        public LuisService(string environmentName, string contentRootPath, IBotTelemetryClient botTelemetryClient = null, HttpClientHandler httpClientHandler = null)
        {
            var builder = new ConfigurationBuilder()
              .SetBasePath(contentRootPath)
              .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
              .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
              .AddEnvironmentVariables();

            var configuration = builder.Build();

            config = new LuisConfig();
            configuration.GetSection("LuisConfig").Bind(config);

            this.botTelemetryClient = botTelemetryClient;
            this.httpClientHandler = httpClientHandler;
            this.LuisServices = BuildDictionary();
        }

        public LuisConfig GetConfiguration() => config;

        public async Task<IEnumerable<LuisAppDetail>> RecognizeInMultipleAppsAsync(ITurnContext context, double score)
        {
            List<LuisAppDetail> result = new List<LuisAppDetail>();

            foreach (var app in config.LuisApplications)
            {
                var recognizerResult = await LuisServices[app.Name].RecognizeAsync(context, new CancellationToken());
                var topIntent = recognizerResult?.GetTopScoringIntent();
                if (topIntent != null && topIntent.HasValue && topIntent.Value.score >= score && topIntent.Value.intent != "None")
                {
                    result.Add(new LuisAppDetail() { Name = app.Name, Intent = topIntent.Value.intent, Score = topIntent.Value.score });
                }
            }

            return result;
        }

        private Dictionary<string, LuisRecognizer> BuildDictionary()
        {
            Dictionary<string, LuisRecognizer> result = new Dictionary<string, LuisRecognizer>();

            foreach (LuisApp app in config.LuisApplications)
            {
                var luis = new LuisApplication(app.AppId, app.AuthoringKey, app.Endpoint);

                LuisPredictionOptions luisPredictionOptions = null;
                LuisRecognizer recognizer = null;

                bool needsPredictionOptions = false;
                if ((!string.IsNullOrEmpty(config.BingSpellCheckSubscriptionKey)) || (botTelemetryClient != null))
                {
                    needsPredictionOptions = true;
                }

                if (needsPredictionOptions)
                {
                    luisPredictionOptions = new LuisPredictionOptions();

                    if (botTelemetryClient != null)
                    {
                        luisPredictionOptions.TelemetryClient = botTelemetryClient;
                        luisPredictionOptions.Log = true;
                        luisPredictionOptions.LogPersonalInformation = true;
                    }

                    if (!string.IsNullOrEmpty(config.BingSpellCheckSubscriptionKey))
                    {
                        luisPredictionOptions.BingSpellCheckSubscriptionKey = config.BingSpellCheckSubscriptionKey;
                        luisPredictionOptions.SpellCheck = true;
                        luisPredictionOptions.IncludeAllIntents = true;
                    }

                    recognizer = (httpClientHandler != null) ? new LuisRecognizer(luis, luisPredictionOptions, false, httpClientHandler) : new LuisRecognizer(luis, luisPredictionOptions);
                }
                else
                {
                    recognizer = (httpClientHandler != null) ? new LuisRecognizer(luis, null, false, httpClientHandler) : new LuisRecognizer(luis);
                }

                result.Add(app.Name, recognizer);
            }

            return result;
        }
    }
}