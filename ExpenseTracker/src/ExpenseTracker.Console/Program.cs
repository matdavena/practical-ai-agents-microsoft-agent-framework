// ============================================================================
// EXPENSE TRACKER - Console Application
// ============================================================================
// Interactive console interface for the AI-powered expense tracker.
// Phase 3: AI Orchestrator with Tools for intelligent expense management.
// ============================================================================

using System.Text;
using ExpenseTracker.Core.Abstractions;
using ExpenseTracker.Core.Agents;
using ExpenseTracker.Core.Domain.Entities;
using ExpenseTracker.Core.Services;
using ExpenseTracker.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using Spectre.Console;

Console.OutputEncoding = Encoding.UTF8;

// ============================================================================
// STARTUP
// ============================================================================

AnsiConsole.Write(new FigletText("Expense Tracker").Color(Color.Green));
AnsiConsole.MarkupLine("[grey]AI-powered expense management - Phase 3 (Orchestrator)[/]");
AnsiConsole.WriteLine();

// Configure database
var databasePath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "ExpenseTracker",
    "expenses.db");

AnsiConsole.MarkupLine($"[grey]Database: {databasePath}[/]");

// Configure OpenAI
var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
var openAiModel = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";

ExpenseParserAgent? parserAgent = null;
OrchestratorAgent? orchestratorAgent = null;
ReceiptParserAgent? receiptAgent = null;
OpenAIClient? openAIClient = null;

if (string.IsNullOrEmpty(openAiApiKey))
{
    AnsiConsole.MarkupLine("[yellow]Warning: OPENAI_API_KEY not set. AI features disabled.[/]");
}
else
{
    AnsiConsole.MarkupLine($"[grey]AI Model: {openAiModel}[/]");
    openAIClient = new OpenAIClient(openAiApiKey);
    parserAgent = ExpenseParserAgent.Create(openAIClient, openAiModel);
    // Receipt parsing needs gpt-4o for better OCR (gpt-4o-mini works but less accurate)
    receiptAgent = ReceiptParserAgent.Create(openAIClient, "gpt-4o");
}

AnsiConsole.WriteLine();

// Configure services
var services = new ServiceCollection();
services.AddExpenseTracker(databasePath);

// Add Vector Store for semantic search (if OpenAI is configured)
bool vectorStoreEnabled = false;
if (openAIClient != null)
{
    try
    {
        services.AddExpenseTrackerVectorStore(openAIClient);
        vectorStoreEnabled = true;
    }
    catch
    {
        AnsiConsole.MarkupLine("[yellow]Warning: Could not configure vector store[/]");
    }
}

var serviceProvider = services.BuildServiceProvider();

// Initialize database
await AnsiConsole.Status()
    .StartAsync("Initializing database...", async ctx =>
    {
        await serviceProvider.InitializeExpenseTrackerAsync();
    });

AnsiConsole.MarkupLine("[green]Database initialized![/]");

// Check vector store status
if (vectorStoreEnabled)
{
    var vectorStore = serviceProvider.GetService<IVectorStore>();
    if (vectorStore != null && await vectorStore.IsAvailableAsync())
    {
        AnsiConsole.MarkupLine("[green]Vector store (Qdrant) connected - Semantic search enabled![/]");
    }
    else
    {
        AnsiConsole.MarkupLine("[yellow]Vector store not available - Semantic search disabled[/]");
        vectorStoreEnabled = false;
    }
}

// Get services
var expenseService = serviceProvider.GetRequiredService<IExpenseService>();
var categoryService = serviceProvider.GetRequiredService<ICategoryService>();
var budgetService = serviceProvider.GetRequiredService<IBudgetService>();
var userRepository = serviceProvider.GetRequiredService<IUserRepository>();

// Create a demo user ID (in production this would come from authentication)
const string demoUserId = "demo-user-001";

// Ensure demo user exists
var existingUser = await userRepository.GetByIdAsync(demoUserId);
if (existingUser == null)
{
    var demoUser = new User
    {
        Id = demoUserId,
        Name = "Demo User",
        CreatedAt = DateTime.UtcNow,
        LastActiveAt = DateTime.UtcNow
    };
    await userRepository.CreateAsync(demoUser);
    AnsiConsole.MarkupLine("[grey]Demo user created.[/]");
}

// Initialize OrchestratorAgent (requires services to be ready)
if (openAIClient != null)
{
    orchestratorAgent = OrchestratorAgent.Create(
        openAIClient,
        expenseService,
        categoryService,
        budgetService,
        demoUserId,
        openAiModel);
    AnsiConsole.MarkupLine("[green]AI Orchestrator ready![/]");
}

