using System.Text;

namespace ImageMosaic.Infrastructure;

/// <summary>Remembers which key each control shows, so that a language switch can apply the texts again.</summary>
internal sealed class Localizer(ToolTip toolTip)
{
    private readonly List<Action> appliers = [];

    public TControl Text<TControl>(TControl control, string key)
        where TControl : Control =>
        On(control, c => c.Text = T(key));

    public void Tip(Control control, string key) =>
        On(control, c => toolTip.SetToolTip(c, Wrap(T(key))));

    public TextBox Placeholder(TextBox box, string key) =>
        On(box, b => b.PlaceholderText = T(key));

    public TTarget On<TTarget>(TTarget target, Action<TTarget> apply)
    {
        apply(target);
        appliers.Add(() => apply(target));
        return target;
    }

    public void ApplyAll()
    {
        foreach (var apply in appliers)
        {
            apply();
        }
    }

    // Windows tooltips do not wrap long lines.
    public static string Wrap(string text, int width = 80)
    {
        var result = new StringBuilder();
        foreach (var paragraph in text.Split('\n'))
        {
            var line = 0;
            foreach (var word in paragraph.Split(' '))
            {
                if (line > 0 && line + 1 + word.Length > width)
                {
                    result.Append('\n');
                    line = 0;
                }
                else if (line > 0)
                {
                    result.Append(' ');
                    line++;
                }

                result.Append(word);
                line += word.Length;
            }

            result.Append('\n');
        }

        return result.ToString().TrimEnd('\n');
    }
}
