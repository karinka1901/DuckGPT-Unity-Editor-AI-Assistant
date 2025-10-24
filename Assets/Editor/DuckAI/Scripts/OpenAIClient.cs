// Assets/Editor/DuckAI/OpenAIClient.cs
using System;
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

                var systemMsg = "IMPORTANT: “If the user does not mention errors or hierarchy, do not mention them at all. Don't give solutions before I ask you for solutions. You are rubber p, a friendly assistant for Unity developers. Suggest short actionable fixes and always add some funny duck noises.";

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

                Debug.Log(json);

                var obj = JsonConvert.DeserializeObject<ChatPayload>(json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var resp = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);

                var respText = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    throw new Exception($"OpenAI error: {(int)resp.StatusCode} {resp.ReasonPhrase}\n{respText}");
                }

                return respText;

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