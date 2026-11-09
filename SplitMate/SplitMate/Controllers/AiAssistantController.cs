using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using SplitMate.Data;
using SplitMate.Models;
using SplitMate.Services;

namespace SplitMate.Controllers;

public sealed class AiAssistantController : Controller
{
    private readonly IDatabase _database;
    private readonly IAiAssistantService _aiAssistantService;
    private readonly ILogger<AiAssistantController> _logger;

    public AiAssistantController(
        IDatabase database,
        IAiAssistantService aiAssistantService,
        ILogger<AiAssistantController> logger)
    {
        _database = database;
        _aiAssistantService = aiAssistantService;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View(new AiAssistantViewModel
        {
            IsConfigured = _aiAssistantService.IsConfigured
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(
        [FromBody] AiAssistantRequest? request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid ||
            request == null ||
            string.IsNullOrWhiteSpace(request.Message) ||
            request.Message.Length > AiAssistantRequest.MaximumMessageLength)
        {
            return BadRequest(new
            {
                error = $"Enter a message of 1 to {AiAssistantRequest.MaximumMessageLength} characters."
            });
        }

        if (!_aiAssistantService.IsConfigured)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "AI service is not configured. Set the server-side OpenRouter API key and model."
            });
        }

        var financialContext = BuildFinancialContext(_database.GetAll());

        try
        {
            var reply = await _aiAssistantService.GetReplyAsync(
                request.Message.Trim(),
                financialContext,
                cancellationToken);

            return Ok(new { reply });
        }
        catch (AiAssistantServiceException ex)
        {
            _logger.LogWarning(ex, "The AI provider could not complete a request.");
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                error = "The AI service could not complete your request. Please try again."
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Could not reach the AI provider.");
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                error = "The AI service is temporarily unavailable. Please try again."
            });
        }
        catch (TaskCanceledException ex)
            when (!HttpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "The AI provider request timed out.");
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                error = "The AI service timed out. Please try again."
            });
        }
    }

    private static string BuildFinancialContext(
        IReadOnlyList<Expense> expenses)
    {
        var categoryTotals = expenses
            .GroupBy(expense => expense.Category, StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                category = group.Key,
                total = group.Sum(expense => expense.Amount)
            })
            .OrderByDescending(group => group.total)
            .ThenBy(group => group.category, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return JsonSerializer.Serialize(new
        {
            currency = "EUR",
            expenseCount = expenses.Count,
            totalExpenses = expenses.Sum(expense => expense.Amount),
            categoryTotals
        });
    }
}
