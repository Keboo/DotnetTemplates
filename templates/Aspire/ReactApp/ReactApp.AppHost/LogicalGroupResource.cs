namespace ReactApp.AppHost;

/// <summary>
/// A logical grouping resource that doesn't run anything but serves as a parent container.
/// Aggregates endpoints and state from all child resources for easy access in the dashboard.
/// </summary>
public class LogicalGroupResource(string name) : Resource(name), IResourceWithEndpoints
{
    public List<IResource> Children { get; } = [];
}