AnsiConsole.WriteLine();

// ============================================================================
// MAIN MENU
// ============================================================================

while (true)
{
    var choices = new List<string>
    {
        "1. Chat with AI (recommended)",
        "2. Parse receipt image (Vision AI)",
        "3. Add expense (manual)",
        "4. Add expense with AI (parse text)",
        "5. View recent expenses",
        "6. View category summary",
        "7. View categories",
        "8. Semantic search (find similar expenses)",
        "9. Quick demo (add sample expenses)",
        "B. Budget management",
        "0. Exit"
    };

    // Disable AI options if not configured
    if (orchestratorAgent == null)
    {
        choices[0] = "[grey]1. Chat with AI (requires OPENAI_API_KEY)[/]";
        choices[1] = "[grey]2. Parse receipt image (requires OPENAI_API_KEY)[/]";
        choices[3] = "[grey]4. Add expense with AI (requires OPENAI_API_KEY)[/]";
    }

    // Disable semantic search if vector store not available
    if (!vectorStoreEnabled)
    {
        choices[7] = "[grey]8. Semantic search (requires Qdrant)[/]";
    }

    var choice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("[cyan]What would you like to do?[/]")
            .AddChoices(choices));

    AnsiConsole.WriteLine();

    switch (choice[0])
    {
        case '1':
            if (orchestratorAgent != null)
                await ChatWithAIAsync();
            else
                AnsiConsole.MarkupLine("[red]AI features require OPENAI_API_KEY environment variable[/]");
            break;
        case '2':
            if (receiptAgent != null)
                await ParseReceiptAsync();
            else
                AnsiConsole.MarkupLine("[red]AI features require OPENAI_API_KEY environment variable[/]");
            break;
        case '3':
            await AddExpenseManualAsync();
            break;
        case '4':
            if (parserAgent != null)
                await AddExpenseWithAIAsync();
            else
                AnsiConsole.MarkupLine("[red]AI features require OPENAI_API_KEY environment variable[/]");
            break;
        case '5':
            await ViewRecentExpensesAsync();
            break;
        case '6':
            await ViewCategorySummaryAsync();
            break;
        case '7':
            await ViewCategoriesAsync();
            break;
        case '8':
            if (vectorStoreEnabled)
                await SemanticSearchAsync();
            else
                AnsiConsole.MarkupLine("[red]Semantic search requires Qdrant to be running[/]");
            break;
        case '9':
            await QuickDemoAsync();
            break;
        case 'B':
            await BudgetManagementAsync();
            break;
        case '0':
            AnsiConsole.MarkupLine("[yellow]Goodbye![/]");
            return;
    }

    AnsiConsole.WriteLine();
}

// ============================================================================
// CHAT WITH AI (Orchestrator)
// ============================================================================

