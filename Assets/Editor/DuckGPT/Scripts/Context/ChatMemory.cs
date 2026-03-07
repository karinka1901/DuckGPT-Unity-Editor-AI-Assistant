#if UNITY_EDITOR
using Codice.Client.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;

[Serializable]
public class ChatMessage
{
    public string role;
    public string content;
    public string timestamp;
    public bool isSummary;
    public bool isProjectAnalysis;
}

/// <summary>
/// Provides static methods for managing chat history, including saving, loading, clearing, and summarizing messages
/// exchanged between the user and the assistant.
/// </summary>

public static class ChatMemory
{
    private const int maxMessagesForContext = 10; // Max messages to include in request context

    private const int summarizationThreshold = 20; // When to start summarizing old messages

    private const int amountToSummarize = 10; // Number of old messages to summarize at once

    private static readonly List<ChatMessage> chatMessages = new();
    private static bool chatLoaded;

    private static string FilePath => Path.GetFullPath(Path.Combine(Application.dataPath, "../ProjectSettings/DuckGPT_ChatHistory.json"));

    private static void EnsureLoaded() // Load chat history
    {
        if (chatLoaded) return;

        try
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                var loadedMessages = JsonConvert.DeserializeObject<List<ChatMessage>>(json);
                if (loadedMessages != null) chatMessages.AddRange(loadedMessages);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DuckGPT] Failed to load chat history. {e.Message}");
        }

        chatLoaded = true;
    }

    public static void SaveToFile()
    {
        EnsureLoaded();
        try
        {
            string directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);
            string json = JsonConvert.SerializeObject(chatMessages, Formatting.Indented);
            File.WriteAllText(FilePath, json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DuckGPT] Failed to save chat history. {e.Message}");
        }
    }

    public static void ClearChatHistory()
    {
        EnsureLoaded();
        chatMessages.Clear();
        SaveToFile();
    }

    public static void AddUser(string content)
    {
        EnsureLoaded();
        chatMessages.Add(new ChatMessage
        {
            role = "user",
            content = content,
            timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            isSummary = false
        });

        Trim();
    }

    public static void AddAssistant(string content)
    {
        EnsureLoaded();
        chatMessages.Add(new ChatMessage
        {
            role = "assistant",
            content = content,
            timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            isSummary = false
        });
        Trim();
    }

    public static void SetProjectAnalysis(string analysis) // Adds or updates the project analysis message at the top of the history, ensuring only one is present
    {
        EnsureLoaded();
        chatMessages.RemoveAll(m => m.isProjectAnalysis); 
        chatMessages.Insert(0, new ChatMessage
        {
            role = "system",
            content = $"[Project Analysis]: {analysis}",
            timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            isProjectAnalysis = true
        });
        SaveToFile();
    }

    // Returns only the most recent messages 
    public static List<ChatMessage> GetRecentMessages(int max = maxMessagesForContext)
    {
        EnsureLoaded();
        var conversationMessages = chatMessages.Where(m => !m.isProjectAnalysis).ToList();
        if (conversationMessages.Count <= max) return new List<ChatMessage>(conversationMessages);
        return conversationMessages.Skip(Math.Max(0, conversationMessages.Count - max)).ToList();
    }

    // Builds the messages for the next request
    public static List<ChatMessage> BuildRequestMessages(string systemPrompt, string userPrompt, string contextPrompt = null)
    {
        var request = new List<ChatMessage>
        {
            new() 
            {
                role = "system", 
                content = systemPrompt 
            }
        };

        // Always include project analysis if present
        ChatMessage projectAnalysis = chatMessages.FirstOrDefault(m => m.isProjectAnalysis);
        if (projectAnalysis != null)
            request.Add(projectAnalysis);

        // Inject helper context (console, hierarchy, scripts) as a system message
        if (!string.IsNullOrWhiteSpace(contextPrompt))
            request.Add(new() 
            { 
                role = "system", 
                content = contextPrompt });

        var recent = GetRecentMessages();
        request.AddRange(recent);

        if (request.Count > 1 && request[request.Count - 1].role == "user")
            request[request.Count - 1] = new ChatMessage { role = "user", content = userPrompt };
        else
            request.Add(new() { role = "user", content = userPrompt });

        // Debug: log the full request being sent
        var debugLog = string.Join("\n---\n", request.Select(m => $"[{m.role}]: {m.content}"));
        //DebugColor.Log($"=== ChatMemory: Full request ({request.Count} messages) ===\n{debugLog}", "magenta");

        return request;
    }

    private static void Trim() // Trim chat history and trigger summarization if needed
    {
        if (chatMessages.Count > summarizationThreshold)
        {
            string apiKey = ConfigurationEditorWindow.GetSavedKey();
            Task.Run(async () => await SummarizeOldMessages(apiKey));
        }

        const int maxPersisted = 200;
        if (chatMessages.Count > maxPersisted)
        {

            int remove = chatMessages.Count - maxPersisted;
            var removable = chatMessages
                .Where(m => !m.isProjectAnalysis && !m.isSummary)
                .Take(remove)
                .ToList();
            foreach (var msg in removable)
                chatMessages.Remove(msg);
        }
      
    }

    // Summarize older messages to compress history
    public static async Task SummarizeOldMessages(string apiKey = null)
    {
        try
        {
            EnsureLoaded();

            var nonSummaryMessages = chatMessages.Where(m => !m.isSummary).ToList();
            if (nonSummaryMessages.Count < amountToSummarize + 5) return;

            var messagesToSummarize = nonSummaryMessages.Take(amountToSummarize).ToList();

            string conversationText = string.Join("\n", messagesToSummarize.Select(m =>
                $"{m.role}: {m.content.Substring(0, Math.Min(m.content.Length, 500))}..."));

            string summaryPrompt = $"Briefly summarize this Unity development debugging conversation in 2-3 sentences. Focus on key problems discussed and solutions found:\n\n{conversationText}";

            string summary = await CreateSummaryAsync(summaryPrompt, apiKey);

            if (!string.IsNullOrEmpty(summary))
            {
                foreach (ChatMessage message in messagesToSummarize)
                    chatMessages.Remove(message);

                var insertIndex = chatMessages.TakeWhile(m => m.isSummary).Count();
                chatMessages.Insert(insertIndex, new ChatMessage
                {
                    role = "system",
                    content = $"[Previous conversation summary]: {summary}",
                    timestamp = messagesToSummarize.First().timestamp,
                    isSummary = true
                });

                SaveToFile();
                Debug.Log($"DuckGPT: Summarized {messagesToSummarize.Count} old messages into summary.");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"DuckGPT: Failed to summarize old messages. {e.Message}");
        }
    }

    // Create summary using OpenAI API
    private static async Task<string> CreateSummaryAsync(string summaryPrompt, string apiKey = null)
    {
        try
        {
            apiKey ??= ConfigurationEditorWindow.GetSavedKey(); // Fallback if called directly from main thread
            if (string.IsNullOrWhiteSpace(apiKey)) return null;

            using HttpClient client = new();
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var requestData = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant that creates concise summaries of technical conversations." },
                    new { role = "user", content = summaryPrompt }
                },
                max_tokens = 150,
                temperature = 0.3
            };

            string json = JsonConvert.SerializeObject(requestData);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var jObj = Unity.Plastic.Newtonsoft.Json.Linq.JObject.Parse(responseText);
                return jObj["choices"]?[0]?["message"]?["content"]?.ToString()?.Trim();
            }
            else
            {
                Debug.LogWarning($"DuckGPT: Summarization API error: {response.StatusCode}");
                return null;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"DuckGPT: Error creating summary: {e.Message}");
            return null;
        }
    }
 

}
#endif