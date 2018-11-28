// <copyright file="Console.cs" company="CSConsole Contributors">
// Copyright © 2006 Art Yerkes
// Copyright © 2018 Neil McNeight
// All rights reserved.
// Licensed under the GNU General Public License v2.0. See the LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;

/// <summary>
///
/// </summary>
public class Console
{
    private readonly int inHandle;
    private readonly int outHandle;
    private readonly int winHandle;
    private readonly ITaskbarList taskbarList;
    private bool cursorState;
    private int newCursorX;
    private int newCursorY;
    private int cursorCount;
    private int oldCursorX;
    private int oldCursorY;
    private IntPtr screenDataMem;
    private NativeMethods.CONSOLE_CURSOR_INFO cursorInfo;
    private NativeMethods.CONSOLE_SCREEN_BUFFER_INFO bufferInfo;
    private NativeMethods.CHAR_INFO[] screenData;
    private NativeMethods.CHAR_INFO[] oldScreenData;
    private ConsoleSettings cSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="Console"/> class.
    /// </summary>
    public unsafe Console()
    {
        NativeMethods.AllocConsole();
        this.inHandle = NativeMethods.GetStdHandle(NativeMethods.STD_INPUT_HANDLE);
        this.outHandle = NativeMethods.GetStdHandle(NativeMethods.STD_OUTPUT_HANDLE);
        this.winHandle = NativeMethods.GetConsoleWindow();
        NativeMethods.SetWindowLong(
            this.winHandle,
            -20,
            NativeMethods.GetWindowLong(this.winHandle, -20) | 0x80000);
        NativeMethods.SetLayeredWindowAttributes(
            this.winHandle,
            0,
            0x80,
            3);

        NativeMethods.GetConsoleScreenBufferInfo(this.outHandle, ref this.bufferInfo);

        this.screenData = new NativeMethods.CHAR_INFO[this.ViewHeight * this.ViewWidth];
        this.oldScreenData = new NativeMethods.CHAR_INFO[this.ViewHeight * this.ViewWidth];
        this.screenDataMem =
        Marshal.AllocHGlobal(this.ViewHeight * this.ViewWidth * sizeof(NativeMethods.CHAR_INFO));

        /* Disable Ctrl-C */
        NativeMethods.SetConsoleCtrlHandler(0, true);

        NativeMethods.ShowWindow(this.winHandle, 2);
        NativeMethods.SetWindowLong(
            this.winHandle,
            -20,
            (NativeMethods.GetWindowLong(this.winHandle, -20) | 128) & ~0x40000);

        // Blatantly stolen from https://github.com/mteinum/MultiAppLauncher
        // Remove the launching console window, but allow the CSConsole icon
        // to show in the taskbar.
        this.taskbarList = (ITaskbarList)new CoTaskbarList();
        this.taskbarList.HrInit();
        this.taskbarList.DeleteTab(new IntPtr(this.winHandle));

        this.cSettings = new ConsoleSettings();

        this.Refresh();
    }

    /// <summary>
    /// Gets 
    /// </summary>
    public int CursorPercentage => this.cursorInfo.dwSize;

    /// <summary>
    /// Gets a value indicating whether the cursor is visible or not.
    /// </summary>
    public bool CursorVisible => this.cursorInfo.bVisible;

    /// <summary>
    /// Gets 
    /// </summary>
    public string Title => System.Console.Title;

    /// <summary>
    /// Gets 
    /// </summary>
    public short ViewWidth => (short)(this.bufferInfo.srWindow.Right - this.bufferInfo.srWindow.Left + 1);

    public short ViewHeight => (short)(this.bufferInfo.srWindow.Bottom - this.bufferInfo.srWindow.Top + 1);

    public short ViewX => this.bufferInfo.srWindow.Left;

    public short ViewY => this.bufferInfo.srWindow.Top;

    public short BufferWidth => this.bufferInfo.dwSize.X;