async Task ChatWithAIAsync()
{
    AnsiConsole.Write(new Rule("[cyan]Chat with AI Expense Assistant[/]"));
    AnsiConsole.MarkupLine("[grey]Talk naturally about your expenses. The AI will help you add, view, and analyze them.[/]");
    AnsiConsole.MarkupLine("[grey]Examples:[/]");
    AnsiConsole.MarkupLine("[grey]  - \"Ho speso 45 euro al supermercato\"[/]");
    AnsiConsole.MarkupLine("[grey]  - \"Quanto ho speso questo mese?\"[/]");
    AnsiConsole.MarkupLine("[grey]  - \"Mostrami le ultime spese\"[/]");
    AnsiConsole.MarkupLine("[grey]  - \"Riepilogo per categoria\"[/]");
    AnsiConsole.MarkupLine("[grey]Type '0', 'exit' or press Enter to return to the main menu.[/]");
    AnsiConsole.WriteLine();

    var thread = orchestratorAgent!.GetNewThread();

    while (true)
    {
        var input = AnsiConsole.Prompt(
            new TextPrompt<string>("[blue]You:[/]")
                .AllowEmpty());

        if (string.IsNullOrEmpty(input) ||
            input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
            input.Equals("0", StringComparison.Ordinal))
        {
            AnsiConsole.MarkupLine("[grey]Returning to menu...[/]");
            break;
        }

        AnsiConsole.WriteLine();

        try
        {
            // Show thinking indicator and stream response
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("[yellow]Thinking...[/]", async ctx =>
                {
                    var response = await orchestratorAgent.ProcessAsync(input, thread);

                    AnsiConsole.MarkupLine("[green]AI:[/]");
                    AnsiConsole.WriteLine(response);
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
        }

        AnsiConsole.WriteLine();
    }
}

// ============================================================================
// PARSE RECEIPT IMAGE (Vision AI)
// ============================================================================

async Task ParseReceiptAsync()
{
    AnsiConsole.Write(new Rule("[cyan]Parse Receipt Image[/]"));
    AnsiConsole.MarkupLine("[grey]Provide the path to a receipt image (JPG, PNG, etc.)[/]");
    AnsiConsole.MarkupLine("[grey]The AI will extract expense information using Vision.[/]");
    AnsiConsole.WriteLine();

    var imagePath = AnsiConsole.Prompt(
        new TextPrompt<string>("[blue]Image path (or 'exit' to cancel):[/]")
            .AllowEmpty());

    if (string.IsNullOrEmpty(imagePath) || imagePath.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        AnsiConsole.MarkupLine("[grey]Cancelled. Returning to menu...[/]");
        return;
    }

    // Expand environment variables and handle quotes
    imagePath = imagePath.Trim('"', '\'');
    imagePath = Environment.ExpandEnvironmentVariables(imagePath);

    if (!File.Exists(imagePath))
    {
        AnsiConsole.MarkupLine($"[red]File not found: {imagePath}[/]");
        return;
    }

    AnsiConsole.WriteLine();

    // Parse the receipt
    var result = await AnsiConsole.Status()
        .Spinner(Spinner.Known.Dots)
        .StartAsync("[yellow]Analyzing receipt with Vision AI...[/]", async ctx =>
        {
            return await receiptAgent!.ParseFromFileAsync(imagePath);
        });

    if (!result.Success)
    {
        AnsiConsole.MarkupLine($"[red]Error: {result.ErrorMessage}[/]");
        return;
    }

    var parsed = result.Expense!;

    // Show parsed result
    var panel = new Panel(
        $"""
        [bold]Amount:[/] [green]{parsed.Amount:N2} EUR[/]
        [bold]Description:[/] {Markup.Escape(parsed.Description)}
        [bold]Category:[/] {parsed.Category}
        [bold]Date:[/] {parsed.Date}
        {(parsed.Location != null ? $"[bold]Location:[/] {Markup.Escape(parsed.Location)}" : "")}
        [bold]Confidence:[/] {parsed.Confidence:P0}
        {(parsed.Notes != null ? $"[grey]Notes: {Markup.Escape(parsed.Notes)}[/]" : "")}
        """)
    {
        Header = new PanelHeader("[cyan]Receipt Analysis Result[/]"),
        Border = BoxBorder.Rounded
    };
    AnsiConsole.Write(panel);
    AnsiConsole.WriteLine();

    // Confirm or edit
    var action = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("What would you like to do?")
            .AddChoices(
                "Save this expense",
                "Edit before saving",
                "Discard"));

    switch (action)
    {
        case "Save this expense":
            var expense = await expenseService.AddExpenseAsync(
                userId: demoUserId,
                amount: parsed.Amount,
                description: parsed.Description,
                categoryId: parsed.Category,
                date: parsed.ParsedDate,
                location: parsed.Location,
                source: ExpenseSource.Receipt);

            AnsiConsole.MarkupLine($"[green]Expense saved successfully![/]");
            AnsiConsole.MarkupLine($"[grey]ID: {expense.Id}[/]");
            break;

        case "Edit before saving":
            await EditAndSaveExpenseAsync(parsed);
            break;

        case "Discard":
            AnsiConsole.MarkupLine("[grey]Discarded. Returning to menu...[/]");
            break;
    }
}

// ============================================================================
// ADD EXPENSE WITH AI (Natural Language)
// ============================================================================

async Task AddExpenseWithAIAsync()
{
    AnsiConsole.Write(new Rule("[cyan]Add Expense with AI[/]"));
    AnsiConsole.MarkupLine("[grey]Describe your expense in natural language.[/]");
    AnsiConsole.MarkupLine("[grey]Examples: \"Ho speso 45€ al supermercato\", \"Cena ieri da Mario 32 euro\"[/]");
    AnsiConsole.WriteLine();

    while (true)
    {
        var input = AnsiConsole.Prompt(
            new TextPrompt<string>("[blue]Expense:[/]")
                .AllowEmpty());

        if (string.IsNullOrEmpty(input))
        {
            AnsiConsole.MarkupLine("[grey]Returning to menu...[/]");
            break;
        }

        // Parse with AI
        var result = await AnsiConsole.Status()
            .StartAsync("Analyzing with AI...", async ctx =>
            {
                return await parserAgent!.ParseAsync(input);
            });

        if (!result.Success)
        {
            AnsiConsole.MarkupLine($"[red]Error: {result.ErrorMessage}[/]");
            AnsiConsole.MarkupLine("[grey]Try again or press Enter to return to menu.[/]");
            AnsiConsole.WriteLine();
            continue;
        }

        var parsed = result.Expense!;

        // Show parsed result
        AnsiConsole.WriteLine();
        var panel = new Panel(
            $"""
            [bold]Amount:[/] [green]{parsed.Amount:N2} EUR[/]
            [bold]Description:[/] {Markup.Escape(parsed.Description)}
            [bold]Category:[/] {parsed.Category}
            [bold]Date:[/] {parsed.Date}
            {(parsed.Location != null ? $"[bold]Location:[/] {Markup.Escape(parsed.Location)}" : "")}
            [bold]Confidence:[/] {parsed.Confidence:P0}
            {(parsed.Notes != null ? $"[grey]Notes: {Markup.Escape(parsed.Notes)}[/]" : "")}
            """)
        {
            Header = new PanelHeader("[cyan]AI Parsed Result[/]"),
            Border = BoxBorder.Rounded
        };
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        // Confirm or edit
        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What would you like to do?")
                .AddChoices(
                    "Save this expense",
                    "Edit before saving",
                    "Discard and try again",
                    "Cancel"));

        switch (action)
        {
            case "Save this expense":
                var expense = await expenseService.AddExpenseAsync(
                    userId: demoUserId,
                    amount: parsed.Amount,
                    description: parsed.Description,
                    categoryId: parsed.Category,
                    date: parsed.ParsedDate,
                    location: parsed.Location,
                    source: ExpenseSource.Text);

                AnsiConsole.MarkupLine($"[green]Expense saved successfully![/]");
                AnsiConsole.MarkupLine($"[grey]ID: {expense.Id}[/]");
                break;

            case "Edit before saving":
                await EditAndSaveExpenseAsync(parsed);
                break;

            case "Discard and try again":
                AnsiConsole.MarkupLine("[grey]Discarded. Enter another expense:[/]");
                continue;

            case "Cancel":
                AnsiConsole.MarkupLine("[grey]Returning to menu...[/]");
                return;
        }

        // Ask if want to add more
        AnsiConsole.WriteLine();
        var addMore = AnsiConsole.Confirm("Add another expense?", false);
        if (!addMore) break;

        AnsiConsole.WriteLine();
    }
}

