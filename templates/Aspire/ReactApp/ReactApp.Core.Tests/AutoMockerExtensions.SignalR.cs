using Microsoft.AspNetCore.SignalR;

namespace Velopack.Testing;

public static partial class AutoMockerExtensions
{
    extension(AutoMocker mocker)
    {
        public void WithSignalR<THub>() where THub : Hub
        {
            mocker.With<IHubContext<THub>, TestableHubContext<THub>>();
        }
    }
}

file class TestableHubContext<THub> : IHubContext<THub>
    where THub : Hub
{
    public IHubClients Clients { get; } = new TestableHubClients();
    public IGroupManager Groups { get; } = new TestableGroupManager();
}

file class TestableHubClients : IHubClients
{
    public IClientProxy All { get; } = new TestableClientProxy();

    public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds)
    {
        return new TestableClientProxy();
    }

    public IClientProxy Client(string connectionId)
    {
        return new TestableClientProxy();
    }

    public IClientProxy Clients(IReadOnlyList<string> connectionIds)
    {
        return new TestableClientProxy();
    }

    public IClientProxy Group(string groupName)
    {
        return new TestableClientProxy();
    }

    public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds)
    {
        return new TestableClientProxy();
    }

    public IClientProxy Groups(IReadOnlyList<string> groupNames)
    {
        return new TestableClientProxy();
    }

    public IClientProxy User(string userId)
    {
        return new TestableClientProxy();
    }

    public IClientProxy Users(IReadOnlyList<string> userIds)
    {
        return new TestableClientProxy();
    }
}

file class TestableClientProxy : IClientProxy
{
    public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

file class TestableGroupManager : IGroupManager
{
    public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
