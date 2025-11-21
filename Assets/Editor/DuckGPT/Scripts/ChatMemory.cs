#if UNITY_EDITOR
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
    public long timestamp;
    public bool isSummary; 
}

public static class ChatMemory
{
    private const int maxMessagesForContext = 16; // Max messages to include in request context

    private const int summarizationThreshold = 24; // When to start summarizing old messages

    private const int amountToSummarize = 10; // Number of old messages to summarize at once

    private static readonly List<ChatMessage> chatMessages = new List<ChatMessage>();
    private static bool chatLoaded;

    private static string FilePath => Path.GetFullPath(Path.Combine(Application.dataPath, "../ProjectSettings/DuckGPT_ChatHistory.json"));

    private static void EnsureLoaded() // Load chat history from disk
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
            Debug.LogWarning($"DuckGPT: Failed to load chat history. {e.Message}");
        }

        chatLoaded = true;
    }

    public static void Save()
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
            Debug.LogWarning($"DuckGPT: Failed to save chat history. {e.Message}");
        }
    }

    public static void Clear()
    {
        EnsureLoaded();
        chatMessages.Clear();
        Save();
    }

    public static void AddUser(string content)
    {
        EnsureLoaded();
        chatMessages.Add(new ChatMessage
        {
            role = "user",
            content = content,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
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
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            isSummary = false
        });
        Trim();
    }

    // Returns only the most recent messages sufficient for context
    public static List<ChatMessage> GetRecentMessages(int max = maxMessagesForContext)
    {
        EnsureLoaded();
        if (chatMessages.Count <= max) return new List<ChatMessage>(chatMessages);
        return chatMessages.Skip(Math.Max(0, chatMessages.Count - max)).ToList();
    }

    // Builds the messages for the next request:
    // - Adds system prompt
    // - Adds recent history
    // - Replaces the final user message (if present) with the augmented userPrompt that includes helpers/context
    public static List<ChatMessage> BuildRequestMessages(string systemPrompt, string userPrompt)
    {
        var request = new List<ChatMessage>
        {
            new() { 
                role = "system", 
                content = systemPrompt 
            }
        };

        var recent = GetRecentMessages();
        request.AddRange(recent);

        if (request.Count > 1 && request[request.Count - 1].role == "user")
        {
            request[request.Count - 1] = new ChatMessage { role = "user", content = userPrompt };
        }
        else
        {
            request.Add(new ChatMessage { role = "user", content = userPrompt });
        }

        return request;
    }

    private static void Trim() // Trim chat history and trigger summarization if needed
    {
        if (chatMessages.Count > summarizationThreshold)
        {
            _ = Task.Run(async () => await TrySummarizeOldMessagesAsync());
        }

        // Keep a bounded history on disk as well
        const int maxPersisted = 200; // persisted cap; request cap is separate
        if (chatMessages.Count > maxPersisted)
        {
            int remove = chatMessages.Count - maxPersisted;
            chatMessages.RemoveRange(0, remove);
        }
    }

    // Summarize older messages to compress history
    public static async Task TrySummarizeOldMessagesAsync()
    {
        try
        {
            EnsureLoaded();
            
            // Only summarize if we have enough non-summary messages
            var nonSummaryMessages = chatMessages.Where(m => !m.isSummary).ToList();
            if (nonSummaryMessages.Count < ChatMemory.amountToSummarize + 5) return; // Keep some recent context

            // Take oldest non-summary messages for summarization
            var messagesToSummarize = nonSummaryMessages.Take(amountToSummarize).ToList();
            
            // Create conversation text for summarization
            var conversationText = string.Join("\n", messagesToSummarize.Select(m => 
                $"{m.role}: {m.content.Substring(0, Math.Min(m.content.Length, 500))}...")); // Truncate long messages

            var summaryPrompt = $"Briefly summarize this Unity development debugging conversation in 2-3 sentences. Focus on key problems discussed and solutions found:\n\n{conversationText}";

            // Call OpenAI for summarization
            string summary = await CreateSummaryAsync(summaryPrompt);
            
            if (!string.IsNullOrEmpty(summary))
            {
                // Remove the old messages
                foreach (var msg in messagesToSummarize)
                {
                    chatMessages.Remove(msg);
                }

                // Insert summary at the beginning (after any existing summaries)
                var insertIndex = chatMessages.TakeWhile(m => m.isSummary).Count();
                chatMessages.Insert(insertIndex, new ChatMessage
                {
                    role = "system",
                    content = $"[Previous conversation summary]: {summary}",
                    timestamp = messagesToSummarize.First().timestamp,
                    isSummary = true
                });

                Save();
                Debug.Log($"DuckGPT: Summarized {messagesToSummarize.Count} old messages into summary.");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"DuckGPT: Failed to summarize old messages. {e.Message}");
        }
    }

    // Create summary using OpenAI API
    private static async Task<string> CreateSummaryAsync(string summaryPrompt)
    {
        try
        {
            // Use a lightweight model for summarization
            string apiKey = ApiConfiguration.GetSavedKey();
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

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Window/RubberDuckHelper/Clear Chat History")]
    private static void MenuClearHistory()
    {
        Clear();
        DebugColor.Log("DuckGPT: Chat history cleared.", "Yellow");
    }

#endif
}
#endif