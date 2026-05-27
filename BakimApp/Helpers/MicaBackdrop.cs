using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace BakimApp.Helpers;

public static class MicaBackdrop
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

    // Backdrop types
    private const int DWMSBT_AUTO = 0;
    private const int DWMSBT_NONE = 1;
    private const int DWMSBT_MAINWINDOW = 2;  // Mica
    private const int DWMSBT_TRANSIENTWINDOW = 3;  // Acrylic
    private const int DWMSBT_TABBEDWINDOW = 4;  // Tabbed

    public static void ApplyMica(Window window)
    {
        if (window.IsLoaded)
        {
            SetMica(window);
        }
        else
        {
            window.Loaded += (s, e) => SetMica(window);
        }
    }

    private static void SetMica(Window window)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            // Set dark mode
            int darkMode = 1;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

            // Set Mica backdrop
            int backdropType = DWMSBT_MAINWINDOW;
            DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));
        }
        catch
        {
            // Silently fail if not supported
        }
    }

    public static void ApplyAcrylic(Window window)
    {
        if (window.IsLoaded)
        {
            SetAcrylic(window);
        }
        else
        {
            window.Loaded += (s, e) => SetAcrylic(window);
        }
    }

    private static void SetAcrylic(Window window)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            // Set dark mode
            int darkMode = 1;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

            // Set Acrylic backdrop
            int backdropType = DWMSBT_TRANSIENTWINDOW;
            DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));
        }
        catch
        {
            // Silently fail if not supported
        }
    }
}
