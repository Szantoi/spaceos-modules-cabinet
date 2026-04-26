namespace SpaceOS.Cabinet.Abstractions;

/// <summary>Sanitizes markdown for assembly instructions (SEC-CAB02-3).</summary>
public interface IMarkdownSanitizer
{
    /// <summary>
    /// Sanitizes raw markdown: whitelists headers, bold, italic, lists, code blocks.
    /// Rejects HTML tags, links, images, scripts.
    /// </summary>
    /// <param name="rawMarkdown">The raw markdown string to sanitize.</param>
    /// <returns>The sanitized markdown string with disallowed constructs removed.</returns>
    string Sanitize(string rawMarkdown);
}