    public short BufferHeight => this.bufferInfo.dwSize.Y;

    public int CursorX => this.newCursorX;

    public int CursorY => this.newCursorY;

    public int NumColors => 16;

    public int CursorTime => this.cSettings.CursorBlinkRate;

    public int CursorFore => this.cSettings.CursorFore;

    public int CursorBack => this.cSettings.CursorBack;

    public int DrawAlpha => this.cSettings.DrawAlpha;

    public object Settings
    {
        get => this.cSettings;
        set => this.cSettings = (ConsoleSettings)value;
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns></returns>
    public unsafe bool Refresh()
    {
        int i;
        short vx = this.ViewX,
              vy = this.ViewY,
              vw = this.ViewWidth,
              vh = this.ViewHeight,
              bw = this.BufferWidth,
              bh = this.BufferHeight;

        NativeMethods.GetConsoleScreenBufferInfo(this.outHandle, ref this.bufferInfo);
        NativeMethods.GetConsoleCursorInfo(this.inHandle, ref this.cursorInfo);

        this.newCursorX = this.bufferInfo.dwCursorPosition.X - this.ViewX;
        this.newCursorY = this.bufferInfo.dwCursorPosition.Y - this.ViewY;

        var changed = bw != this.BufferWidth || bh != this.BufferHeight ||
            vx != this.ViewX || vy != this.ViewY || vw != this.ViewWidth || vh != this.ViewHeight;

        if (vw != this.ViewWidth || vh != this.ViewHeight)
        {
            this.screenData = new NativeMethods.CHAR_INFO[this.ViewHeight * this.ViewWidth];
            this.oldScreenData = new NativeMethods.CHAR_INFO[this.ViewHeight * this.ViewWidth];
            this.screenDataMem = Marshal.AllocHGlobal(this.ViewHeight * this.ViewWidth * sizeof(NativeMethods.CHAR_INFO));
        }

        var where = new NativeMethods.COORD(0, 0);

        var what = new NativeMethods.SMALL_RECT(
            this.ViewX,
            this.ViewY,
            (short)(this.ViewX + this.ViewWidth - 1),
            (short)(this.ViewY + this.ViewHeight - 1));

        NativeMethods.ReadConsoleOutput(
            this.outHandle,
            this.screenDataMem,
            ((int)this.ViewHeight << 16) | (int)this.ViewWidth,
            0,
            ref what);

        for (i = 0; i < this.screenData.Length; i++)
        {
            this.screenData[i] = (NativeMethods.CHAR_INFO)Marshal.PtrToStructure(
                new IntPtr(this.screenDataMem.ToInt32() + (i * 4)),
                typeof(NativeMethods.CHAR_INFO));
        }

        return changed;
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns></returns>
    public LinkedList<Point> DamageList()
    {
        int row;
        var pts = new LinkedList<Point>();

        for (var i = 0; i < this.ViewHeight; i++)
        {
            row = i * this.ViewWidth;
            for (var j = 0; j < this.ViewWidth; j++)
            {
                if (!this.screenData[row + j].Equals(this.oldScreenData[row + j]))
                {
                    pts.AddLast(new Point(j, i));
                }
            }
        }

        if (pts.Count > 0 ||
           !this.cursorState ||
            this.oldCursorX != this.newCursorX ||
            this.oldCursorY != this.newCursorY)
        {
            if (this.oldCursorX != this.newCursorX || this.oldCursorY != this.newCursorY)
            {
                pts.AddLast(new Point(this.oldCursorX, this.oldCursorY));
            }

            if (--this.cursorCount < 1)
            {
                this.cursorCount = this.CursorTime;
                this.cursorState = !this.cursorState;
            }

            pts.AddLast(new Point(this.newCursorX, this.newCursorY));

            this.oldCursorX = this.newCursorX;
            this.oldCursorY = this.newCursorY;

            for (var i = 0; i < this.oldScreenData.Length; i++)
            {
                this.oldScreenData[i] = this.screenData[i];
            }
        }

        return pts;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public int GetBackColor(int x, int y)
    {
        if (y >= 0 && y < this.ViewHeight && x >= 0 && x < this.ViewWidth)
        {
            return (x == this.CursorX && y == this.CursorY && this.cursorState) ?
            this.CursorBack :
            (this.screenData[(y * this.BufferWidth) + x].Attributes >> 4) & 0xf;
        }
        else
        {
            return 0;
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public int GetBackColor(int c)
    {
        return (this.DrawAlpha << 24) | this.cSettings.ColorTable[c];
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public int GetForeColor(int x, int y)
    {
        if (y >= 0 && y < this.ViewHeight && x >= 0 && x < this.ViewWidth)
        {
            return (x == this.CursorX && y == this.CursorY && this.cursorState) ?
            this.CursorFore :
            this.screenData[(y * this.BufferWidth) + x].Attributes & 0xf;
        }
        else
        {
            return 0;
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public int GetForeColor(int c)
    {
        return (int)(0xff000000 | this.cSettings.ColorTable[c]);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public char GetCharacter(int x, int y)
    {
        if (y >= 0 && y < this.ViewHeight && x >= 0 && x < this.ViewWidth)
        {
            return this.screenData[(y * this.BufferWidth) + x].UnicodeChar;
        }
        else
        {
            return '\0';
        }
    }

    /// <summary>
    ///
    /// </summary>
    public void Destroy()
    {
        NativeMethods.SendMessage(this.winHandle, 16, 0, 0);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="percentage"></param>
    public void ScrollTo(float percentage)
    {
        var r = this.bufferInfo.srWindow;
        var newTop = (int)((this.BufferHeight - this.ViewHeight) * percentage);
        r.Top = (short)newTop;
        r.Bottom = (short)(newTop + this.ViewHeight - 1);
        NativeMethods.SetConsoleWindowInfo(this.outHandle, true, ref r);
        this.Refresh();
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="down"></param>
    /// <param name="vk"></param>
    /// <param name="shift"></param>
    /// <param name="c"></param>
    public unsafe void WriteKey(bool down, short vk, int shift, char c)
    {
        if (c != '\0')
        {
            NativeMethods.SendMessage(this.winHandle, 258, c, 0);
        }
        else
        {
            NativeMethods.SendMessage(
                this.winHandle,
                256 + (!down ? 1 : 0),
                vk,
                1 | (NativeMethods.MapVirtualKey(vk, 0) << 16) | ((down ? 0 : 1) << 30) | ((down ? 1 : 0) << 31));
        }
    }

    /// <summary>
    /// Settings for the console.
    /// </summary>
    public class ConsoleSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleSettings"/> class.
        /// </summary>
        public ConsoleSettings()
        {
            this.CursorFore = 0;
            this.CursorBack = 10;
            this.CursorBlinkRate = 5;
            this.DrawAlpha = 0xa0;

            this.ColorTable = new int[]
            {
                0x000000, 0x0000b0, 0x00b000, 0x00b0b0,
                0xb00000, 0xb000b0, 0xb0b000, 0xb0b0b0,
                0x404040, 0x4040ff, 0x40ff40, 0x40ffff,
                0xff4040, 0xff40ff, 0xffff40, 0xffffff,
            };
        }

        /// <summary>
        /// Gets or sets the cursor blink rate.
        /// </summary>
        public int CursorBlinkRate { get; set; }

        /// <summary>
        /// Gets or sets the color table.
        /// </summary>
        public int[] ColorTable { get; set; }

        /// <summary>
        /// Gets or sets the cursor foreground color.
        /// </summary>
        public int CursorFore { get; set; }

        /// <summary>
        /// Gets or sets the cursor background color.
        /// </summary>
        public int CursorBack { get; set; }

        /// <summary>
        /// Gets or sets the alpha color(?).
        /// </summary>
        public int DrawAlpha { get; set; }
    }
}