// ============================================================================
// EDIT AND SAVE EXPENSE
// ============================================================================

async Task EditAndSaveExpenseAsync(ExpenseTracker.Core.Models.ParsedExpense parsed)
{
    AnsiConsole.Write(new Rule("[cyan]Edit Expense[/]"));

    var amount = AnsiConsole.Prompt(
        new TextPrompt<decimal>("Amount (EUR):")
            .DefaultValue(parsed.Amount)
            .Validate(a => a > 0 ? ValidationResult.Success() : ValidationResult.Error("Must be positive")));

    var description = AnsiConsole.Prompt(
        new TextPrompt<string>("Description:")
            .DefaultValue(parsed.Description));

    var categories = await categoryService.GetAllCategoriesAsync();
    var categoryChoices = categories.Select(c => $"{c.Icon} {c.Name} ({c.Id})").ToList();
    var defaultCategory = categoryChoices.FirstOrDefault(c => c.Contains($"({parsed.Category})")) ?? categoryChoices.First();

    var categoryChoice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("Category:")
            .AddChoices(categoryChoices)
            .UseConverter(c => c == defaultCategory ? $"{c} [grey](AI suggested)[/]" : c));

    var categoryId = categoryChoice.Split('(').Last().TrimEnd(')');

    var date = AnsiConsole.Prompt(
        new TextPrompt<DateTime>("Date (yyyy-MM-dd):")
            .DefaultValue(parsed.ParsedDate));

    var location = AnsiConsole.Prompt(
        new TextPrompt<string>("Location (optional):")
            .DefaultValue(parsed.Location ?? "")
            .AllowEmpty());

    var expense = await expenseService.AddExpenseAsync(
        userId: demoUserId,
        amount: amount,
        description: description,
        categoryId: categoryId,
        date: date,
        location: string.IsNullOrEmpty(location) ? null : location,
        source: ExpenseSource.Text);

    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine($"[green]Expense saved successfully![/]");
    AnsiConsole.MarkupLine($"[grey]ID: {expense.Id}[/]");
}

