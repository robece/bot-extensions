using Extensions.BotBuilder.QnAMaker.Domain;
using Extensions.BotBuilder.QnAMaker.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.AI.QnA;
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
    public class QnAMakerServiceTest : IDisposable
    {
        private string EnvironmentName { get; set; } = nameof(QnAMakerServiceTest);
        private string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

        private QnAMakerConfig configuration = new QnAMakerConfig()
        {
            KbId = "kbid",
            Name = "name",
            EndpointKey = "endpoint_key",
            Hostname = "hostname"
        };

        public QnAMakerServiceTest()
        {
            dynamic dynamicConfiguration = new ExpandoObject();
            dynamicConfiguration.QnAMakerConfig = configuration;
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
            HttpClient httpClient = new HttpClient();
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
                    IQnAMakerService qnAMakerService = new QnAMakerService(httpClient, EnvironmentName, ContentRootPath);
                    QnAMakerConfig config = qnAMakerService.GetConfiguration();

                    // assert
                    Assert.Equal(configuration.KbId, config.KbId);
                    Assert.Equal(configuration.Name, config.Name);
                    Assert.Equal(configuration.EndpointKey, config.EndpointKey);
                    Assert.Equal(configuration.Hostname, config.Hostname);

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
            var expectedAnswer = "answer";
            var expectedScore = 100;
            var expectedId = 10;

            List<Metadata> metadata = new List<Metadata>();
            metadata.Add(new Metadata() { Name = "sample", Value = "value" });

            List<string> questions = new List<string>();
            questions.Add("hello");

            List<QueryResult> listQueryResults = new List<QueryResult>();
            listQueryResults.Add(new QueryResult()
            {
                Id = expectedId,
                Answer = expectedAnswer,
                Score = expectedScore,
                Metadata = metadata.ToArray(),
                Questions = questions.ToArray()
            });

            QueryResults queryResults = new QueryResults();
            queryResults.Answers = listQueryResults.ToArray();

            var jsonRecognizerResult = JsonConvert.SerializeObject(queryResults);

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
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

            var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://localhost/") };
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
                    IQnAMakerService qnAMakerService = new QnAMakerService(httpClient, EnvironmentName, ContentRootPath);
                    var result = await qnAMakerService.QnAMakerServices["name"].GetAnswersAsync(step.Context);
                    var item = result.FirstOrDefault();

                    // assert
                    Assert.Equal(expectedId, item.Id);
                    Assert.Equal(expectedAnswer, item.Answer);
                    Assert.Equal(expectedScore/100, item.Score);

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