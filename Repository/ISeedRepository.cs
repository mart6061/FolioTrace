namespace Repository;

public interface ISeedRepository
{
    Task Build(CancellationToken cancellationToken = default);
}

