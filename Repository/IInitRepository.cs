namespace Repository;

public interface IInitRepository
{
    Task Build(CancellationToken cancellationToken = default);
}
