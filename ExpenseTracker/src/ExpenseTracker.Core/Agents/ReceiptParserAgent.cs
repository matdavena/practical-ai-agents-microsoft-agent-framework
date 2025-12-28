// ============================================================================
// ReceiptParserAgent
// ============================================================================
// AI agent specialized in parsing expense information from receipt images.
// Uses GPT-4o Vision capabilities for OCR and data extraction.
//
// BOOK CHAPTER NOTE:
// This agent demonstrates:
// 1. Vision AI - Sending images to GPT-4o for analysis
// 2. Multimodal Input - Combining text instructions with image data
// 3. Structured Output from Images - Extracting typed data from visual content
// ============================================================================

using System.ClientModel;
using ExpenseTracker.Core.Models;
using OpenAI;
using OpenAI.Chat;

namespace ExpenseTracker.Core.Agents;

/// <summary>
/// Agent specialized in parsing expense information from receipt images.
/// Uses GPT-4o Vision to extract structured data from photos of receipts.
/// </summary>
public class ReceiptParserAgent
{
    private readonly ChatClient _chatClient;
    private readonly string _todayDate;

    /// <summary>
    /// System prompt for receipt parsing.
    /// </summary>
    private static string GetSystemPrompt(string todayDate)
    {
        return $"""
            Sei un assistente specializzato nell'estrazione di informazioni da scontrini e ricevute.
            La data di oggi è: {todayDate}

            ANALISI IMMAGINE:
            Quando ricevi un'immagine di uno scontrino, estrai:
            1. Totale: L'importo totale dello scontrino (cerca "TOTALE", "TOTAL", "TOT")
            2. Data: La data sullo scontrino (se presente, altrimenti usa oggi)
            3. Esercente: Il nome del negozio/ristorante
            4. Categoria: Determina la categoria in base al tipo di esercizio

            CATEGORIE:
            - food: supermercati, alimentari (Conad, Coop, Esselunga, Lidl, etc.)
            - restaurant: ristoranti, bar, pizzerie, fast food
            - fuel: distributori benzina (Eni, Q8, IP, etc.)
            - health: farmacie
            - shopping: abbigliamento, elettronica, negozi generici
            - bills: utenze, servizi
            - other: se non riconoscibile

            REGOLE:
            - Cerca sempre il TOTALE FINALE, non subtotali
            - Se ci sono più importi, usa quello più grande (solitamente il totale)
            - Se la data non è leggibile, usa {todayDate}
            - Se non riesci a leggere lo scontrino, imposta confidence a 0
            - Fornisci una descrizione che includa il nome dell'esercente

            OUTPUT JSON (esempio):
            """ + """
            {"amount": 12.50, "description": "Spesa al supermercato", "category": "food", "date": "2024-12-23", "location": "Conad", "confidence": 0.9, "notes": null}
            """;
    }

    /// <summary>
    /// Creates a new ReceiptParserAgent.
    /// </summary>
    /// <param name="chatClient">The OpenAI chat client (must support vision, e.g., gpt-4o).</param>
    public ReceiptParserAgent(ChatClient chatClient)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        _todayDate = DateTime.Today.ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// Creates a new ReceiptParserAgent from an OpenAI client.
    /// </summary>
    /// <param name="openAIClient">The OpenAI client.</param>
    /// <param name="model">The model to use (default: gpt-4o - must support vision).</param>
    public static ReceiptParserAgent Create(OpenAIClient openAIClient, string model = "gpt-4o")
    {
        // Note: gpt-4o-mini also supports vision, but gpt-4o is more accurate for OCR
        var chatClient = openAIClient.GetChatClient(model);
        return new ReceiptParserAgent(chatClient);
    }

    /// <summary>
    /// Parses expense information from a receipt image file.
    /// </summary>
    /// <param name="imagePath">Path to the image file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parsing result containing the structured expense data.</returns>
    public async Task<ExpenseParseResult> ParseFromFileAsync(
        string imagePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return ExpenseParseResult.Fail("Il percorso dell'immagine è vuoto", imagePath);
        }

        if (!File.Exists(imagePath))
        {
            return ExpenseParseResult.Fail($"File non trovato: {imagePath}", imagePath);
        }

