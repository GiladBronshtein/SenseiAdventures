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
                GameHasImage = gameToAdd.GameHasImage,
                GameImage = gameToAdd.GameImage, // Handle image information
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

        private async Task<bool> UpdateAnswer(GameAnswers answer)
        {
            string answerImage = string.IsNullOrEmpty(answer.AnswerImage) ? "empty" : answer.AnswerImage;
            bool hasImage = answerImage != "empty";

            var answerParam = new
            {
                AnswerDescription = answer.AnswerDescription,
                IsCorrect = answer.IsCorrect,
                HasImage = hasImage,
                AnswerImage = answerImage,
                AnswerId = answer.ID
            };

            string updateAnswerQuery = @"
            UPDATE Answers SET 
                AnswerDescription = @AnswerDescription, 
                IsCorrect = @IsCorrect, 
                HasImage = @HasImage, 
                AnswerImage = @AnswerImage 
            WHERE ID = @AnswerId";

            var updateCount = await _db.SaveDataAsync(updateAnswerQuery, answerParam);
            return updateCount > 0;
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

        private async Task UpdateGameSettings(int gameId, GameSettingsToUpdate gameToUpdate)
        {
            object param = new
            {
                ID = gameId,
                GameName = gameToUpdate.GameName,
                EndingMessage = gameToUpdate.EndingMessage,
                GameHasImage = gameToUpdate.GameHasImage,
                GameImage = gameToUpdate.GameImage
            };
            string updateGameQuery = "UPDATE Games SET GameName = @GameName, " +
                "EndingMessage = @EndingMessage, " +
                "GameHasImage = @GameHasImage, " +
                "GameImage = @GameImage " +
                "WHERE ID = @ID";
            int isGameUpdate = await _db.SaveDataAsync(updateGameQuery, param);
        }




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
            // Return the detailed list of games
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
            // Update the game code
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
            // Check if the user exists
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
                //get questions of game
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
                StageID = questionToAdd.StageID,
                isActive = 1
            };
            string insertQuestionQuery = @"INSERT INTO Questions (GameID, HasImage, QuestionDescription, QuestionImage, StageID, isActive) 
                                    VALUES (@GameID, @HasImage, @QuestionDescription, @QuestionImage, @StageID, @isActive);";
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
                    HasImage = answer.HasImage,
                    AnswerImage = answer.AnswerImage,
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
                    string getActiveStagesQuery = "SELECT COUNT(DISTINCT StageID) FROM Questions WHERE GameID = (SELECT ID FROM Games WHERE GameCode = @GameCode) and isActive = 1";
                    var activeStages = await _db.GetRecordsAsync<int>(getActiveStagesQuery, param3);
                    return Ok(activeStages.FirstOrDefault());
                }
                return BadRequest("Game Not Found");
            }
            return BadRequest("User Not Found");
        }

        [HttpGet("getActiveQuestions/{gameCode}")]
        public async Task<IActionResult> GetActiveQuestions(int userId, int gameCode)
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
                    string getActiveQuestionsQuery = "SELECT Distinct ID FROM Questions WHERE GameID = (SELECT ID FROM Games WHERE GameCode = @GameCode)  and isActive = 1";
                    var activeQuestions = await _db.GetRecordsAsync<GameQuestions>(getActiveQuestionsQuery, param3);
                    return Ok(activeQuestions.ToList().Count);
                }
                return BadRequest("Game Not Found");
            }
            return BadRequest("User Not Found");
        }

        [HttpPut("updateGameSettings/{gameCode}")]
        public async Task<IActionResult> UpdateGameSettings(int userId, int gameCode, GameSettingsToUpdate gameSettings)
        {
            object param = new
            {
                UserId = userId
            };
            string userQuery = "SELECT FirstName,ID FROM Users WHERE ID = @UserId";
            var userRecords = await _db.GetRecordsAsync<UserWithGames>(userQuery, param);
            UserWithGames user = userRecords.FirstOrDefault();
            if (user != null)
            {
                object param2 = new
                {
                    GameCode = gameCode
                };
                string gameQuery = "SELECT GameName,EndingMessage,GameCode,ID,GameImage,GameHasImage " +
                    "FROM Games WHERE GameCode = @GameCode";
                var gameRecords = await _db.GetRecordsAsync<GameSettingsToUpdate>(gameQuery, param2);
                GameSettingsToUpdate game = gameRecords.FirstOrDefault();
                if (game != null)
                {
                    await UpdateGameSettings(game.ID, gameSettings);
                    return Ok("Game settings updated");
                }
                return BadRequest("Game Not Found");
            }
            return BadRequest("User Not Found");
        }

        [HttpGet("getQuestion/{questionId}")]
        public async Task<IActionResult> GetQuestion(int userId, int questionId)
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
                    string getQuestionQuery = "SELECT * FROM Questions WHERE ID = @QuestionId";
                    var questionDetails = await _db.GetRecordsAsync<GameQuestions>(getQuestionQuery, param3);
                    return Ok(questionDetails.FirstOrDefault());
                }
                return BadRequest("Question Not Found");
            }
            return BadRequest("User Not Found");
        }

        [HttpGet("getAnswers/{questionId}")]
        public async Task<IActionResult> GetAnswers(int userId, int questionId)
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
                    string getAnswersQuery = "SELECT * FROM Answers WHERE QuestionID = @QuestionId";
                    var answers = await _db.GetRecordsAsync<GameAnswers>(getAnswersQuery, param3);
                    return Ok(answers.ToList());
                }
                return BadRequest("Question Not Found");
            }
            return BadRequest("User Not Found");
        }

        [HttpPut("updateQuestion/{questionId}")]
        public async Task<IActionResult> UpdateQuestion(int userId, int questionId, QuestionToAdd questionToUpdate)
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
                        QuestionId = questionId,
                        QuestionDescription = questionToUpdate.QuestionDescription,
                        HasImage = questionToUpdate.HasImage,
                        QuestionImage = questionToUpdate.QuestionImage,
                        StageID = questionToUpdate.StageID
                    };
                    string updateQuestionQuery = "UPDATE Questions SET " +
                        "QuestionDescription = @QuestionDescription, " +
                        "HasImage = @HasImage, " +
                        "QuestionImage = @QuestionImage, " +
                        "StageID = @StageID " +
                        "WHERE ID = @QuestionId";
                    int isQuestionUpdate = await _db.SaveDataAsync(updateQuestionQuery, param3);
                    if (isQuestionUpdate > 0)
                    {
                        return Ok("Question updated");
                    }
                    return BadRequest("Question not updated");
                }
                return BadRequest("Question Not Found");
            }
            return BadRequest("User Not Found");
        }

        [HttpPut("updateAnswers/{answerId}")]
        public async Task<IActionResult> UpdateAnswers(int userId, int answerId, GameAnswers answerToUpdate)
        {
            // Verify User
            object userParam = new { UserId = userId };
            string userQuery = "SELECT ID FROM Users WHERE ID = @UserId";
            var userRecords = await _db.GetRecordsAsync<UserWithGames>(userQuery, userParam);
            if (userRecords.FirstOrDefault() == null)
            {
                return BadRequest("User Not Found");
            }

            // Retrieve the existing answer to check existence
            string existingAnswerQuery = "SELECT * FROM Answers WHERE ID = @AnswerId";
            var existingAnswer = await _db.GetRecordsAsync<GameAnswers>(existingAnswerQuery, new { AnswerId = answerId });
            if (existingAnswer.FirstOrDefault() == null)
            {
                return BadRequest("Answer Not Found");
            }

            // Update answer details
            object param = new
            {
                AnswerDescription = answerToUpdate.AnswerDescription,
                IsCorrect = answerToUpdate.IsCorrect,
                HasImage = answerToUpdate.HasImage,  // Make sure this is correctly handled
                AnswerImage = answerToUpdate.AnswerImage,
                AnswerId = answerId
            };

            string updateAnswerQuery = @"
        UPDATE Answers SET 
        AnswerDescription = @AnswerDescription, 
        IsCorrect = @IsCorrect, 
        HasImage = @HasImage, 
        AnswerImage = @AnswerImage 
        WHERE ID = @AnswerId";

            int updateCount = await _db.SaveDataAsync(updateAnswerQuery, param);
            if (updateCount > 0)
            {
                return Ok("Answer updated successfully");
            }
            else
            {
                return BadRequest("Failed to update answer");
            }
        }

        [HttpPut("publishGame/{gameCode}")]
        public async Task<IActionResult> PublishGame(int userId, int gameCode)
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
                    string getActiveStagesQuery = "SELECT COUNT(DISTINCT StageID) FROM Questions WHERE GameID = (SELECT ID FROM Games WHERE GameCode = @GameCode)  AND isActive = 1";
                    var activeStages = await _db.GetRecordsAsync<int>(getActiveStagesQuery, param3);
                    if (activeStages.FirstOrDefault() >= 2)
                    {
                        object param4 = new
                        {
                            GameCode = gameCode
                        };
                        string getActiveQuestionsQuery = "SELECT Distinct ID FROM Questions WHERE GameID = (SELECT ID FROM Games WHERE GameCode = @GameCode)  AND isActive = 1";
                        var activeQuestions = await _db.GetRecordsAsync<GameQuestions>(getActiveQuestionsQuery, param4);
                        if (activeQuestions.ToList().Count >= 20)
                        {
                            object param5 = new
                            {
                                GameCode = gameCode
                            };
                            string publishGameQuery = "UPDATE Games SET IsPublished = 1 WHERE GameCode = @GameCode";
                            int isPublished = await _db.SaveDataAsync(publishGameQuery, param5);
                            if (isPublished > 0)
                            {
                                return Ok("Game published");
                            }
                            return BadRequest("Game not published");
                        }
                        return BadRequest("Game not ready to be published");
                    }
                    return BadRequest("Game not ready to be published");
                }
                return BadRequest("Game Not Found");
            }
            return BadRequest("User Not Found");
        }

        [HttpDelete("deleteAnswer/{answerId}")]
        public async Task<IActionResult> DeleteAnswer(int userId, int answerId)
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
                    AnswerId = answerId
                };
                string answerQuery = "SELECT AnswerDescription FROM Answers WHERE ID = @AnswerId";
                var answerRecords = await _db.GetRecordsAsync<UserWithGames>(answerQuery, param2);
                UserWithGames answer = answerRecords.FirstOrDefault();
                if (answer != null)
                {
                    object param3 = new
                    {
                        AnswerId = answerId
                    };
                    string deleteAnswerQuery = "DELETE FROM Answers WHERE ID = @AnswerId";
                    int isDelete = await _db.SaveDataAsync(deleteAnswerQuery, param3);
                    if (isDelete > 0)
                    {
                        return Ok("Answer deleted");
                    }
                    return BadRequest("Answer not deleted");
                }
                return BadRequest("Answer Not Found");
            }
            return BadRequest("User Not Found");
        }

        [HttpPut("unpublishGame/{gameCode}")]
        public async Task<IActionResult> UnpublishGame(int userId, int gameCode)
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
                    string unpublishGameQuery = "UPDATE Games SET IsPublished = 0 WHERE GameCode = @GameCode";
                    int isUnpublished = await _db.SaveDataAsync(unpublishGameQuery, param3);
                    if (isUnpublished > 0)
                    {
                        return Ok("Game unpublished");
                    }
                    return BadRequest("Game not unpublished");
                }
                return BadRequest("Game Not Found");
            }
            return BadRequest("User Not Found");
        }

        [HttpPut("canPublishGame/{gameCode}")]
        public async Task<IActionResult> CanPublishGame(int userId, int gameCode)
        {
            // Check if the user exists
            string userQuery = "SELECT FirstName FROM Users WHERE ID = @UserId";
            var userRecords = await _db.GetRecordsAsync<UserWithGames>(userQuery, new { UserId = userId });
            UserWithGames user = userRecords.FirstOrDefault();
            if (user == null)
            {
                return BadRequest("User Not Found");
            }

            // Check if the game exists
            string gameQuery = "SELECT GameName FROM Games WHERE GameCode = @GameCode";
            var gameRecords = await _db.GetRecordsAsync<UserWithGames>(gameQuery, new { GameCode = gameCode });
            UserWithGames game = gameRecords.FirstOrDefault();
            if (game == null)
            {
                return BadRequest("Game Not Found");
            }

            // Get the count of active stages
            string getActiveStagesQuery = "SELECT COUNT(DISTINCT StageID) FROM Questions WHERE GameID = (SELECT ID FROM Games WHERE GameCode = @GameCode) AND isActive = 1";
            var activeStages = await _db.GetRecordsAsync<int>(getActiveStagesQuery, new { GameCode = gameCode });

            // Get the count of active questions
            string getActiveQuestionsQuery = "SELECT COUNT(DISTINCT ID) FROM Questions WHERE GameID = (SELECT ID FROM Games WHERE GameCode = @GameCode)  AND isActive = 1";
            var activeQuestions = await _db.GetRecordsAsync<int>(getActiveQuestionsQuery, new { GameCode = gameCode });

            bool canPublish = activeStages.FirstOrDefault() >= 2 && activeQuestions.FirstOrDefault() >= 20;

            // Update the CanPublish status
            string canPublishGameQuery = "UPDATE Games SET CanPublish = @CanPublish WHERE GameCode = @GameCode";
            int isCanPublished = await _db.SaveDataAsync(canPublishGameQuery, new { CanPublish = canPublish, GameCode = gameCode });

            if (isCanPublished > 0)
            {
                return Ok(canPublish);
            }

            return BadRequest("Failed to update game publish status");
        }

        // method to get stage statistics
        [HttpGet("getStageStatistics/{gameid}")]
        public async Task<IActionResult> GetStageStatistics(int userId, int gameid)
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
                    GameId = gameid
                };
                string gameQuery = "SELECT GameName FROM Games WHERE ID = @GameId";
                var gameRecords = await _db.GetRecordsAsync<UserWithGames>(gameQuery, param2);
                UserWithGames game = gameRecords.FirstOrDefault();
                if (game != null)
                {
                    object param3 = new
                    {
                        GameId = gameid
                    };
                    string getStageStatisticsQuery = "SELECT * FROM StatisticsStages WHERE GameID = @GameId";
                    var stageStatistics = await _db.GetRecordsAsync<StatisticsStages>(getStageStatisticsQuery, param3);
                    return Ok(stageStatistics.ToList());
                }
                return BadRequest("Game Not Found");
            }
            return BadRequest("User Not Found");
        }

        // metho to post statistics (will be used by client on VR Game) 
        [HttpPost("postStatistics")]
        public async Task<IActionResult> PostStatistics(int userId, StatisticsStages statistics)
        {
            // Verify User
            object userParam = new { UserId = userId };
            string userQuery = "SELECT ID FROM Users WHERE ID = @UserId";
            var userRecords = await _db.GetRecordsAsync<UserWithGames>(userQuery, userParam);
            if (userRecords.FirstOrDefault() == null)
            {
                return BadRequest("User Not Found");
            }

            // Insert statistics
            object statisticsParam = new
            {
                GameID = statistics.GameID,
                StageID = statistics.StageID,
                Trophy = statistics.Trophy,
                StageGrade = statistics.StageGrade,
                StageTime = statistics.StageTime,
                WrongAnsweredIDs = statistics.WrongAnsweredIDs
                //TimeSpent = statistics.TimeSpent,
                //CorrectAnswers = statistics.CorrectAnswers,
                //WrongAnswers = statistics.WrongAnswers,
                //TotalQuestions = statistics.TotalQuestions,
                //TotalScore = statistics.TotalScore
            };
            string insertStatisticsQuery = @"INSERT INTO StatisticsStages (GameID, StageID, Trophy, StageGrade, StageTime, WrongAnsweredIDs) 
                                    VALUES (@GameID, @StageID, @Trophy, @StageGrade, @StageTime, @WrongAnsweredIDs);";
            int statisticsId = await _db.InsertReturnIdAsync(insertStatisticsQuery, statisticsParam);
            if (statisticsId > 0)
            {
                return Ok(statisticsId); // Return the ID of the newly added statistics
            }
            return BadRequest("Statistics not added");
        }

        // method to make stage inactive - all question related will have isactive = 0
        [HttpPut("makeStageInactive/{gameid}/{stageid}")]
        public async Task<IActionResult> MakeStageInactive(int userId, int gameid, int stageid)
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
                    GameId = gameid
                };
                string gameQuery = "SELECT GameName FROM Games WHERE ID = @GameId";
                var gameRecords = await _db.GetRecordsAsync<UserWithGames>(gameQuery, param2);
                UserWithGames game = gameRecords.FirstOrDefault();
                if (game != null)
                {
                    object param3 = new
                    {
                        GameId = gameid,
                        StageId = stageid
                    };
                    string makeStageInactiveQuery = "UPDATE Questions SET isActive = 0 WHERE GameID = @GameId AND StageID = @StageId";
                    int isInactive = await _db.SaveDataAsync(makeStageInactiveQuery, param3);
                    if (isInactive > 0)
                    {
                        return Ok("Stage made inactive");
                    }
                    return BadRequest("Stage not made inactive");
                }
                return BadRequest("Game Not Found");
            }
            return BadRequest("User Not Found");
        }

        // method to make stage active - all question related will have isactive = 1
        [HttpPut("makeStageActive/{gameid}/{stageid}")]
        public async Task<IActionResult> MakeStageActive(int userId, int gameid, int stageid)
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
                    GameId = gameid
                };
                string gameQuery = "SELECT GameName FROM Games WHERE ID = @GameId";
                var gameRecords = await _db.GetRecordsAsync<UserWithGames>(gameQuery, param2);
                UserWithGames game = gameRecords.FirstOrDefault();
                if (game != null)
                {
                    object param3 = new
                    {
                        GameId = gameid,
                        StageId = stageid
                    };
                    string makeStageActiveQuery = "UPDATE Questions SET isActive = 1 WHERE GameID = @GameId AND StageID = @StageId";
                    int isActive = await _db.SaveDataAsync(makeStageActiveQuery, param3);
                    if (isActive > 0)
                    {
                        return Ok("Stage made active");
                    }
                    return BadRequest("Stage not made active");
                }
                return BadRequest("Game Not Found");
            }
            return BadRequest("User Not Found");
        }
    }
}