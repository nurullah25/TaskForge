namespace TaskForge.Api.Storage;

// The one interface in the project that exists for a real reason: swapping local disk
// for blob storage later only means writing another implementation of this.
public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string fileExtension, CancellationToken cancellationToken);
    Stream Open(string storedFileName);
    void Delete(string storedFileName);
}

public class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IWebHostEnvironment environment, IConfiguration configuration)
    {
        var configured = configuration["Storage:UploadPath"] ?? "App_Data/uploads";
        _root = Path.IsPathRooted(configured) ? configured : Path.Combine(environment.ContentRootPath, configured);
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream content, string fileExtension, CancellationToken cancellationToken)
    {
        // A generated name: the name the user picked never touches the file system.
        var storedFileName = $"{Guid.NewGuid():N}{fileExtension}";

        await using var file = File.Create(Path.Combine(_root, storedFileName));
        await content.CopyToAsync(file, cancellationToken);

        return storedFileName;
    }

    public Stream Open(string storedFileName) =>
        File.OpenRead(Path.Combine(_root, SafeName(storedFileName)));

    public void Delete(string storedFileName)
    {
        var path = Path.Combine(_root, SafeName(storedFileName));
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static string SafeName(string storedFileName) => Path.GetFileName(storedFileName);
}
