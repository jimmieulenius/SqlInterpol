namespace SqlInterpol;

/// <summary>
/// A high-performance SQL template that executes a pre-rendered format string,
/// injecting dynamic parameters into the target context in O(1) time.
/// </summary>
public class SqlTemplate : ISqlTemplate
{
    private readonly string _formatString;
    private readonly SqlTemplateArgument[] _arguments;

    public SqlTemplate(string formatString, SqlTemplateArgument[] arguments)
    {
        _formatString = formatString;
        _arguments = arguments;
    }

    /// <inheritdoc />
    public string Render(ISqlContext context, object? arguments = null)
    {
        if (_arguments.Length == 0) return _formatString;

        IReadOnlyDictionary<string, Func<object, object?>>? getters = null;
        if (arguments != null)
        {
            getters = SqlMetadataRegistry.GetArgumentGetters(arguments.GetType());
        }

        var paramNames = new string[_arguments.Length];

        for (int i = 0; i < _arguments.Length; i++)
        {
            var argInfo = _arguments[i];
            object? val;

            if (argInfo.IsCaptured)
            {
                val = argInfo.CapturedValue;
            }
            else
            {
                if (getters != null && getters.TryGetValue(argInfo.Name!, out var getter))
                {
                    val = getter(arguments!);
                }
                else
                {
                    throw new ArgumentException($"The SQL template requires an argument named '{argInfo.Name}', but it was not provided in the payload.");
                }
            }

            // Injects the value into the parent timeline and generates a native marker (e.g. @p5)
            paramNames[i] = context.AddParameter(val);
        }

        return string.Format(_formatString, (object[])paramNames);
    }
}