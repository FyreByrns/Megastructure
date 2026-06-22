using Hexa.NET.ImGui;
using PixelEngine;

namespace TileBased;

static class ImGuiHelper {
    public static float imPreviousTop = 0;
    public static float imPreviousLeft = 0;
    public static float imPreviousRight = 0;
    public static float imPreviousBottom = 0;

    public static float imPreviousWidth = 0;
    public static float imPreviousHeight = 0;

    public static ImDrawListPtr imForeground => ImGui.GetForegroundDrawList();
    public static ImDrawListPtr imBackground => ImGui.GetBackgroundDrawList();
    public static ImDrawListPtr imWindow => ImGui.GetWindowDrawList();

    public static void imDrawRect(float x, float y, float width, float height, PixelEngine.Pixel colour, float rounding = 0, bool filled = false, bool foreground = false, bool window = false) {
        ImDrawListPtr target = imBackground;
        if (foreground) { target = imForeground; }
        if (window) { target = imWindow; }

        if (filled) {
            target.AddRectFilled(new(x, y), new(x + width, y + height), colour, rounding);
        }
        else {
            target.AddRect(new(x, y), new(x + width, y + height), colour, rounding);
        }
    }

    public static void imSprite(Sprite sprite) {
        sprite.MakeGPUTexture();
        sprite.UpdateGPUTexture();
        unsafe {
            ImGui.Image(new(texId: sprite.GPUTextureHandle), new(sprite.Width, sprite.Height));
        }
    }

    public static void imBegin(string title, nv2? position = null, nv2? size = null, float alpha = 0.5f, bool allowRepositioning = false, bool allowResizing = true, params wf[] windowFlags) {
        nv2 pos = position ?? nv2.Zero;
        nv2 siz = size ?? nv2.Zero;

        wf flags = wf.None;
        foreach (var flag in windowFlags) {
            flags |= flag;
        }

        ImGui.SetNextWindowPos(pos, allowRepositioning ? ImGuiCond.Always : ImGuiCond.Appearing);
        ImGui.SetNextWindowSize(siz, allowResizing ? ImGuiCond.Appearing : ImGuiCond.Always);
        ImGui.SetNextWindowBgAlpha(alpha);

        ImGui.Begin(title, flags);
    }
    public static void imEnd() {
        imPreviousTop = ImGui.GetWindowPos().Y;
        imPreviousLeft = ImGui.GetWindowPos().X;
        imPreviousWidth = ImGui.GetWindowSize().X;
        imPreviousHeight = ImGui.GetWindowSize().Y;
        imPreviousRight = imPreviousLeft + imPreviousWidth;
        imPreviousBottom = imPreviousTop + imPreviousHeight;
        ImGui.End();
    }
}
