// Assets/Editor/DuckAI/OpenAIClient.cs
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Unity.Plastic.Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Sends a chat message to the OpenAI API and retrieves the response.
/// </summary>
/// <remarks>This method constructs a chat payload with a system message and the user's prompt, sends it to the
/// OpenAI API, and returns the API's response. The system message is designed to assist Unity developers by providing
/// concise code examples and humorous duck noises.</remarks>

#if UNITY_EDITOR
public static class OpenAIClient 
{
    public static string DefaultModel = "gpt-4.1-mini";
    public static string baseUrl = "https://api.openai.com/v1/chat/completions";
    public static string voice = "echo";
    public static string audioFileName = "speech.mp3";

    public static async Task<string> SendChatAsync(string apiKey, string userPrompt, string model, int maxTokens = 400, float temperature = 0.15f)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("API key required. Set it up in Window/RubberDuckHelper/SetAPIKey");
        if (string.IsNullOrWhiteSpace(model)) { 
            model = DefaultModel;
            Debug.LogError($"No model specified, using default: {model}");
        }
        else
        {
            //DebugColor.Log($"Using model: {model}", "Green");
        }

        using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                var systemMsg = "You are a friendly rubber duck AI. You do not give code or solutions unless asked. Instead, you ask guiding questions to help the developer reason through their problem. You respond conversationally and encourage the user to explain their thought process. Always add some duck noises";

                var payload = new ChatPayload
                {
                    model = model,
                    messages = new object[]
                    {
                    new { role = "system", content = systemMsg },
                    new { role = "user", content = userPrompt }
                    },
                };

                string json = JsonConvert.SerializeObject(payload);


                var obj = JsonConvert.DeserializeObject<ChatPayload>(json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var resp = await client.PostAsync(baseUrl, content);

                var respText = await resp.Content.ReadAsStringAsync();

                

            if (!resp.IsSuccessStatusCode)
                {
                    throw new Exception($"OpenAI error: {(int)resp.StatusCode} {resp.ReasonPhrase}\n{respText}");
                }

                return respText;

            }
    }

    public static async Task<string> RequestSpeechAsync(string apiKey, string text, string voice, string model = "tts-1", string audioFileName = "duck_speech.mp3")
    {
        if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("API key required.");
        using (var client = new HttpClient())
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var payload = new
            {
                model = model,
                voice = voice,
                input = text,
                instructions = "Generate a friendly and engaging speech suitable for a rubber duck character."
            };

            string json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var resp = await client.PostAsync("https://api.openai.com/v1/audio/speech", content);

            if (!resp.IsSuccessStatusCode)
            {
                var respText = await resp.Content.ReadAsStringAsync();
                throw new Exception($"OpenAI TTS error: {(int)resp.StatusCode} {resp.ReasonPhrase}\n{respText}");
            }

            // Save the audio file

            
            var audioBytes = await resp.Content.ReadAsByteArrayAsync();
            // var audioPath = Path.Combine("Assets/Editor/DuckAI/Audio/", audioFileName);
            var audioPath = Path.GetTempPath() + audioFileName;

            File.WriteAllBytes(audioPath, audioBytes);


            DebugColor.Log($"Audio file saved as: {audioFileName} at {audioPath}", "magenta");
     

            return Path.GetFullPath(audioPath);
        }
    }
}

[System.Serializable]
class ChatPayload
{
    public string model;
    public object[] messages;
}
#endif

//references:
//https://platform.openai.com/docs/api-reference/chat/create
//https://dev.to/1geek/unity-openai-vision-and-voice-3930 text to speech
//https://api.openai.com/v1/chat/completions