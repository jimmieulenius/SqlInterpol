namespace SqlInterpol.Segments;

/// <summary>
/// Marker interface that identifies a fragment as a collection of items rendered with a separator.
/// Allows O(1) collection detection in the rendering pipeline, replacing the previous
/// inheritance-chain walk.
/// </summary>
public interface ISqlCollectionFragment : ISqlFragment
{
}