// ============================================================================
// ADD EXPENSE MANUAL
// ============================================================================

async Task AddExpenseManualAsync()
{
    AnsiConsole.Write(new Rule("[cyan]Add New Expense (Manual)[/]"));
    AnsiConsole.MarkupLine("[grey]Enter 0 as amount to cancel.[/]");
    AnsiConsole.WriteLine();

    var amount = AnsiConsole.Prompt(
        new TextPrompt<decimal>("Amount (EUR):")
            .Validate(a => a >= 0 ? ValidationResult.Success() : ValidationResult.Error("Amount must be positive or 0 to cancel")));

    if (amount == 0)
    {
        AnsiConsole.MarkupLine("[grey]Cancelled. Returning to menu...[/]");
        return;
    }

    var description = AnsiConsole.Ask<string>("Description:");

    var categories = await categoryService.GetAllCategoriesAsync();
    var categoryChoices = categories.Select(c => $"{c.Icon} {c.Name} ({c.Id})").ToList();

    var categoryChoice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("Category:")
            .AddChoices(categoryChoices));

    var categoryId = categoryChoice.Split('(').Last().TrimEnd(')');

    var date = AnsiConsole.Prompt(
        new TextPrompt<DateTime>("Date (yyyy-MM-dd):")
            .DefaultValue(DateTime.Today));

    var location = AnsiConsole.Prompt(
        new TextPrompt<string>("Location (optional):")
            .AllowEmpty());

    var expense = await expenseService.AddExpenseAsync(
        userId: demoUserId,
        amount: amount,
        description: description,
        categoryId: categoryId,
        date: date,
        location: string.IsNullOrEmpty(location) ? null : location,
        source: ExpenseSource.Manual);

    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine($"[green]Expense added successfully![/]");
    AnsiConsole.MarkupLine($"[grey]ID: {expense.Id}[/]");
}

// ============================================================================
// VIEW RECENT EXPENSES
// ============================================================================

async Task ViewRecentExpensesAsync()
{
    AnsiConsole.Write(new Rule("[cyan]Recent Expenses (Last 30 Days)[/]"));

    var expenses = await expenseService.GetRecentExpensesAsync(demoUserId);
    var expenseList = expenses.ToList();

    if (expenseList.Count == 0)
    {
        AnsiConsole.MarkupLine("[yellow]No expenses found. Try adding some![/]");
        return;
    }

    var categories = (await categoryService.GetAllCategoriesAsync()).ToDictionary(c => c.Id);

    var table = new Table()
        .Border(TableBorder.Rounded)
        .AddColumn("Date")
        .AddColumn("Category")
        .AddColumn("Description")
        .AddColumn("Source")
        .AddColumn(new TableColumn("Amount").RightAligned());

    foreach (var expense in expenseList)
    {
        var category = categories.GetValueOrDefault(expense.CategoryId);
        var categoryDisplay = category != null ? $"{category.Icon} {category.Name}" : expense.CategoryId;
        var sourceIcon = expense.Source switch
        {
            ExpenseSource.Text => "[blue]AI[/]",
            ExpenseSource.Receipt => "[purple]Img[/]",
            _ => "[grey]Man[/]"
        };

        table.AddRow(
            expense.Date.ToString("dd/MM/yyyy"),
            categoryDisplay,
            Markup.Escape(expense.Description),
            sourceIcon,
            $"[green]{expense.Amount:N2} EUR[/]");
    }

    AnsiConsole.Write(table);

    var total = expenseList.Sum(e => e.Amount);
    AnsiConsole.MarkupLine($"\n[bold]Total: {total:N2} EUR[/] ({expenseList.Count} expenses)");
}

// ============================================================================
// VIEW CATEGORY SUMMARY
// ============================================================================

