using Microsoft.AspNetCore.Mvc;
using template.Server.Data;
using template.Shared.Models.Games;

namespace template.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UnityController : ControllerBase
    {
        private readonly DbRepository _db;

        public UnityController(DbRepository db)
        {
            _db = db;
        }
        [HttpGet("GameCode/{GameCode}")]
        public async Task<IActionResult> GetGameCode(int GameCode)
        {
            object getParam = new
            {
                codeFromUser = GameCode
            };

            //Get course details for the codeFromUser if it exists and is published
            string getGameDetailsQuery = "select * from games g where GameCode = @codeFromUser";
            var getGameDetailsRecords = await _db.GetRecordsAsync<GameDetails>(getGameDetailsQuery, getParam);
            GameDetails gameDetails = getGameDetailsRecords.FirstOrDefault();

            //If no game found, return bad request
            if (gameDetails == null)
            {
                return BadRequest("No game found for game code: " + GameCode);
            }

            //get questions of game
            object getQuestionsParam = new
            {
                GameID = gameDetails.ID
            };
            string getQuestionsQuery = "select * from questions q where GameID = @GameID";
            var getQuestionsRecords = await _db.GetRecordsAsync<GameQuestions>(getQuestionsQuery, getQuestionsParam);
            gameDetails.Questions = getQuestionsRecords.ToList();

            //get answer of questions
            foreach (GameQuestions question in gameDetails.Questions)
            {
                object getAnswersParam = new
                {
                    QuestionID = question.ID
                };
                string getAnswersQuery = "select * from answers a where QuestionID = @QuestionID";
                var getAnswersRecords = await _db.GetRecordsAsync<GameAnswers>(getAnswersQuery, getAnswersParam);
                question.Answers = getAnswersRecords.ToList();
            }
            return Ok(gameDetails);
        }
    }
}