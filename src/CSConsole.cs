// <copyright file="CSConsole.cs" company="CSConsole Contributors">
// Copyright © 2006 Art Yerkes
// Copyright © 2018 Neil McNeight
// All rights reserved.
// Licensed under the GNU General Public License v2.0. See the LICENSE file in the project root for full license information.
// </copyright>

using System.Diagnostics;

internal class CSConsole
{
    private readonly Console console;
    private readonly ConWindow conwindow;
    private readonly Settings settings;

    public CSConsole()
    {
        this.settings = Settings.Load();
        this.console = new Console();
        this.conwindow = new ConWindow(this.console);
        if (this.settings == null)
        {
            this.settings = new Settings
            {
                ConsoleSettings = this.console.Settings,
                WindowSettings = this.conwindow.Settings,
            };
            this.settings.WriteDefault();
        }
        else
        {
            this.console.Settings = this.settings.ConsoleSettings;
            this.conwindow.Settings = this.settings.WindowSettings;
        }
    }

    public static void Main(string[] args)
    {
        var c = new CSConsole();
        var p = new Process();

        if (args.Length > 0)
        {
            p.StartInfo.FileName = args[0];
        }
        else
        {
            p.StartInfo.FileName = "cmd.exe";
        }

        p.StartInfo.UseShellExecute = false;
        if (args.Length == 2)
        {
            p.StartInfo.Arguments = args[1];
        }

        p.Start();
        c.Go();
    }

    public void Go()
    {
        this.conwindow.Go();
    }
}
