using System.Collections.Generic;
using UnityEngine;

// Draws the pixel-art pop-up ads that replace obstacles after the player eats tracker cookies.
// Layout adapts to the box size, so the same design works for small birds and tall beanstalks.
public class AdPainter
{
    private const int Border = 2;
    private const int BarHeight = 9;
    private const int ButtonHeight = 13;
    private const int LineSpacing = 9;
    private const int MaxTextScale = 6;

    private enum Pattern { None, Stripes, Dots, Sparkles, Cookies }

    private class Design
    {
        public string headline, button;
        public Pattern pattern;
        public Color32 background, patternColor, bar, barText, text, shadow, buttonFill, buttonText;
    }

    private static readonly Color32 Clear = new Color32(0, 0, 0, 0);
    private static readonly Color32 Ink = new Color32(15, 25, 40, 255);
    private static readonly Color32 White = new Color32(255, 255, 255, 255);
    private static readonly Color32 Gold = new Color32(255, 215, 60, 255);
    private static readonly Color32 Red = new Color32(225, 45, 45, 255);

    private static readonly Design[] Designs =
    {
        new Design { headline = "BUY NOW!", button = "SHOP", pattern = Pattern.Stripes,
            background = new Color32(220, 45, 45, 255), patternColor = new Color32(190, 30, 35, 255),
            bar = Gold, barText = Ink, text = Gold, shadow = Ink, buttonFill = Gold, buttonText = Ink },
        new Design { headline = "YOU WON!", button = "CLAIM", pattern = Pattern.Sparkles,
            background = new Color32(115, 50, 170, 255), patternColor = Gold,
            bar = Gold, barText = Ink, text = Gold, shadow = Ink,
            buttonFill = new Color32(60, 190, 90, 255), buttonText = White },
        new Design { headline = "50% OFF", button = "TODAY ONLY", pattern = Pattern.Dots,
            background = new Color32(40, 165, 80, 255), patternColor = new Color32(75, 195, 110, 255),
            bar = White, barText = Ink, text = White, shadow = Ink, buttonFill = Red, buttonText = White },
        new Design { headline = "CLICK HERE", button = "OK", pattern = Pattern.None,
            background = new Color32(195, 195, 195, 255), patternColor = Clear,
            bar = new Color32(20, 40, 140, 255), barText = White, text = Ink, shadow = Clear,
            buttonFill = new Color32(225, 225, 225, 255), buttonText = Ink },
        new Design { headline = "WE USE COOKIES", button = "ACCEPT ALL", pattern = Pattern.Cookies,
            background = new Color32(255, 243, 222, 255), patternColor = new Color32(238, 212, 170, 255),
            bar = new Color32(140, 85, 45, 255), barText = White, text = new Color32(95, 55, 25, 255),
            shadow = Clear, buttonFill = new Color32(40, 120, 220, 255), buttonText = White },
    };

    // 7-row pixel font; '#' is a filled pixel and '/' separates rows.
    private static readonly Dictionary<char, string> Glyphs = new Dictionary<char, string>
    {
        { 'A', ".###./#...#/#...#/#####/#...#/#...#/#...#" },
        { 'B', "####./#...#/#...#/####./#...#/#...#/####." },
        { 'C', ".###./#...#/#..../#..../#..../#...#/.###." },
        { 'D', "####./#...#/#...#/#...#/#...#/#...#/####." },
        { 'E', "#####/#..../#..../####./#..../#..../#####" },
        { 'F', "#####/#..../#..../####./#..../#..../#...." },
        { 'H', "#...#/#...#/#...#/#####/#...#/#...#/#...#" },
        { 'I', "###/.#./.#./.#./.#./.#./###" },
        { 'K', "#...#/#..#./#.#../##.../#.#../#..#./#...#" },
        { 'L', "#..../#..../#..../#..../#..../#..../#####" },
        { 'M', "#...#/##.##/#.#.#/#.#.#/#...#/#...#/#...#" },
        { 'N', "#...#/##..#/##..#/#.#.#/#..##/#..##/#...#" },
        { 'O', ".###./#...#/#...#/#...#/#...#/#...#/.###." },
        { 'P', "####./#...#/#...#/####./#..../#..../#...." },
        { 'R', "####./#...#/#...#/####./#.#../#..#./#...#" },
        { 'S', ".####/#..../#..../.###./....#/....#/####." },
        { 'T', "#####/..#../..#../..#../..#../..#../..#.." },
        { 'U', "#...#/#...#/#...#/#...#/#...#/#...#/.###." },
        { 'W', "#...#/#...#/#...#/#.#.#/#.#.#/##.##/#...#" },
        { 'Y', "#...#/#...#/.#.#./..#../..#../..#../..#.." },
        { '0', ".###./#...#/#..##/#.#.#/##..#/#...#/.###." },
        { '5', "#####/#..../####./....#/....#/#...#/.###." },
        { '%', "##..#/##..#/...#./..#../.#.../#..##/#..##" },
        { '!', "#/#/#/#/#/./#" },
        { ' ', "../../../../../../.." },
    };

