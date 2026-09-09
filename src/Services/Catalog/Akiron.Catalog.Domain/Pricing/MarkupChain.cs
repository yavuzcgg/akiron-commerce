using Akiron.Catalog.Domain.Common;

namespace Akiron.Catalog.Domain.Pricing;

/// <summary>
/// The markups between a price and the buyer, in the order they apply: the top dealer
/// first, then their sub-dealer, and so on.
/// </summary>
/// <remarks>
/// The chain compounds rather than adding up. Ten percent followed by ten percent turns
/// 100 into 121, not 120, because the second dealer marks up the price they themselves
/// pay — which already includes the first dealer's margin. Getting this wrong
/// understates every price down a chain of more than one.
/// </remarks>
public sealed class MarkupChain
{
    /// <summary>
    /// Five levels of resale is already further than any real dealer network in this
    /// business goes; a longer chain is a sign of a loop or a bad import, not a deal.
    /// </summary>
    public const int MaxDepth = 5;

    public static readonly MarkupChain Empty = new([]);

    private MarkupChain(IReadOnlyList<Markup> markups) => Markups = markups;

    public IReadOnlyList<Markup> Markups { get; }

    public int Depth => Markups.Count;

    public bool IsEmpty => Markups.Count == 0;

    public static MarkupChain Create(IEnumerable<Markup>? markups)
    {
        var ordered = markups?.ToArray() ?? [];

        if (ordered.Length > MaxDepth)
        {
            throw new DomainValidationException(
                CatalogErrorCodes.MarkupChainTooDeep,
                $"A markup chain may hold at most {MaxDepth} levels, but had {ordered.Length}.",
                new Dictionary<string, object?> { ["maxDepth"] = MaxDepth, ["depth"] = ordered.Length });
        }

        return ordered.Length == 0 ? Empty : new MarkupChain(ordered);
    }

    /// <summary>
    /// The compounded multiplier for the whole chain, unrounded.
    /// </summary>
    /// <remarks>
    /// Returned as a factor rather than applied step by step on purpose: rounding to
    /// kuruş after every level would drift, and the drift grows with the chain. The
    /// caller multiplies once and rounds once.
    /// </remarks>
    public decimal Multiplier =>
        Markups.Aggregate(1m, (accumulated, markup) => accumulated * markup.Multiplier);
}
