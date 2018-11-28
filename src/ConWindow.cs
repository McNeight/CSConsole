// <copyright file="ConWindow.cs" company="CSConsole Contributors">
// Copyright © 2006 Art Yerkes
// Copyright © 2018 Neil McNeight
// All rights reserved.
// Licensed under the GNU General Public License v2.0. See the LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

/// <summary>
///
/// </summary>
public class ConWindow : Form
{
    private readonly Brush[] foreBrushes;
    private readonly Brush[] backBrushes;
    private readonly Console con;
    private readonly Timer repaint;
    private readonly Wallpaper wallpaper;
    private Font conFont;
    private Bitmap dblBuffer;
    private Bitmap wallBuffer;
    private Graphics dblGraphic;
    private Point textSize;
    private bool started;
    private bool closed;
    private WindowSettings wSettings;
    private RectangleF scrollArea;
    private RectangleF scrollPos;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConWindow"/> class.
    /// </summary>
    /// <param name="con"></param>
    public ConWindow(Console con)
    {
        int i;

        this.con = con;
        this.SetStyle(ControlStyles.ResizeRedraw, true);

        this.wSettings = new WindowSettings();
        this.SizeText();

        this.repaint = new Timer
        {
            Interval = 100,
        };
        this.repaint.Tick += new EventHandler(this.OnRefreshTimer);

        this.wallpaper = new Wallpaper();

        this.foreBrushes = new Brush[con.NumColors];
        this.backBrushes = new Brush[con.NumColors];

        for (i = 0; i < this.backBrushes.Length; i++)
        {
            this.backBrushes[i] = new SolidBrush(Color.FromArgb(con.GetBackColor(i)));
            this.foreBrushes[i] = new SolidBrush(Color.FromArgb(con.GetForeColor(i)));
        }

        this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);

