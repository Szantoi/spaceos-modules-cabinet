namespace SpaceOS.Cabinet.Assembly;

using System.Text.RegularExpressions;
using SpaceOS.Cabinet.Abstractions;

/// <summary>
/// Default implementation of <see cref="IMarkdownSanitizer"/> (SEC-CAB02-3).
/// Whitelists: headers (# ## ###), bold (**), italic (*), unordered lists (-), ordered lists (1.), code blocks (```).
/// Removes: HTML tags, links ([label](url)), images (![alt](url)), javascript: protocol.
/// </summary>
public sealed class MarkdownSanitizer : IMarkdownSanitizer
{
    // Images must be stripped before links to avoid partial matches
    private static readonly Regex ImageRegex =
        new(@"!\[([^\]]*)\]\([^)]*\)", RegexOptions.Compiled, TimeSpan.FromMilliseconds(500));

    private static readonly Regex LinkRegex =
        new(@"\[([^\]]*)\]\([^)]*\)", RegexOptions.Compiled, TimeSpan.FromMilliseconds(500));

    private static readonly Regex HtmlTagRegex =
        new(@"<[^>]+>", RegexOptions.Compiled, TimeSpan.FromMilliseconds(500));

    private static readonly Regex ScriptRegex =
        new(@"javascript:", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(500));

    /// <inheritdoc/>
    public string Sanitize(string rawMarkdown)
    {
        if (string.IsNullOrEmpty(rawMarkdown))
            return string.Empty;

        var result = rawMarkdown;
        result = ImageRegex.Replace(result, "$1");   // remove image syntax, keep alt text
        result = LinkRegex.Replace(result, "$1");    // remove link syntax, keep label
        result = HtmlTagRegex.Replace(result, "");   // strip HTML tags
        result = ScriptRegex.Replace(result, "");    // strip javascript: protocol
        return result.Trim();
    }
}
