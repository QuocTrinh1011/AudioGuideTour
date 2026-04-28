namespace AudioGuideOwnerPortal.Helpers;

public sealed class AudioStorageOptions
{
    public AudioStorageOptions(string rootPath)
    {
        RootPath = rootPath;
    }

    public string RootPath { get; }
}
