using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 程序化生成裸眼 3D 色彩视差霓虹手绘纯文字海报贴图。
/// 采用高饱和色场 + 黑色草稿偏置重影 + 软光晕 + 双笔涂鸦白芯，实现零外置资源的矢量手绘质感。
/// </summary>
public static class EraPosterGenerator
{
    public struct PosterTheme
    {
        public string digits;       // 旧接口时代数字前缀（"19" | "20" | "21"）
        public string titleText;    // 通用定制文案（为空时回退 digits + "Century"）
        public Color bgColor;       // 高饱和背景底色
        public Color neonColor;     // 霓虹主荧光色
        public Color glowColor;     // 散射发光光晕色
        public Color coreColor;     // 内芯高亮发白光色
        public Color accentColor;   // 辅色点缀（双杠/光晕次色）
        public int motifType;       // 0: 穹顶星环  1: 电气弧环  2: 流线磁浮环  3: 授勋星冠  4: 星轨飞翼  5: 全通极光星徽
        public int bigPrefixCount;  // 前缀放大强调字符数（时代海报为 3，通用海报为 0 或 1）
    }

    static readonly Texture2D[] Cached = new Texture2D[3];
    static readonly Dictionary<string, Texture2D> CustomCache = new Dictionary<string, Texture2D>();

    /// <summary>
    /// 通用裸眼 3D 霓虹视差海报生成并缓存接口，供授勋、排行榜、成就通关等高光卡片调用。
    /// </summary>
    public static Texture2D GetCustomPoster(string cacheKey, string text, int paletteIndex, int motifType, int width = 768, int height = 248, int bigPrefixCount = 1)
    {
        string fullKey = (cacheKey ?? text ?? "poster") + "_" + paletteIndex + "_" + motifType + "_" + width + "x" + height;
        if (CustomCache.TryGetValue(fullKey, out var cached) && cached != null) return cached;

        PosterTheme theme = BuildPalette(paletteIndex);
        theme.titleText = string.IsNullOrEmpty(text) ? "RAILWAY" : text;
        theme.motifType = motifType;
        theme.bigPrefixCount = bigPrefixCount;

        int seed = 2029 + Mathf.Abs(fullKey.GetHashCode() % 10000);
        var tex = RenderPosterTexture(width, height, theme, seed);
        CustomCache[fullKey] = tex;
        return tex;
    }

    static PosterTheme BuildPalette(int index)
    {
        int p = ((index % 4) + 4) % 4;
        if (p == 0)
        {
            return new PosterTheme
            {
                bgColor = new Color(0.025f, 0.085f, 0.96f, 1f),    // 电光钴蓝
                neonColor = new Color(1.00f, 0.11f, 0.58f, 1f),    // 荧光洋红
                glowColor = new Color(0.95f, 0.08f, 0.62f, 1f),
                coreColor = new Color(1.00f, 0.84f, 0.93f, 1f),
                accentColor = new Color(0.45f, 0.62f, 1.00f, 1f),
                bigPrefixCount = 3
            };
        }
        if (p == 1)
        {
            return new PosterTheme
            {
                bgColor = new Color(0.19f, 0.03f, 0.38f, 1f),      // 深邃皇家绛紫
                neonColor = new Color(0.04f, 0.95f, 0.85f, 1f),    // 荧光青碧
                glowColor = new Color(0.06f, 0.78f, 0.96f, 1f),
                coreColor = new Color(0.86f, 1.00f, 0.98f, 1f),
                accentColor = new Color(1.00f, 0.24f, 0.68f, 1f),
                bigPrefixCount = 3
            };
        }
        if (p == 2)
        {
            return new PosterTheme
            {
                bgColor = new Color(0.015f, 0.21f, 0.20f, 1f),     // 深幽墨翠松石
                neonColor = new Color(1.00f, 0.34f, 0.10f, 1f),    // 炽烈珊瑚朱橙
                glowColor = new Color(1.00f, 0.24f, 0.06f, 1f),
                coreColor = new Color(1.00f, 0.90f, 0.78f, 1f),
                accentColor = new Color(0.28f, 0.90f, 1.00f, 1f),
                bigPrefixCount = 3
            };
        }
        return new PosterTheme
        {
            bgColor = new Color(0.12f, 0.04f, 0.26f, 1f),          // 暮夜暗靛紫（高贵颁奖系）
            neonColor = new Color(1.00f, 0.74f, 0.12f, 1f),        // 荧光耀金
            glowColor = new Color(1.00f, 0.56f, 0.08f, 1f),
            coreColor = new Color(1.00f, 0.96f, 0.84f, 1f),
            accentColor = new Color(0.24f, 0.94f, 0.92f, 1f),
            bigPrefixCount = 1
        };
    }

