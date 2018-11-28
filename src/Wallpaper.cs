// <copyright file="Wallpaper.cs" company="CSConsole Contributors">
// Copyright © 2006 Art Yerkes
// Copyright © 2018 Neil McNeight
// All rights reserved.
// Licensed under the GNU General Public License v2.0. See the LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Drawing;

using Microsoft.Win32;

internal class Wallpaper
{
    private Rectangle clientArea;

    public Wallpaper()
    {
    }

    public enum Style : int
    {
        Tiled,
        Centered,
        Stretched,
    }

    public static string WallpaperBmp
    {
        get
        {
            try
            {
                var key = Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop", true);
                return (string)key.GetValue(@"Wallpaper");
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }

    public static Style WallpaperStyle
    {
        get
        {
            var key = Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop", true);
            try
            {
                return key.GetValue(@"TileWallpaper").Equals("1") ?
                    Style.Tiled :
                    key.GetValue(@"WallpaperStyle").Equals("1") ?
                    Style.Centered : Style.Stretched;
            }
            catch (Exception)
            {
                return Style.Stretched;
            }
        }
    }

    public static Point ScreenSize => new Point(
        NativeMethods.GetSystemMetrics(0),
        NativeMethods.GetSystemMetrics(1));

    public void SizeMove(Rectangle r)
    {
        this.clientArea = r;
    }

    public Bitmap GetBackground()
    {
        if (WallpaperBmp == string.Empty)
        {
            return new Bitmap(1, 1);
        }

        var wpBitmap = new Bitmap(WallpaperBmp);
        var outBitmap = new Bitmap(this.clientArea.Width, this.clientArea.Height);
        var style = WallpaperStyle;
        var g = Graphics.FromImage(outBitmap);
        var screenSize = ScreenSize;

        if (style == Style.Stretched)
        {
            g.DrawImage(
                wpBitmap,
                new Rectangle(
                    -this.clientArea.Left,
                    -this.clientArea.Top,
                    screenSize.X,
                    screenSize.Y));
        }
        else if (style == Style.Tiled)
        {
            /* Not supported yet */
        }
        else if (style == Style.Centered)
        {
            g.DrawImage(
                wpBitmap,
                new Point(
                    -this.clientArea.Left + (screenSize.X / 2) - (this.clientArea.Width / 2),
                    -this.clientArea.Top + (screenSize.Y / 2) - (this.clientArea.Height / 2)));
        }

        return outBitmap;
    }
}
