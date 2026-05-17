using Microsoft.Extensions.Hosting;

namespace Repository;

public sealed class InMemoryEventsRepositoryInitializer(IEventRepository eventRepository) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) =>
        eventRepository.InitializeAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
