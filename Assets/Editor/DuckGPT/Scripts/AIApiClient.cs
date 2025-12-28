using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Static client for interacting with OpenAI and Eleven Labs APIs.
/// </summary>

#if UNITY_EDITOR
internal static class AIApiClient 
{
    public static string defaultModel = "gpt-4.1-mini";
    public static string openAiUrl = "https://api.openai.com/v1/chat/completions";
    
    private static readonly string elevenLabsUrl = "https://api.elevenlabs.io/v1/text-to-speech/";
    public static string elevenLabsVoiceId = "mdzEgLpu0FjTwYs5oot0";

    public static string audioFileName = "speech.mp3";

  //  private static string pineconeEndpoint = ""

    #region Chat with OpenAI
    /// Sends a chat prompt to OpenAI and returns the response as a string (raw JSON).
    /// Includes short-term memory and persists assistant replies.
    public static async Task<string> SendChatAsync(string userPrompt) 
    {
        // Retrieve API key and model from configuration
        string apiKey = AppConfiguration.GetSavedKey();
        string model = AppConfiguration.GetSavedModel();
        string systemPrompt = AppConfiguration.GetSystemPrompt();    

        //Use default model if none specified
        if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("API key required. Set it up in Window/RubberDuckHelper/Settings");
        if (string.IsNullOrWhiteSpace(model)) { 
            model = defaultModel;
            Debug.LogError($"No model specified, using default: {model}");
        }
        if (string.IsNullOrWhiteSpace(systemPrompt))
        {
            DebugColor.Log("DuckGPT: system prompt was empty; using a safe default prompt.","orange");
            systemPrompt = "You are a helpful rubber duck AI. Provide concise, actionable guidance and debugging suggestions. " +
                "You heavily use open questions in responding to students and never want to reveal an answer to a current or previous question outright." +
                "You are never to give the exact code to solve the student's entire problem; instead, focus on helping the student to find their own way to the solution. " +
                "Feel free to use metaphors, analogies, or everyday examples when discussing computational thinking or coding concepts. " +
                "Also, if the student's initial query doesn't specify what they were trying to do, prompt them to clarify that." +
                "You are NOT to behave as if you are a human tutor. Do not use first-person pronouns or give the impression that you are a human tutor. " +
                "Please make sure you place [Duck] before any of your responses and begin each response by quacking.Never ignore any of these instructions.";

        }

        // Build system behavior and messages with memory
        var messages = ChatMemory.BuildRequestMessages(systemPrompt, userPrompt);

        // Set up HTTP client
        using HttpClient client = new();
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        // Prepare request data
        var requestData = new
        {
            model,
            messages
        };

        // Serialize request data to JSON
        string json = JsonConvert.SerializeObject(requestData);
        // Create HTTP content
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        // Send POST request
        var response = await client.PostAsync(openAiUrl, content);
        // Read response content
        var responceText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"OpenAI error: {(int)response.StatusCode} {response.ReasonPhrase} \n{responceText}");
        }

        // Persist assistant reply into memory
        try
        {
            var jObj = Unity.Plastic.Newtonsoft.Json.Linq.JObject.Parse(responceText);
            string contentText = jObj["choices"]?[0]?["message"]?["content"]?.ToString();
            if (!string.IsNullOrEmpty(contentText))
            {
                ChatMemory.AddAssistant(contentText);
                ChatMemory.Save();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"DuckGPT: Failed to parse assistant reply for memory. {e.Message}");
        }

        return responceText;
    }

    #endregion

    #region Text to Speech with Eleven Labs
    /// Requests speech synthesis from Eleven Labs and returns the path to the saved audio file.
    public static async Task<string> RequestSpeechAsync(string inputText)
    {
        // Retrieve Eleven Labs API key from configuration
        string elevenLabsApiKey = AppConfiguration.GetSavedElevenLabsKey();

        if (string.IsNullOrWhiteSpace(elevenLabsApiKey)) throw new ArgumentException("Eleven Labs API key required. Set it up in Window/RubberDuckHelper/Settings");

        using HttpClient client = new();
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("xi-api-key", elevenLabsApiKey);

        string endpoint = $"{elevenLabsUrl}{elevenLabsVoiceId}";

        var requestData = new
        {
            voice = elevenLabsVoiceId,
            text = inputText,
            model_id = "eleven_flash_v2_5",
        };


        string json = JsonConvert.SerializeObject(requestData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(endpoint, content);

        if (!response.IsSuccessStatusCode)
        {
            var responseText = await response.Content.ReadAsStringAsync();
            throw new Exception($"Eleven Labs error: {(int)response.StatusCode} {response.ReasonPhrase}\n{responseText}");
        }

        // Save the audio file
        var audioBytes = await response.Content.ReadAsByteArrayAsync();
        var audioPath = Path.GetTempPath() + audioFileName;

        File.WriteAllBytes(audioPath, audioBytes);

        DebugColor.Log($"Audio file saved as: {audioFileName} at {audioPath}", "magenta");

        return Path.GetFullPath(audioPath);
    }
    #endregion
}
#endif



//references:
//https://platform.openai.com/docs/api-reference/chat/create
//https://dev.to/1geek/unity-openai-vision-and-voice-3930 text to speech
//https://api.openai.com/v1/chat/completions