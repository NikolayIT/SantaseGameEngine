using SkiaSharp;

// Renders the Google Play store graphics (512x512 icon + 1024x500 feature graphic)
// from the same card motif used by the app icon. Pure SkiaSharp, no SVG dependency.
// Usage: dotnet run --project tools/IconRender -- <outputDir>

const string HeartData =
    "M23.6,0c-3.4,0-6.3,2.7-7.6,5.6C14.7,2.7,11.8,0,8.4,0C3.8,0,0,3.8,0,8.4" +
    "c0,9.4,9.5,11.9,16,21.2c6.1-9.3,16-12,16-21.2C32,3.8,28.2,0,23.6,0z";
const string Digit1 = "M218,267 C197,263 182,284 182,308 C182,327 196,334 206,334 C218,334 226,323 226,310 C226,299 217,291 205,291 C195,291 186,298 183,307";
const string Digit2 = "M268,267 C247,263 232,284 232,308 C232,327 246,334 256,334 C268,334 276,323 276,310 C276,299 267,291 255,291 C245,291 236,298 233,307";

if (args.Length > 0 && args[0] == "screenshots")
{
    string inDir = args.Length > 1 ? args[1] : ".";
    string scOut = args.Length > 2 ? args[2] : ".";
    Directory.CreateDirectory(scOut);
    RenderScreenshot(Path.Combine(inDir, "in-game.png"), Path.Combine(scOut, "1.png"),
        "Classic Santase / 66", "Beat a world-class on-device AI");
    RenderScreenshot(Path.Combine(inDir, "home-page.png"), Path.Combine(scOut, "2.png"),
        "Choose your opponent", "Five AI levels, from beginner to 2400 ELO");
    Console.WriteLine("Done.");
    return;
}

string outDir = args.Length > 0 ? args[0] : ".";
Directory.CreateDirectory(outDir);

RenderIcon(Path.Combine(outDir, "icon.png"));
RenderFeature(Path.Combine(outDir, "featureGraphic.png"));
Console.WriteLine("Done.");

void RenderIcon(string path)
{
    using var surface = SKSurface.Create(new SKImageInfo(512, 512, SKColorType.Rgba8888, SKAlphaType.Opaque));
    var c = surface.Canvas;
    DrawFelt(c, 512, 512);
    c.Save();
    c.Scale(512f / 456f);          // motif authored in a 456x456 space
    DrawCardMotif(c);
    c.Restore();
    Save(surface, path);
}

void RenderFeature(string path)
{
    const int W = 1024, H = 500;
    using var surface = SKSurface.Create(new SKImageInfo(W, H, SKColorType.Rgba8888, SKAlphaType.Opaque));
    var c = surface.Canvas;
    DrawFelt(c, W, H);

    // Card motif on the left.
    c.Save();
    c.Translate(298, 250);
    c.Scale(1.30f);
    c.Translate(-228, -228);
    DrawCardMotif(c);
    c.Restore();

    // Wordmark on the right.
    using var titleTf = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold);
    using var subTf = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Normal);
    using var titleFont = new SKFont(titleTf, 96);
    using var subFont = new SKFont(subTf, 33);
    using var smallFont = new SKFont(titleTf, 27);
    using var white = new SKPaint { Color = SKColors.White, IsAntialias = true };
    using var faint = new SKPaint { Color = SKColor.Parse("#CFE8D8"), IsAntialias = true };
    using var gold = new SKPaint { Color = SKColor.Parse("#C8A24B"), IsAntialias = true };

    float tx = 486;
    c.DrawText("Santase 66", tx, 244, SKTextAlign.Left, titleFont, white);
    c.DrawRoundRect(new SKRect(tx + 3, 266, tx + 3 + 196, 274), 4, 4, gold);   // accent underline
    c.DrawText("Classic two-player card game", tx, 322, SKTextAlign.Left, subFont, faint);
    c.DrawText("66  ·  Schnapsen  ·  Сантасе", tx, 364, SKTextAlign.Left, smallFont, gold);

    Save(surface, path);
}

