using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace WindowsTrayTasks.Views;

public sealed class MarkdownTextBlock : TextBlock
{
    public static readonly DependencyProperty MarkdownTextProperty =
        DependencyProperty.Register(
            nameof(MarkdownText),
            typeof(string),
            typeof(MarkdownTextBlock),
            new PropertyMetadata("", OnMarkdownTextChanged));

    public string MarkdownText
    {
        get => (string)GetValue(MarkdownTextProperty);
        set => SetValue(MarkdownTextProperty, value);
    }

    private static void OnMarkdownTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MarkdownTextBlock block)
        {
            block.RenderMarkdown(e.NewValue as string ?? "");
        }
    }

    private void RenderMarkdown(string text)
    {
        Inlines.Clear();
        if (string.IsNullOrEmpty(text))
            return;

        var lines = text.Replace("\r\n", "\n").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (i > 0) Inlines.Add(new LineBreak());
            AddInlineMarkdown(lines[i]);
        }
    }

    private void AddInlineMarkdown(string text)
    {
        var index = 0;
        while (index < text.Length)
        {
            if (TryConsumeLink(text, ref index)) continue;
            if (TryConsumeDelimited(text, ref index, "**", s => new Bold(new Run(s)))) continue;
            if (TryConsumeDelimited(text, ref index, "`", s => new Run(s)
                {
                    FontFamily = new FontFamily("Consolas"),
                    Background = new SolidColorBrush(Color.FromRgb(0xEE, 0xEE, 0xEE)),
                })) continue;
            if (TryConsumeDelimited(text, ref index, "*", s => new Italic(new Run(s)))) continue;

            Inlines.Add(new Run(text[index].ToString()));
            index++;
        }
    }

    private bool TryConsumeLink(string text, ref int index)
    {
        if (text[index] != '[') return false;
        var closeLabel = text.IndexOf("](", index, StringComparison.Ordinal);
        if (closeLabel < 0) return false;
        var closeUrl = text.IndexOf(')', closeLabel + 2);
        if (closeUrl < 0) return false;

        var label = text[(index + 1)..closeLabel];
        var url = text[(closeLabel + 2)..closeUrl];
        if (string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(url)) return false;

        var link = new Hyperlink(new Run(label))
        {
            NavigateUri = Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri : null,
            Cursor = Cursors.Hand,
        };
        link.Click += (_, _) => OpenLink(url);
        Inlines.Add(link);
        index = closeUrl + 1;
        return true;
    }

    private bool TryConsumeDelimited(string text, ref int index, string delimiter, Func<string, Inline> createInline)
    {
        if (!text.AsSpan(index).StartsWith(delimiter, StringComparison.Ordinal)) return false;
        var close = text.IndexOf(delimiter, index + delimiter.Length, StringComparison.Ordinal);
        if (close < 0) return false;

        var content = text[(index + delimiter.Length)..close];
        if (content.Length == 0) return false;

        Inlines.Add(createInline(content));
        index = close + delimiter.Length;
        return true;
    }

    private static void OpenLink(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // Broken or unsupported links are ignored in the preview surface.
        }
    }
}
