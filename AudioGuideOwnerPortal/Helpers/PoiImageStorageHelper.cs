namespace AudioGuideOwnerPortal.Helpers;

public static class PoiImageStorageHelper
{
    public static async Task<string> SaveImageAsync(IFormFile? imageFile, string? currentUrl, ImageStorageOptions options)
    {
        if (imageFile == null || imageFile.Length <= 0)
        {
            return (currentUrl ?? string.Empty).Trim();
        }

        Directory.CreateDirectory(options.RootPath);
        var extension = Path.GetExtension(imageFile.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".png";
        }

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(options.RootPath, fileName);
        await using var stream = new FileStream(physicalPath, FileMode.Create);
        await imageFile.CopyToAsync(stream);
        return $"/images/{fileName}";
    }
}