    public static int DesignCount => Designs.Length;

    private readonly int width;
    private readonly int height;
    private readonly Color32[] pixels;
    private readonly System.Random random;

    public AdPainter(int width, int height, int seed)
    {
        this.width = width;
        this.height = height;
        pixels = new Color32[width * height];
        random = new System.Random(seed);
    }

    public Color32[] Paint(int design)
    {
        Draw(Designs[design]);
        return pixels;
    }

    private void Draw(Design design)
    {
        Fill(0, 0, width, height, design.background);
        DrawPattern(design.pattern, design.patternColor);

        // Title bar with an "AD" tag and a tiny close button.
        Fill(Border, Border, width - 2 * Border, BarHeight, design.bar);
        Fill(Border, Border + BarHeight, width - 2 * Border, 1, Ink);
        DrawText("AD", Border + 2, Border + 1, 1, design.barText, Clear);
        DrawCloseButton(width - Border - 8, Border + 1);

        int left = Border + 2;
        int areaWidth = width - 2 * left;
        int top = Border + BarHeight + 3;
        int bottom = height - Border - 2;

        // Only add the button when it doesn't force the headline to shrink.
        string label = FitLabel(design.button, areaWidth - 8);
        int buttonTop = bottom - ButtonHeight - 1;
        FitHeadline(design.headline, areaWidth, bottom - top, out int fullScale);
        FitHeadline(design.headline, areaWidth, buttonTop - 3 - top, out int scaleWithButton);
        if (label != null && scaleWithButton == fullScale)
        {
            DrawButton(label, buttonTop, design.buttonFill, design.buttonText);
            bottom = buttonTop - 3;
        }

        List<string> lines = FitHeadline(design.headline, areaWidth, bottom - top, out int scale);
        if (lines != null)
        {
            int y = top + (bottom - top - BlockHeight(lines.Count, scale)) / 2;
            foreach (string line in lines)
            {
                int x = left + (areaWidth - scale - MeasureText(line, scale)) / 2;
                DrawText(line, x, y, scale, design.text, design.shadow);
                y += LineSpacing * scale;
            }
        }

        DrawFrame();
    }

    private void DrawPattern(Pattern pattern, Color32 color)
    {
        switch (pattern)
        {
            case Pattern.Stripes:
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                        if ((x + y) / 6 % 2 == 0) SetPixel(x, y, color);
                break;
            case Pattern.Dots:
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                        if (y % 8 < 2 && (x + y / 8 % 2 * 4) % 8 < 2) SetPixel(x, y, color);
                break;
            case Pattern.Sparkles:
                for (int i = 0; i < width * height / 150; i++)
                    DrawSparkle(random.Next(width), random.Next(height), random.Next(1, 3), color);
                break;
            case Pattern.Cookies:
                for (int y = 6; y < height; y += 16)
                    for (int x = y / 16 % 2 * 8 + 6; x < width; x += 16)
                        DrawMiniCookie(x, y, color);
                break;
        }
    }

    private void DrawSparkle(int x, int y, int arm, Color32 color)
    {
        for (int i = -arm; i <= arm; i++)
        {
            SetPixel(x + i, y, color);
            SetPixel(x, y + i, color);
        }
    }

    private void DrawMiniCookie(int centerX, int centerY, Color32 color)
    {
        Color32 chip = new Color32((byte)(color.r * 3 / 4), (byte)(color.g * 3 / 4), (byte)(color.b * 3 / 4), 255);
        for (int y = -4; y <= 4; y++)
            for (int x = -4; x <= 4; x++)
                if (x * x + y * y <= 17) SetPixel(centerX + x, centerY + y, color);
        SetPixel(centerX - 2, centerY - 1, chip);
        SetPixel(centerX + 1, centerY - 2, chip);
        SetPixel(centerX + 2, centerY + 1, chip);
        SetPixel(centerX - 1, centerY + 2, chip);
    }

