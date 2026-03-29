namespace API;

public interface IFactRepository
{
    Task AddFactAsync(string fact, CancellationToken cancellationToken);
}