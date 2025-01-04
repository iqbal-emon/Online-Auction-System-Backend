namespace angularAuctionBackend.Shared
{
    public class ImageSave
    {
        private readonly IConfiguration _configuration;
        private object ImageField;
        private readonly IWebHostEnvironment _environment;

        public ImageSave(IWebHostEnvironment environment)
        {
            _environment = environment;
        }
        public async Task<string> SaveImageToServer(IFormFile imageFile)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsFolderPath = Path.Combine(_environment.ContentRootPath, "Images", "Uploads");
                if (!Directory.Exists(uploadsFolderPath))
                {
                    Directory.CreateDirectory(uploadsFolderPath);
                }

                var imageName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
                var imagePath = Path.Combine(uploadsFolderPath, imageName);

                using (var stream = System.IO.File.Create(imagePath))
                {
                    await imageFile.CopyToAsync(stream);
                }

                return imagePath; // You can store this path in the database
            }
            else
            {
                return null;
            }
        }
    }
}
