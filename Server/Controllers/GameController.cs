using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using template.Server.Data;
using template.Shared.Models.Games; 
using template.Shared.Models.Users;

namespace template.Server.Controllers
{
    [Route("api/[controller]/{userId}")]
    [ApiController]
    public class GameController : ControllerBase
    {
        List<string> DeleteImages = new List<string>(); // List to hold the images to delete

        private readonly DbRepository _db;

        public GameController(DbRepository db)
        {
            _db = db;
        }

        #region Helpers

        private async Task<UserWithGames> GetUserWithFirstName(int userId)
        {
            string userQuery = "SELECT FirstName FROM Users WHERE ID = @UserId";
            var userRecords = await _db.GetRecordsAsync<UserWithGames>(userQuery, new { UserId = userId });
            return userRecords.FirstOrDefault();
        }

        private async Task<List<Games>> GetUserGames(int userId)
        {
            string gameQuery = "SELECT GameName FROM Games WHERE UserId = @UserId";
            var gamesRecords = await _db.GetRecordsAsync<Games>(gameQuery, new { UserId = userId });
            return gamesRecords.ToList();
        }

        private async Task<List<Games>> GetUserDetailedGames(int userId)
        {
            string gameQuery = "SELECT ID, GameName, GameCode, IsPublished, CanPublish FROM Games WHERE UserId = @UserId";
            var gamesRecords = await _db.GetRecordsAsync<Games>(gameQuery, new { UserId = userId });
            return gamesRecords.ToList();
        }

        private async Task<int> CreateGame(GameToAdd gameToAdd, int userId)
        {
            var newGameParam = new
            {
                CanPublish = false, // Assuming all new games cannot be published initially
                DifficultLevel = 1, // Default difficulty level
                EndingMessage = gameToAdd.EndingMessage, // Default empty ending message
                GameCode = 0, // Initial game code, will be updated later
                GameHasImage = false, // From input
                GameImage = "empty", // Handle image information
                GameName = gameToAdd.GameName,
                IsPublished = false, // New games are not published initially
                UserID = userId
            };

            string insertGameQuery = @"
            INSERT INTO Games (CanPublish, DifficultLevel, EndingMessage, GameCode, GameHasImage, GameImage, GameName, IsPublished, UserID) 
            VALUES (@CanPublish, @DifficultLevel, @EndingMessage, @GameCode, @GameHasImage, @GameImage, @GameName, @IsPublished, @UserID)";
            return await _db.InsertReturnIdAsync(insertGameQuery, newGameParam);
        }

        private async Task<bool> UpdateGameCode(int gameId, int gameCode)
        {
            string updateCodeQuery = "UPDATE Games SET GameCode = @GameCode WHERE ID = @ID";
            int updateCount = await _db.SaveDataAsync(updateCodeQuery, new { ID = gameId, GameCode = gameCode });
            return updateCount > 0;
        }

        private async Task<Games> GetGameById(int gameId)
        {
            string gameQuery = "SELECT ID, GameName, GameCode, IsPublished, CanPublish FROM Games WHERE ID = @ID";
            var gameRecord = await _db.GetRecordsAsync<Games>(gameQuery, new { ID = gameId });
            return gameRecord.FirstOrDefault();
        }

        #endregion


        [HttpGet]
        public async Task<IActionResult> GetGamesByUser(int userId)
        {
            // Attempt to retrieve the user along with their first name
            var user = await GetUserWithFirstName(userId);
            if (user == null)
            {
                return BadRequest("User Not Found");
            }
            // Retrieve and assign the list of games associated with the user
            user.Games = await GetUserGames(userId);
            return Ok(user);
        }

        [HttpGet("All")]
        public async Task<IActionResult> GetGamesByUserWithDetails(int userId)
        {
            // Attempt to retrieve the user along with their first name to ensure they exist
            var user = await GetUserWithFirstName(userId);
            if (user == null)
            {
                return BadRequest("User Not Found");
            }

            // Retrieve and return the detailed list of games associated with the user
            var detailedGames = await GetUserDetailedGames(userId);
            return Ok(detailedGames);
        }