        this.FormClosed += new FormClosedEventHandler(this.OnClose);
        this.KeyDown += new KeyEventHandler(this.OnKeyDown);
        this.KeyUp += new KeyEventHandler(this.OnKeyUp);
        this.KeyPress += new KeyPressEventHandler(this.OnKeyPressed);
        this.ResizeEnd += new EventHandler(this.OnMoveSize);
        this.MouseDown += new MouseEventHandler(this.OnClick);
        this.Load += new EventHandler(this.OnMoveSize);
    }

    /// <summary>
    /// Gets the console window font name.
    /// </summary>
    public string FontName => this.wSettings.FontName;

    /// <summary>
    /// Gets the console window font size.
    /// </summary>
    public int FontSize => this.wSettings.FontSize;

    /// <summary>
    /// Gets the console window width adjustment.
    /// </summary>
    public int WidthAdj => this.wSettings.WidthAdj;

    /// <summary>
    /// Gets or sets settings.
    /// </summary>
    public object Settings
    {
        get => this.wSettings;
        set
        {
            this.wSettings = (WindowSettings)value;
            this.SizeText();
            this.ChangeSize();
            this.Redraw(null);
        }
    }

    /// <inheritdoc/>
    protected override bool IsInputKey(Keys data)
    {
        return true;
    }

    /// <inheritdoc/>
    protected override bool ProcessDialogKey(Keys keydata)
    {
        return true;
    }

    private void SizeText()
    {
        var testBitmap = new Bitmap(1, 1);
        var testGraphics = Graphics.FromImage(testBitmap);
        this.conFont = new Font(this.FontName, this.FontSize);
        this.textSize = this.MeasureDisplayStringWidth(testGraphics, "X", this.conFont);
        this.scrollArea =
        new RectangleF(
            this.con.ViewWidth * this.textSize.X,
            0,
            this.textSize.X,
            this.con.ViewHeight * this.textSize.Y);
    }

    private void OnPaint(object sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.DrawImage(this.dblBuffer, 0, 0);
        /* Scroll bar */
        g.DrawImage(this.wallBuffer, this.scrollArea, this.scrollArea, GraphicsUnit.Pixel);
        g.FillRectangle(this.backBrushes[this.con.CursorBack], this.scrollPos);
    }

    private void ChangeSize()
    {
        /* Make room for a simple scroll bar */
        this.ClientSize = new Size(
        (this.textSize.X * (this.con.ViewWidth + 1)) + NativeMethods.GetSystemMetrics(32),
        (this.textSize.Y * this.con.ViewHeight) + NativeMethods.GetSystemMetrics(33));
        this.dblBuffer = new Bitmap(
        this.textSize.X * this.con.ViewWidth, this.textSize.Y * this.con.ViewHeight);
        this.dblGraphic = Graphics.FromImage(this.dblBuffer);
        if (!this.started)
        {
            this.repaint.Start();
            this.Paint += new PaintEventHandler(this.OnPaint);
            this.started = true;
        }
    }

    private void OnMoveSize(object sender, EventArgs e)
    {
        this.ChangeSize();
        this.wallpaper.SizeMove(
        this.RectangleToScreen(new Rectangle(
            0,
            0,
            (this.con.ViewWidth + 1) * this.textSize.X,
            this.con.ViewHeight * this.textSize.Y)));
        this.wallBuffer = this.wallpaper.GetBackground();
        this.Redraw(null);
    }

    private void Redraw(LinkedList<Point> damage)
    {
        var g = this.CreateGraphics();
        Rectangle r;
        var single = string.Empty;

        if (this.wallBuffer == null)
        {
            return;
        }

        if (damage != null)
        {
            for (var head = damage.First; head != null; head = head.Next)
            {
                r = new Rectangle(head.Value.X * this.textSize.X, head.Value.Y * this.textSize.Y, this.textSize.X, this.textSize.Y);
                g.Clip.Union(new Region(r));
            }
        }

        if (damage == null || damage.Count > this.con.ViewWidth / 4)
        {
            g.Clip.Union(new Region(this.scrollArea));
            this.scrollPos = new RectangleF(
                this.scrollArea.Left,
                (float)this.con.ViewY / this.con.BufferHeight * this.ClientSize.Height,
                this.scrollArea.Width,
                (float)this.con.ViewHeight / this.con.BufferHeight * this.ClientSize.Height);
            /* Scroll bar */
            g.DrawImage(this.wallBuffer, this.scrollArea, this.scrollArea, GraphicsUnit.Pixel);
            g.FillRectangle(this.backBrushes[this.con.CursorBack], this.scrollPos);

            this.dblGraphic.DrawImage(this.wallBuffer, 0, 0);
            for (var i = 0; i < this.con.ViewHeight; i++)
            {
                for (var j = 0; j < this.con.ViewWidth; j++)
                {
                    int left = j * this.textSize.X, top = i * this.textSize.Y;
                    r = new Rectangle(
                        left,
                        top,
                        this.textSize.X,
                        this.textSize.Y);

                    this.dblGraphic.FillRectangle(
                        this.backBrushes[this.con.GetBackColor(j, i)],
                        r);
                    var x = single + this.con.GetCharacter(j, i);
                    this.dblGraphic.DrawString(
                        x,
                        this.conFont,
                        this.foreBrushes[this.con.GetForeColor(j, i)],
                        r.Left,
                        r.Top);
                }
            }

            g.DrawImage(this.dblBuffer, 0, 0);
        }
        else
        {
            for (var head = damage.First; head != null; head = head.Next)
            {
                int left = head.Value.X * this.textSize.X, top = head.Value.Y * this.textSize.Y;
                r = new Rectangle(
                        left,
                        top,
                        this.textSize.X,
                        this.textSize.Y);

                if (!(this.con.CursorX == head.Value.X && this.con.CursorY == head.Value.Y))
                {
                    this.dblGraphic.DrawImage(this.wallBuffer, r, r, GraphicsUnit.Pixel);
                }

                this.dblGraphic.FillRectangle(
                    this.backBrushes[this.con.GetBackColor(head.Value.X, head.Value.Y)],
                    r);
                var x = single + this.con.GetCharacter(head.Value.X, head.Value.Y);
                this.dblGraphic.DrawString(
                    x,
                    this.conFont,
                    this.foreBrushes[this.con.GetForeColor(head.Value.X, head.Value.Y)],
                    r.Left,
                    r.Top);
                g.DrawImage(this.dblBuffer, r, r, GraphicsUnit.Pixel);
            }
        }

        g.Dispose();
    }

    private void OnRefreshTimer(object sender, EventArgs e)
    {
        var change = this.con.Refresh();
        var damage = this.con.DamageList();

        if (change)
        {
            this.ChangeSize();
            this.Redraw(null);
        }
        else
        {
            this.Redraw(damage);
        }

        if (this.Text != this.con.Title)
        {
            this.Text = this.con.Title;
        }
    }

    private void OnClose(object sender, FormClosedEventArgs e)
    {
        this.closed = true;
        this.con.Destroy();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        this.DoWriteKey(true, e);
        e.Handled = true;
    }

    private void OnKeyUp(object sender, KeyEventArgs e)
    {
        this.DoWriteKey(false, e);
        e.Handled = true;
    }

    private void OnKeyPressed(object sender, KeyPressEventArgs e)
    {
        this.con.WriteKey(true, 0, 0, e.KeyChar);
    }

    private void DoWriteKey(bool down, KeyEventArgs e)
    {
        this.con.WriteKey(
            down,
            (short)(e.KeyCode & ~(Keys.Alt | Keys.Control | Keys.Shift)),
            (((e.KeyCode & Keys.Alt) == Keys.Alt) ? NativeMethods.LEFT_ALT_PRESSED : 0) | (((e.KeyCode & Keys.Control) == Keys.Control) ? NativeMethods.LEFT_CTRL_PRESSED : 0) | (((e.KeyCode & Keys.Shift) == Keys.Shift) ? NativeMethods.SHIFT_PRESSED : 0),
            '\0');
    }

    /// <summary>
    ///
    /// </summary>
    /// <remarks>
    /// Thanks: http://www.codeproject.com/cs/media/measurestring.asp
    /// https://www.codeproject.com/Articles/2118/Bypass-Graphics-MeasureString-limitations
    /// </remarks>
    /// <param name="graphics"></param>
    /// <param name="text"></param>
    /// <param name="font"></param>
    /// <returns></returns>
    public Point MeasureDisplayStringWidth(Graphics graphics, string text, Font font)
    {
        var format = new StringFormat();
        var rect = new RectangleF(0, 0, 1000, 1000);
        CharacterRange[] ranges = { new CharacterRange(0, text.Length), };
        var regions = new Region[1];

        format.SetMeasurableCharacterRanges(ranges);

        regions = graphics.MeasureCharacterRanges(text, font, rect, format);
        rect = regions[0].GetBounds(graphics);

        return new Point((int)(rect.Right + this.WidthAdj), (int)(rect.Bottom - rect.Top));
    }

    private void OnClick(object sender, MouseEventArgs e)
    {
        if (e.X > (this.con.ViewWidth * this.textSize.X) &&
            this.scrollPos != null &&
            e.Y < (this.con.ViewHeight * this.textSize.Y) - this.scrollPos.Height)
        {
            this.con.ScrollTo(e.Y / (this.ClientSize.Height - this.scrollPos.Height));
        }
    }

    /// <summary>
    ///
    /// </summary>
    public void Go()
    {
        while (!this.closed)
        {
            Application.Run(this);
        }
    }

    /// <summary>
    /// Settings for the console window.
    /// </summary>
    public class WindowSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowSettings"/> class.
        /// </summary>
        public WindowSettings()
        {
            this.FontName = "Lucida Console";
            this.FontSize = 12;
            this.WidthAdj = -2;
        }

        /// <summary>
        /// Gets or sets console window font name.
        /// </summary>
        public string FontName { get; set; }

        /// <summary>
        /// Gets or sets console window font size.
        /// </summary>
        public int FontSize { get; set; }

        /// <summary>
        /// Gets or sets console window width adjustment.
        /// </summary>
        public int WidthAdj { get; set; }
    }
}