    private void DrawCloseButton(int x, int y)
    {
        Fill(x, y, 7, 7, Red);
        for (int i = 1; i < 6; i++)
        {
            SetPixel(x + i, y + i, White);
            SetPixel(x + 6 - i, y + i, White);
        }
    }

    private void DrawButton(string label, int y, Color32 fill, Color32 textColor)
    {
        int buttonWidth = MeasureText(label, 1) + 8;
        int x = (width - buttonWidth) / 2;
        Fill(x + 1, y + 1, buttonWidth, ButtonHeight, Ink);
        Fill(x, y, buttonWidth, ButtonHeight, Ink);
        Fill(x + 1, y + 1, buttonWidth - 2, ButtonHeight - 2, fill);
        DrawText(label, x + 4, y + 3, 1, textColor, Clear);
    }

    private void DrawFrame()
    {
        Fill(0, 0, width, Border, Ink);
        Fill(0, height - Border, width, Border, Ink);
        Fill(0, 0, Border, height, Ink);
        Fill(width - Border, 0, Border, height, Ink);
        // Knock out the corner pixels so the box looks slightly rounded.
        SetPixel(0, 0, Clear);
        SetPixel(width - 1, 0, Clear);
        SetPixel(0, height - 1, Clear);
        SetPixel(width - 1, height - 1, Clear);
    }

    private void DrawText(string text, int x, int y, int scale, Color32 color, Color32 shadow)
    {
        if (shadow.a > 0) DrawText(text, x + scale, y + scale, scale, shadow, Clear);
        foreach (char c in text)
        {
            string[] rows = Glyphs[c].Split('/');
            for (int row = 0; row < rows.Length; row++)
                for (int column = 0; column < rows[row].Length; column++)
                    if (rows[row][column] == '#') Fill(x + column * scale, y + row * scale, scale, scale, color);
            x += (rows[0].Length + 1) * scale;
        }
    }

    // The button label, or just its first word if the whole label is too wide (null if neither fits).
    private static string FitLabel(string label, int maxWidth)
    {
        if (MeasureText(label, 1) <= maxWidth) return label;
        string firstWord = label.Split(' ')[0];
        return MeasureText(firstWord, 1) <= maxWidth ? firstWord : null;
    }

    // Wraps the headline at the largest text scale that fits the area; scale is 0 if nothing fits.
    private static List<string> FitHeadline(string text, int areaWidth, int areaHeight, out int scale)
    {
        for (scale = MaxTextScale; scale >= 1; scale--)
        {
            // Each scale step also reserves room for the text's drop shadow.
            List<string> lines = Wrap(text, areaWidth - scale, scale);
            if (lines != null && BlockHeight(lines.Count, scale) <= areaHeight) return lines;
        }
        return null;
    }

    private static List<string> Wrap(string text, int maxWidth, int scale)
    {
        List<string> lines = new List<string>();
        foreach (string word in text.Split(' '))
        {
            if (MeasureText(word, scale) > maxWidth) return null;
            int last = lines.Count - 1;
            if (last >= 0 && MeasureText(lines[last] + " " + word, scale) <= maxWidth) lines[last] += " " + word;
            else lines.Add(word);
        }
        return lines;
    }

    private static int BlockHeight(int lineCount, int scale)
    {
        return (lineCount * LineSpacing - 1) * scale;
    }

    private static int MeasureText(string text, int scale)
    {
        int total = 0;
        foreach (char c in text) total += (Glyphs[c].IndexOf('/') + 1) * scale;
        return total - scale;
    }

    // Coordinates start at the top-left corner; textures store rows from the bottom up.
    private void Fill(int x, int y, int fillWidth, int fillHeight, Color32 color)
    {
        for (int row = Mathf.Max(0, y); row < Mathf.Min(height, y + fillHeight); row++)
            for (int column = Mathf.Max(0, x); column < Mathf.Min(width, x + fillWidth); column++)
                pixels[(height - 1 - row) * width + column] = color;
    }

    private void SetPixel(int x, int y, Color32 color)
    {
        Fill(x, y, 1, 1, color);
    }
}
