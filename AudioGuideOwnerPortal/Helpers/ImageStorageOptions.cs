namespace AudioGuideOwnerPortal.Helpers;

public sealed class ImageStorageOptions
{
    public ImageStorageOptions(string rootPath)
    {
        RootPath = rootPath;
    }

    public string RootPath { get; }
}