async Task ViewCategorySummaryAsync()
{
    AnsiConsole.Write(new Rule("[cyan]Category Summary (Current Month)[/]"));

    var today = DateTime.Today;
    var fromDate = new DateTime(today.Year, today.Month, 1);
    var toDate = fromDate.AddMonths(1).AddDays(-1);

    var summary = await expenseService.GetCategorySummaryAsync(demoUserId, fromDate, toDate);
    var summaryList = summary.ToList();

    if (summaryList.Count == 0)
    {
        AnsiConsole.MarkupLine("[yellow]No expenses this month. Try adding some![/]");
        return;
    }

    var table = new Table()
        .Border(TableBorder.Rounded)
        .AddColumn("Category")
        .AddColumn(new TableColumn("Amount").RightAligned())
        .AddColumn(new TableColumn("Count").Centered())
        .AddColumn(new TableColumn("%").RightAligned());

    foreach (var item in summaryList)
    {
        table.AddRow(
            $"{item.CategoryIcon} {item.CategoryName}",
            $"[green]{item.TotalAmount:N2} EUR[/]",
            item.ExpenseCount.ToString(),
            $"{item.Percentage:N1}%");
    }

    AnsiConsole.Write(table);

    var total = summaryList.Sum(s => s.TotalAmount);
    AnsiConsole.MarkupLine($"\n[bold]Total: {total:N2} EUR[/]");

    // Show bar chart
    AnsiConsole.WriteLine();
    var chart = new BarChart()
        .Width(60)
        .Label("[bold]Expenses by Category[/]");

    foreach (var item in summaryList.Take(5))
    {
        chart.AddItem(item.CategoryName, (double)item.TotalAmount, GetCategoryColor(item.CategoryId));
    }

    AnsiConsole.Write(chart);
}

// ============================================================================
// VIEW CATEGORIES
// ============================================================================

async Task ViewCategoriesAsync()
{
    AnsiConsole.Write(new Rule("[cyan]Available Categories[/]"));

    var categories = await categoryService.GetAllCategoriesAsync();

    var table = new Table()
        .Border(TableBorder.Rounded)
        .AddColumn("Icon")
        .AddColumn("Name")
        .AddColumn("ID")
        .AddColumn("Type");

    foreach (var category in categories)
    {
        table.AddRow(
            category.Icon,
            category.Name,
            $"[grey]{category.Id}[/]",
            category.IsDefault ? "[green]Default[/]" : "[blue]Custom[/]");
    }

    AnsiConsole.Write(table);
}

// ============================================================================
// QUICK DEMO
// ============================================================================

async Task QuickDemoAsync()
{
    AnsiConsole.Write(new Rule("[cyan]Quick Demo - Adding Sample Expenses[/]"));

    var sampleExpenses = new[]
    {
        (45.50m, "Spesa settimanale al supermercato", "food", -3),
        (32.00m, "Cena al ristorante giapponese", "restaurant", -2),
        (25.00m, "Benzina auto", "fuel", -1),
        (12.99m, "Netflix mensile", "entertainment", 0),
        (89.00m, "Bolletta luce", "bills", -5),
        (150.00m, "Scarpe nuove", "shopping", -4),
        (28.50m, "Pranzo di lavoro", "restaurant", -1),
        (55.00m, "Farmacia", "health", -2),
        (18.00m, "Cinema con amici", "entertainment", 0),
        (62.30m, "Spesa alimentari", "food", 0)
    };

    await AnsiConsole.Progress()
        .StartAsync(async ctx =>
        {
            var task = ctx.AddTask("[cyan]Adding sample expenses...[/]", maxValue: sampleExpenses.Length);

            foreach (var (amount, description, categoryId, daysAgo) in sampleExpenses)
            {
                await expenseService.AddExpenseAsync(
                    userId: demoUserId,
                    amount: amount,
                    description: description,
                    categoryId: categoryId,
                    date: DateTime.Today.AddDays(daysAgo),
                    source: ExpenseSource.Manual);

                task.Increment(1);
                await Task.Delay(100); // Small delay for visual effect
            }
        });

    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine($"[green]Added {sampleExpenses.Length} sample expenses![/]");
    AnsiConsole.MarkupLine("[grey]Go to 'View recent expenses' or 'Category summary' to see them.[/]");
}

// ============================================================================
// HELPERS
// ============================================================================

static Color GetCategoryColor(string categoryId) => categoryId switch
{
    "food" => Color.Green,
    "restaurant" => Color.Orange1,
    "transport" => Color.Blue,
    "fuel" => Color.Grey,
    "health" => Color.HotPink,
    "entertainment" => Color.Purple,
    "shopping" => Color.Cyan1,
    "bills" => Color.Maroon,
    "home" => Color.Navy,
    _ => Color.Grey
};

// ============================================================================
// SEMANTIC SEARCH
// ============================================================================

