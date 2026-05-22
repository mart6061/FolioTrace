namespace Repository;

public interface ISeedRepository
{
    Task Build(CancellationToken cancellationToken = default);

    Task Build(IProgress<BuildProgress>? progress, CancellationToken cancellationToken = default);
}

