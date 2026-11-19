using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using OES.MODELS.Ai;

namespace OES.SERVICES.Ai;

public sealed class GeminiAiService(
    HttpClient httpClient,
    string apiKey,
    string model,
    ILogger<GeminiAiService> logger) : IAiService
{
    private readonly string _apiKey = apiKey ?? string.Empty;
    private readonly string _model = string.IsNullOrWhiteSpace(model) ? "gemini-1.5-flash" : model;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<AiGeneratedQuestion>> GenerateQuestionsAsync(
        AiGenerateQuestionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var count = Math.Clamp(request.Count, 1, 15);
        var topic = string.IsNullOrWhiteSpace(request.Topic) ? "General Knowledge" : request.Topic.Trim();

        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            try
            {
                var prompt = $$"""
                    You are an expert academic examiner. Create exactly {{count}} multiple-choice questions (MCQs) for an online examination.
                    Topic: "{{topic}}"
                    Difficulty Level: {{request.Difficulty}}

                    Each question must have:
                    - "questionText": Clear, unambiguous question text.
                    - "explanation": Brief explanation of the answer.
                    - "options": An array of exactly 4 options. Exactly ONE option must have "isCorrect": true, and the other 3 must have "isCorrect": false.
                    - "optionText": The text of each option.

                    Return ONLY valid JSON matching this schema:
                    [
                      {
                        "questionText": "...",
                        "explanation": "...",
                        "options": [
                          { "optionText": "...", "isCorrect": true },
                          { "optionText": "...", "isCorrect": false },
                          { "optionText": "...", "isCorrect": false },
                          { "optionText": "...", "isCorrect": false }
                        ]
                      }
                    ]
                    """;

                var aiResponse = await CallGeminiAsync(prompt, cancellationToken);
                if (!string.IsNullOrWhiteSpace(aiResponse))
                {
                    var cleanJson = ExtractJson(aiResponse);
                    var questions = JsonSerializer.Deserialize<List<AiGeneratedQuestion>>(cleanJson, JsonOptions);
                    if (questions is not null && questions.Count > 0)
                    {
                        return questions;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Gemini API call failed for question generation. Using fallback.");
            }
        }

        // High quality fallback questions if API key is not configured or fails
        return GenerateFallbackQuestions(topic, count, request.Difficulty);
    }

    public async Task<AiStudyRecommendation> GenerateStudyRecommendationsAsync(
        string examTitle,
        decimal obtainedMarks,
        decimal totalMarks,
        decimal percentage,
        bool isPassed,
        int correctCount,
        int wrongCount,
        int unattemptedCount,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            try
            {
                var passStatus = isPassed ? "PASSED" : "FAILED";
                var prompt = $$"""
                    You are an AI Academic Mentor and Career Counselor. Analyze this student's exam performance:
                    - Exam: "{{examTitle}}"
                    - Score: {{obtainedMarks}} out of {{totalMarks}} ({{percentage}}%)
                    - Result: {{passStatus}}
                    - Correct Answers: {{correctCount}}
                    - Wrong Answers: {{wrongCount}}
                    - Unattempted Questions: {{unattemptedCount}}

                    Provide personalized feedback. Return ONLY valid JSON:
                    {
                      "overallFeedback": "2-3 encouraging sentences analyzing performance.",
                      "strengths": ["Strength 1", "Strength 2"],
                      "areasToImprove": ["Area 1", "Area 2"],
                      "studyTips": ["Actionable tip 1", "Actionable tip 2", "Actionable tip 3"]
                    }
                    """;

                var aiResponse = await CallGeminiAsync(prompt, cancellationToken);
                if (!string.IsNullOrWhiteSpace(aiResponse))
                {
                    var cleanJson = ExtractJson(aiResponse);
                    var recommendation = JsonSerializer.Deserialize<AiStudyRecommendation>(cleanJson, JsonOptions);
                    if (recommendation is not null)
                    {
                        return recommendation;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Gemini API call failed for study recommendations. Using fallback.");
            }
        }

        return GenerateFallbackRecommendations(examTitle, percentage, isPassed, correctCount, wrongCount, unattemptedCount);
    }

    private async Task<string?> CallGeminiAsync(string prompt, CancellationToken cancellationToken)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.4,
                response_mime_type = "application/json"
            }
        };

        var response = await httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("Gemini API error {StatusCode}: {Error}", response.StatusCode, err);
            return null;
        }

        var json = await response.Content.ReadFromJsonAsync<GeminiApiResponse>(cancellationToken: cancellationToken);
        return json?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
    }

    private static string ExtractJson(string text)
    {
        text = text.Trim();
        if (text.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            text = text[7..];
        }
        else if (text.StartsWith("```"))
        {
            text = text[3..];
        }

        if (text.EndsWith("```"))
        {
            text = text[..^3];
        }

        return text.Trim();
    }

    private static IReadOnlyList<AiGeneratedQuestion> GenerateFallbackQuestions(string topic, int count, string difficulty)
    {
        var list = new List<AiGeneratedQuestion>();

        for (var i = 1; i <= count; i++)
        {
            list.Add(new AiGeneratedQuestion
            {
                QuestionText = $"[{difficulty}] Question {i} on {topic}: Which of the following is considered a core principle or best practice in {topic}?",
                Explanation = $"Option A is the fundamental standard practice widely accepted in modern {topic} development.",
                Options =
                [
                    new() { OptionText = $"Standardized convention and modular design for {topic}", IsCorrect = true },
                    new() { OptionText = $"Hardcoding all runtime parameters without abstraction", IsCorrect = false },
                    new() { OptionText = $"Skipping validation and exception handling layers", IsCorrect = false },
                    new() { OptionText = $"Coupling business logic directly inside user interface controls", IsCorrect = false }
                ]
            });
        }

        return list;
    }

    private static AiStudyRecommendation GenerateFallbackRecommendations(
        string examTitle,
        decimal percentage,
        bool isPassed,
        int correctCount,
        int wrongCount,
        int unattemptedCount)
    {
        var strengths = new List<string>();
        var areasToImprove = new List<string>();
        var tips = new List<string>();

        if (isPassed)
        {
            strengths.Add($"Strong performance overall, scoring {percentage}% on {examTitle}.");
            strengths.Add($"Answered {correctCount} questions accurately.");
            areasToImprove.Add($"Review the {wrongCount} incorrect questions to achieve perfection.");
            tips.Add("Focus on time optimization during mock tests.");
            tips.Add("Keep revising summary notes before full-length tests.");
        }
        else
        {
            strengths.Add($"Completed the attempt and tackled fundamental concepts.");
            areasToImprove.Add($"Total {wrongCount} wrong answers indicate need for concept clarification.");
            if (unattemptedCount > 0)
            {
                areasToImprove.Add($"{unattemptedCount} questions were left unattempted — focus on time pacing.");
            }
            tips.Add("Re-read core textbook chapters corresponding to missed topics.");
            tips.Add("Solve topic-wise flashcards before attempting another full exam.");
            tips.Add("Analyze negative marking impact to minimize blind guessing.");
        }

        return new AiStudyRecommendation
        {
            OverallFeedback = isPassed
                ? $"Congratulations! You demonstrated a solid grasp of {examTitle} with a passing score of {percentage}%."
                : $"You scored {percentage}% in {examTitle}. With targeted practice and concept revision, you can easily qualify on your next attempt.",
            Strengths = strengths,
            AreasToImprove = areasToImprove,
            StudyTips = tips
        };
    }

    private sealed class GeminiApiResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate>? Candidates { get; set; }
    }

    private sealed class Candidate
    {
        [JsonPropertyName("content")]
        public Content? Content { get; set; }
    }

    private sealed class Content
    {
        [JsonPropertyName("parts")]
        public List<Part>? Parts { get; set; }
    }

    private sealed class Part
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