        try
        {
            // Read image and convert to base64
            var imageBytes = await File.ReadAllBytesAsync(imagePath, cancellationToken);
            var base64Image = Convert.ToBase64String(imageBytes);

            // Determine MIME type from extension
            var mimeType = GetMimeType(imagePath);

            return await ParseFromBase64Async(base64Image, mimeType, imagePath, cancellationToken);
        }
        catch (Exception ex)
        {
            return ExpenseParseResult.Fail($"Errore durante la lettura del file: {ex.Message}", imagePath);
        }
    }

    /// <summary>
    /// Parses expense information from a base64-encoded image.
    /// </summary>
    /// <param name="base64Image">Base64-encoded image data.</param>
    /// <param name="mimeType">MIME type of the image (e.g., "image/jpeg").</param>
    /// <param name="originalInput">Original input reference for error messages.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parsing result.</returns>
    public async Task<ExpenseParseResult> ParseFromBase64Async(
        string base64Image,
        string mimeType = "image/jpeg",
        string? originalInput = null,
        CancellationToken cancellationToken = default)
    {
        originalInput ??= "[base64 image]";

        try
        {
            // Create the image content part
            var imageData = BinaryData.FromBytes(Convert.FromBase64String(base64Image));
            var imagePart = ChatMessageContentPart.CreateImagePart(imageData, mimeType);

            // Create the text instruction part
            var textPart = ChatMessageContentPart.CreateTextPart(
                "Analizza questo scontrino ed estrai le informazioni sulla spesa. Rispondi in JSON.");

            // Create messages with system prompt and user message containing the image
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(GetSystemPrompt(_todayDate)),
                new UserChatMessage(textPart, imagePart)
            };

            // Call the API
            var response = await _chatClient.CompleteChatAsync(
                messages,
                new ChatCompletionOptions
                {
                    MaxOutputTokenCount = 500,
                    Temperature = 0.1f // Low temperature for more consistent extraction
                },
                cancellationToken);

            var content = response.Value.Content[0].Text;

            // Parse JSON response
            var parsedExpense = ParseJsonResponse(content);

            if (parsedExpense == null)
            {
                return ExpenseParseResult.Fail("Non sono riuscito a interpretare lo scontrino", originalInput);
            }

            if (parsedExpense.Amount <= 0)
            {
                return ExpenseParseResult.Fail("Non ho trovato un importo valido nello scontrino", originalInput);
            }

            if (parsedExpense.Confidence < 0.3f)
            {
                return ExpenseParseResult.Fail(
                    $"Non sono sicuro dell'interpretazione: {parsedExpense.Notes ?? "scontrino poco leggibile"}",
                    originalInput);
            }

            return ExpenseParseResult.Ok(parsedExpense, originalInput);
        }
        catch (ClientResultException ex)
        {
            return ExpenseParseResult.Fail($"Errore API OpenAI: {ex.Message}", originalInput);
        }
        catch (Exception ex)
        {
            return ExpenseParseResult.Fail($"Errore durante il parsing: {ex.Message}", originalInput);
        }
    }

    /// <summary>
    /// Parses expense information from an image URL.
    /// </summary>
    /// <param name="imageUrl">URL of the image.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parsing result.</returns>
    public async Task<ExpenseParseResult> ParseFromUrlAsync(
        string imageUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return ExpenseParseResult.Fail("L'URL dell'immagine è vuoto", imageUrl);
        }

        try
        {
            // Create the image content part from URL
            var imagePart = ChatMessageContentPart.CreateImagePart(new Uri(imageUrl));

            // Create the text instruction part
            var textPart = ChatMessageContentPart.CreateTextPart(
                "Analizza questo scontrino ed estrai le informazioni sulla spesa. Rispondi in JSON.");

            // Create messages
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(GetSystemPrompt(_todayDate)),
                new UserChatMessage(textPart, imagePart)
            };

            // Call the API
            var response = await _chatClient.CompleteChatAsync(
                messages,
                new ChatCompletionOptions
                {
                    MaxOutputTokenCount = 500,
                    Temperature = 0.1f
                },
                cancellationToken);

            var content = response.Value.Content[0].Text;

            // Parse JSON response
            var parsedExpense = ParseJsonResponse(content);

            if (parsedExpense == null)
            {
                return ExpenseParseResult.Fail("Non sono riuscito a interpretare lo scontrino", imageUrl);
            }

            if (parsedExpense.Amount <= 0)
            {
                return ExpenseParseResult.Fail("Non ho trovato un importo valido nello scontrino", imageUrl);
            }

            return ExpenseParseResult.Ok(parsedExpense, imageUrl);
        }
        catch (Exception ex)
        {
            return ExpenseParseResult.Fail($"Errore durante il parsing: {ex.Message}", imageUrl);
        }
    }

    /// <summary>
    /// Parses JSON response into ParsedExpense.
    /// </summary>
    private static ParsedExpense? ParseJsonResponse(string jsonContent)
    {
        try
        {
            // Clean up the response - remove markdown code blocks if present
            var json = jsonContent.Trim();
            if (json.StartsWith("```json"))
            {
                json = json[7..];
            }
            else if (json.StartsWith("```"))
            {
                json = json[3..];
            }

            if (json.EndsWith("```"))
            {
                json = json[..^3];
            }

            json = json.Trim();

            return System.Text.Json.JsonSerializer.Deserialize<ParsedExpense>(json,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the MIME type from file extension.
    /// </summary>
    private static string GetMimeType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "image/jpeg" // Default to JPEG
        };
    }
}
