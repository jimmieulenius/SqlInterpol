using System.Runtime.CompilerServices;
using SqlInterpol.Execution;
using SqlInterpol.Pipeline;
using SqlInterpol.Segments;

namespace SqlInterpol;

public partial class SqlBuilder
{
    /// <summary>
    /// Compiles an interpolated SQL string into a high-performance, reusable template.
    /// The resulting template bypasses stream processing during execution, natively injecting arguments in O(1) time.
    /// </summary>
    /// <param name="handler">The compiler-routed interpolation handler tracking the token stream.</param>
    /// <returns>The compiled SQL template.</returns>
    public ISqlTemplate Template([InterpolatedStringHandlerArgument("")] ref SqlQueryInterpolatedStringHandler handler)
    {
        var segments = new List<SqlSegment>();
        handler.TransferSegments(segments);

        var preprocessor = Context.Options.Preprocessor ?? SqlSegmentPreprocessor.Instance;
        var pipeline = new SqlPipeline(preprocessor, Context.Options.Rewriters);
        
        var compiledSegments = pipeline.Process(segments, Context);

        var vsb = new ValueStringBuilder(stackalloc char[2048]);
        try
        {
            var templateArgs = new List<SqlTemplateArgument>();
            int holeIndex = 0;

            for (int i = 0; i < compiledSegments.Count; i++)
            {
                var segment = compiledSegments[i];
                
                if (segment.Type == SqlSegmentType.Raw && segment.Value is SqlArgumentFragment argFrag)
                {
                    vsb.Append($"{{{holeIndex++}}}");
                    templateArgs.Add(new SqlTemplateArgument(argFrag.Name));
                }
                else if (segment.Type == SqlSegmentType.Unresolved || segment.Type == SqlSegmentType.Parameter)
                {
                    vsb.Append($"{{{holeIndex++}}}");
                    templateArgs.Add(new SqlTemplateArgument(segment.Value));
                }
                else
                {
                    CurrentRenderIndex = i;
                    var rendered = Renderer.Render(Context, segment, i, compiledSegments);
                    if (rendered != null)
                    {
                        rendered = rendered.Replace("{", "{{").Replace("}", "}}");
                        vsb.Append(rendered);
                    }
                }
            }

#pragma warning disable SQLIA10 
            return new SqlTemplate(vsb.ToString(), templateArgs.ToArray());
#pragma warning restore SQLIA10
        }
        finally
        {
            vsb.Dispose();
        }
    }

    /// <summary>
    /// Compiles an interpolated SQL string into a high-performance, reusable template,
    /// assigning it to the output parameter and returning the builder to allow fluent chaining.
    /// </summary>
    /// <param name="template">The compiled SQL template.</param>
    /// <param name="handler">The compiler-routed interpolation handler tracking the token stream.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public SqlBuilder Template(out ISqlTemplate template, [InterpolatedStringHandlerArgument("")] ref SqlQueryInterpolatedStringHandler handler)
    {
#pragma warning disable SQLIA07
        template = Template(ref handler);
#pragma warning restore SQLIA07
        return this;
    }
}