async Task SemanticSearchAsync()
{
    AnsiConsole.Write(new Rule("[cyan]Semantic Search[/]"));
    AnsiConsole.MarkupLine("[grey]Search expenses using natural language.[/]");
    AnsiConsole.MarkupLine("[grey]Examples:[/]");
    AnsiConsole.MarkupLine("[grey]  - \"spese per mangiare\"[/]");
    AnsiConsole.MarkupLine("[grey]  - \"acquisti online\"[/]");
    AnsiConsole.MarkupLine("[grey]  - \"trasporti e benzina\"[/]");
    AnsiConsole.MarkupLine("[grey]Type '0', 'exit' or press Enter to return to menu.[/]");
    AnsiConsole.WriteLine();

    while (true)
    {
        var query = AnsiConsole.Prompt(
            new TextPrompt<string>("[blue]Search:[/]")
                .AllowEmpty());

        if (string.IsNullOrEmpty(query) ||
            query.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
            query.Equals("0", StringComparison.Ordinal))
        {
            AnsiConsole.MarkupLine("[grey]Returning to menu...[/]");
            break;
        }

        var results = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("[yellow]Searching...[/]", async ctx =>
            {
                return await expenseService.SemanticSearchAsync(demoUserId, query, 10);
            });

        var resultList = results.ToList();

        if (resultList.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No similar expenses found.[/]");
            AnsiConsole.MarkupLine("[grey]Try adding more expenses first with 'Quick demo'.[/]");
        }
        else
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[green]Found {resultList.Count} similar expenses:[/]");
            AnsiConsole.WriteLine();

            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("Score")
                .AddColumn("Date")
                .AddColumn("Category")
                .AddColumn("Description")
                .AddColumn(new TableColumn("Amount").RightAligned());

            foreach (var result in resultList)
            {
                var scoreColor = result.Score > 0.8f ? "green" : result.Score > 0.6f ? "yellow" : "grey";
                table.AddRow(
                    $"[{scoreColor}]{result.Score:P0}[/]",
                    result.Expense.Date.ToString("dd/MM/yyyy"),
                    result.CategoryName,
                    Markup.Escape(result.Expense.Description),
                    $"[green]{result.Expense.Amount:N2} EUR[/]");
            }

            AnsiConsole.Write(table);
        }

        AnsiConsole.WriteLine();
    }
}

// ============================================================================
// BUDGET MANAGEMENT
// ============================================================================

async Task BudgetManagementAsync()
{
    while (true)
    {
        AnsiConsole.Write(new Rule("[cyan]Budget Management[/]"));

        var budgetChoices = new List<string>
        {
            "1. View budget status",
            "2. Set/Update budget",
            "3. Check budget alerts",
            "4. Delete budget",
            "0. Return to main menu"
        };

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[cyan]What would you like to do?[/]")
                .AddChoices(budgetChoices));

        AnsiConsole.WriteLine();

        switch (choice[0])
        {
            case '1':
                await ViewBudgetStatusAsync();
                break;
            case '2':
                await SetBudgetAsync();
                break;
            case '3':
                await CheckBudgetAlertsAsync();
                break;
            case '4':
                await DeleteBudgetAsync();
                break;
            case '0':
                return;
        }

        AnsiConsole.WriteLine();
    }
}

async Task ViewBudgetStatusAsync()
{
    AnsiConsole.Write(new Rule("[cyan]Budget Status[/]"));

    var statuses = await budgetService.GetBudgetStatusAsync(demoUserId);
    var statusList = statuses.ToList();

    if (statusList.Count == 0)
    {
        AnsiConsole.MarkupLine("[yellow]No budgets configured. Use 'Set/Update budget' to create one.[/]");
        return;
    }

    var table = new Table()
        .Border(TableBorder.Rounded)
        .AddColumn("Status")
        .AddColumn("Category")
        .AddColumn("Period")
        .AddColumn(new TableColumn("Budget").RightAligned())
        .AddColumn(new TableColumn("Spent").RightAligned())
        .AddColumn(new TableColumn("Remaining").RightAligned())
        .AddColumn(new TableColumn("Usage").RightAligned());

    foreach (var status in statusList)
    {
        var statusIcon = status.IsOverBudget ? "[red]🔴[/]" : status.IsWarning ? "[yellow]🟡[/]" : "[green]🟢[/]";
        var remainingColor = status.RemainingAmount < 0 ? "red" : "green";

        table.AddRow(
            statusIcon,
            $"{status.CategoryIcon} {status.CategoryName}",
            status.Period.ToString(),
            $"{status.BudgetAmount:N2} EUR",
            $"{status.SpentAmount:N2} EUR",
            $"[{remainingColor}]{status.RemainingAmount:N2} EUR[/]",
            $"{status.UsagePercentage:P0}");
    }

    AnsiConsole.Write(table);

    // Show period info
    var firstStatus = statusList.First();
    AnsiConsole.MarkupLine($"\n[grey]Current period: {firstStatus.PeriodStart:dd/MM/yyyy} - {firstStatus.PeriodEnd:dd/MM/yyyy}[/]");
}

