using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;
using Vibecheck.Utility;

namespace Vibecheck.Inference.Tools;

/// <summary>
/// Provides the ability to dynamically discover and interact with extensible tools.
/// </summary>
public sealed class ToolHost : IDisposable
{
    private static readonly JsonSerializerOptions sOptions = new()
    {
        WriteIndented = true
    };

    private readonly List<ToolClassInfoInternal> mToolClasses = [];
    private readonly Dictionary<(string ClassName, string MethodName), ToolMethodInfoInternal> mToolLookup = [];
    private readonly ToolContext mToolContext;
    private bool mLoaded = false;

    public ToolContext ToolContext => mToolContext;

    public bool Loaded => mLoaded;

    public ToolHost()
    {
        mToolContext = new ToolContext();
    }

    /// <summary>
    /// Dynamically discovers all tools, and initialises the lookup table.
    /// </summary>
    public void Load()
    {
        using var activity = Tracing.Start();

        mToolClasses.Clear();
        mToolLookup.Clear();

        var loadedAssemblies = AssemblyLoadContext.Default.Assemblies
            .ToDictionary(a => a.GetName().Name ?? string.Empty, StringComparer.OrdinalIgnoreCase);

        var baseDir = AppContext.BaseDirectory;
        var dllFiles = Directory.GetFiles(baseDir, "*.dll", SearchOption.TopDirectoryOnly);

        foreach (var dllPath in dllFiles)
        {
            try
            {
                var assemblyName = AssemblyName.GetAssemblyName(dllPath);
                if (!loadedAssemblies.ContainsKey(assemblyName.Name ?? string.Empty))
                {
                    Tracing.WriteLine($"Loading assembly: {dllPath}", LogLevel.INFO);
                    var asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(dllPath);
                    loadedAssemblies[assemblyName.Name ?? string.Empty] = asm;
                }
            }
            catch (BadImageFormatException)
            {
                // Not a .NET assembly — skip
            }
            catch (Exception ex)
            {
                Tracing.WriteLine($"Failed to load assembly '{dllPath}': {ex}", LogLevel.WARNING);
            }
        }

        foreach (var assembly in loadedAssemblies.Values)
        {
            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    this.AddType(type);
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var t in ex.Types.Where(t => t != null))
                {
                    this.AddType(t!);
                }
            }
            catch (Exception ex)
            {
                Tracing.WriteLine($"Error scanning assembly '{assembly.FullName}': {ex}", LogLevel.WARNING);
            }
        }

        foreach (var cls in mToolClasses)
        {
            foreach (var method in cls.Methods)
            {
                var key = (cls.Type.Name, method.Method.Name);
                mToolLookup[key] = method;
            }
        }

        mLoaded = true;
    }

    /// <summary>
    /// Get information about all loaded tools.
    /// </summary>
    public IEnumerable<ToolInfo> GetAllToolInfo()
        => mToolClasses
            .Select(c => new ToolInfo()
            {
                // Only select methods where they are availble, or don't have a predicate
                Methods = [.. c.Methods
                    .Where(m => 
                    {
                        if (m.Attribute.AvailabilityType is not null)
                        {
                            var instance = (IToolAvailability)Activator.CreateInstance(m.Attribute.AvailabilityType);
                            return instance?.IsAvailable(mToolContext) ?? true;
                        }

                        // True by default
                        return true;
                    })
                    .Select(m => new ToolMethodInfo()
                    {
                        Name = m.Method.Name,
                        Description = m.Attribute.Description,
                        Parameters = [.. m.Parameters
                            .Where(p => p.Parameter.ParameterType != typeof(ToolContext))
                            .Select(p => new ToolParameterInfo()
                            {
                                Name = p.Parameter.Name ?? string.Empty,
                                Description = p.Attribute?.Description ?? string.Empty,
                                Type = p.Parameter.ParameterType
                            })]
                    })
                ]
            })
            .Where(t => t.Methods.Any());

    public string GetToolContextJson()
    {
        using var activity = Tracing.Start();
        activity.AddTag("tool.context.format", "json");

        var all = this.GetAllToolInfo();

        // We can't just convert the class
        // I could write a serializer but that's boring
        // Just convert it to a string-safe representation here
        var safe = all.Select(t => new
        {
            Methods = t.Methods.Select(m => new
            {
                m.Name,
                m.Description,
                Parameters = m.Parameters.Select(p => new
                {
                    p.Name,
                    p.Description,
                    Type = p.Type.Name
                })
            })
        });

        return JsonSerializer.Serialize(safe, sOptions);
    }

    /// <summary>
    /// Turn all captured tool info into a message to provide context to an inference source.
    /// </summary>
    public string GenerateToolContextMessage()
    {
        using var activity = Tracing.Start();
        activity.AddTag("tool.context.build", "true");

        var builder = new StringBuilder();

        builder.AppendLine("Available Tools:");
        builder.AppendLine();

        foreach (var tool in this.GetAllToolInfo())
        {
            foreach (var method in tool.Methods)
            {
                builder.AppendLine($"  Method: {method.Name}");
                if (!string.IsNullOrWhiteSpace(method.Description))
                {
                    builder.AppendLine($"    Description: {method.Description}");
                }

                if (method.Parameters.Count > 0)
                {
                    builder.AppendLine("    Parameters:");
                    foreach (var param in method.Parameters)
                    {
                        builder.AppendLine($"      - {param.Name} ({param.Type.Name}): {param.Description}");
                    }
                }
                else
                {
                    builder.AppendLine("    Parameters: none");
                }

                builder.AppendLine();
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    public object? InvokeTool(ToolInvocation toolInvocation)
    {
        using var activity = Tracing.Start();

        ArgumentNullException.ThrowIfNull(toolInvocation);

        var toolName = toolInvocation.Tool;
        if (string.IsNullOrWhiteSpace(toolName))
        {
            throw new ArgumentException("Tool name cannot be null or empty.", nameof(toolInvocation));
        }

        var candidates = mToolLookup
            .Where(kvp => string.Equals(kvp.Key.MethodName, toolName, StringComparison.Ordinal))
            .Select(kvp => kvp.Value)
            .ToList();

        if (candidates.Count == 0)
        {
            throw new InvalidOperationException($"No tool method found with name '{toolName}'.");
        }
        if (candidates.Count > 1)
        {
            // If there are multiple with same method name across classes, prefer exact class match if provided
            if (!string.IsNullOrEmpty(toolInvocation.Parameters?["__class"]?.ToString()))
            {
                var clsName = toolInvocation.Parameters["__class"].ToString()!;
                candidates = [.. candidates.Where(c => c.Method.DeclaringType?.Name == clsName)];
            }
        }

        var methodInfo = candidates.First();

        var paramInfos = methodInfo.Parameters;
        var args = new object?[paramInfos.Count];

        for (var i = 0; i < paramInfos.Count; i++)
        {
            var paramInfo = paramInfos[i];
            var paramName = paramInfo.Parameter.Name ?? $"param{i}";
            if (paramInfo.Parameter.ParameterType == typeof(ToolContext))
            {
                args[i] = mToolContext;
            }
            else if (toolInvocation.Parameters != null && toolInvocation.Parameters.TryGetValue(paramName, out var value))
            {
                args[i] = ConvertArgument(value, paramInfo.Parameter.ParameterType);
            }
            else if (paramInfo.Parameter.HasDefaultValue)
            {
                args[i] = paramInfo.Parameter.DefaultValue;
            }
            else
            {
                throw new ArgumentException($"Missing required parameter '{paramName}' for tool '{toolName}'.");
            }
        }

        object? instance = null;
        if (!methodInfo.Method.IsStatic)
        {
            instance = Activator.CreateInstance(methodInfo.Method.DeclaringType!);
        }

        try
        {
            return methodInfo.Method.Invoke(instance, args);
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException ?? ex;
        }
    }

    /// <summary>
    /// Invoke a discovered tool method dynamically.
    /// </summary>
    /// <param name="className">Name of the class marked with [ToolClass]</param>
    /// <param name="methodName">Name of the method marked with [ToolMethod]</param>
    /// <param name="args">Arguments in string form (will be converted)</param>
    public object? InvokeTool(string className, string methodName, params object?[] args)
    {
        using var activity = Tracing.Start();
        activity.AddTag("tool.invoke.class", className);
        activity.AddTag("tool.invoke.method", methodName);

        if (!mToolLookup.TryGetValue((className, methodName), out var methodInfo))
        {
            throw new InvalidOperationException($"Tool method not found: {className}.{methodName}");
        }

        var parameters = methodInfo.Parameters;
        if (parameters.Count != args.Length)
        {
            throw new ArgumentException($"Parameter count mismatch: expected {parameters.Count}, got {args.Length}");
        }

        var convertedArgs = new object?[args.Length];
        for (var i = 0; i < args.Length; i++)
        {
            var paramType = parameters[i].Parameter.ParameterType;
            convertedArgs[i] = ConvertArgument(args[i], paramType);
        }

        object? instance = null;
        if (!methodInfo.Method.IsStatic)
        {
            instance = Activator.CreateInstance(methodInfo.Method.DeclaringType!);
        }

        try
        {
            return methodInfo.Method.Invoke(instance, convertedArgs);
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException ?? ex;
        }
    }

    private static object? ConvertArgument(object? input, Type targetType)
    {
        // Just kinda go by vibes in here - this might just end up being a string in the future.
        using var activity = Tracing.Start();

        if (input == null)
        {
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }

        if (targetType.IsInstanceOfType(input))
        {
            return input;
        }

        try
        {
            if (targetType.IsEnum && input is string str)
            {
                return Enum.Parse(targetType, str, ignoreCase: true);
            }

            return Convert.ChangeType(input, targetType);
        }
        catch
        {
            return input;
        }
    }

    private void AddType(Type type)
    {
        using var activity = Tracing.Start();
        activity.AddTag("tool.class", type.FullName ?? type.Name);

        var toolAttr = type.GetCustomAttribute<ToolClassAttribute>(false);
        if (toolAttr == null)
        {
            return;
        }

        Tracing.WriteLine($"Loading tool class {type.Name}", LogLevel.INFO);
        var methodInfos = new List<ToolMethodInfoInternal>();
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
        {
            var methodAttr = method.GetCustomAttribute<ToolMethodAttribute>(false);
            if (methodAttr == null)
            {
                continue;
            }

            var parameters = method.GetParameters()
                .Select(p => new ToolParameterInfoInternal(
                    p,
                    p.GetCustomAttribute<ToolParameterAttribute>(false)
                ))
                .ToList();

            methodInfos.Add(new ToolMethodInfoInternal(method, methodAttr, parameters));
        }

        mToolClasses.Add(new ToolClassInfoInternal(type, toolAttr, methodInfos));
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        mToolClasses.Clear();
    }

    private sealed record ToolClassInfoInternal
    {
        public Type Type { get; }
        public ToolClassAttribute Attribute { get; }
        public List<ToolMethodInfoInternal> Methods { get; }

        public ToolClassInfoInternal(Type type, ToolClassAttribute attribute, List<ToolMethodInfoInternal> methods)
        {
            Type = type;
            Attribute = attribute;
            Methods = methods;
        }
    }

    private sealed record ToolMethodInfoInternal
    {
        public MethodInfo Method { get; }
        public ToolMethodAttribute Attribute { get; }
        public List<ToolParameterInfoInternal> Parameters { get; }
        public Predicate<ToolContext>? IsAvailable { get; }

        public ToolMethodInfoInternal(MethodInfo method, ToolMethodAttribute attribute, List<ToolParameterInfoInternal> parameters)
        {
            Method = method;
            Attribute = attribute;
            Parameters = parameters;
        }
    }

    private sealed record ToolParameterInfoInternal
    {
        public ParameterInfo Parameter { get; }
        public ToolParameterAttribute? Attribute { get; }

        public ToolParameterInfoInternal(ParameterInfo parameter, ToolParameterAttribute? attribute)
        {
            Parameter = parameter;
            Attribute = attribute;
        }
    }
}
