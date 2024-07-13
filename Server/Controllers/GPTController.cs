using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;
using template.Shared.Models.GPT;

namespace template.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GPTController : ControllerBase
    {
        private readonly HttpClient _client;
        public GPTController(IConfiguration config)
        {
            // Initialize the private HttpClient instance
            _client = new HttpClient();

            // Retrieve the API key from the configuration settings
            string api_key = config.GetValue<string>("OpenAI:Key");

            // Create the authorization header using the API key
            string auth = "Bearer " + api_key;

            // Add the authorization header to the default request headers of the HttpClient instance
            _client.DefaultRequestHeaders.Add("Authorization", auth);
        }

        [HttpPost("GPTChat")]
        public async Task<IActionResult> GPTChat(QuestionsFromGPT promptFromUser)
        {
            // API endpoint for OpenAI GPT
            string endpoint = "https://api.openai.com/v1/chat/completions";
            // Specifies the model to use for chat completions (GPT-3.5 Turbo)
            string model = "gpt-3.5-turbo";
            // Maximum number of tokens in the generated response
            int max_tokens = 120;
            // Construct the prompt to send to the model
            string promptToSend = $"Please generate an open question related to the subject of {promptFromUser.information} in the level of {promptFromUser.audienceDescription}." +
            $"The question should be clear, concise, and designed to assess someone's knowledge " +
            $"or understanding of the topic. Keep your answer under 300 characters. Return the answer only in hebrew language.";

            // Create a GPTRequest object to send to the API
            GPTRequest request = new GPTRequest()
            {
                max_tokens = max_tokens,
                model = model,
                messages = new List<Message>() {
            new Message
            {
                role = "user",
                content = promptToSend
            }
        }
            };

            // Send the GPTRequest object to the OpenAI API
            var res = await _client.PostAsJsonAsync(endpoint, request);

            // Check if the API response indicates an error
            if (!res.IsSuccessStatusCode)
                return BadRequest("problem: " + res.Content.ReadAsStringAsync());

            // Read the JSON response from the API
            JsonObject? jsonFromGPT = res.Content.ReadFromJsonAsync<JsonObject>().Result;
            if (jsonFromGPT == null)
                return BadRequest("empty");

            // Extract the generated content from the JSON response
            string content = jsonFromGPT["choices"][0]["message"]["content"].ToString();

            // Return the generated content
            return Ok(content);
        }
    }
}