    public static Texture2D GetPoster(int index)
    {
        int clamped = Mathf.Clamp(index, 0, 2);
        if (Cached[clamped] != null) return Cached[clamped];

        PosterTheme theme = BuildPalette(clamped);
        theme.digits = clamped == 0 ? "19" : clamped == 1 ? "20" : "21";
        theme.titleText = theme.digits + "Century";
        theme.motifType = clamped;
        theme.bigPrefixCount = 3;

        Cached[clamped] = RenderPosterTexture(768, 248, theme, 1007 + clamped * 313);
        return Cached[clamped];
    }

    struct StrokeSeg
    {
        public Vector2 a, b;
        public float halfWidth;
    }

    static Texture2D RenderPosterTexture(int w, int h, PosterTheme theme, int seed)
    {
        var pixels = new Color[w * h];
        // 填充背景并带极其轻微的径向暗角，衬托中心霓虹聚光感
        Vector2 center = new Vector2(w * 0.5f, h * 0.52f);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float nx = (x - center.x) / (w * 0.55f);
                float ny = (y - center.y) / (h * 0.7f);
                float vign = Mathf.Clamp01(1f - (nx * nx + ny * ny) * 0.14f);
                pixels[y * w + x] = new Color(
                    theme.bgColor.r * (0.86f + 0.14f * vign),
                    theme.bgColor.g * (0.86f + 0.14f * vign),
                    theme.bgColor.b * (0.88f + 0.12f * vign),
                    1f
                );
            }
        }

        // 收集主体文字矢量路径与装饰线条（统一以画布像素为坐标，左下角为原点）
        string label = string.IsNullOrEmpty(theme.titleText) ? (theme.digits + "Century") : theme.titleText;
        var textPaths = new List<List<Vector2>>();
        BuildTextPaths(label, theme.bigPrefixCount, w, h, textPaths);

        var decoNeonPaths = new List<List<Vector2>>();
        var decoAccentPaths = new List<List<Vector2>>();
        BuildMotifAndDecoPaths(theme.motifType, w, h, decoNeonPaths, decoAccentPaths);

        float rs = h / 248f; // 分辨率缩放因子，确保小徽章尺寸下描边与光晕不糊团

        // 1. 最底层：手绘黑色起稿速写轮廓线（多遍错位，构成强烈纸面立体厚度）
        Color sketchInk = new Color(0.015f, 0.015f, 0.03f, 1f);
        DrawSketchPass(pixels, w, h, textPaths, new Vector2(7f, -5.5f) * rs, 1.45f * rs, sketchInk, 0.90f, seed + 1);
        DrawSketchPass(pixels, w, h, textPaths, new Vector2(-4.5f, 3.5f) * rs, 1.20f * rs, sketchInk, 0.62f, seed + 2);
        DrawSketchPass(pixels, w, h, textPaths, new Vector2(10.5f, -8.5f) * rs, 1.05f * rs, sketchInk, 0.48f, seed + 3);
        DrawSketchPass(pixels, w, h, decoNeonPaths, new Vector2(5.5f, -4.5f) * rs, 1.20f * rs, sketchInk, 0.72f, seed + 4);
        // 偏置黑色线框投影五角星（置于两侧空旷区，不碰主字）
        DrawStandaloneStar(pixels, w, h, new Vector2(w * 0.915f, h * 0.64f), 13.5f * rs, sketchInk, 1.45f * rs, 0.88f, -0.15f);
        DrawStandaloneStar(pixels, w, h, new Vector2(w * 0.125f, h * 0.16f), 11.5f * rs, sketchInk, 1.35f * rs, 0.86f, 0.24f);

        // 2. 次层：柔和大范围霓虹扩散光晕（Screen Glow）
        DrawGlowPass(pixels, w, h, textPaths, 14.0f * rs, theme.glowColor, 0.56f);
        DrawGlowPass(pixels, w, h, decoNeonPaths, 9.5f * rs, theme.glowColor, 0.40f);
        DrawGlowPass(pixels, w, h, decoAccentPaths, 8.0f * rs, theme.accentColor, 0.32f);

        // 3. 中层：高饱和霓虹多重涂鸦描边（厚实主笔 + 两侧手绘排线涂抹笔触）
        DrawNeonStrokePass(pixels, w, h, textPaths, Vector2.zero, 4.3f * rs, theme.neonColor, 0.97f, seed + 11);
        DrawNeonStrokePass(pixels, w, h, textPaths, new Vector2(-2.2f, 1.8f) * rs, 2.3f * rs, Color.Lerp(theme.neonColor, theme.coreColor, 0.25f), 0.76f, seed + 12);
        DrawNeonStrokePass(pixels, w, h, textPaths, new Vector2(2.1f, -1.6f) * rs, 1.4f * rs, theme.neonColor, 0.65f, seed + 16);
        DrawNeonStrokePass(pixels, w, h, decoNeonPaths, Vector2.zero, 2.3f * rs, theme.neonColor, 0.95f, seed + 13);
        DrawNeonStrokePass(pixels, w, h, decoNeonPaths, new Vector2(1.4f, -1.0f) * rs, 1.25f * rs, theme.neonColor, 0.50f, seed + 14);
        DrawNeonStrokePass(pixels, w, h, decoAccentPaths, Vector2.zero, 2.8f * rs, theme.accentColor, 0.96f, seed + 15);

        // 4. 最顶层：双笔白炽发光高亮细芯，打造强烈内发光手绘涂鸦质感
        DrawNeonStrokePass(pixels, w, h, textPaths, new Vector2(-0.5f, 0.5f) * rs, 1.45f * rs, theme.coreColor, 0.96f, seed + 21);
        DrawNeonStrokePass(pixels, w, h, textPaths, new Vector2(1.2f, -0.8f) * rs, 0.75f * rs, theme.coreColor, 0.62f, seed + 23);
        DrawNeonStrokePass(pixels, w, h, decoNeonPaths, Vector2.zero, 0.95f * rs, theme.coreColor, 0.88f, seed + 22);

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.DontSave,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        tex.SetPixels(pixels);
        tex.Apply(false, true);
        return tex;
    }

    // ---------- 手写动感字形矢量定义与变形排版 ----------
    static void BuildTextPaths(string word, int bigPrefixCount, int w, int h, List<List<Vector2>> outPaths)
    {
        if (string.IsNullOrEmpty(word)) return;
        // 为每个字符计算字宽并排布为一体流利倾仰曲线
        float totalAdvance = 0f;
        var advances = new float[word.Length];
        for (int i = 0; i < word.Length; i++)
        {
            advances[i] = CharAdvance(word[i]);
            totalAdvance += advances[i];
        }

        float scaleY = h * 0.47f;
        float charH = scaleY;
        float maxSpanW = w * 0.68f;
        float scaleX = Mathf.Min(charH * 0.62f, maxSpanW / Mathf.Max(0.5f, totalAdvance));
        float actualTextW = totalAdvance * scaleX;
        float startX = (w - actualTextW) * 0.5f - charH * 0.06f;
        float baseY = h * 0.27f;
        float cursor = 0f;

        for (int i = 0; i < word.Length; i++)
        {
            char ch = word[i];
            float adv = advances[i];
            bool isBigPrefix = i < bigPrefixCount;
            float localScale = isBigPrefix ? 1.12f : 0.95f;
            float liftY = (cursor / Mathf.Max(0.1f, totalAdvance)) * (h * 0.055f); // 整体向右上方飘逸飞扬

            var rawGlyph = GetGlyphStrokes(ch);
            for (int s = 0; s < rawGlyph.Count; s++)
            {
                var pts = rawGlyph[s];
                var worldPts = new List<Vector2>(pts.Count);
                for (int p = 0; p < pts.Count; p++)
                {
                    float gx = pts[p].x;
                    float gy = pts[p].y;
                    // 手写右侧斜切 shear + 上扬弧线
                    float px = startX + (cursor + gx * adv * 0.92f) * scaleX + gy * charH * localScale * 0.19f;
                    float py = baseY + liftY + gy * charH * localScale;
                    worldPts.Add(new Vector2(px, py));
                }
                outPaths.Add(worldPts);
            }
            cursor += adv;
        }
    }

    static float CharAdvance(char ch)
    {
        switch (ch)
        {
            case ' ': return 0.45f;
            case '!': return 0.48f;
            case 'I': return 0.52f;
            case '1': return 0.72f;
            case 't':
            case 'r': return 0.56f;
            case 'L': return 0.74f;
            case 'e': return 0.76f;
            case 'y': return 0.78f;
            case 'n':
            case 'u': return 0.80f;
            case 'E': return 0.82f;
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8': return 0.88f;
            case '0':
            case 'D':
            case 'R':
            case 'Y': return 0.90f;
            case '9':
            case 'A':
            case 'C': return 0.94f;
            case 'W': return 1.12f;
            default: return 0.78f;
        }
    }

    static List<List<Vector2>> GetGlyphStrokes(char ch)
    {
        var strokes = new List<List<Vector2>>();
        switch (ch)
        {
            case ' ':
                break;
            case '!':
                strokes.Add(Poly(V(0.56f, 0.98f), V(0.46f, 0.26f)));
                strokes.Add(EllipseArc(V(0.44f, 0.06f), 0.07f, 0.07f, 0f, Mathf.PI * 2f + 0.1f, 10, 0f));
                break;
            case '0':
                strokes.Add(Cubic(V(0.62f, 0.98f), V(0.14f, 0.98f), V(0.04f, 0.38f), V(0.28f, 0.08f), 14));
                strokes.Add(Cubic(V(0.28f, 0.08f), V(0.54f, -0.10f), V(0.94f, 0.48f), V(0.62f, 0.98f), 14));
                break;
            case '1':
                strokes.Add(Poly(V(0.16f, 0.72f), V(0.56f, 0.98f), V(0.49f, 0.02f)));
                break;
            case '2':
                strokes.Add(Cubic(V(0.14f, 0.76f), V(0.32f, 1.06f), V(0.92f, 0.98f), V(0.76f, 0.56f), 14));
                strokes.Add(Poly(V(0.76f, 0.56f), V(0.12f, 0.04f), V(0.88f, 0.06f)));
                break;
            case '3':
                strokes.Add(Cubic(V(0.16f, 0.86f), V(0.56f, 1.06f), V(0.92f, 0.74f), V(0.44f, 0.54f), 14));
                strokes.Add(Cubic(V(0.44f, 0.54f), V(0.94f, 0.42f), V(0.86f, -0.06f), V(0.14f, 0.10f), 14));
                break;
            case '4':
                strokes.Add(Poly(V(0.72f, 0.98f), V(0.10f, 0.36f), V(0.92f, 0.38f)));
                strokes.Add(Poly(V(0.72f, 0.98f), V(0.64f, 0.00f)));
                break;
            case '5':
                strokes.Add(Poly(V(0.84f, 0.96f), V(0.22f, 0.96f), V(0.16f, 0.54f)));
                strokes.Add(Cubic(V(0.16f, 0.54f), V(0.74f, 0.66f), V(0.94f, 0.14f), V(0.14f, 0.04f), 14));
                break;
            case '6':
                strokes.Add(Cubic(V(0.78f, 0.92f), V(0.22f, 1.02f), V(0.04f, 0.24f), V(0.44f, 0.02f), 14));
                strokes.Add(Cubic(V(0.44f, 0.02f), V(0.86f, -0.04f), V(0.90f, 0.56f), V(0.20f, 0.44f), 14));
                break;
            case '7':
                strokes.Add(Poly(V(0.14f, 0.94f), V(0.88f, 0.98f), V(0.32f, 0.00f)));
                strokes.Add(Poly(V(0.36f, 0.48f), V(0.72f, 0.50f)));
                break;
            case '8':
                strokes.Add(Cubic(V(0.52f, 0.52f), V(0.12f, 0.64f), V(0.18f, 1.02f), V(0.56f, 0.98f), 12));
                strokes.Add(Cubic(V(0.56f, 0.98f), V(0.92f, 0.94f), V(0.86f, 0.58f), V(0.48f, 0.52f), 12));
                strokes.Add(Cubic(V(0.48f, 0.52f), V(0.08f, 0.38f), V(0.12f, -0.04f), V(0.52f, 0.02f), 12));
                strokes.Add(Cubic(V(0.52f, 0.02f), V(0.94f, 0.06f), V(0.90f, 0.44f), V(0.50f, 0.52f), 12));
                break;
            case '9':
                strokes.Add(Cubic(V(0.80f, 0.84f), V(0.34f, 1.08f), V(0.02f, 0.72f), V(0.14f, 0.48f), 14));
                strokes.Add(Cubic(V(0.14f, 0.48f), V(0.28f, 0.30f), V(0.76f, 0.46f), V(0.84f, 0.88f), 12));
                strokes.Add(Cubic(V(0.86f, 0.94f), V(0.76f, 0.52f), V(0.68f, 0.22f), V(0.60f, 0.00f), 12));
                break;
            case 'A':
                strokes.Add(Poly(V(0.08f, 0.00f), V(0.50f, 1.00f), V(0.90f, 0.00f)));
                strokes.Add(Poly(V(0.22f, 0.36f), V(0.76f, 0.40f)));
                break;
            case 'C':
                strokes.Add(Cubic(V(0.88f, 0.76f), V(0.78f, 1.06f), V(0.14f, 1.00f), V(0.08f, 0.46f), 16));
                strokes.Add(Cubic(V(0.08f, 0.46f), V(0.02f, 0.02f), V(0.54f, -0.08f), V(0.86f, 0.18f), 14));
                break;
            case 'D':
                strokes.Add(Poly(V(0.16f, 0.98f), V(0.14f, 0.02f)));
                strokes.Add(Cubic(V(0.14f, 0.96f), V(0.96f, 1.04f), V(0.98f, 0.02f), V(0.14f, 0.04f), 16));
                break;
            case 'E':
                strokes.Add(Poly(V(0.82f, 0.98f), V(0.18f, 0.96f), V(0.14f, 0.02f), V(0.84f, 0.04f)));
                strokes.Add(Poly(V(0.16f, 0.52f), V(0.68f, 0.54f)));
                break;
            case 'I':
                strokes.Add(Poly(V(0.26f, 0.98f), V(0.76f, 0.98f)));
                strokes.Add(Poly(V(0.52f, 0.98f), V(0.46f, 0.02f)));
                strokes.Add(Poly(V(0.22f, 0.02f), V(0.74f, 0.04f)));
                break;
            case 'L':
                strokes.Add(Poly(V(0.20f, 0.98f), V(0.14f, 0.04f), V(0.84f, 0.06f)));
                break;
            case 'R':
                strokes.Add(Poly(V(0.14f, 0.98f), V(0.12f, 0.00f)));
                strokes.Add(Cubic(V(0.14f, 0.96f), V(0.92f, 1.02f), V(0.88f, 0.48f), V(0.14f, 0.48f), 14));
                strokes.Add(Poly(V(0.42f, 0.48f), V(0.86f, 0.00f)));
                break;
            case 'W':
                strokes.Add(Poly(V(0.06f, 0.98f), V(0.26f, 0.02f), V(0.50f, 0.64f), V(0.72f, 0.02f), V(0.96f, 0.98f)));
                break;
            case 'Y':
                strokes.Add(Poly(V(0.10f, 0.98f), V(0.48f, 0.48f), V(0.88f, 0.98f)));
                strokes.Add(Poly(V(0.48f, 0.48f), V(0.44f, 0.00f)));
                break;
            case 'e':
                strokes.Add(Poly(V(0.12f, 0.38f), V(0.84f, 0.40f)));
                strokes.Add(Cubic(V(0.84f, 0.40f), V(0.86f, 0.78f), V(0.22f, 0.80f), V(0.12f, 0.40f), 14));
                strokes.Add(Cubic(V(0.12f, 0.40f), V(0.06f, 0.02f), V(0.54f, -0.06f), V(0.82f, 0.14f), 12));
                break;
            case 'n':
                strokes.Add(Poly(V(0.14f, 0.72f), V(0.10f, 0.00f)));
                strokes.Add(Cubic(V(0.12f, 0.44f), V(0.30f, 0.80f), V(0.76f, 0.78f), V(0.78f, 0.38f), 12));
                strokes.Add(Poly(V(0.78f, 0.38f), V(0.80f, 0.00f)));
                break;
            case 't':
                strokes.Add(Cubic(V(0.46f, 0.94f), V(0.42f, 0.48f), V(0.42f, 0.02f), V(0.80f, 0.06f), 14));
                strokes.Add(Poly(V(0.10f, 0.64f), V(0.84f, 0.68f)));
                break;
            case 'u':
                strokes.Add(Cubic(V(0.16f, 0.72f), V(0.08f, 0.18f), V(0.28f, -0.04f), V(0.74f, 0.18f), 14));
                strokes.Add(Poly(V(0.80f, 0.72f), V(0.76f, 0.00f)));
                break;
            case 'r':
                strokes.Add(Poly(V(0.20f, 0.72f), V(0.16f, 0.00f)));
                strokes.Add(Cubic(V(0.18f, 0.46f), V(0.32f, 0.74f), V(0.68f, 0.76f), V(0.88f, 0.60f), 10));
                break;
            case 'y':
                strokes.Add(Cubic(V(0.12f, 0.72f), V(0.14f, 0.14f), V(0.46f, 0.08f), V(0.78f, 0.36f), 12));
                strokes.Add(Cubic(V(0.84f, 0.72f), V(0.72f, 0.16f), V(0.56f, -0.32f), V(0.06f, -0.34f), 14));
                break;
        }
        return strokes;
    }

    // ---------- 顶部时代穹顶/星轨光环 & 周围涂鸦元素 ----------
    static void BuildMotifAndDecoPaths(int motifType, int w, int h, List<List<Vector2>> neonPaths, List<List<Vector2>> accentPaths)
    {
        float rs = h / 248f;
        // 1. 顶部中央穹顶/时代标志环（上移并微收尺寸，悬停在文字上方不干涉字母）
        Vector2 domeC = new Vector2(w * 0.56f, h * 0.755f);
        float rx = 60f * rs, ry = 42f * rs;
        if (motifType == 0)
        {
            // I代：经典水晶宫/核热推进穹顶拱顶 + 斜椭圆光环
            neonPaths.Add(EllipseArc(domeC, rx, ry, -0.05f, Mathf.PI + 0.05f, 18, 0.08f));
            neonPaths.Add(EllipseArc(domeC, rx * 0.52f, ry * 0.95f, 0.15f, Mathf.PI - 0.15f, 12, 0.08f));
            neonPaths.Add(EllipseArc(new Vector2(domeC.x, domeC.y + ry + 5f * rs), 7.5f * rs, 6f * rs, -0.2f, Mathf.PI + 0.2f, 10, 0.08f));
        }
        else if (motifType == 1)
        {
            // II代：双菱形聚变磁轨拱弧 + 椭圆星环
            neonPaths.Add(Poly(
                new Vector2(domeC.x - 46f * rs, domeC.y + 4f * rs),
                new Vector2(domeC.x - 20f * rs, domeC.y + 42f * rs),
                new Vector2(domeC.x + 24f * rs, domeC.y + 42f * rs),
                new Vector2(domeC.x + 48f * rs, domeC.y + 6f * rs)
            ));
            neonPaths.Add(Poly(
                new Vector2(domeC.x - 36f * rs, domeC.y + 49f * rs),
                new Vector2(domeC.x + 40f * rs, domeC.y + 49f * rs)
            ));
        }
        else if (motifType == 2)
        {
            // III代：曲速流线型鼻锥弧 + 双星轨信号波
            neonPaths.Add(Cubic(
                new Vector2(domeC.x - 58f * rs, domeC.y + 2f * rs),
                new Vector2(domeC.x - 26f * rs, domeC.y + 54f * rs),
                new Vector2(domeC.x + 36f * rs, domeC.y + 50f * rs),
                new Vector2(domeC.x + 60f * rs, domeC.y + 8f * rs), 16));
            neonPaths.Add(Poly(
                new Vector2(domeC.x - 32f * rs, domeC.y + 22f * rs),
                new Vector2(domeC.x + 40f * rs, domeC.y + 28f * rs)));
        }
        else if (motifType == 3)
        {
            // 授勋大赏：星轨三峰荣冠 + 月桂星轨光弧
            neonPaths.Add(Poly(
                new Vector2(domeC.x - 44f * rs, domeC.y + 6f * rs),
                new Vector2(domeC.x - 36f * rs, domeC.y + 34f * rs),
                new Vector2(domeC.x - 14f * rs, domeC.y + 18f * rs),
                new Vector2(domeC.x, domeC.y + 44f * rs),
                new Vector2(domeC.x + 16f * rs, domeC.y + 18f * rs),
                new Vector2(domeC.x + 38f * rs, domeC.y + 34f * rs),
                new Vector2(domeC.x + 46f * rs, domeC.y + 6f * rs)
            ));
        }
        else if (motifType == 4)
        {
            // 星航名录：双翼飞轮徽记 + 冲刺速度线
            neonPaths.Add(EllipseArc(new Vector2(domeC.x, domeC.y + 20f * rs), 20f * rs, 20f * rs, 0f, Mathf.PI * 2f + 0.1f, 20, 0f));
            neonPaths.Add(Poly(new Vector2(domeC.x - 24f * rs, domeC.y + 24f * rs), new Vector2(domeC.x - 66f * rs, domeC.y + 34f * rs)));
            neonPaths.Add(Poly(new Vector2(domeC.x - 22f * rs, domeC.y + 14f * rs), new Vector2(domeC.x - 54f * rs, domeC.y + 20f * rs)));
            neonPaths.Add(Poly(new Vector2(domeC.x + 24f * rs, domeC.y + 24f * rs), new Vector2(domeC.x + 68f * rs, domeC.y + 34f * rs)));
            neonPaths.Add(Poly(new Vector2(domeC.x + 22f * rs, domeC.y + 14f * rs), new Vector2(domeC.x + 56f * rs, domeC.y + 20f * rs)));
        }
        else
        {
            // 全通纪念章：中置大五角星芒
            neonPaths.Add(StarPath(new Vector2(domeC.x - 4f * rs, domeC.y + 22f * rs), 26f * rs, 10f * rs, 0.12f));
        }
        // 共用斜切星环，环绕顶部徽章
        neonPaths.Add(EllipseArc(new Vector2(domeC.x + 3f * rs, domeC.y + 12f * rs), 94f * rs, 14f * rs, -0.18f, Mathf.PI + 0.25f, 22, 0.16f));

        // 2. 贯穿文字下方的倾斜大星轨半环（收窄弧角避免右端弯折突兀）+ `//` 双斜线交叉 + 正下方闪电折线
        Vector2 orbitC = new Vector2(w * 0.50f, h * 0.35f);
        neonPaths.Add(EllipseArc(orbitC, w * 0.37f, h * 0.115f, Mathf.PI * 0.96f, Mathf.PI * 1.98f, 36, 0.08f));
        neonPaths.Add(EllipseArc(new Vector2(orbitC.x + 2f * rs, orbitC.y - 3f * rs), w * 0.355f, h * 0.105f, Mathf.PI * 1.04f, Mathf.PI * 1.93f, 30, 0.08f));

        // 底部星轨环左右交叉小斜线 `//`
        float lx1 = w * 0.36f, ly1 = h * 0.17f;
        neonPaths.Add(Poly(new Vector2(lx1 - 7f * rs, ly1 - 10f * rs), new Vector2(lx1 + 2f * rs, ly1 + 11f * rs)));
        neonPaths.Add(Poly(new Vector2(lx1 + 3f * rs, ly1 - 10f * rs), new Vector2(lx1 + 12f * rs, ly1 + 11f * rs)));
        float lx2 = w * 0.665f, ly2 = h * 0.23f;
        neonPaths.Add(Poly(new Vector2(lx2 - 7f * rs, ly2 - 10f * rs), new Vector2(lx2 + 2f * rs, ly2 + 11f * rs)));
        neonPaths.Add(Poly(new Vector2(lx2 + 3f * rs, ly2 - 10f * rs), new Vector2(lx2 + 12f * rs, ly2 + 11f * rs)));

        // 正下方 M/Z 闪电折线
        neonPaths.Add(Poly(
            new Vector2(w * 0.43f, h * 0.11f),
            new Vector2(w * 0.485f, h * 0.195f),
            new Vector2(w * 0.495f, h * 0.095f),
            new Vector2(w * 0.555f, h * 0.205f),
            new Vector2(w * 0.565f, h * 0.12f),
            new Vector2(w * 0.605f, h * 0.175f)
        ));

        // 3. 左右最外侧放射短脉冲线条（彻底错开文字和椭圆环）
        neonPaths.Add(Poly(new Vector2(w * 0.085f, h * 0.72f), new Vector2(w * 0.118f, h * 0.65f)));
        neonPaths.Add(Poly(new Vector2(w * 0.118f, h * 0.79f), new Vector2(w * 0.138f, h * 0.70f)));
        neonPaths.Add(Poly(new Vector2(w * 0.150f, h * 0.81f), new Vector2(w * 0.160f, h * 0.72f)));

        neonPaths.Add(Poly(new Vector2(w * 0.885f, h * 0.47f), new Vector2(w * 0.935f, h * 0.49f)));
        neonPaths.Add(Poly(new Vector2(w * 0.885f, h * 0.40f), new Vector2(w * 0.935f, h * 0.37f)));

        // 4. 错位平行短涂鸦双杠（左上方空白处 + 右下方空白处）
        accentPaths.Add(Poly(new Vector2(w * 0.225f, h * 0.86f), new Vector2(w * 0.265f, h * 0.88f)));
        neonPaths.Add(Poly(new Vector2(w * 0.205f, h * 0.815f), new Vector2(w * 0.245f, h * 0.835f)));
        neonPaths.Add(Poly(new Vector2(w * 0.745f, h * 0.13f), new Vector2(w * 0.785f, h * 0.13f)));
        accentPaths.Add(Poly(new Vector2(w * 0.720f, h * 0.095f), new Vector2(w * 0.760f, h * 0.095f)));

        // 5. 发光五角星（右上与左下外侧空阔区）与空心小圆圈散点（绝不压文字）
        neonPaths.Add(StarPath(new Vector2(w * 0.875f, h * 0.78f), 20f * rs, 8.5f * rs, 0.22f));
        neonPaths.Add(StarPath(new Vector2(w * 0.082f, h * 0.27f), 19f * rs, 8f * rs, -0.18f));

        Vector2[] circles =
        {
            new Vector2(w * 0.195f, h * 0.89f),
            new Vector2(w * 0.305f, h * 0.88f),
            new Vector2(w * 0.885f, h * 0.56f),
            new Vector2(w * 0.290f, h * 0.11f),
            new Vector2(w * 0.855f, h * 0.16f)
        };
        float[] radii = { 8.5f, 6.2f, 6.5f, 7.2f, 7.5f };
        for (int c = 0; c < circles.Length; c++)
            neonPaths.Add(EllipseArc(circles[c], radii[c] * rs, radii[c] * rs, 0f, Mathf.PI * 2f + 0.12f, 16, 0f));
    }

    static void DrawStandaloneStar(Color[] px, int w, int h, Vector2 center, float r, Color col, float width, float alpha, float rot)
    {
        var single = new List<List<Vector2>> { StarPath(center, r, r * 0.42f, rot) };
        DrawNeonStrokePass(px, w, h, single, Vector2.zero, width, col, alpha, 777);
    }

    // ---------- 高速 SDF 光栅化渲染通道 ----------
    static void DrawSketchPass(Color[] px, int w, int h, List<List<Vector2>> paths, Vector2 offset, float width, Color col, float alpha, int seed)
    {
        for (int i = 0; i < paths.Count; i++)
        {
            var pts = paths[i];
            for (int j = 0; j < pts.Count - 1; j++)
            {
                float jx1 = (Hash01(seed + i * 97 + j * 13) - 0.5f) * 3.2f;
                float jy1 = (Hash01(seed + i * 53 + j * 29) - 0.5f) * 2.6f;
                float jx2 = (Hash01(seed + i * 97 + (j + 1) * 13) - 0.5f) * 3.2f;
                float jy2 = (Hash01(seed + i * 53 + (j + 1) * 29) - 0.5f) * 2.6f;
                RasterSegmentBlend(px, w, h, pts[j] + offset + new Vector2(jx1, jy1), pts[j + 1] + offset + new Vector2(jx2, jy2), width, col, alpha, false);
            }
        }
    }

    static void DrawNeonStrokePass(Color[] px, int w, int h, List<List<Vector2>> paths, Vector2 offset, float width, Color col, float alpha, int seed)
    {
        for (int i = 0; i < paths.Count; i++)
        {
            var pts = paths[i];
            for (int j = 0; j < pts.Count - 1; j++)
            {
                float jx1 = (Hash01(seed + i * 41 + j * 7) - 0.5f) * 1.1f;
                float jy1 = (Hash01(seed + i * 67 + j * 11) - 0.5f) * 1.0f;
                float jx2 = (Hash01(seed + i * 41 + (j + 1) * 7) - 0.5f) * 1.1f;
                float jy2 = (Hash01(seed + i * 67 + (j + 1) * 11) - 0.5f) * 1.0f;
                RasterSegmentBlend(px, w, h, pts[j] + offset + new Vector2(jx1, jy1), pts[j + 1] + offset + new Vector2(jx2, jy2), width, col, alpha, false);
            }
        }
    }

    static void DrawGlowPass(Color[] px, int w, int h, List<List<Vector2>> paths, float radius, Color col, float maxAlpha)
    {
        // 用独立亮度缓冲聚合光晕（避免线段交界处反复叠加产生亮斑），再统一 Screen 混合到底图
        var glow = new float[w * h];
        for (int i = 0; i < paths.Count; i++)
        {
            var pts = paths[i];
            for (int j = 0; j < pts.Count - 1; j++)
            {
                Vector2 a = pts[j], b = pts[j + 1];
                int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x) - radius));
                int maxX = Mathf.Min(w - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x) + radius));
                int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y) - radius));
                int maxY = Mathf.Min(h - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y) + radius));
                Vector2 ab = b - a;
                float lenSq = Mathf.Max(0.0001f, ab.sqrMagnitude);
                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        float t = Mathf.Clamp01(Vector2.Dot(new Vector2(x, y) - a, ab) / lenSq);
                        float d = Vector2.Distance(new Vector2(x, y), a + ab * t);
                        if (d >= radius) continue;
                        float norm = d / radius;
                        float g = (1f - norm);
                        g = g * g * 0.85f;
                        int idx = y * w + x;
                        if (g > glow[idx]) glow[idx] = g;
                    }
                }
            }
        }

        for (int i = 0; i < pixelsLength(w, h); i++)
        {
            float a = glow[i] * maxAlpha;
            if (a <= 0.002f) continue;
            Color dst = px[i];
            // Screen 叠加发光，保持荧光饱和度不发灰
            px[i] = new Color(
                Mathf.Clamp01(dst.r + (1f - dst.r) * col.r * a),
                Mathf.Clamp01(dst.g + (1f - dst.g) * col.g * a),
                Mathf.Clamp01(dst.b + (1f - dst.b) * col.b * a),
                1f
            );
        }
    }

    static int pixelsLength(int w, int h) { return w * h; }

    static void RasterSegmentBlend(Color[] px, int w, int h, Vector2 a, Vector2 b, float halfW, Color col, float alpha, bool additive)
    {
        float margin = halfW + 1.4f;
        int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x) - margin));
        int maxX = Mathf.Min(w - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x) + margin));
        int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y) - margin));
        int maxY = Mathf.Min(h - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y) + margin));
        Vector2 ab = b - a;
        float lenSq = Mathf.Max(0.0001f, ab.sqrMagnitude);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float t = Mathf.Clamp01(Vector2.Dot(new Vector2(x, y) - a, ab) / lenSq);
                float d = Vector2.Distance(new Vector2(x, y), a + ab * t);
                float cov = Mathf.Clamp01(halfW + 0.75f - d) * alpha;
                if (cov <= 0.003f) continue;
                int idx = y * w + x;
                Color dst = px[idx];
                if (additive)
                {
                    px[idx] = new Color(Mathf.Clamp01(dst.r + col.r * cov), Mathf.Clamp01(dst.g + col.g * cov), Mathf.Clamp01(dst.b + col.b * cov), 1f);
                }
                else
                {
                    px[idx] = Color.Lerp(dst, col, cov);
                }
            }
        }
    }

    // ---------- 曲线几何构造方法 ----------
    static Vector2 V(float x, float y) { return new Vector2(x, y); }

    static List<Vector2> Poly(params Vector2[] pts) { return new List<Vector2>(pts); }

    static List<Vector2> Cubic(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, int steps)
    {
        var list = new List<Vector2>(steps + 1);
        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            float u = 1f - t;
            Vector2 pt = u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
            list.Add(pt);
        }
        return list;
    }

    static List<Vector2> EllipseArc(Vector2 c, float rx, float ry, float startRad, float endRad, int steps, float tilt)
    {
        var list = new List<Vector2>(steps + 1);
        float cosT = Mathf.Cos(tilt), sinT = Mathf.Sin(tilt);
        for (int i = 0; i <= steps; i++)
        {
            float a = Mathf.Lerp(startRad, endRad, (float)i / steps);
            float lx = Mathf.Cos(a) * rx;
            float ly = Mathf.Sin(a) * ry;
            list.Add(new Vector2(c.x + lx * cosT - ly * sinT, c.y + lx * sinT + ly * cosT));
        }
        return list;
    }

    static List<Vector2> StarPath(Vector2 c, float outerR, float innerR, float rot)
    {
        var list = new List<Vector2>(11);
        for (int i = 0; i <= 10; i++)
        {
            float a = rot + i * Mathf.PI * 0.2f + Mathf.PI * 0.5f;
            float r = (i % 2 == 0) ? outerR : innerR;
            list.Add(new Vector2(c.x + Mathf.Cos(a) * r, c.y + Mathf.Sin(a) * r));
        }
        return list;
    }

    static float Hash01(int n)
    {
        unchecked
        {
            uint x = (uint)n;
            x = (x ^ 61u) ^ (x >> 16);
            x *= 9u;
            x = x ^ (x >> 4);
            x *= 0x27d4eb2du;
            x = x ^ (x >> 15);
            return (x & 0xFFFFu) / 65535f;
        }
    }
}
