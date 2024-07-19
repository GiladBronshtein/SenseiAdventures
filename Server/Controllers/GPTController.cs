using DocumentFormat.OpenXml.ExtendedProperties;
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
            _client = new HttpClient();
            string api_key = config.GetValue<string>("OpenAI:Key");
            string auth = "Bearer " + api_key;
            _client.DefaultRequestHeaders.Add("Authorization", auth);
        }

        [HttpPost("GPTChat")]
        public async Task<IActionResult> GPTChat(QuestionsFromGPT promptFromUser)
        {
            string endpoint = "https://api.openai.com/v1/chat/completions";

            string model = "gpt-4o-mini";

            // Maximum number of tokens in the generated response
            int max_tokens = 1200;

            // Temperature parameter for the model
            double temperature = 0.5;



            //string promptToSend = $"Generate {promptFromUser.countQuestions} closed questions with 4 answers each, related to the subject of {promptFromUser.description}. " +
            //    $"The questions should be tailored to the level of {promptFromUser.audienceDescription}, and written in a proper grammatically Hebrew. " +
            //    $"Base your knowledge on following content to build the questions and answers: {promptFromUser.information}. " +
            //    $"The questions should be clear, concise, and designed to assess someone’s knowledge or understanding of the topic. " +
            //    $"The questions will be created in such a way that they are methodologically appropriate single-choice questions and their purpose is learning and testing knowledge. " +
            //    $"Each question description should be maximum 50 characters (try to use as many chars as possible). " +
            //    $"Each answer description should be maximum 30 characters. (try to use as many chars as possible). " +
            //    $"Each question will contain 4 answers to choose from. ALWAYS make sure to put the correct answer first, always. " +
            //    $"Make sure not to use the same answer twice. " +
            //    $"Validate all words and ensure they are correctly written. " +
            //    $"Respond in JSON format as follows: "+
            //    " `{'questions':[{'question':'text','options':['option1','option2','option3','option4'],'answer':'correct option'}]}`. ";

            string promptToSend = $"Generate {promptFromUser.countQuestions} closed questions, each with 4 answers, where the correct answer must always be listed first, " +
                                    $"related to the subject of: {promptFromUser.description}. " +
                                    $"Questions should positively ask for the correct answer and be tailored for audience of: {promptFromUser.audienceDescription}, " +
                                    $"written in grammatically correct Hebrew. " +
                                    $"Base your questions on various subjects throughout the content, not limited to the top of the following content: {promptFromUser.information}. " +
                                    $"Ensure questions are clear, concise, and suitable for assessing knowledge. " +
                                    $"The questions will be created in such a way that they are methodologically appropriate single-choice positive questions and their purpose is learning and testing knowledge. " +
                                    $"Each question description should be no more than 50 characters long, but try to use as many characters as possible. " +
                                    $"Each answer should be no more than 30 characters long, but try to use as many characters as possible. " +
                                    $"The correct answer must always be listed first. " +
                                    $"Avoid repeating questions and answers. " +
                                    $"Validate spelling and grammar. " +
                                    $"Response format should be in JSON: " +
                                    "`{'questions':[{'question':'text','options':['option1','option2','option3','option4'],'answer':'correct option'}]}`. " +
                                    "The correct answer must always be listed first.";



            Console.WriteLine(promptToSend);

            // Construct the promptToSystem to send to the model
            //string promptToSystem = "Upon receiving a request reply in JSON:" +
            //    " `{'questions':[{'question':'text','options':['option1','option2','option3','option4'],'answer':'correct option'}]}`. " +
            //    "ALWAYS make sure to put the correct answer first, always., no matter what.";


            string promptToSystem = "Generate closed questions with 4 answers each, where the correct answer must always be listed first, structured in JSON format: " +
                                "`{'questions':[{'question':'text','options':['option1','option2','option3','option4'],'answer':'correct option'}]}`. " +
                                "Ensure the correct answer is always listed first. The correct answer must always be listed first.";




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
    }
}
