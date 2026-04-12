using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Markdig;

namespace PiratesQuest.Server.Services;

public sealed class WikiService(IWebHostEnvironment environment)
{
    private static readonly Regex MarkdownLinkPattern = new(
        "href=\"(?<prefix>\\.?/)?(?<slug>[a-z0-9-]+)\\.md\"",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly MarkdownPipeline _markdownPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private readonly string _wikiRootPath = ResolveWikiRootPath(environment.ContentRootPath);

    public WikiPage? GetPage(string slug)
    {
        var normalizedSlug = NormalizeSlug(slug);
        var filePath = Path.Combine(_wikiRootPath, $"{normalizedSlug}.md");
        if (!File.Exists(filePath))
            return null;

        var raw = File.ReadAllText(filePath);
        var parsed = ParseMarkdownFile(raw);
        var title = parsed.Frontmatter.TryGetValue("title", out var frontmatterTitle) &&
                    !string.IsNullOrWhiteSpace(frontmatterTitle)
            ? frontmatterTitle
            : HumanizeSlug(normalizedSlug);

        var summary = parsed.Frontmatter.TryGetValue("summary", out var frontmatterSummary)
            ? frontmatterSummary
            : string.Empty;

        var html = Markdown.ToHtml(parsed.Body, _markdownPipeline);
        html = RewriteMarkdownLinks(html);

        return new WikiPage(
            normalizedSlug,
            title,
            summary,
            parsed.Frontmatter,
            html);
    }

    public IReadOnlyList<WikiNavSection> GetNavigation()
    {
        var path = Path.Combine(_wikiRootPath, "navigation.json");
        if (!File.Exists(path))
            return [];

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<WikiNavSection>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? [];
    }

    public string RenderDocument(string currentSlug, WikiPage page, IReadOnlyList<WikiNavSection> navigation)
    {
        var escapedTitle = WebUtility.HtmlEncode(page.Title);
        var escapedSummary = WebUtility.HtmlEncode(page.Summary);
        var navHtml = RenderNavigation(currentSlug, navigation);

        return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>{{escapedTitle}} | PiratesQuest Wiki</title>
  <meta name="description" content="{{escapedSummary}}">
  <style>
    :root {
      color-scheme: light;
      --bg: #f6f0e3;
      --panel: rgba(255, 250, 240, 0.92);
      --panel-strong: #fff8ec;
      --ink: #1f2b2f;
      --muted: #5c6a6f;
      --accent: #0e6d7a;
      --accent-strong: #084d57;
      --border: rgba(16, 55, 61, 0.16);
      --shadow: 0 24px 60px rgba(18, 37, 41, 0.14);
    }

    * { box-sizing: border-box; }

    body {
      margin: 0;
      font-family: Georgia, "Times New Roman", serif;
      color: var(--ink);
      background:
        radial-gradient(circle at top, rgba(234, 208, 152, 0.45), transparent 32%),
        linear-gradient(180deg, #f7f2e9 0%, #ecdfc8 100%);
    }

    a {
      color: var(--accent-strong);
    }

    .shell {
      width: min(1200px, calc(100% - 32px));
      margin: 32px auto;
      display: grid;
      grid-template-columns: 280px minmax(0, 1fr);
      gap: 24px;
      align-items: start;
    }

    .panel {
      background: var(--panel);
      border: 1px solid var(--border);
      border-radius: 24px;
      box-shadow: var(--shadow);
      backdrop-filter: blur(8px);
    }

    .sidebar {
      padding: 24px 20px;
      position: sticky;
      top: 24px;
    }

    .brand {
      display: block;
      margin-bottom: 20px;
      text-decoration: none;
      color: inherit;
    }

    .brand-kicker {
      margin: 0 0 6px;
      font-size: 0.8rem;
      letter-spacing: 0.18em;
      text-transform: uppercase;
      color: var(--muted);
    }

    .brand-title {
      margin: 0;
      font-size: 1.9rem;
      line-height: 1;
    }

    .brand-subtitle {
      margin: 10px 0 0;
      color: var(--muted);
      line-height: 1.5;
    }

    .nav-section + .nav-section {
      margin-top: 18px;
      padding-top: 18px;
      border-top: 1px solid var(--border);
    }

    .nav-section-title {
      margin: 0 0 10px;
      font-size: 0.78rem;
      text-transform: uppercase;
      letter-spacing: 0.16em;
      color: var(--muted);
    }

    .nav-list {
      list-style: none;
      padding: 0;
      margin: 0;
    }

    .nav-list li + li {
      margin-top: 6px;
    }

    .nav-link {
      display: block;
      text-decoration: none;
      padding: 10px 12px;
      border-radius: 12px;
      color: var(--ink);
    }

    .nav-link:hover,
    .nav-link.active {
      background: rgba(14, 109, 122, 0.1);
      color: var(--accent-strong);
    }

    .content {
      padding: 32px 36px 40px;
    }

    .eyebrow {
      margin: 0;
      font-size: 0.8rem;
      letter-spacing: 0.18em;
      text-transform: uppercase;
      color: var(--muted);
    }

    .content h1 {
      margin-top: 10px;
      font-size: clamp(2.2rem, 4vw, 3.5rem);
      line-height: 1.05;
    }

    .content h2,
    .content h3 {
      margin-top: 1.8em;
    }

    .content p,
    .content li,
    .content blockquote {
      font-size: 1.06rem;
      line-height: 1.7;
    }

    .content table {
      width: 100%;
      border-collapse: collapse;
      margin: 1.25rem 0;
      background: var(--panel-strong);
      border-radius: 16px;
      overflow: hidden;
    }

    .content th,
    .content td {
      text-align: left;
      padding: 12px 14px;
      border-bottom: 1px solid var(--border);
      vertical-align: top;
    }

    .content th {
      background: rgba(14, 109, 122, 0.09);
    }

    .content blockquote {
      margin: 1.25rem 0;
      padding: 14px 18px;
      border-left: 4px solid var(--accent);
      background: rgba(14, 109, 122, 0.08);
      border-radius: 12px;
    }

    .content code {
      background: rgba(16, 55, 61, 0.08);
      padding: 0.12rem 0.34rem;
      border-radius: 6px;
    }

    @media (max-width: 920px) {
      .shell {
        grid-template-columns: 1fr;
      }

      .sidebar {
        position: static;
      }

      .content {
        padding: 24px 20px 28px;
      }
    }
  </style>
</head>
<body>
  <div class="shell">
    <aside class="sidebar panel">
      <a class="brand" href="/wiki">
        <p class="brand-kicker">PiratesQuest</p>
        <h1 class="brand-title">Game Wiki</h1>
        <p class="brand-subtitle">Help pages written in Markdown and served by the API.</p>
      </a>
      {{navHtml}}
    </aside>

    <main class="content panel">
      <p class="eyebrow">PiratesQuest Wiki</p>
      {{page.Html}}
    </main>
  </div>
</body>
</html>
""";
    }

    private static string ResolveWikiRootPath(string contentRootPath)
    {
        var localPath = Path.Combine(contentRootPath, "wiki");
        if (Directory.Exists(localPath))
            return localPath;

        var repoPath = Path.GetFullPath(Path.Combine(contentRootPath, "..", "wiki"));
        if (Directory.Exists(repoPath))
            return repoPath;

        throw new DirectoryNotFoundException($"Wiki folder was not found at '{localPath}' or '{repoPath}'.");
    }

    private static string NormalizeSlug(string slug)
    {
        var normalized = (slug ?? string.Empty).Trim().Trim('/');
        return string.IsNullOrWhiteSpace(normalized) ? "index" : normalized.ToLowerInvariant();
    }

    private static ParsedMarkdownFile ParseMarkdownFile(string raw)
    {
        using var reader = new StringReader(raw);
        var firstLine = reader.ReadLine();
        if (firstLine != "---")
            return new ParsedMarkdownFile(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), raw);

        var frontmatter = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line == "---")
                break;

            var separatorIndex = line.IndexOf(':');
            if (separatorIndex <= 0)
                continue;

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();
            frontmatter[key] = value;
        }

        var body = reader.ReadToEnd().TrimStart();
        return new ParsedMarkdownFile(frontmatter, body);
    }

    private static string RewriteMarkdownLinks(string html)
    {
        return MarkdownLinkPattern.Replace(html, match =>
        {
            var slug = match.Groups["slug"].Value.ToLowerInvariant();
            var href = slug == "index" ? "/wiki" : $"/wiki/{slug}";
            return $"href=\"{href}\"";
        });
    }

    private static string HumanizeSlug(string slug)
    {
        return string.Join(' ', slug.Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    }

    private static string RenderNavigation(string currentSlug, IReadOnlyList<WikiNavSection> navigation)
    {
        var builder = new StringBuilder();

        foreach (var section in navigation)
        {
            builder.Append("<section class=\"nav-section\">");
            builder.Append("<p class=\"nav-section-title\">");
            builder.Append(WebUtility.HtmlEncode(section.Title));
            builder.Append("</p><ul class=\"nav-list\">");

            foreach (var page in section.Pages.OrderBy(page => page.Order))
            {
                var normalizedSlug = NormalizeSlug(page.Slug);
                var href = normalizedSlug == "index" ? "/wiki" : $"/wiki/{normalizedSlug}";
                var classes = normalizedSlug == NormalizeSlug(currentSlug) ? "nav-link active" : "nav-link";

                builder.Append($"<li><a class=\"{classes}\" href=\"{href}\">");
                builder.Append(WebUtility.HtmlEncode(page.Title));
                builder.Append("</a></li>");
            }

            builder.Append("</ul></section>");
        }

        return builder.ToString();
    }
}

public sealed record WikiPage(
    string Slug,
    string Title,
    string Summary,
    IReadOnlyDictionary<string, string> Frontmatter,
    string Html);

public sealed record WikiNavSection(
    string Title,
    List<WikiNavPage> Pages);

public sealed record WikiNavPage(
    string Title,
    string Slug,
    string Path,
    int Order);

internal sealed record ParsedMarkdownFile(
    Dictionary<string, string> Frontmatter,
    string Body);
