namespace SqlInterpol.Parsing;

internal enum SegmentType
{
    Literal,
    Projection,
    Reference,
    Fragment,
    Parameter
}