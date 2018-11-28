// <copyright file="Settings.cs" company="CSConsole Contributors">
// Copyright © 2006 Art Yerkes
// Copyright © 2018 Neil McNeight
// All rights reserved.
// Licensed under the GNU General Public License v2.0. See the LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.IO;
using System.Xml.Serialization;

/// <summary>
///
/// </summary>
[XmlInclude(typeof(Console.ConsoleSettings))]
[XmlInclude(typeof(ConWindow.WindowSettings))]
public class Settings
{
    public object ConsoleSettings;
    public object WindowSettings;

    /// <summary>
    /// Gets path to settings file.
    /// </summary>
    public static string SettingsFile => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\CSConsoleSettings.xml";

    /// <summary>
    ///
    /// </summary>
    /// <returns></returns>
    public static Settings Load()
    {
        // var s = new XmlSerializer(typeof(Settings));
        // https://stackoverflow.com/questions/1127431/xmlserializer-giving-filenotfoundexception-at-constructor
        var s = new XmlSerializer(typeof(Settings), typeof(Settings).GetNestedTypes());
        TextReader r;

        try
        {
            r = new StreamReader(SettingsFile);

            return (Settings)s.Deserialize(r);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>
    /// Writes out the default settings file.
    /// </summary>
    public void WriteDefault()
    {
        // var s = new XmlSerializer(typeof(Settings));
        // https://stackoverflow.com/questions/1127431/xmlserializer-giving-filenotfoundexception-at-constructor
        var s = new XmlSerializer(typeof(Settings), typeof(Settings).GetNestedTypes());
        TextWriter w;

        if (!File.Exists(SettingsFile))
        {
            w = new StreamWriter(SettingsFile);
            s.Serialize(w, this);
            w.Close();
        }
    }
}
