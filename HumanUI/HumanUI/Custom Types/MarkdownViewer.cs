using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Eto.Forms;

namespace HumanUI
{
    /// <summary>
    /// Eto markdown viewer: renders a small subset of CommonMark (headings,
    /// paragraphs, lists, code, emphasis, links, images, hr) to HTML and shows
    /// it in a WebView. The Eto port doesn't try to match Markdown.Xaml's
    /// styling fidelity — it skips Styles File entirely and bakes a minimal
    /// stylesheet inline. Asset Directory becomes the &lt;base&gt; href so
    /// relative image paths resolve.
    /// </summary>
    public class MarkdownViewer : Panel
    {
        private readonly WebView _view;
        private string _assetDir;
        private string _markdownText;

        public string MarkdownText
        {
            get => _markdownText;
            set
            {
                _markdownText = value ?? string.Empty;
                _view.LoadHtml(BuildHtml(_markdownText, _assetDir));
            }
        }

        public MarkdownViewer(string text, string styleFile = "", string assetDir = "")
        {
            _assetDir = string.IsNullOrEmpty(assetDir) ? Environment.CurrentDirectory : assetDir;
            _view = new WebView();
            Content = _view;
            ID = "GH_MarkdownViewer";
            MarkdownText = text;
        }

        private static string BuildHtml(string markdown, string assetDir)
        {
            var body = RenderMarkdown(markdown ?? string.Empty);
            string baseHref = "";
            if (!string.IsNullOrEmpty(assetDir))
            {
                try
                {
                    var uri = new Uri(Path.GetFullPath(assetDir).TrimEnd('\\', '/') + "/");
                    baseHref = $"<base href=\"{uri}\">";
                }
                catch { }
            }
            return $@"<!doctype html><html><head>{baseHref}<meta charset=""utf-8"">
<style>
body {{ font-family: -apple-system, Segoe UI, Helvetica, Arial, sans-serif; font-size: 13px; color: #222; padding: 8px 12px; line-height: 1.45; }}
h1,h2,h3,h4 {{ margin: 0.6em 0 0.3em; line-height: 1.2; }}
h1 {{ font-size: 1.6em; border-bottom: 1px solid #ddd; padding-bottom: 4px; }}
h2 {{ font-size: 1.35em; border-bottom: 1px solid #eee; padding-bottom: 3px; }}
h3 {{ font-size: 1.15em; }}
p {{ margin: 0.5em 0; }}
ul, ol {{ margin: 0.4em 0 0.4em 1.4em; }}
li {{ margin: 0.15em 0; }}
code {{ background: #f4f4f4; padding: 1px 4px; border-radius: 3px; font-family: Consolas, Menlo, monospace; }}
pre {{ background: #f4f4f4; padding: 8px; border-radius: 4px; overflow-x: auto; }}
pre code {{ background: transparent; padding: 0; }}
hr {{ border: 0; border-top: 1px solid #ccc; margin: 1em 0; }}
a {{ color: #0366d6; text-decoration: none; }}
a:hover {{ text-decoration: underline; }}
img {{ max-width: 100%; }}
</style></head><body>{body}</body></html>";
        }

        /// <summary>
        /// Minimal block-level markdown renderer. Walks line-by-line, recognizing
        /// fenced code blocks, headings, unordered/ordered lists, horizontal
        /// rules, and paragraphs. Inline formatting (emphasis, links, images,
        /// inline code) is handled per-line by RenderInline.
        /// </summary>
        private static string RenderMarkdown(string md)
        {
            var sb = new StringBuilder();
            var lines = md.Replace("\r\n", "\n").Split('\n');
            bool inUl = false, inOl = false, inCode = false;
            var para = new StringBuilder();

            void closeLists()
            {
                if (inUl) { sb.Append("</ul>"); inUl = false; }
                if (inOl) { sb.Append("</ol>"); inOl = false; }
            }
            void flushPara()
            {
                if (para.Length > 0) { sb.Append("<p>").Append(RenderInline(para.ToString())).Append("</p>"); para.Clear(); }
            }

            foreach (var rawLine in lines)
            {
                var line = rawLine;
                if (line.TrimStart().StartsWith("```"))
                {
                    flushPara(); closeLists();
                    if (inCode) { sb.Append("</code></pre>"); inCode = false; }
                    else { sb.Append("<pre><code>"); inCode = true; }
                    continue;
                }
                if (inCode) { sb.Append(HtmlEscape(line)).Append('\n'); continue; }

                if (string.IsNullOrWhiteSpace(line)) { flushPara(); closeLists(); continue; }
                if (Regex.IsMatch(line, @"^-{3,}\s*$") || Regex.IsMatch(line, @"^\*{3,}\s*$"))
                {
                    flushPara(); closeLists(); sb.Append("<hr>"); continue;
                }
                var h = Regex.Match(line, @"^(#{1,4})\s+(.+)$");
                if (h.Success)
                {
                    flushPara(); closeLists();
                    int level = h.Groups[1].Value.Length;
                    sb.Append($"<h{level}>").Append(RenderInline(h.Groups[2].Value)).Append($"</h{level}>");
                    continue;
                }
                var ul = Regex.Match(line, @"^\s*[-*+]\s+(.+)$");
                if (ul.Success)
                {
                    flushPara();
                    if (inOl) { sb.Append("</ol>"); inOl = false; }
                    if (!inUl) { sb.Append("<ul>"); inUl = true; }
                    sb.Append("<li>").Append(RenderInline(ul.Groups[1].Value)).Append("</li>");
                    continue;
                }
                var ol = Regex.Match(line, @"^\s*\d+\.\s+(.+)$");
                if (ol.Success)
                {
                    flushPara();
                    if (inUl) { sb.Append("</ul>"); inUl = false; }
                    if (!inOl) { sb.Append("<ol>"); inOl = true; }
                    sb.Append("<li>").Append(RenderInline(ol.Groups[1].Value)).Append("</li>");
                    continue;
                }
                if (para.Length > 0) para.Append(' ');
                para.Append(line);
            }
            flushPara(); closeLists();
            if (inCode) sb.Append("</code></pre>");
            return sb.ToString();
        }

        private static string RenderInline(string s)
        {
            s = HtmlEscape(s);
            // images ![alt](src)
            s = Regex.Replace(s, @"!\[(.*?)\]\(([^)\s]+)(?:\s+&quot;(.*?)&quot;)?\)",
                m => $"<img src=\"{m.Groups[2].Value}\" alt=\"{m.Groups[1].Value}\"" +
                     (m.Groups[3].Success ? $" title=\"{m.Groups[3].Value}\"" : "") + ">");
            // links [text](href)
            s = Regex.Replace(s, @"\[(.*?)\]\(([^)\s]+)(?:\s+&quot;(.*?)&quot;)?\)",
                m => $"<a href=\"{m.Groups[2].Value}\"" +
                     (m.Groups[3].Success ? $" title=\"{m.Groups[3].Value}\"" : "") + $">{m.Groups[1].Value}</a>");
            // inline code `code`
            s = Regex.Replace(s, @"`([^`]+?)`", "<code>$1</code>");
            // bold **text**
            s = Regex.Replace(s, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
            // italic *text*  (but not inside **…**, which we already replaced)
            s = Regex.Replace(s, @"(?<!\*)\*(?!\*)([^*]+?)(?<!\*)\*(?!\*)", "<em>$1</em>");
            return s;
        }

        private static string HtmlEscape(string s) =>
            s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
    }
}
