using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SplitMate.Controllers;
using SplitMate.Data;
using SplitMate.Models;
using SplitMate.Services;

namespace SplitMate.Tests;

public class AiAssistantPhase2Tests
{
    [Fact]
    public async Task OpenRouterService_SendsAuthenticatedChatCompletionAndReturnsReply()
    {
        var handler = new StubHttpMessageHandler(
            HttpStatusCode.OK,
            """{"choices":[{"message":{"content":"Track dining costs weekly."}}]}""");
        var httpClient = new HttpClient(handler);
        var service = new OpenRouterAiAssistantService(
            httpClient,
            CreateConfiguration(
                ("OpenRouter:ApiKey", "test-key"),
                ("OpenRouter:Model", "test/model"),
                ("OpenRouter:BaseUrl", "https://example.test/api/v1/")));

        var reply = await service.GetReplyAsync(
            "How can I budget?",
            """{"totalExpenses":12.5}""",
            CancellationToken.None);

        Assert.Equal("Track dining costs weekly.", reply);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal(
            "https://openrouter.ai/api/v1/chat/completions",
            handler.RequestUri?.ToString());
        Assert.Equal("Bearer", handler.Authorization?.Scheme);
        Assert.Equal("test-key", handler.Authorization?.Parameter);

        using var requestJson = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal(
            "test/model",
            requestJson.RootElement.GetProperty("model").GetString());
        var messages = requestJson.RootElement.GetProperty("messages");
        Assert.Contains(
            """{"totalExpenses":12.5}""",
            messages[0].GetProperty("content").GetString());
        Assert.Equal(
            "How can I budget?",
            messages[1].GetProperty("content").GetString());
    }

    [Fact]
    public async Task OpenRouterService_ReportsNotConfiguredWithoutApiKeyOrModel()
    {
        var service = new OpenRouterAiAssistantService(
            new HttpClient(new StubHttpMessageHandler(
                HttpStatusCode.OK,
                """{"choices":[{"message":{"content":"reply"}}]}""")),
            CreateConfiguration(("OpenRouter:Model", "test/model")));

        Assert.False(service.IsConfigured);
        await Assert.ThrowsAsync<AiAssistantServiceException>(() =>
            service.GetReplyAsync("Question", "{}", CancellationToken.None));
    }

    [Fact]
    public async Task OpenRouterService_HidesProviderErrorDetails()
    {
        var service = new OpenRouterAiAssistantService(
            new HttpClient(new StubHttpMessageHandler(
                HttpStatusCode.BadRequest,
                """{"error":{"message":"sensitive provider detail"}}""")),
            CreateConfiguration(
                ("OpenRouter:ApiKey", "test-key"),
                ("OpenRouter:Model", "test/model")));

        var exception = await Assert.ThrowsAsync<AiAssistantServiceException>(
            () => service.GetReplyAsync("Question", "{}", CancellationToken.None));

        Assert.DoesNotContain("sensitive provider detail", exception.Message);
    }

    [Fact]
    public async Task Send_ReturnsReplyAndOnlyAggregatedFinancialContext()
    {
        var database = new TestDatabase(
            new Expense
            {
                Title = "Private dinner title",
                Amount = 20.25m,
                Category = "Food",
                PaidBy = "Private payer",
                GroupName = "Friends"
            },
            new Expense
            {
                Title = "Another private title",
                Amount = 9.75m,
                Category = "Food",
                PaidBy = "Another payer",
                GroupName = "Friends"
            },
            new Expense
            {
                Title = "Private rent title",
                Amount = 100m,
                Category = "Housing",
                PaidBy = "Private payer",
                GroupName = "Home"
            });
        var assistant = new StubAiAssistantService(true, "A real reply from the service.");
        var controller = CreateController(database, assistant);

        var result = await controller.Send(
            new AiAssistantRequest { Message = "Summarize my spending." },
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        using var responseJson = JsonDocument.Parse(
            JsonSerializer.Serialize(okResult.Value));
        Assert.Equal(
            "A real reply from the service.",
            responseJson.RootElement.GetProperty("reply").GetString());
        Assert.Equal(1, database.ReadCount);

        var financialContext = assistant.FinancialContext!;
        using var contextJson = JsonDocument.Parse(financialContext);
        Assert.Equal(
            130m,
            contextJson.RootElement.GetProperty("totalExpenses").GetDecimal());
        Assert.Equal(
            3,
            contextJson.RootElement.GetProperty("expenseCount").GetInt32());
        var categories = contextJson.RootElement.GetProperty("categoryTotals");
        Assert.Equal(2, categories.GetArrayLength());
        Assert.Equal("Housing", categories[0].GetProperty("category").GetString());
        Assert.Equal(100m, categories[0].GetProperty("total").GetDecimal());
        Assert.Equal("Food", categories[1].GetProperty("category").GetString());
        Assert.Equal(30m, categories[1].GetProperty("total").GetDecimal());
        Assert.DoesNotContain("Private dinner title", financialContext);
        Assert.DoesNotContain("Private payer", financialContext);
    }

    [Fact]
    public async Task Send_ReturnsServiceUnavailableWhenNotConfigured()
    {
        var database = new TestDatabase();
        var assistant = new StubAiAssistantService(false, null);
        var controller = CreateController(database, assistant);

        var result = await controller.Send(
            new AiAssistantRequest { Message = "Question" },
            CancellationToken.None);

        var response = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, response.StatusCode);
        Assert.Equal(0, database.ReadCount);
        Assert.Null(assistant.FinancialContext);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("too long")]
    public async Task Send_RejectsInvalidMessages(string message)
    {
        var invalidMessage = message == "too long"
            ? new string('x', AiAssistantRequest.MaximumMessageLength + 1)
            : message;
        var database = new TestDatabase();
        var assistant = new StubAiAssistantService(true, "not used");
        var controller = CreateController(database, assistant);

        var result = await controller.Send(
            new AiAssistantRequest { Message = invalidMessage },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, database.ReadCount);
        Assert.Null(assistant.FinancialContext);
    }