void RenderScreenshot(string inPath, string outPath, string headline, string subline)
{
    const int W = 1080, H = 1920;
    using var data = SKData.Create(inPath);
    if (data is null) { Console.WriteLine($"skip (not found): {inPath}"); return; }
    using var shot = SKImage.FromEncodedData(data);
    if (shot is null) { Console.WriteLine($"skip (cannot decode): {inPath}"); return; }

    using var surface = SKSurface.Create(new SKImageInfo(W, H, SKColorType.Rgba8888, SKAlphaType.Opaque));
    var c = surface.Canvas;
    DrawFelt(c, W, H);

    // Caption.
    using var titleTf = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold);
    using var subTf = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Normal);
    using var hFont = new SKFont(titleTf, 60);
    using var sFont = new SKFont(subTf, 33);
    using var white = new SKPaint { Color = SKColors.White, IsAntialias = true };
    using var faint = new SKPaint { Color = SKColor.Parse("#CFE8D8"), IsAntialias = true };
    c.DrawText(headline, W / 2f, 162, SKTextAlign.Center, hFont, white);
    c.DrawText(subline, W / 2f, 214, SKTextAlign.Center, sFont, faint);

    // Device shot: scaled to width with margins, rounded corners + soft shadow.
    const float margin = 64, top = 300, bottomLimit = H - 150, radius = 26;
    float iw = W - (2 * margin);
    float ih = shot.Height * (iw / shot.Width);
    if (top + ih > bottomLimit)
    {
        ih = bottomLimit - top;
        iw = shot.Width * (ih / shot.Height);
    }

    float l = (W - iw) / 2f;
    var dest = new SKRect(l, top, l + iw, top + ih);

    using (var sh = new SKPaint { Color = new SKColor(0, 0, 0, 95), IsAntialias = true, MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 16) })
    {
        c.DrawRoundRect(new SKRect(dest.Left, dest.Top + 12, dest.Right, dest.Bottom + 12), radius, radius, sh);
    }

    c.Save();
    c.ClipRoundRect(new SKRoundRect(dest, radius, radius), SKClipOperation.Intersect, true);
    c.DrawImage(shot, dest, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));
    c.Restore();

    using (var border = new SKPaint { Color = new SKColor(255, 255, 255, 40), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 3 })
    {
        c.DrawRoundRect(dest, radius, radius, border);
    }

    Save(surface, outPath);
}

void DrawFelt(SKCanvas c, float w, float h)
{
    using var shader = SKShader.CreateRadialGradient(
        new SKPoint(w * 0.5f, h * 0.40f),
        MathF.Max(w, h) * 0.78f,
        new[] { SKColor.Parse("#139A4D"), SKColor.Parse("#0C5F30"), SKColor.Parse("#073A1D") },
        new[] { 0f, 0.55f, 1f },
        SKShaderTileMode.Clamp);
    using var p = new SKPaint { Shader = shader, IsAntialias = true };
    c.DrawRect(0, 0, w, h, p);
}

// Faithful port of Resources/AppIcon/appiconfg.svg, authored in 456x456 space.
void DrawCardMotif(SKCanvas c)
{
    // Back card (depth), peeking up-left.
    c.Save();
    c.Translate(-22, -10);
    RotateAbout(c, -13, 228, 228);
    RoundRect(c, 148, 110, 178, 252, 16, new SKColor(0, 0, 0, 41), null, 0);          // shadow
    RoundRect(c, 148, 102, 178, 252, 16, SKColor.Parse("#ECEFF4"), SKColor.Parse("#CED4DE"), 2);
    c.Restore();

    // Hero card.
    c.Save();
    RotateAbout(c, 4, 228, 228);
    RoundRect(c, 138, 105, 180, 264, 18, new SKColor(0, 0, 0, 56), null, 0);           // shadow
    RoundRect(c, 138, 96, 180, 264, 18, SKColors.White, SKColor.Parse("#E2E6EC"), 2);

    Heart(c, 228, 171, 2.6875f, SKColor.Parse("#E11D2A"));                              // big heart

    using (var p = new SKPaint
    {
        Color = SKColor.Parse("#0B6B34"),
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 13,
        StrokeCap = SKStrokeCap.Round,
        StrokeJoin = SKStrokeJoin.Round,
    })
    {
        using var d1 = SKPath.ParseSvgPathData(Digit1);
        using var d2 = SKPath.ParseSvgPathData(Digit2);
        c.DrawPath(d1, p);
        c.DrawPath(d2, p);
    }

    Heart(c, 170, 134, 0.78f, SKColor.Parse("#E11D2A"));                                // corner index
    c.Restore();
}

void RotateAbout(SKCanvas c, float deg, float px, float py)
{
    c.Translate(px, py);
    c.RotateDegrees(deg);
    c.Translate(-px, -py);
}

void RoundRect(SKCanvas c, float x, float y, float w, float h, float r, SKColor fill, SKColor? stroke, float sw)
{
    var rect = new SKRect(x, y, x + w, y + h);
    using (var p = new SKPaint { Color = fill, IsAntialias = true, Style = SKPaintStyle.Fill })
    {
        c.DrawRoundRect(rect, r, r, p);
    }

    if (stroke.HasValue)
    {
        using var p2 = new SKPaint { Color = stroke.Value, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = sw };
        c.DrawRoundRect(rect, r, r, p2);
    }
}

void Heart(SKCanvas c, float tx, float ty, float scale, SKColor color)
{
    c.Save();
    c.Translate(tx, ty);
    c.Scale(scale);
    c.Translate(-16, -14.8f);
    using var path = SKPath.ParseSvgPathData(HeartData);
    using var p = new SKPaint { Color = color, IsAntialias = true, Style = SKPaintStyle.Fill };
    c.DrawPath(path, p);
    c.Restore();
}

void Save(SKSurface surface, string path)
{
    using var image = surface.Snapshot();
    using var data = image.Encode(SKEncodedImageFormat.Png, 100);
    using var fs = File.OpenWrite(path);
    data.SaveTo(fs);
    Console.WriteLine($"wrote {path}");
}
