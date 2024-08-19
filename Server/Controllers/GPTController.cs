using DocumentFormat.OpenXml.ExtendedProperties;
using iTextSharp.text.pdf.parser.clipper;
using Microsoft.AspNetCore.Mvc;
using NPOI.SS.Formula.Functions;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Net.Http.Json;
using template.Shared.Models.GPT;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace template.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GPTController : ControllerBase
    {
        private readonly HttpClient _client;
        public GPTController(IConfiguration config)
        {
            _client = new HttpClient();
            string api_key = config.GetValue<string>("OpenAI:Key");
            string auth = "Bearer " + api_key;
            _client.DefaultRequestHeaders.Add("Authorization", auth);
        }

        [HttpPost("GPTChatStage1")]
        public async Task<IActionResult> GPTChatStage1(QuestionsFromGPT promptFromUser)
        {
            string endpoint = "https://api.openai.com/v1/chat/completions";

            string model = "gpt-4o-mini";

            // Maximum number of tokens in the generated response
            int max_tokens = 1200;

            // Temperature parameter for the model
            double temperature = 0.7;


            string promptToSend = $@"Generate {promptFromUser.countQuestions} closed positive questions, each with four answers.

                         Requirements:

                         The correct positive answer must always be listed first on the list as the first option.
                         All questions must be related to the subject of: {promptFromUser.description}.
                         All questions must ask for the correct answer.
                         The questions should be tailored for an audience of: {promptFromUser.audienceDescription}, and written in grammatically correct Hebrew.
                         Base your questions on various subjects throughout the content provided, not limited to the top of the following content: {promptFromUser.information}.
                         Ensure questions are clear, concise, and suitable for assessing knowledge.
                         The questions will be created in such a way that they are methodologically appropriate single-choice positive questions and their purpose is learning and testing knowledge.
                         Each question description should be no more than 50 characters long, but try to use as many characters as possible.
                         Each answer should be no more than 30 characters long, but try to use as many characters as possible.
                         Ensure that none of the question strings contain a quote sign ("" or '). If quotes are present within words, replace them with an empty string to ensure words remain intact.
                         Avoid repeating questions and answers.
                         Validate spelling and grammar.
                         The response format should be in JSON as shown below:

                         {{
                           ""questions"": [
                             {{
                               ""question"": ""text"",
                               ""options"": [""option1"", ""option2"", ""option3"", ""option4""],
                               ""answer"": ""correct option""
                             }}
                           ]
                         }}
                         The correct positive answer must always be listed first as the first option, option1.";

            //Console.WriteLine(promptToSend);

            string promptToSystem = "\nYour task is to generate positive closed questions, each with four answer choices, formatted in JSON. " +
                                    "Ensure that: " +
                                    "1. The correct answer (option users must choose) is always the first option in the list, as option1. " +
                                    "2. The JSON format matches the structure shown below. " +
                                    "Example output:\n```json\n{\n  \"questions\": [\n    {\n      \"question\": \"Which of the following is a fruit?\",\n      \"options\": [\"Apple\", \"Carrot\", \"Banana\", \"Orange\"],\n      \"answer\": \"Apple\"\n    },\n    {\n      \"question\": \"Which of the following is a programming language?\",\n      \"options\": [\"Python\", \"HTML\", \"Java\", \"C++\"],\n      \"answer\": \"Python\"\n    }\n  ]\n}\n```\n\nRequired JSON structure:\n```json\n{\n  \"questions\": [\n    {\n      \"question\": \"text\",\n      \"options\": [\"option1\", \"option2\", \"option3\", \"option4\"],\n      \"answer\": \"correct option\"\n    }\n  ]\n}\n```\nPlease ensure that each generated question follows this format.\n";


            // Construct the promptToAssistant to send to the model
            string promptToAssistant = "";



            // Create a GPTRequest object to send to the API
            GPTRequest request = new GPTRequest()
            {
                response_format = new { type = "json_object" },
                max_tokens = max_tokens,
                model = model,
                temperature = temperature,
                messages = new List<Message>()
                {
                     new Message
                    {
                        role = "system",
                        content = promptToSystem
                    },
                    //new Message
                    //{
                    //    role = "assistant",
                    //    content = promptToAssistant
                    //},
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

        [HttpPost("GPTChatStage2")]
        public async Task<IActionResult> GPTChatStage2(QuestionsFromGPT promptFromUser)
        {
            string endpoint = "https://api.openai.com/v1/chat/completions";

            string model = "gpt-4o-mini";

            // Maximum number of tokens in the generated response
            int max_tokens = 1200;

            // Temperature parameter for the model
            double temperature = 0.7;


            string promptToSend = $@"
                    Generate {promptFromUser.countQuestions} true/false questions, each with a correct answer.

                    Requirements:

                    All questions must be related to the subject of: {promptFromUser.description}.
                    All questions must ask for the correctness of a statement.
                    The questions should be tailored for an audience of: {promptFromUser.audienceDescription}, and written in grammatically correct Hebrew.
                    Base your questions on various subjects throughout the content provided, not limited to the top of the following content: {promptFromUser.information}.
                    Ensure questions are clear, concise, and suitable for assessing knowledge.
                    Each question description should be no more than 50 characters long, but try to use as many characters as possible.
                    Each answer should be no more than 30 characters long, but try to use as many characters as possible.
                    Validate spelling and grammar.
                    Ensure that none of the question strings contain a quote sign ("" or '), even within words.
                    - Replace any quotes within words with an empty string, ensuring words remain intact.
                    The response format should be in JSON as shown below:

                    {{
                      ""questions"": [
                        {{
                          ""question"": ""text"",
                          ""answer"": true/false
                        }}
                      ]
                    }}

                    The answer should be either true or false.
                    ";



            string promptToSystem = "\nYour task is to generate true/false questions, each with a correct answer, formatted in JSON. " +
                                    "Ensure that the JSON format matches the structure shown below. " +
                                    "Example output:\n```json\n{\n  \"questions\": [\n    {\n      \"question\": \"Is the sky blue?\",\n      \"answer\": true\n    },\n    {\n      \"question\": \"Is the sun a planet?\",\n      \"answer\": false\n    }\n  ]\n}\n```\n\nRequired JSON structure:\n```json\n{\n  \"questions\": [\n    {\n      \"question\": \"text\",\n      \"answer\": true/false\n    }\n  ]\n}\n```\nPlease ensure that each generated question follows this format.\n";



            // Construct the promptToAssistant to send to the model
            string promptToAssistant = "";



            // Create a GPTRequest object to send to the API
            GPTRequest request = new GPTRequest()
            {
                response_format = new { type = "json_object" },
                max_tokens = max_tokens,
                model = model,
                temperature = temperature,
                messages = new List<Message>()
                {
                     new Message
                    {
                        role = "system",
                        content = promptToSystem
                    },
                    //new Message
                    //{
                    //    role = "assistant",
                    //    content = promptToAssistant
                    //},
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

        [HttpPost("GPTChatStage3")]
        public async Task<IActionResult> GPTChatStage3(QuestionsFromGPT promptFromUser)
        {
            string endpoint = "https://api.openai.com/v1/chat/completions";

            string model = "gpt-4o-mini";

            // Maximum number of tokens in the generated response
            int max_tokens = 1200;

            // Temperature parameter for the model
            double temperature = 0.9;


            string promptToSend = $@"Generate {promptFromUser.countQuestions} closed negative questions, each with four answers.
                                Requirements:
                                - The correct answer must always be the first option in the list.
                                - All questions must be related to the subject of: {promptFromUser.description}.
                                - All questions must ask for the incorrect answer.
                                - Write the questions in grammatically correct Hebrew, suitable for an audience of: {promptFromUser.audienceDescription}.
                                - Base your questions on various subjects throughout the provided content: {promptFromUser.information}.
                                - Ensure questions are clear, concise, suitable for assessing knowledge, and are no more than 50 characters long.
                                - Each answer should be no more than 30 characters long.
                                - Ensure that none of the question strings contain a quotation mark (""\"""" or ''), and replace any quotes within words with an empty string.
                                - Avoid repeating questions and answers.
                                - Validate spelling and grammar.
                                - Make sure the correct answer is always the first option within the options to choose from.. 
                                - Double-check that the first option is always the answer which the user should choose.
                                  The response format should be in JSON as shown below:

                                                {{
                                                  ""questions"": [
                                                    {{
                                                      ""question"": ""{{question_text}}"",
                                                      ""options"": [""option1"", ""option2"", ""option3"", ""option4""],
                                                      ""answer"": ""option1""
                                                    }}
                                                  ]
                                                }}";


            string promptToSystem = $@"Your task is to generate negative closed questions, each with four answer choices, formatted in JSON. 
                                    Ensure the following:
                                    1. The incorrect answer (option users must choose) is always listed first as option1 in the list.
                                    2. The JSON format matches the structure shown below.

                                    Example output - For each and every question and answers, make sure the correct option which is the answer is listed first in the options array:
                                    {{
                                      ""questions"": [
                                        {{
                                          ""question"": ""Which of the following is NOT a fruit?"",
                                          ""options"": [""Carrot"", ""Apple"", ""Banana"", ""Orange""],
                                          ""answer"": ""Carrot""
                                        }},
                                        {{
                                          ""question"": ""Which of the following is NOT a programming language?"",
                                          ""options"": [""HTML"", ""Python"", ""Java"", ""C++""],
                                          ""answer"": ""HTML""
                                        }}
                                      ]
                                    }}";

            // Construct the promptToAssistant to send to the model
            string promptToAssistant = "";



            // Create a GPTRequest object to send to the API
            GPTRequest request = new GPTRequest()
            {
                response_format = new { type = "json_object" },
                max_tokens = max_tokens,
                model = model,
                temperature = temperature,
                messages = new List<Message>()
                {
                     new Message
                    {
                        role = "system",
                        content = promptToSystem
                    },
                    //new Message
                    //{
                    //    role = "assistant",
                    //    content = promptToAssistant
                    //},
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

            // Deserialize the content into a JObject
            JObject jsonObject = JObject.Parse(content);

            // Prepare a list to store parsed questions
            var generatedQuestions = new List<GeneratedQuestion>();

            // Extract and process each question in the JSON object
            foreach (var questionToken in jsonObject["questions"])
            {
                var questionObject = (JObject)questionToken;

                // Extract question properties
                string questionText = questionObject["question"].ToString();
                var optionsArray = questionObject["options"] as JArray;
                string correctAnswer = questionObject["answer"].ToString();

                // Ensure correct answer is first in the options array
                if (optionsArray != null && optionsArray.Any())
                {
                    var optionsList = optionsArray.Select(o => o.ToString()).ToList();

                    // Move correct answer to the first position
                    if (optionsList.Contains(correctAnswer))
                    {
                        optionsList.Remove(correctAnswer);
                        optionsList.Insert(0, correctAnswer);
                    }

                    // Create a new GeneratedQuestion object
                    var generatedQuestion = new GeneratedQuestion
                    {
                        Question = questionText,
                        Options = optionsList,
                        Answer = correctAnswer
                    };

                    // Add to the list
                    generatedQuestions.Add(generatedQuestion);
                }
            }

            // Serialize the modified list back to JSON string
            var modifiedContent = JsonConvert.SerializeObject(generatedQuestions);

            // Return the modified content
            return Ok(modifiedContent);


        }

        [HttpPost("GPTChatStage4")]
        public async Task<IActionResult> GPTChatStage4(QuestionsFromGPT promptFromUser)
        {
            try
            {
                string endpoint = "https://api.openai.com/v1/chat/completions";
                string model = "gpt-4o-mini";
                int max_tokens = 1200;
                double temperature = 0.7;

                // Construct the promptToSend to send to OpenAI
                string promptToSend = $@"
            Requirements:
            Generate {promptFromUser.countQuestions} answers for the prompt: ""{promptFromUser.description}""
            Requirements:
            - Provide as many answers as requested, including both correct and incorrect ones.
            - Answers should include correct and incorrect options about {promptFromUser.description}.
            - Base your questions on various subjects throughout the provided content: {promptFromUser.information}.
            - Ensure questions are clear, concise, suitable for assessing knowledge, and are no more than 50 characters long.
            - Each answer should be no more than 20 characters long.
            - Ensure that none of the question strings contain a quotation mark (""\"""" or ''), and replace any quotes within words with an empty string.
            - Avoid repeating questions and answers.
            - Validate spelling and grammar.
            - Make sure the correct answer is always the first option within the options to choose from.. 
            - Double-check that the first option is always the answer which the user should choose.
            The response format should be in JSON as shown below:
            {{
              ""answers"": [
                {{
                  ""answer"": ""A"",
                  ""correct"": true
                }},
                {{
                  ""answer"": ""Б"",
                  ""correct"": false
                }},
                {{
                  ""answer"": ""C"",
                  ""correct"": true
                }},
                // Add more answer objects as needed
              ]
            }}
            ";

                // Construct the promptToSystem to send to OpenAI
                string promptToSystem = $@"
            Your task is to generate answers for the prompt: ""{promptFromUser.description}""
            Requirements:
            - Provide as many answers as requested, including both correct and incorrect ones.
            - Answers should include correct options and incorrect options.
            Example output:
            {{
              ""answers"": [
                {{
                  ""answer"": ""A"",
                  ""correct"": true
                }},
                {{
                  ""answer"": ""Б"",
                  ""correct"": false
                }},
                {{
                  ""answer"": ""C"",
                  ""correct"": true
                }},
                // Add more answer objects as needed
              ]
            }}
            ";

                // Create a GPTRequest object to send to the API
                GPTRequest request = new GPTRequest()
                {
                    response_format = new { type = "json_object" },
                    max_tokens = max_tokens,
                    model = model,
                    temperature = temperature,
                    messages = new List<Message>()
            {
                new Message
                {
                    role = "system",
                    content = promptToSystem
                },
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
                    return BadRequest("Problem: " + await res.Content.ReadAsStringAsync());

                // Read the JSON response from the API
                JsonObject? jsonFromGPT = await res.Content.ReadFromJsonAsync<JsonObject>();
                if (jsonFromGPT == null || !jsonFromGPT.ContainsKey("choices"))
                    return BadRequest("Empty or invalid response from OpenAI");

                // Extract the generated content from the JSON response
                var choices = jsonFromGPT["choices"] as JsonArray;
                if (choices == null || choices.Count == 0)
                    return BadRequest("No valid choices found in response");

                var content = choices[0]["message"]["content"].ToString();
                if (string.IsNullOrEmpty(content))
                    return BadRequest("Empty content received");

                // Return the generated content
                return Ok(content);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }
}
