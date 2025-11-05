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
public static class AIApiClient 
{
    public static string defaultModel = "gpt-4.1-mini";
    public static string openAiUrl = "https://api.openai.com/v1/chat/completions";
    
    private static string elevenLabsUrl = "https://api.elevenlabs.io/v1/text-to-speech/";
    public static string elevenLabsVoiceId = "mdzEgLpu0FjTwYs5oot0";

    public static string audioFileName = "speech.mp3";

    #region Chat with OpenAI
    /// Sends a chat prompt to OpenAI and returns the response as a string.
    public static async Task<string> SendChatAsync(string userPrompt) 
    {
        // Retrieve API key and model from configuration
        string apiKey = ApiConfiguration.GetSavedKey();
        string model = ApiConfiguration.GetSavedModel();

        //Use default model if none specified
        if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("API key required. Set it up in Window/RubberDuckHelper/Settings");
        if (string.IsNullOrWhiteSpace(model)) { 
            model = defaultModel;
            Debug.LogError($"No model specified, using default: {model}");
        }

        // Set up HTTP client
        using HttpClient client = new();
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        // Prepare request data
        var requestData = new
        {
            model,
            messages = new[]
            {
              new { role = "assistant", content = "You are a friendly rubber duck AI You respond conversationally and encourage the user to explain their thought process. Always add some duck noises" },
              new { role = "user", content = userPrompt }
            }
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

        return responceText;
    }

    #endregion

    #region Text to Speech with Eleven Labs
    /// Requests speech synthesis from Eleven Labs and returns the path to the saved audio file.
    public static async Task<string> RequestSpeechAsync(string inputText)
    {
        // Retrieve Eleven Labs API key from configuration
        string elevenLabsApiKey = ApiConfiguration.GetSavedElevenLabsKey();

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