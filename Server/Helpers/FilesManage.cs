using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
namespace template.Server.Helpers
{
	public class FilesManage
	{
        private readonly IWebHostEnvironment _env;

        public FilesManage(IWebHostEnvironment env)
        {
            _env = env;
        }

        public void DeleteFile(string fileName, string containerName)
        {
            string folderPath = Path.Combine(_env.WebRootPath, containerName);

            string savingPath = Path.Combine(folderPath, fileName);

            if (File.Exists(savingPath))
            {
                File.Delete(savingPath);
            }
        }

        public async Task<string> SaveFile(string imageBase64, string extension, string containerName)
        {  
            byte[] picture = Convert.FromBase64String(imageBase64);
            using (Image image = Image.Load(picture))
            {

                image.Mutate(x => x
                     .Resize(new ResizeOptions
                     {
                         Mode = ResizeMode.Max,
                         Size = new Size(400, 400)
                     }));

                var fileName = $"{Guid.NewGuid()}.{extension}";
                string folderPath = Path.Combine(_env.WebRootPath, containerName);

                string savingPath = Path.Combine(folderPath, fileName);

                await image.SaveAsync(savingPath); // Automatic encoder selected based on extension.

                return fileName;
            }
        }
    }
}

