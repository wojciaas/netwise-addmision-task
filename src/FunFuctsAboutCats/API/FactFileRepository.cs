using System.ComponentModel.DataAnnotations;
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

        await File.AppendAllTextAsync(filePath, fact + Environment.NewLine, cancellationToken);
    }
}

public class FactFileRepositoryOptions
{
    public const string SectionName = "FactFileRepository";

    [Required(ErrorMessage = "File path is required.")]
    [DataType(DataType.Text)]
    public required string FilePath { get; init; }
}