async Task SetBudgetAsync()
{
    AnsiConsole.Write(new Rule("[cyan]Set/Update Budget[/]"));

    // Choose scope (global or category)
    var scopeChoice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("Budget scope:")
            .AddChoices(
                "Global (all categories)",
                "Specific category"));

    string? categoryId = null;
    if (scopeChoice.StartsWith("Specific"))
    {
        var categories = await categoryService.GetAllCategoriesAsync();
        var categoryChoices = categories.Select(c => $"{c.Icon} {c.Name} ({c.Id})").ToList();

        var categoryChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select category:")
                .AddChoices(categoryChoices));

        categoryId = categoryChoice.Split('(').Last().TrimEnd(')');
    }

    // Enter amount
    var amount = AnsiConsole.Prompt(
        new TextPrompt<decimal>("Budget amount (EUR):")
            .Validate(a => a > 0 ? ValidationResult.Success() : ValidationResult.Error("Amount must be positive")));

    // Choose period
    var periodChoice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("Budget period:")
            .AddChoices("Monthly (recommended)", "Weekly", "Yearly"));

    var period = periodChoice switch
    {
        "Weekly" => BudgetPeriod.Weekly,
        "Yearly" => BudgetPeriod.Yearly,
        _ => BudgetPeriod.Monthly
    };

    // Save budget
    var budget = await budgetService.SetBudgetAsync(demoUserId, amount, period, categoryId);

    var scopeText = budget.IsGlobal ? "global" : $"category '{categoryId}'";
    AnsiConsole.MarkupLine($"[green]Budget set successfully![/]");
    AnsiConsole.MarkupLine($"[grey]{period} budget of {amount:N2} EUR for {scopeText}[/]");
}

async Task CheckBudgetAlertsAsync()
{
    AnsiConsole.Write(new Rule("[cyan]Budget Alerts[/]"));

    var alerts = await budgetService.CheckBudgetAlertsAsync(demoUserId);
    var alertList = alerts.ToList();

    if (alertList.Count == 0)
    {
        AnsiConsole.MarkupLine("[green]All budgets are within limits. No alerts at this time.[/]");
        return;
    }

    AnsiConsole.MarkupLine($"[yellow]Found {alertList.Count} budget alert(s):[/]");
    AnsiConsole.WriteLine();

    foreach (var alert in alertList)
    {
        var color = alert.Level switch
        {
            BudgetAlertLevel.Critical => "red",
            BudgetAlertLevel.Exceeded => "red",
            _ => "yellow"
        };

        var panel = new Panel($"[{color}]{alert.Message}[/]")
        {
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 0)
        };
        AnsiConsole.Write(panel);
    }
}

async Task DeleteBudgetAsync()
{
    AnsiConsole.Write(new Rule("[cyan]Delete Budget[/]"));

    var budgets = await budgetService.GetBudgetsAsync(demoUserId);
    var budgetList = budgets.Where(b => b.IsActive).ToList();

    if (budgetList.Count == 0)
    {
        AnsiConsole.MarkupLine("[yellow]No budgets to delete.[/]");
        return;
    }

    var categories = (await categoryService.GetAllCategoriesAsync()).ToDictionary(c => c.Id);

    var budgetChoices = budgetList.Select(b =>
    {
        if (b.IsGlobal)
            return $"💰 Global - {b.Amount:N2} EUR ({b.Period})";

        var cat = categories.GetValueOrDefault(b.CategoryId!);
        var catName = cat?.Name ?? b.CategoryId;
        var catIcon = cat?.Icon ?? "📦";
        return $"{catIcon} {catName} - {b.Amount:N2} EUR ({b.Period})";
    }).ToList();

    budgetChoices.Add("Cancel");

    var choice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("Select budget to delete:")
            .AddChoices(budgetChoices));

    if (choice == "Cancel")
    {
        AnsiConsole.MarkupLine("[grey]Cancelled.[/]");
        return;
    }

    var index = budgetChoices.IndexOf(choice);
    var budgetToDelete = budgetList[index];

    if (AnsiConsole.Confirm($"Delete this budget?", false))
    {
        await budgetService.DeleteBudgetAsync(budgetToDelete.Id);
        AnsiConsole.MarkupLine("[green]Budget deleted successfully![/]");
    }
    else
    {
        AnsiConsole.MarkupLine("[grey]Cancelled.[/]");
    }
}
