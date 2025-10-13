using System.Reflection;
using System.Runtime.Loader;
using Vibecheck.Utility;

namespace Vibecheck.Inference.Tools;

public sealed class ToolHost : IDisposable
{
    private readonly List<ToolClassAttribute> mToolClasses = [];
    private readonly List<ToolMethodAttribute> mToolMethods = [];

    public IReadOnlyList<ToolClassAttribute> Tools => mToolClasses;

    public void Load()
    {
        using var activity = Tracing.Start();

        mToolClasses.Clear();
        foreach (var assembly in AssemblyLoadContext.Default.Assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                this.AddAttribute(type);
            }
        }
    }

    public IEnumerable<ToolInfo> GetAllToolInfo()
        => mToolClasses.Select(t => new ToolInfo()
        {
            Name = "",
            Description = ""
        });

    private void AddAttribute(Type type)
    {
        using var activity = Tracing.Start();
        activity.AddTag("tool.class", type.Name);

        mToolClasses.AddRange(type.GetCustomAttributes<ToolClassAttribute>(false));
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
        {
            mToolClasses.AddRange(method.GetCustomAttributes<ToolClassAttribute>(false));
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        mToolClasses.Clear();
    }
}
