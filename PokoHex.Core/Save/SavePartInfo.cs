namespace PokoHex.Core;

/// <summary>
/// Informational, read-only inventory of files in a Pokopia save.
/// Hashes refer to original imported bytes, not any unsaved editor changes.
/// </summary>
public sealed record SavePartInfo(string RelativePath, string Kind, string EditSupport,
    long SizeBytes, string OriginalSha256, bool SupportsEditing);
