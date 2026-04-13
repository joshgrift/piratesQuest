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

    private static readonly Regex HeadingPattern = new(
        "<h(?<level>[1-3])>(?<text>.*?)</h\\k<level>>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex TableCellPattern = new(
        "<td>(?<value>[^<]+)</td>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex NavLabelPattern = new(
        "(?<open><strong>)(?<label>[^<]+)(?<close></strong>:)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex HtmlTagPattern = new(
        "(<[^>]+>)",
        RegexOptions.Compiled);

    private static readonly Regex InventoryWordPattern = new(
        "(?<![\\p{L}])(?<word>cannonballs|cannonball|coins|coin|gold|wood|iron|fish|tea|health)(?![\\p{L}])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Dictionary<string, string> ResourceIconMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Wood"] = "/images/inventory/wood.png",
        ["Iron"] = "/images/inventory/iron.png",
        ["Gold"] = "/images/inventory/coin.png",
        ["Coin"] = "/images/inventory/coin.png",
        ["Cannonballs"] = "/images/inventory/cannon_ball.png",
        ["CannonBall"] = "/images/inventory/cannon_ball.png",
        ["Cannon Ball"] = "/images/inventory/cannon_ball.png",
        ["Fish"] = "/images/inventory/fish.png",
        ["Tea"] = "/images/inventory/tea.png",
        ["Health"] = "/images/inventory/health.png"
    };

    private static readonly Dictionary<string, (string IconPath, string AccessibleLabel)> InventoryWordMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["wood"] = ("/images/inventory/wood.png", "Wood"),
            ["iron"] = ("/images/inventory/iron.png", "Iron"),
            ["gold"] = ("/images/inventory/coin.png", "Gold"),
            ["coin"] = ("/images/inventory/coin.png", "Coin"),
            ["coins"] = ("/images/inventory/coin.png", "Coins"),
            ["fish"] = ("/images/inventory/fish.png", "Fish"),
            ["tea"] = ("/images/inventory/tea.png", "Tea"),
            ["cannonballs"] = ("/images/inventory/cannon_ball.png", "Cannonballs"),
            ["cannonball"] = ("/images/inventory/cannon_ball.png", "Cannonball"),
            ["health"] = ("/images/inventory/health.png", "Health")
        };

    private static readonly Dictionary<string, string> LabelIconMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Trading"] = "/images/components/revenue.png",
        ["Combat"] = "/images/status/combat.png",
        ["Questing"] = "/images/quest.png",
        ["Quests"] = "/images/quest.png",
        ["Ports"] = "/images/money_bag.png",
        ["Port"] = "/images/money_bag.png",
        ["Shop"] = "/images/inventory/coin.png",
        ["Characters"] = "/images/quest.png",
        ["Components"] = "/images/components/hull.png",
        ["Healing"] = "/images/status/health.png",
        ["Vault"] = "/images/money_bag.png",
        ["Shipyard"] = "/images/components/acceleration.png",
        ["Ships"] = "/images/components/speed.png",
        ["Inventory"] = "/images/inventory/master.png",
        ["World"] = "/images/cannon_shooting.png",
        ["Safety"] = "/images/status/health.png"
    };

    private static readonly Dictionary<string, string> ComponentIconMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Advanced Sails"] = "/images/components/acceleration.png",
        ["Reinforced Sails"] = "/images/components/speed.png",
        ["Reinforced Hull"] = "/images/components/hull.png",
        ["Expanded Cargo Hold"] = "/images/components/cargo.png",
        ["Masterwork Cannons"] = "/images/components/damage.png",
        ["Long-Range Cannons"] = "/images/components/range.png",
        ["Auto Health Regen"] = "/images/components/heal.png",
        ["Advanced Fish Nets"] = "/images/components/collect_fish.png",
        ["Reinforced Lumber Tools"] = "/images/components/collect_wood.png",
        ["Enhanced Mining Tools"] = "/images/components/collect_iron.png"
    };

    private static readonly Dictionary<string, string> PageIconMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["index"] = "/images/quest.png",
        ["getting-started"] = "/images/status/health.png",
        ["trading"] = "/images/components/revenue.png",
        ["ports-and-stores"] = "/images/money_bag.png",
        ["ships-and-components"] = "/images/components/hull.png",
        ["quests-and-unlocks"] = "/images/quest.png",
        ["combat-and-death"] = "/images/status/combat.png",
        ["multiplayer-and-world"] = "/images/cannon_shooting.png"
    };

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

        var headings = ParseHeadings(parsed.Body);
        var html = Markdown.ToHtml(parsed.Body, _markdownPipeline);
        html = RewriteMarkdownLinks(html);
        html = AddHeadingAnchors(html, headings);
        html = EnhanceHtml(html);

        return new WikiPage(
            normalizedSlug,
            title,
            summary,
            parsed.Frontmatter,
            parsed.Body,
            html,
            headings);
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
        var tocHtml = RenderTableOfContents(page);
        var pageIcon = ResolvePageIcon(page.Slug);
        var sectionLabel = page.Frontmatter.TryGetValue("section", out var section)
            ? WebUtility.HtmlEncode(section)
            : "Captain's Guide";
        var heroBadges = RenderHeroBadges(page);
        var tocPanelClass = string.IsNullOrWhiteSpace(tocHtml) ? "toc panel is-empty" : "toc panel";

        return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>{{escapedTitle}} | PiratesQuest Wiki</title>
  <meta name="description" content="{{escapedSummary}}">
  <style>
    @font-face {
      font-family: "New Rocker";
      src: url("/images/fonts/NewRocker-Regular.ttf") format("truetype");
      font-display: swap;
    }

    @font-face {
      font-family: "Texturina";
      src: url("/images/fonts/Texturina-VariableFont_opsz,wght.ttf") format("truetype");
      font-weight: 100 900;
      font-display: swap;
    }

    :root {
      color-scheme: light;
      --bg-deep: #10283b;
      --bg-mid: #18374f;
      --sand: #d9c39a;
      --sand-soft: #eadcba;
      --paper: rgba(246, 236, 212, 0.92);
      --paper-strong: rgba(252, 245, 227, 0.95);
      --ink: #1b2430;
      --muted: #5d675f;
      --gold: #ddb46a;
      --gold-bright: #f2d392;
      --gold-shadow: rgba(77, 49, 16, 0.22);
      --teal: #74b7dd;
      --teal-strong: #2d7da8;
      --rope: rgba(90, 64, 34, 0.28);
      --border: rgba(92, 64, 30, 0.18);
      --shadow: 0 28px 60px rgba(6, 16, 28, 0.34);
    }

    * { box-sizing: border-box; }

    html {
      scroll-behavior: smooth;
    }

    body {
      margin: 0;
      color: var(--ink);
      font-family: "Texturina", Georgia, serif;
      background:
        linear-gradient(180deg, rgba(7, 18, 30, 0.38), rgba(7, 18, 30, 0.08)),
        radial-gradient(circle at top left, rgba(116, 183, 221, 0.28), transparent 32%),
        radial-gradient(circle at top right, rgba(234, 191, 112, 0.18), transparent 26%),
        url("/images/wiki/sand.jpg") center top / 480px auto repeat,
        linear-gradient(180deg, var(--bg-mid) 0%, #1f4258 18%, #31586e 28%, #d6bf95 29%, #e8d9b5 100%);
    }

    body::before {
      content: "";
      position: fixed;
      inset: 0;
      pointer-events: none;
      background:
        linear-gradient(135deg, rgba(255, 239, 205, 0.08), transparent 24%),
        linear-gradient(225deg, rgba(255, 239, 205, 0.08), transparent 24%);
      opacity: 0.9;
    }

    a {
      color: var(--teal-strong);
      text-decoration-thickness: 1.5px;
      text-underline-offset: 3px;
    }

    a:hover {
      color: #164c68;
    }

    img {
      max-width: 100%;
      display: block;
    }

    .shell {
      width: min(1380px, calc(100% - 32px));
      margin: 28px auto 40px;
      display: grid;
      grid-template-columns: 290px minmax(0, 1fr) 250px;
      gap: 22px;
      align-items: start;
      position: relative;
      z-index: 1;
    }

    .panel {
      position: relative;
      background:
        linear-gradient(180deg, rgba(255, 249, 235, 0.78), rgba(241, 228, 199, 0.94)),
        linear-gradient(140deg, rgba(255, 255, 255, 0.16), transparent 32%);
      border: 1px solid var(--border);
      border-radius: 26px;
      box-shadow: var(--shadow);
      overflow: hidden;
      backdrop-filter: blur(8px);
    }

    .panel::before {
      content: "";
      position: absolute;
      inset: 0;
      pointer-events: none;
      background:
        linear-gradient(135deg, rgba(255, 255, 255, 0.14), transparent 26%),
        linear-gradient(315deg, rgba(110, 77, 30, 0.05), transparent 28%);
    }

    .sidebar,
    .toc {
      padding: 24px 20px;
      position: sticky;
      top: 22px;
    }

    .brand {
      display: block;
      text-decoration: none;
      color: inherit;
      margin-bottom: 18px;
    }

    .brand-kicker,
    .eyebrow,
    .hero-section,
    .nav-section-title,
    .toc-title,
    .hero-badge {
      font-size: 0.78rem;
      line-height: 1;
      letter-spacing: 0.18em;
      text-transform: uppercase;
    }

    .brand-kicker,
    .nav-section-title,
    .eyebrow,
    .hero-section,
    .toc-title {
      color: var(--muted);
    }

    .brand-header {
      display: flex;
      gap: 12px;
      align-items: center;
      margin: 10px 0 12px;
    }

    .brand-crest {
      width: 58px;
      height: 58px;
      padding: 10px;
      border-radius: 16px;
      background:
        radial-gradient(circle at 30% 30%, rgba(242, 211, 146, 0.9), rgba(196, 142, 65, 0.96));
      box-shadow:
        inset 0 0 0 1px rgba(255, 247, 221, 0.5),
        0 10px 20px rgba(60, 33, 11, 0.22);
    }

    .brand-title {
      margin: 0;
      font-family: "New Rocker", serif;
      font-size: 2.05rem;
      line-height: 0.95;
      color: #2d2113;
      text-shadow: 0 2px 0 rgba(255, 245, 223, 0.35);
    }

    .brand-subtitle,
    .hero-summary,
    .toc-empty {
      color: var(--muted);
      line-height: 1.6;
    }

    .banner {
      position: relative;
      border-radius: 18px;
      overflow: hidden;
      min-height: 132px;
      margin: 18px 0 20px;
      border: 1px solid rgba(87, 56, 19, 0.18);
      background:
        linear-gradient(180deg, rgba(8, 22, 39, 0.12), rgba(8, 22, 39, 0.5)),
        url("/images/banner.png") center / cover no-repeat;
      box-shadow: inset 0 0 0 1px rgba(255, 235, 194, 0.14);
    }

    .banner::after {
      content: "";
      position: absolute;
      inset: 0;
      background:
        linear-gradient(135deg, rgba(242, 211, 146, 0.3), transparent 38%),
        linear-gradient(225deg, rgba(116, 183, 221, 0.22), transparent 34%);
    }

    .nav-section + .nav-section {
      margin-top: 18px;
      padding-top: 18px;
      border-top: 1px solid rgba(90, 64, 34, 0.16);
    }

    .nav-list,
    .toc-list {
      list-style: none;
      padding: 0;
      margin: 0;
    }

    .nav-list li + li,
    .toc-list li + li {
      margin-top: 8px;
    }

    .nav-link,
    .toc-link {
      display: flex;
      gap: 10px;
      align-items: center;
      text-decoration: none;
      border-radius: 14px;
      transition: transform 0.14s ease, background 0.14s ease, box-shadow 0.14s ease;
    }

    .nav-link {
      padding: 10px 12px;
      color: var(--ink);
    }

    .nav-link:hover,
    .nav-link.active {
      transform: translateX(2px);
      background: linear-gradient(90deg, rgba(116, 183, 221, 0.14), rgba(221, 180, 106, 0.15));
      box-shadow: inset 0 0 0 1px rgba(76, 118, 144, 0.14);
    }

    .nav-icon,
    .label-icon {
      width: 22px;
      height: 22px;
      object-fit: contain;
      flex: 0 0 auto;
    }

    .nav-text {
      line-height: 1.3;
    }

    .content {
      padding: 26px;
    }

    .hero {
      position: relative;
      overflow: hidden;
      border-radius: 24px;
      padding: 26px 26px 24px;
      margin-bottom: 22px;
      color: #f8edd5;
      background:
        linear-gradient(145deg, rgba(16, 36, 54, 0.96), rgba(36, 24, 14, 0.94)),
        radial-gradient(circle at 18% 24%, rgba(116, 183, 221, 0.2), transparent 36%),
        radial-gradient(circle at 82% 30%, rgba(242, 211, 146, 0.18), transparent 28%);
      border: 1px solid rgba(221, 180, 106, 0.28);
      box-shadow:
        inset 0 0 0 1px rgba(255, 235, 194, 0.12),
        0 18px 36px rgba(8, 16, 26, 0.28);
    }

    .hero::before {
      content: "";
      position: absolute;
      inset: 0;
      background:
        linear-gradient(120deg, rgba(255, 235, 194, 0.05), transparent 38%),
        url("/images/banner.png") right center / cover no-repeat;
      mix-blend-mode: screen;
      opacity: 0.22;
      pointer-events: none;
    }

    .hero-inner {
      position: relative;
      display: grid;
      grid-template-columns: minmax(0, 1fr) 128px;
      gap: 16px;
      align-items: center;
    }

    .hero-title {
      margin: 10px 0 8px;
      font-family: "New Rocker", serif;
      font-size: clamp(2.6rem, 5vw, 4.2rem);
      line-height: 0.95;
      color: #f3d08d;
      text-shadow:
        0 2px 0 rgba(34, 16, 5, 0.55),
        0 18px 32px rgba(0, 0, 0, 0.26);
    }

    .hero-summary {
      max-width: 54ch;
      margin: 0;
      color: rgba(248, 237, 213, 0.86);
      font-size: 1.06rem;
    }

    .hero-badges {
      display: flex;
      gap: 10px;
      flex-wrap: wrap;
      margin-top: 18px;
    }

    .hero-badge {
      display: inline-flex;
      gap: 8px;
      align-items: center;
      padding: 10px 12px;
      border-radius: 999px;
      color: #f7ecd6;
      background: rgba(255, 244, 217, 0.08);
      border: 1px solid rgba(242, 211, 146, 0.18);
      box-shadow: inset 0 0 0 1px rgba(255, 244, 217, 0.06);
    }

    .hero-badge img {
      width: 18px;
      height: 18px;
      object-fit: contain;
    }

    .hero-icon-wrap {
      position: relative;
      width: 128px;
      height: 128px;
      justify-self: end;
      border-radius: 28px;
      padding: 24px;
      background:
        radial-gradient(circle at 30% 30%, rgba(242, 211, 146, 0.94), rgba(195, 139, 60, 0.96));
      box-shadow:
        inset 0 0 0 1px rgba(255, 247, 221, 0.5),
        0 16px 30px rgba(18, 10, 4, 0.28);
    }

    .hero-icon-wrap::after {
      content: "";
      position: absolute;
      inset: 10px;
      border-radius: 20px;
      border: 1px solid rgba(111, 73, 28, 0.2);
    }

    .hero-icon {
      width: 100%;
      height: 100%;
      object-fit: contain;
      filter: drop-shadow(0 10px 12px rgba(54, 30, 9, 0.18));
    }

    .wiki-body > :first-child {
      margin-top: 0;
    }

    .wiki-body h1,
    .wiki-body h2,
    .wiki-body h3 {
      scroll-margin-top: 28px;
    }

    .wiki-body h1 {
      margin: 0 0 1rem;
      font-family: "New Rocker", serif;
      font-size: clamp(2.2rem, 4vw, 3rem);
      line-height: 0.98;
      color: #2f220f;
    }

    .wiki-body h2 {
      margin-top: 2rem;
      margin-bottom: 0.75rem;
      padding-top: 0.2rem;
      font-size: 1.6rem;
      color: #214962;
      border-top: 1px solid rgba(90, 64, 34, 0.12);
    }

    .wiki-body h3 {
      margin-top: 1.4rem;
      margin-bottom: 0.55rem;
      font-size: 1.15rem;
      color: #634725;
    }

    .wiki-body p,
    .wiki-body li,
    .wiki-body blockquote {
      font-size: 1.08rem;
      line-height: 1.72;
    }

    .wiki-body ul,
    .wiki-body ol {
      padding-left: 1.3rem;
    }

    .wiki-body li + li {
      margin-top: 0.32rem;
    }

    .wiki-body strong {
      color: #3a2913;
    }

    .wiki-body table {
      width: 100%;
      margin: 1.3rem 0;
      border-collapse: collapse;
      overflow: hidden;
      border-radius: 18px;
      border: 1px solid rgba(90, 64, 34, 0.12);
      box-shadow:
        inset 0 0 0 1px rgba(255, 248, 231, 0.34),
        0 10px 20px rgba(41, 30, 18, 0.08);
      background:
        linear-gradient(180deg, rgba(255, 248, 232, 0.96), rgba(243, 232, 205, 0.96));
    }

    .wiki-body th,
    .wiki-body td {
      padding: 13px 15px;
      text-align: left;
      border-bottom: 1px solid rgba(90, 64, 34, 0.1);
      vertical-align: top;
    }

    .wiki-body th {
      color: #2f220f;
      background:
        linear-gradient(180deg, rgba(242, 211, 146, 0.46), rgba(223, 186, 117, 0.28));
    }

    .wiki-body tr:last-child td {
      border-bottom: none;
    }

    .wiki-body blockquote {
      margin: 1.25rem 0;
      padding: 16px 18px;
      border-left: 4px solid var(--gold);
      border-radius: 14px;
      background:
        linear-gradient(90deg, rgba(242, 211, 146, 0.18), rgba(116, 183, 221, 0.1));
      color: #4c402c;
    }

    .wiki-body code {
      padding: 0.15rem 0.4rem;
      border-radius: 8px;
      background: rgba(21, 52, 71, 0.09);
      color: #123a51;
      font-size: 0.96em;
    }

    .wiki-body hr {
      border: none;
      border-top: 1px solid rgba(90, 64, 34, 0.16);
      margin: 1.8rem 0;
    }

    .label-with-icon {
      display: inline-flex;
      align-items: center;
      gap: 9px;
      font-weight: 700;
      color: inherit;
    }

    .label-with-icon img {
      width: 22px;
      height: 22px;
      object-fit: contain;
    }

    .inline-item-icon {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 1.25em;
      height: 1.25em;
      vertical-align: -0.18em;
      margin: 0 0.08em;
    }

    .inline-item-icon img {
      width: 100%;
      height: 100%;
      object-fit: contain;
      filter: drop-shadow(0 1px 1px rgba(44, 28, 10, 0.18));
    }

    .sr-only {
      position: absolute;
      width: 1px;
      height: 1px;
      padding: 0;
      margin: -1px;
      overflow: hidden;
      clip: rect(0, 0, 0, 0);
      white-space: nowrap;
      border: 0;
    }

    .toc.is-empty {
      display: none;
    }

    .toc-title {
      margin: 0 0 12px;
    }

    .toc-link {
      padding: 8px 10px;
      color: var(--ink);
      line-height: 1.35;
    }

    .toc-link:hover {
      background: rgba(116, 183, 221, 0.12);
    }

    .toc-link.depth-3 {
      margin-left: 14px;
      color: #5c4a2d;
    }

    @media (max-width: 1120px) {
      .shell {
        grid-template-columns: 280px minmax(0, 1fr);
      }

      .toc {
        display: none;
      }
    }

    @media (max-width: 840px) {
      .shell {
        width: min(100%, calc(100% - 18px));
        margin-top: 12px;
        grid-template-columns: 1fr;
        gap: 16px;
      }

      .sidebar {
        position: static;
      }

      .content {
        padding: 16px;
      }

      .hero {
        padding: 22px 18px 18px;
      }

      .hero-inner {
        grid-template-columns: 1fr;
      }

      .hero-icon-wrap {
        width: 96px;
        height: 96px;
        justify-self: start;
      }

      .wiki-body th,
      .wiki-body td {
        padding: 11px 12px;
      }
    }
  </style>
</head>
<body>
  <div class="shell">
    <aside class="sidebar panel">
      <a class="brand" href="/wiki">
        <p class="brand-kicker">PiratesQuest</p>
        <div class="brand-header">
          <img class="brand-crest" src="/images/wiki/game-icon.png" alt="PiratesQuest crest">
          <h1 class="brand-title">Game Wiki</h1>
        </div>
        <p class="brand-subtitle">Quick help for sailing, trading, quests, inventory, and surviving the open sea.</p>
      </a>
      <div class="banner" aria-hidden="true"></div>
      {{navHtml}}
    </aside>

    <main class="content panel">
      <section class="hero">
        <div class="hero-inner">
          <div>
            <p class="hero-section">{{sectionLabel}}</p>
            <h1 class="hero-title">{{escapedTitle}}</h1>
            <p class="hero-summary">{{escapedSummary}}</p>
            <div class="hero-badges">{{heroBadges}}</div>
          </div>
          <div class="hero-icon-wrap">
            <img class="hero-icon" src="{{pageIcon}}" alt="">
          </div>
        </div>
      </section>

      <p class="eyebrow">PiratesQuest Wiki</p>
      <div class="wiki-body">
        {{page.Html}}
      </div>
    </main>

    <aside class="{{tocPanelClass}}">
      {{tocHtml}}
    </aside>
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

    private static List<WikiHeading> ParseHeadings(string markdownBody)
    {
        var headings = new List<WikiHeading>();

        foreach (var rawLine in markdownBody.Split('\n'))
        {
            var line = rawLine.Trim();
            if (!line.StartsWith('#'))
                continue;

            var depth = 0;
            while (depth < line.Length && line[depth] == '#')
                depth++;

            if (depth < 2 || depth > 3)
                continue;

            var text = line[depth..].Trim();
            if (string.IsNullOrWhiteSpace(text))
                continue;

            headings.Add(new WikiHeading(depth, text, Slugify(text)));
        }

        return headings;
    }

    private static string AddHeadingAnchors(string html, IReadOnlyList<WikiHeading> headings)
    {
        if (headings.Count == 0)
            return html;

        var headingIndex = 0;
        return HeadingPattern.Replace(html, match =>
        {
            var level = int.Parse(match.Groups["level"].Value);
            if (level is < 2 or > 3 || headingIndex >= headings.Count)
                return match.Value;

            var heading = headings[headingIndex++];
            var text = match.Groups["text"].Value;
            return $"<h{level} id=\"{heading.Id}\">{text}</h{level}>";
        });
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

    private static string EnhanceHtml(string html)
    {
        html = html
            .Replace("<table>", "<table class=\"wiki-table\">")
            .Replace("<ul>", "<ul class=\"wiki-list\">")
            .Replace("<ol>", "<ol class=\"wiki-list\">");

        html = ReplaceInventoryWordsInTextNodes(html);
        html = DecorateTableCells(html, ResourceIconMap);
        html = DecorateTableCells(html, ComponentIconMap);
        html = NavLabelPattern.Replace(html, match =>
        {
            var label = WebUtility.HtmlDecode(match.Groups["label"].Value.Trim());
            return LabelIconMap.TryGetValue(label, out var icon)
                ? $"{match.Groups["open"].Value}{RenderInlineLabel(label, icon)}{match.Groups["close"].Value}"
                : match.Value;
        });

        return html;
    }

    private static string DecorateTableCells(string html, IReadOnlyDictionary<string, string> iconMap)
    {
        return TableCellPattern.Replace(html, match =>
        {
            var value = WebUtility.HtmlDecode(match.Groups["value"].Value.Trim());
            return iconMap.TryGetValue(value, out var icon)
                ? $"<td>{RenderInlineLabel(value, icon)}</td>"
                : match.Value;
        });
    }

    private static string RenderInlineLabel(string value, string iconPath)
    {
        var escapedValue = WebUtility.HtmlEncode(value);
        var escapedIcon = WebUtility.HtmlEncode(iconPath);
        return $"<span class=\"label-with-icon\"><img class=\"label-icon\" src=\"{escapedIcon}\" alt=\"\"> <span>{escapedValue}</span></span>";
    }

    private static string ReplaceInventoryWordsInTextNodes(string html)
    {
        var segments = HtmlTagPattern.Split(html);
        for (var index = 0; index < segments.Length; index++)
        {
            var segment = segments[index];
            if (string.IsNullOrEmpty(segment) || segment.StartsWith('<'))
                continue;

            segments[index] = ReplaceInventoryWordsInText(segment);
        }

        return string.Concat(segments);
    }

    private static string ReplaceInventoryWordsInText(string text)
    {
        return InventoryWordPattern.Replace(text, match =>
        {
            var normalizedWord = match.Groups["word"].Value;
            if (!InventoryWordMap.TryGetValue(normalizedWord, out var item))
                return match.Value;

            return RenderInlineIcon(item.IconPath, item.AccessibleLabel, match.Value);
        });
    }

    private static string RenderInlineIcon(string iconPath, string accessibleLabel, string visibleText)
    {
        var escapedIcon = WebUtility.HtmlEncode(iconPath);
        var escapedLabel = WebUtility.HtmlEncode(accessibleLabel);
        var escapedText = WebUtility.HtmlEncode(visibleText);
        return $"<span class=\"inline-item-icon\" role=\"img\" aria-label=\"{escapedLabel}\" title=\"{escapedLabel}\"><img src=\"{escapedIcon}\" alt=\"\"><span class=\"sr-only\">{escapedText}</span></span>";
    }

    private static string RenderTableOfContents(WikiPage page)
    {
        var showToc = !page.Frontmatter.TryGetValue("showToc", out var rawValue) ||
                      !rawValue.Equals("false", StringComparison.OrdinalIgnoreCase);

        if (!showToc || page.Headings.Count == 0)
            return string.Empty;

        var builder = new StringBuilder();
        builder.Append("<p class=\"toc-title\">On This Page</p><ul class=\"toc-list\">");

        foreach (var heading in page.Headings)
        {
            var text = WebUtility.HtmlEncode(heading.Text);
            builder.Append($"<li><a class=\"toc-link depth-{heading.Depth}\" href=\"#{heading.Id}\">{text}</a></li>");
        }

        builder.Append("</ul>");
        return builder.ToString();
    }

    private static string RenderHeroBadges(WikiPage page)
    {
        var badges = new List<(string Label, string Icon)>
        {
            ("In-Game Help", "/images/quest.png")
        };

        if (page.Frontmatter.TryGetValue("section", out var section) &&
            LabelIconMap.TryGetValue(section, out var sectionIcon))
        {
            badges.Add((section, sectionIcon));
        }

        if (PageIconMap.TryGetValue(page.Slug, out var pageIcon))
        {
            badges.Add((HumanizeSlug(page.Slug), pageIcon));
        }

        return string.Join(string.Empty, badges
            .Distinct()
            .Select(badge =>
            {
                var label = WebUtility.HtmlEncode(badge.Label);
                var icon = WebUtility.HtmlEncode(badge.Icon);
                return $"<span class=\"hero-badge\"><img src=\"{icon}\" alt=\"\"> <span>{label}</span></span>";
            }));
    }

    private static string ResolvePageIcon(string slug)
    {
        return PageIconMap.TryGetValue(slug, out var icon)
            ? icon
            : "/images/quest.png";
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
                var icon = ResolvePageIcon(normalizedSlug);

                builder.Append($"<li><a class=\"{classes}\" href=\"{href}\">");
                builder.Append($"<img class=\"nav-icon\" src=\"{icon}\" alt=\"\">");
                builder.Append("<span class=\"nav-text\">");
                builder.Append(WebUtility.HtmlEncode(page.Title));
                builder.Append("</span></a></li>");
            }

            builder.Append("</ul></section>");
        }

        return builder.ToString();
    }

    private static string Slugify(string text)
    {
        var normalized = text.ToLowerInvariant();
        normalized = Regex.Replace(normalized, "[^a-z0-9\\s-]", string.Empty);
        normalized = Regex.Replace(normalized, "\\s+", "-");
        return normalized.Trim('-');
    }
}

public sealed record WikiPage(
    string Slug,
    string Title,
    string Summary,
    IReadOnlyDictionary<string, string> Frontmatter,
    string MarkdownBody,
    string Html,
    IReadOnlyList<WikiHeading> Headings);

public sealed record WikiNavSection(
    string Title,
    List<WikiNavPage> Pages);

public sealed record WikiNavPage(
    string Title,
    string Slug,
    string Path,
    int Order);

public sealed record WikiHeading(
    int Depth,
    string Text,
    string Id);

internal sealed record ParsedMarkdownFile(
    Dictionary<string, string> Frontmatter,
    string Body);