        [HttpPost("addGame")]
        public async Task<IActionResult> AddGames(int userId, GameToAdd gameToAdd)
        {
            // Check if user exists
            var user = await GetUserWithFirstName(userId);
            if (user == null)
            {
                return BadRequest("User Not Found");
            }

            // Add new game to the database and receive the new game ID
            int newGameId = await CreateGame(gameToAdd, userId);
            if (newGameId == 0)
            {
                return BadRequest("Game not created");
            }

            // Calculate the game code, update it, and retrieve game details
            int gameCode = newGameId + 100;
            bool isUpdateSuccessful = await UpdateGameCode(newGameId, gameCode);
            if (!isUpdateSuccessful)
            {
                return BadRequest("Game code not updated");
            }

            var newGame = await GetGameById(newGameId);
            return Ok(newGame);
        }
        
        [HttpGet("getGame/{gameCode}")]
        public async Task<IActionResult> GetGame(int userId, int gameCode)
        {
            object param = new
            {
                UserId = userId
            };
            string userQuery = "SELECT FirstName FROM Users WHERE ID = @UserId";
            var userRecords = await _db.GetRecordsAsync<UserWithGames>(userQuery, param);
            UserWithGames user = userRecords.FirstOrDefault();
            //בדיקה שיש משתמש כזה במחולל שלנו
            if (user != null)
            {
                object getParam = new
                {
                    codeFromUser = gameCode,
                    UserId = userId
                };

                //Get course details for the codeFromUser if it exists and is published
                string getGameDetailsQuery = "select * from games g where GameCode = @codeFromUser and UserID=@UserId";
                var getGameDetailsRecords = await _db.GetRecordsAsync<GameDetails>(getGameDetailsQuery, getParam);
                GameDetails gameDetails = getGameDetailsRecords.FirstOrDefault();

                //If no game found, return bad request
                if (gameDetails == null)
                {
                    Response.Headers.Add("X-Error", "BadRequest");
                    return BadRequest(new { message = "No game found for game code: " + gameCode });
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
            return BadRequest("User Not Found");

        }

        [HttpPost("addQuestion/{gameCode}")]
        public async Task<IActionResult> AddQuestion(int userId, int gameCode, QuestionToAdd questionToAdd)
        {
            // Verify User
            object userParam = new { UserId = userId };
            string userQuery = "SELECT ID FROM Users WHERE ID = @UserId";
            var userRecords = await _db.GetRecordsAsync<UserWithGames>(userQuery, userParam);
            if (userRecords.FirstOrDefault() == null)
            {
                return BadRequest("User Not Found");
            }

            // Verify Game
            object gameParam = new { GameCode = gameCode };
            string gameQuery = "SELECT ID FROM Games WHERE GameCode = @GameCode";
            var gameRecords = await _db.GetRecordsAsync<UserWithGames>(gameQuery, gameParam);
            if (gameRecords.FirstOrDefault() == null)
            {
                return BadRequest("Game Not Found");
            }
            int gameID = gameRecords.FirstOrDefault().ID; // Assuming you have ID field in your Games table

            // Insert Question and return ID
            object questionParam = new
            {
                GameID = gameID,
                HasImage = questionToAdd.HasImage,
                QuestionDescription = questionToAdd.QuestionDescription,
                QuestionImage = questionToAdd.QuestionImage,
                StageID = questionToAdd.StageID
            };
            string insertQuestionQuery = @"
        INSERT INTO Questions (GameID, HasImage, QuestionDescription, QuestionImage, StageID) 
        VALUES (@GameID, @HasImage, @QuestionDescription, @QuestionImage, @StageID);
        ";
            int questionId = await _db.InsertReturnIdAsync(insertQuestionQuery, questionParam);
            if (questionId > 0)
            {
                return Ok(questionId); // Return the ID of the newly added question
            }
            return BadRequest("Question not added");
        }

        [HttpPost("addAnswers/{questionId}")]
        public async Task<IActionResult> AddAnswers(int userId, int questionId, List<GameAnswers> answers)
        {
            // Verify User
            object userParam = new { UserId = userId };
            string userQuery = "SELECT ID FROM Users WHERE ID = @UserId";
            var userRecords = await _db.GetRecordsAsync<UserWithGames>(userQuery, userParam);
            if (userRecords.FirstOrDefault() == null)
            {
                return BadRequest("User Not Found");
            }

            foreach (GameAnswers answer in answers)
            {
                object answerParam = new
                {
                    AnswerDescription = answer.AnswerDescription,
                    IsCorrect = answer.IsCorrect,
                    HasImage = false, // Assuming no image for the answer
                    AnswerImage = "empty", // Default value if no image
                    QuestionID = questionId
                };
                string insertAnswerQuery = "INSERT INTO Answers (AnswerDescription, IsCorrect, HasImage, AnswerImage, QuestionID) " +
                                           "VALUES (@AnswerDescription, @IsCorrect, @HasImage, @AnswerImage, @QuestionID)";
                int isAnswerAdded = await _db.SaveDataAsync(insertAnswerQuery, answerParam);
            }

            return Ok("Answers added successfully");
        }

        [HttpDelete("deleteGame/{deleteGameCode}")]
        public async Task<IActionResult> DeleteGame(int userId, int deleteGameCode)
        {
            Console.WriteLine("deleteGameCode");
           
                    object param = new
                    {
                        UserId = userId
                    };
                    string userQuery = "SELECT FirstName FROM Users WHERE ID = @UserId";
                    var userRecords = await _db.GetRecordsAsync<UserWithGames>(userQuery, param);
                    UserWithGames user = userRecords.FirstOrDefault();
                    if (user != null)
                    {
                        object param2 = new
                        {
                            GameCodeId = deleteGameCode
                        };
                        string gameQuery = "SELECT GameName FROM Games WHERE GameCode = @GameCodeId";
                        var gameRecords = await _db.GetRecordsAsync<UserWithGames>(gameQuery, param2);
                        UserWithGames game = gameRecords.FirstOrDefault();
                        if (game != null)
                        {
                            object param3 = new
                            {
                                GameCodeId = deleteGameCode
                            };
                            string deleteGameQuery = "DELETE FROM Games WHERE GameCode = @GameCodeId";
                            int isDelete = await _db.SaveDataAsync(deleteGameQuery, param3);
                            if (isDelete > 0)
                            {
                                return Ok("Game deleted");
                            }


                            return BadRequest("Game not deleted");
                        }
                        return BadRequest("Game Not Found");
                    }
                    return BadRequest("User Not Found");
        }

        [HttpDelete("deleteQuestion/{questionId}")]
        public async Task<IActionResult> DeleteQuestion(int userId, int questionId)
        {
            object param = new
            {
                UserId = userId
            };
            string userQuery = "SELECT FirstName FROM Users WHERE ID = @UserId";
            var userRecords = await _db.GetRecordsAsync<UserWithGames>(userQuery, param);
            UserWithGames user = userRecords.FirstOrDefault();
            if (user != null)
            {
                object param2 = new
                {
                    QuestionId = questionId
                };
                string questionQuery = "SELECT QuestionDescription FROM Questions WHERE ID = @QuestionId";
                var questionRecords = await _db.GetRecordsAsync<UserWithGames>(questionQuery, param2);
                UserWithGames question = questionRecords.FirstOrDefault();
                if (question != null)
                {
                    object param3 = new
                    {
                        QuestionId = questionId
                    };
                    string deleteQuestionQuery = "DELETE FROM Questions WHERE ID = @QuestionId";
                    int isDelete = await _db.SaveDataAsync(deleteQuestionQuery, param3);
                    if (isDelete > 0)
                    {
                        return Ok("Question deleted");
                    }
                    return BadRequest("Question not deleted");
                }
                return BadRequest("Question Not Found");
            }
            return BadRequest("User Not Found");
        }

        [HttpGet("getActiveStages/{gameCode}")]
        public async Task<IActionResult> GetActiveStages(int userId, int gameCode)
        {
            object param = new
            {
                UserId = userId
            };
            string userQuery = "SELECT FirstName FROM Users WHERE ID = @UserId";
            var userRecords = await _db.GetRecordsAsync<UserWithGames>(userQuery, param);
            UserWithGames user = userRecords.FirstOrDefault();
            if (user != null)
            {
                object param2 = new
                {
                    GameCode = gameCode
                };
                string gameQuery = "SELECT GameName FROM Games WHERE GameCode = @GameCode";
                var gameRecords = await _db.GetRecordsAsync<UserWithGames>(gameQuery, param2);
                UserWithGames game = gameRecords.FirstOrDefault();
                if (game != null)
                {
                    object param3 = new
                    {
                        GameCode = gameCode
                    };
                    string getActiveStagesQuery = "SELECT DISTINCT StageID FROM Questions WHERE GameID = (SELECT ID FROM Games WHERE GameCode = @GameCode)";
                    var activeStages = await _db.GetRecordsAsync<int>(getActiveStagesQuery, param3);
                    return Ok(activeStages.FirstOrDefault());
                }
                return BadRequest("Game Not Found");
            }
            return BadRequest("User Not Found");
        }



        //NOTE: Didn't work on that yet 





        [HttpPut("updateGame/{updateGameCode}")]
        public async Task<IActionResult> UpdateGame(int userId, int updateGameCode, GameToUpdate gameToUpdate)
        {
            int? sessionId = HttpContext.Session.GetInt32("userId");
            if (sessionId != null)
            {
                if (userId == sessionId)
                {
                    object param = new
                    {
                        UserId = userId
                    };
                    string userQuery = "SELECT FirstName FROM Users WHERE ID = @UserId";
                    var userRecords = await _db.GetRecordsAsync<UserWithGames>(userQuery, param);
                    UserWithGames user = userRecords.FirstOrDefault();
                    if (user != null)
                    {
                        object param2 = new
                        {
                            GameCode = updateGameCode
                        };
                        string gameQuery = "SELECT GameName FROM Games WHERE GameCode = @GameCode";
                        var gameRecords = await _db.GetRecordsAsync<UserWithGames>(gameQuery, param2);
                        UserWithGames game = gameRecords.FirstOrDefault();
                        if (game != null)
                        {
                            object param3 = new
                            {
                                GameCode = updateGameCode,
                                GameName = gameToUpdate.GameName,
                                GameQuestion = gameToUpdate.QuestionDescription,
                                GameCorrectCategory = gameToUpdate.QuestionCorrectCategory,
                                GameWrongCategory = gameToUpdate.QuestionWrongCategory,
                                TheGameEndMessage = gameToUpdate.GameEndMessage,
                                QuestionHasImage = gameToUpdate.QuestionHasImage,
                                QuestionImageText = gameToUpdate.QuestionImageText
                                //ADD MORE PARAMS TO UPDATE
                            };
                            string updateGameQuery =
                                "UPDATE Games SET GameName = @GameName, " +
                                "QuestionDescription = @GameQuestion, " +
                                "QuestionCorrectCategory = @GameCorrectCategory, " +
                                "QuestionWrongCategory = @GameWrongCategory, " +
                                "GameEndMessage = @TheGameEndMessage, " +
                                "QuestionHasImage = @QuestionHasImage," +
                                "QuestionImageText = @QuestionImageText " +
                                "WHERE GameCode = @GameCode";
                            //CHANGE QUERY ACCORDINGLY TO ADD MORE PARAMS TO UPDATE
                            int isGameUpdate = await _db.SaveDataAsync(updateGameQuery, param3);

                            UpdateAnswers(updateGameCode, gameToUpdate);

                            AddAnswers(gameToUpdate.ID, gameToUpdate);

                            DeleteAnswers(gameToUpdate.ID, gameToUpdate);


                            if (isGameUpdate > 0)
                            {
                                return Ok("Game updated");
                            }

                            return BadRequest("Game not updated");
                        }
                        return BadRequest("Game Not Found");
                    }
                    return BadRequest("User Not Found");
                }
                return BadRequest("User Not Logged In");
            }
            return BadRequest("No Session");
        }
       
        [HttpPost("publishGame")]
        public async Task<IActionResult> publishGame(int userId, PublishGame game)
        {
            int? sessionId = HttpContext.Session.GetInt32("userId");
            if (sessionId != null)
            {
                if (userId == sessionId)
                {
                    object param = new
                    {
                        UserId = userId,
                        gameID = game.ID
                    };
                    Console.WriteLine(param);

                    string checkQuery = "SELECT GameName FROM Games WHERE UserId = @UserId and ID=@gameID";
                    var checkRecords = await _db.GetRecordsAsync<string>(checkQuery, param);
                    string gameName = checkRecords.FirstOrDefault();
                    if (gameName != null)
                    {

                        if (game.IsPublished == true)

                        {
                            await CanPublishFunc(game.ID);

                            object canPublishParam = new
                            {
                                gameID = game.ID
                            };
                            string canPublishQuery = "SELECT CanPublish FROM Games WHERE ID=@gameID";
                            var canPublishRecords = await _db.GetRecordsAsync<bool>(canPublishQuery, canPublishParam);
                            bool canPublish = canPublishRecords.FirstOrDefault();
                            if (canPublish == false)
                            {
                                return BadRequest("This game cannot be published");
                            }
                        }
                        string updateQuery = "UPDATE Games SET IsPublished=@IsPublished WHERE ID=@ID";
                        int isUpdate = await _db.SaveDataAsync(updateQuery, game);
                        if (isUpdate > 0)
                        {
                            return Ok();
                        }
                        return BadRequest("Update Failed");
                    }
                    return BadRequest("It's Not Your Game");
                }
                return BadRequest("User Not Logged In");
            }
            return BadRequest("No Session");
        }

        [HttpGet("canPublish/{gameId}")]
        public async Task<IActionResult> CanPublish(int userId, int gameId)
        {
            int? sessionId = HttpContext.Session.GetInt32("userId");
            if (sessionId != null)
            {
                if (userId == sessionId)
                {
                    object param = new
                    {
                        UserId = userId,
                        gameID = gameId
                    };

                    string checkQuery = "SELECT GameName FROM Games WHERE UserId = @UserId and ID=@gameID";
                    var checkRecords = await _db.GetRecordsAsync<string>(checkQuery, param);
                    string gameName = checkRecords.FirstOrDefault();
                    if (gameName != null)
                    {
                        CanPublishFunc(gameId);
                        return Ok();
                    }
                    return BadRequest("It's Not Your Game");
                }
                return BadRequest("User Not Logged In");
            }
            return BadRequest("No Session");
        }

        private async Task CanPublishFunc(int gameId)
        {
            int minQuestions = 18;
            bool isPublished = false;
            bool canPublish = false;
            object param = new
            {
                ID = gameId
            };
            string queryQuestionCount = "SELECT Count(ID) from Items WHERE GameID = @ID";
            var recordQuestionCount = await _db.GetRecordsAsync<int>(queryQuestionCount, param);
            int numberOfQuestions = recordQuestionCount.FirstOrDefault();

            // Fetch the count of correct answers
            string queryCorrectCount = "SELECT Count(ID) from Items WHERE GameID = @ID AND IsCorrect = 1"; // Assuming IsCorrect is a boolean or bit field
            int correctAnswersCount = (await _db.GetRecordsAsync<int>(queryCorrectCount, param)).FirstOrDefault();

            // Fetch the count of wrong answers
            int wrongAnswersCount = numberOfQuestions - correctAnswersCount;

            // Check the 2:1 rate condition
            bool isRateValid = (correctAnswersCount / 2) == wrongAnswersCount;

            if (numberOfQuestions >= minQuestions && isRateValid)
            {
                canPublish = true;
            }
            if (canPublish == true)
            {
                string updateQuery = "UPDATE Games SET CanPublish = true WHERE ID = @ID";
                int isUpdate = await _db.SaveDataAsync(updateQuery, param);
                Console.WriteLine($"The update of game: {gameId} was completed successfully {isUpdate}");
            }
            else
            {
                string updateQuery = "UPDATE Games SET IsPublished = false, CanPublish = false WHERE ID = @ID";
                int isUpdate = await _db.SaveDataAsync(updateQuery, param);
                Console.WriteLine($"The update of game: {gameId} was completed successfully {isUpdate}");
            }
        }

        private async Task UpdateAnswers(int gameCode, GameToUpdate gameToUpdate)
        {
            //Update existing answers
            foreach (GameAnswers item in gameToUpdate.Answers)
            {
                object param2 = new
                {
                    //id = item.ID,
                    answerDescription = item.AnswerDescription,
                    isCorrect = item.IsCorrect,
                    hasImage = item.HasImage,
                    imageText = item.AnswerImage
                };
                string updateAnswerQuery = "UPDATE Items SET AnswerDescription = @answerDescription, " +
                    "IsCorrect = @isCorrect, " +
                    "HasImage = @hasImage, " +
                    "AnswerImageText = @imageText " +
                    "WHERE ID = @id";
                int isAnswersUpdate = await _db.SaveDataAsync(updateAnswerQuery, param2);

            }
        }

        private async Task AddAnswers(int gameId, GameToUpdate gameToUpdate)
        {
            ////check if there are new answers
            //foreach (GameAnswers item in gameToUpdate.Answers)
            //{
            //    if (item.ID == 0)
            //    {
            //        //add new answers
            //        object param1 = new
            //        {
            //            GameID = gameId,
            //            AnswerDescription = item.AnswerDescription,
            //            IsCorrect = item.IsCorrect,
            //            HasImage = item.HasImage,
            //            AnswerImageText = item.AnswerImage
            //        };
            //        string insertAnswerQuery = "INSERT INTO Items (GameID, AnswerDescription, IsCorrect, HasImage, AnswerImageText) " +
            //                                        "VALUES (@GameID, @AnswerDescription, @IsCorrect, @HasImage, @AnswerImageText)";
            //        int addAnswer = await _db.SaveDataAsync(insertAnswerQuery, param1);
            //    }
            //}

        }

        private async Task DeleteAnswers(int gameId, GameToUpdate gameToUpdate)
        {
            //foreach (GameAnswers item in gameToUpdate.AnswersToDelete)
            //{
            //    if (item.ID != 0)
            //    {
            //        //delete answers
            //        object param1 = new
            //        {
            //            ID = item.ID
            //        };
            //        string deleteAnswerQuery = "DELETE FROM Items WHERE ID = @ID";
            //        int deleteAnswer = await _db.SaveDataAsync(deleteAnswerQuery, param1);
            //    }
            //}
        }

        [HttpGet("GetImageFilesForDeletion/{gameCode}")]
        public async Task<IActionResult> GetImageFilesForDeletion(int userId, int gameCode)
        {
            int? sessionId = HttpContext.Session.GetInt32("userId");
            if (sessionId != null)
            {
                if (userId == sessionId)
                {

                    try
                    {
                        List<string> deleteImages = new List<string>();

                        // Retrieve image file names from the Games table where they are not 'empty' or '-'
                        string gameImagesQuery = "SELECT QuestionImageText FROM Games WHERE GameCode = @GameCode AND QuestionImageText <> 'empty' AND QuestionImageText <> '-'";
                        var gameImages = await _db.GetRecordsAsync<string>(gameImagesQuery, new { GameCode = gameCode });
                        deleteImages.AddRange(gameImages.Where(image => !string.IsNullOrEmpty(image)));

                        // Retrieve image file names from the Items table where they are not 'empty' or '-'
                        string itemImagesQuery = "SELECT AnswerImageText FROM Items JOIN Games ON Items.GameID = Games.ID WHERE Games.GameCode = @GameCode AND Items.AnswerImageText <> 'empty' AND Items.AnswerImageText <> '-'";
                        var itemImages = await _db.GetRecordsAsync<string>(itemImagesQuery, new { GameCode = gameCode });
                        deleteImages.AddRange(itemImages.Where(image => !string.IsNullOrEmpty(image)));

                        if (!deleteImages.Any())
                        {
                            return NotFound("No image files found for deletion.");
                        }

                        // Return the list of image files to the client
                        return Ok(deleteImages);
                    }
                    catch (Exception ex)
                    {
                        // Log the exception here
                        return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving image files for deletion.");
                    }
                    return BadRequest("It's Not Your Game");
                }
                return BadRequest("User Not Logged In");
            }
            return BadRequest("No Session");


        }

    }
}
