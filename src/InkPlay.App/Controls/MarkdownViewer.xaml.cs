using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

namespace InkPlay.App.Controls;

public sealed partial class MarkdownViewer : UserControl
{
    public static readonly DependencyProperty MarkdownProperty =
        DependencyProperty.Register(nameof(Markdown), typeof(string), typeof(MarkdownViewer),
            new PropertyMetadata(string.Empty, OnMarkdownChanged));

    public string Markdown
    {
        get => (string)GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    public MarkdownViewer()
    {
        InitializeComponent();
    }

    private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MarkdownViewer viewer && e.NewValue is string markdown)
        {
            viewer.RenderMarkdown(markdown);
        }
    }

    private void RenderMarkdown(string markdown)
    {
        ContentBlock.Blocks.Clear();

        if (string.IsNullOrWhiteSpace(markdown)) return;

        var lines = markdown.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        var i = 0;

        while (i < lines.Length)
        {
            var line = lines[i].TrimEnd();

            // Empty line = paragraph break
            if (string.IsNullOrWhiteSpace(line))
            {
                i++;
                continue;
            }

            // Headers
            if (line.StartsWith("###"))
            {
                var text = line.TrimStart('#').TrimStart();
                if (!string.IsNullOrEmpty(text))
                {
                    ContentBlock.Blocks.Add(CreateHeaderParagraph(text, 17));
                }
            }
            else if (line.StartsWith("##"))
            {
                var text = line.TrimStart('#').TrimStart();
                if (!string.IsNullOrEmpty(text))
                {
                    ContentBlock.Blocks.Add(CreateHeaderParagraph(text, 20));
                }
            }
            else if (line.StartsWith("#"))
            {
                var text = line.TrimStart('#').TrimStart();
                if (!string.IsNullOrEmpty(text))
                {
                    ContentBlock.Blocks.Add(CreateHeaderParagraph(text, 24));
                }
            }
            // Horizontal rule
            else if (line.Trim() == "---" || line.Trim() == "***")
            {
                var para = new Paragraph();
                para.Inlines.Add(new Run { Text = "─" });
                ContentBlock.Blocks.Add(para);
            }
            // Blockquote
            else if (line.StartsWith("> "))
            {
                var para = new Paragraph();
                para.Inlines.Add(new Run { Text = "│ ", Foreground = new SolidColorBrush(Microsoft.UI.Colors.CornflowerBlue) });
                AddFormattedRuns(para, line[2..]);
                ContentBlock.Blocks.Add(para);
            }
            // Unordered list
            else if (line.StartsWith("- ") || line.StartsWith("* "))
            {
                var para = new Paragraph();
                para.Inlines.Add(new Run { Text = "•  " });
                AddFormattedRuns(para, line[2..]);
                ContentBlock.Blocks.Add(para);
            }
            // Ordered list
            else if (line.Length > 2 && char.IsDigit(line[0]) && line.Contains(". "))
            {
                var dotIndex = line.IndexOf(". ");
                var num = line[..dotIndex];
                var content = line[(dotIndex + 2)..];
                var para = new Paragraph();
                para.Inlines.Add(new Run { Text = $"{num}. " });
                AddFormattedRuns(para, content);
                ContentBlock.Blocks.Add(para);
            }
            // Normal paragraph
            else
            {
                var para = new Paragraph();
                AddFormattedRuns(para, line);
                ContentBlock.Blocks.Add(para);
            }

            i++;
        }
    }

    private static Paragraph CreateHeaderParagraph(string text, double fontSize)
    {
        var para = new Paragraph();
        para.Margin = new Thickness(0, 8, 0, 4);
        var run = new Run
        {
            Text = text,
            FontSize = fontSize,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White)
        };
        para.Inlines.Add(run);
        return para;
    }

    private static void AddFormattedRuns(Paragraph para, string text)
    {
        var i = 0;
        while (i < text.Length)
        {
            // Bold ***text***
            if (i + 2 < text.Length && text[i] == '*' && text[i + 1] == '*' && text[i + 2] == '*')
            {
                var end = text.IndexOf("***", i + 3);
                if (end > i)
                {
                    var content = text[(i + 3)..end];
                    var run = new Run { Text = content, FontWeight = Microsoft.UI.Text.FontWeights.Bold, FontStyle = Windows.UI.Text.FontStyle.Italic };
                    para.Inlines.Add(run);
                    i = end + 3;
                    continue;
                }
            }

            // Bold **text**
            if (i + 1 < text.Length && text[i] == '*' && text[i + 1] == '*')
            {
                var end = text.IndexOf("**", i + 2);
                if (end > i)
                {
                    var content = text[(i + 2)..end];
                    var run = new Run { Text = content, FontWeight = Microsoft.UI.Text.FontWeights.Bold };
                    para.Inlines.Add(run);
                    i = end + 2;
                    continue;
                }
            }

            // Italic *text*
            if (text[i] == '*' && (i + 1 < text.Length && text[i + 1] != '*'))
            {
                var end = text.IndexOf('*', i + 1);
                if (end > i)
                {
                    var content = text[(i + 1)..end];
                    var run = new Run { Text = content, FontStyle = Windows.UI.Text.FontStyle.Italic };
                    para.Inlines.Add(run);
                    i = end + 1;
                    continue;
                }
            }

            // Strikethrough ~~text~~
            if (i + 1 < text.Length && text[i] == '~' && text[i + 1] == '~')
            {
                var end = text.IndexOf("~~", i + 2);
                if (end > i)
                {
                    var content = text[(i + 2)..end];
                    var run = new Run { Text = content, TextDecorations = Windows.UI.Text.TextDecorations.Strikethrough };
                    para.Inlines.Add(run);
                    i = end + 2;
                    continue;
                }
            }

            // Inline code `text`
            if (text[i] == '`')
            {
                var end = text.IndexOf('`', i + 1);
                if (end > i)
                {
                    var content = text[(i + 1)..end];
                    var run = new Run { Text = content, FontFamily = new FontFamily("Consolas") };
                    para.Inlines.Add(run);
                    i = end + 1;
                    continue;
                }
            }

            // Normal text - collect until next special char
            var start = i;
            while (i < text.Length && text[i] != '*' && text[i] != '~' && text[i] != '`')
            {
                i++;
            }
            if (i > start)
            {
                para.Inlines.Add(new Run { Text = text[start..i] });
            }
        }
    }
}