    [Fact]
    public async Task Send_ReturnsSafeErrorWhenProviderFails()
    {
        var assistant = new StubAiAssistantService(
            true,
            null,
            new AiAssistantServiceException());
        var controller = CreateController(new TestDatabase(), assistant);

        var result = await controller.Send(
            new AiAssistantRequest { Message = "Question" },
            CancellationToken.None);

        var response = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status502BadGateway, response.StatusCode);
        Assert.DoesNotContain(
            "provider could not complete",
            JsonSerializer.Serialize(response.Value));
    }

    [Fact]
    public void SendPost_RequiresAntiforgeryValidation()
    {
        var sendMethod = typeof(AiAssistantController).GetMethod(
            nameof(AiAssistantController.Send));

        Assert.NotNull(sendMethod);
        Assert.NotNull(
            sendMethod.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
    }

    private static AiAssistantController CreateController(
        IDatabase database,
        IAiAssistantService assistant)
    {
        var controller = new AiAssistantController(
            database,
            assistant,
            NullLogger<AiAssistantController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
    }

    private static IConfiguration CreateConfiguration(
        params (string Key, string Value)[] values)
    {
        var settings = values.ToDictionary(
            value => value.Key,
            value => (string?)value.Value);
        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _responseBody;

        public StubHttpMessageHandler(
            HttpStatusCode statusCode,
            string responseBody)
        {
            _statusCode = statusCode;
            _responseBody = responseBody;
        }

        public HttpMethod? Method { get; private set; }
        public Uri? RequestUri { get; private set; }
        public AuthenticationHeaderValue? Authorization { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Method = request.Method;
            RequestUri = request.RequestUri;
            Authorization = request.Headers.Authorization;
            RequestBody = await request.Content!.ReadAsStringAsync(
                cancellationToken);

            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(
                    _responseBody,
                    Encoding.UTF8,
                    "application/json")
            };
        }
    }

    private sealed class StubAiAssistantService : IAiAssistantService
    {
        private readonly string? _reply;
        private readonly Exception? _exception;

        public StubAiAssistantService(
            bool isConfigured,
            string? reply,
            Exception? exception = null)
        {
            IsConfigured = isConfigured;
            _reply = reply;
            _exception = exception;
        }

        public bool IsConfigured { get; }
        public string? FinancialContext { get; private set; }

        public Task<string> GetReplyAsync(
            string message,
            string financialContext,
            CancellationToken cancellationToken)
        {
            FinancialContext = financialContext;

            return _exception == null
                ? Task.FromResult(_reply!)
                : Task.FromException<string>(_exception);
        }
    }

    private sealed class TestDatabase : IDatabase
    {
        private readonly IReadOnlyList<Expense> _expenses;

        public TestDatabase(params Expense[] expenses)
        {
            _expenses = expenses;
        }

        public int ReadCount { get; private set; }

        public IReadOnlyList<Expense> GetAll(
            string? searchTerm = null,
            string? category = null,
            string? groupName = null)
        {
            ReadCount++;
            return _expenses;
        }

        public Expense? GetById(int id) => null;
        public int Add(Expense expense) => throw new NotImplementedException();
        public bool Update(Expense expense) => throw new NotImplementedException();
        public bool Delete(int id) => throw new NotImplementedException();
    }
}
