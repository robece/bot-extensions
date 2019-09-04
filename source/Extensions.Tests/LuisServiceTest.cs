using Extensions.BotBuilder.Luis.Domain;
using Extensions.BotBuilder.Luis.Services;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Extensions.Tests
{
    public class LuisServiceTest : IDisposable
    {
        private string EnvironmentName { get; set; } = nameof(LuisServiceTest);
        private string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

        private LuisConfig configuration = new LuisConfig()
        {
            BingSpellCheckSubscriptionKey = Guid.NewGuid().ToString(),
            LuisApplications = new List<LuisApp>() { new LuisApp() { AppId = Guid.NewGuid().ToString(), AuthoringKey = Guid.NewGuid().ToString(), Endpoint = "http://endpoint", Name = "name" } },
            LuisRouterUrl = "http://luis_router_url"
        };

        public LuisServiceTest()
        {
            dynamic dynamicConfiguration = new ExpandoObject();
            dynamicConfiguration.LuisConfig = configuration;
            var jsonConfiguration = JsonConvert.SerializeObject(dynamicConfiguration);
            File.WriteAllText(Path.Combine(ContentRootPath, $"appsettings.{EnvironmentName}.json"), jsonConfiguration);
        }

        public void Dispose()
        {
            File.Delete(Path.Combine(ContentRootPath, $"appsettings.{EnvironmentName}.json"));
        }

        [Fact]
        public async void GetConfigurationTest()
        {
            // arrage
            var storage = new MemoryStorage();
            var userState = new UserState(storage);
            var conversationState = new ConversationState(storage);
            var adapter = new TestAdapter().Use(new AutoSaveStateMiddleware(conversationState));
            var dialogState = conversationState.CreateProperty<DialogState>("dialogState");
            var dialogs = new DialogSet(dialogState);
            var steps = new WaterfallStep[]
            {
                async (step, cancellationToken) =>
                {
                    await step.Context.SendActivityAsync("response");

                    // act
                    ILuisService luisService = new LuisService(EnvironmentName, ContentRootPath, null, null);
                    LuisConfig config = luisService.GetConfiguration();

                    // assert
                    Assert.Equal(configuration.BingSpellCheckSubscriptionKey, config.BingSpellCheckSubscriptionKey);
                    Assert.Collection<LuisApp>(configuration.LuisApplications, x=> Xunit.Assert.Contains("name", x.Name));
                    Assert.Equal(configuration.LuisRouterUrl, config.LuisRouterUrl);

                    return Dialog.EndOfTurn;
                }
            };
            dialogs.Add(new WaterfallDialog(
                "test",
                steps));

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);
                await dc.ContinueDialogAsync(cancellationToken);
                if (!turnContext.Responded)
                {
                    await dc.BeginDialogAsync("test", null, cancellationToken);
                }
            })
            .Send("ask")
            .AssertReply("response")
            .StartTestAsync();
        }

        [Fact]
        public async void RecognizeTest()
        {
            // arrage
            var expectedIntent = "sampleIntent";
            LuisResult luisResult = new LuisResult();
            List<IntentModel> intents = new List<IntentModel>();
            intents.Add(new IntentModel() { Intent = expectedIntent, Score = 100 });
            luisResult.Intents = intents;
            luisResult.TopScoringIntent = new IntentModel() { Intent = "sampleIntent", Score = 100 };
            luisResult.CompositeEntities = new List<CompositeEntityModel>();
            luisResult.Entities = new List<EntityModel>();

            var jsonRecognizerResult = JsonConvert.SerializeObject(luisResult);

            var handlerMock = new Mock<HttpClientHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = new StringContent(jsonRecognizerResult),
               })
               .Verifiable();

            var storage = new MemoryStorage();
            var userState = new UserState(storage);
            var conversationState = new ConversationState(storage);
            var adapter = new TestAdapter().Use(new AutoSaveStateMiddleware(conversationState));
            var dialogState = conversationState.CreateProperty<DialogState>("dialogState");
            var dialogs = new DialogSet(dialogState);
            var steps = new WaterfallStep[]
            {
                async (step, cancellationToken) =>
                {
                    await step.Context.SendActivityAsync("response");

                    // act
                    step.Context.Activity.Text = "hello";
                    ILuisService luisService = new LuisService(EnvironmentName, ContentRootPath, null, handlerMock.Object);
                    var result = await luisService.LuisServices["name"].RecognizeAsync(step.Context, new CancellationToken());
                    var item = result.Intents.FirstOrDefault();

                    // assert
                    Assert.Equal(expectedIntent, item.Key);

                    return Dialog.EndOfTurn;
                }
            };
            dialogs.Add(new WaterfallDialog(
                "test",
                steps));

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);
                await dc.ContinueDialogAsync(cancellationToken);
                if (!turnContext.Responded)
                {
                    await dc.BeginDialogAsync("test", null, cancellationToken);
                }
            })
            .Send("ask")
            .AssertReply("response")
            .StartTestAsync();
        }

        [Fact]
        public async void RecognizeInMultipleAppsTest()
        {
            // arrage
            var expectedIntent = "sampleIntent";
            LuisResult luisResult = new LuisResult();
            List<IntentModel> intents = new List<IntentModel>();
            intents.Add(new IntentModel() { Intent = expectedIntent, Score = 100 });
            luisResult.Intents = intents;
            luisResult.TopScoringIntent = new IntentModel() { Intent = "sampleIntent", Score = 100 };
            luisResult.CompositeEntities = new List<CompositeEntityModel>();
            luisResult.Entities = new List<EntityModel>();

            var jsonRecognizerResult = JsonConvert.SerializeObject(luisResult);

            var handlerMock = new Mock<HttpClientHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = new StringContent(jsonRecognizerResult),
               })
               .Verifiable();

            var storage = new MemoryStorage();
            var userState = new UserState(storage);
            var conversationState = new ConversationState(storage);
            var adapter = new TestAdapter().Use(new AutoSaveStateMiddleware(conversationState));
            var dialogState = conversationState.CreateProperty<DialogState>("dialogState");
            var dialogs = new DialogSet(dialogState);
            var steps = new WaterfallStep[]
            {
                async (step, cancellationToken) =>
                {
                    await step.Context.SendActivityAsync("response");

                    // act
                    step.Context.Activity.Text = "hello";
                    ILuisService luisService = new LuisService(EnvironmentName, ContentRootPath, null, handlerMock.Object);

                    var list = await luisService.RecognizeInMultipleAppsAsync(step.Context, .90);
                    var item = list.ToList().FirstOrDefault();

                    // assert
                    Assert.Equal(expectedIntent, item.Intent);

                    return Dialog.EndOfTurn;
                }
            };
            dialogs.Add(new WaterfallDialog(
                "test",
                steps));

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);
                await dc.ContinueDialogAsync(cancellationToken);
                if (!turnContext.Responded)
                {
                    await dc.BeginDialogAsync("test", null, cancellationToken);
                }
            })
            .Send("ask")
            .AssertReply("response")
            .StartTestAsync();
        }
    }
}