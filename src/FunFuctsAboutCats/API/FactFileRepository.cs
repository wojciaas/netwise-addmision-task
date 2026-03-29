using Microsoft.Extensions.Options;

namespace API;

public class FactFileRepository(IOptions<FactFileRepositoryOptions> options) : IFactRepository
{
    public async Task AddFactAsync(string fact, CancellationToken cancellationToken)
    {
        var filePath = options.Value.FilePath;
        
        var directory = Path.GetDirectoryName(filePath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(filePath, fact + Environment.NewLine, cancellationToken);
    }
}

public class FactFileRepositoryOptions
{
    public const string SectionName = "FactFileRepository";

    public required string FilePath
    {
        get;
        init
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("File path cannot be null or whitespace.", nameof(FilePath));
            }
            
            field = value;
        }
    }
}