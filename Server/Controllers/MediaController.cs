using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SenseiAdventures.Server.Data;

namespace SenseiAdventures.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MediaController : ControllerBase
    {
        private readonly FilesManage _filesmanage;

        public MediaController(FilesManage filesmanage)
        {
            _filesmanage = filesmanage;
        }

        //Upload file API
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromBody] string imageBase64)
        {
            string filName = await _filesmanage.SaveFile(imageBase64, "png", "uploadedFiles");
            return Ok(filName);
        }

        //Delete file API
        [HttpPost("deleteImages")]
        public async Task<IActionResult> DeleteImages([FromBody] List<string> images)
        {
            var countFalseTry = 0;
            foreach (string img in images)
            {
                if (_filesmanage.DeleteFile(img, "") == false)
                {
                    countFalseTry++;
                }
            }
            if (countFalseTry > 0)
            {
                return BadRequest("Some files not deleted");
            }
            return Ok("Deleted");
        }
    }
}
