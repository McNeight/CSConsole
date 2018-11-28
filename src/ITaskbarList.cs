// <copyright file="ITaskbarList.cs" company="CSConsole Contributors">
// Copyright © 2006 Art Yerkes
// Copyright © 2018 Neil McNeight
// All rights reserved.
// Licensed under the GNU General Public License v2.0. See the LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Runtime.InteropServices;

/// <summary>
/// Exposes methods that control the taskbar. It allows you to dynamically add, remove, and activate items on the taskbar.
/// </summary>
/// <remarks>
/// Blatantly stolen from https://github.com/mteinum/MultiAppLauncher/blob/master/ITaskbarList.cs .
/// </remarks>
[ComImport]
[Guid("56fdf342-fd6d-11d0-958a-006097c9a090")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface ITaskbarList
{
    /// <summary>
    /// Initializes the taskbar list object. This method must be called before any other ITaskbarList methods can be called.
    /// </summary>
    void HrInit();

    /// <summary>
    /// Adds an item to the taskbar.
    /// </summary>
    /// <param name="hWnd">A handle to the window to be added to the taskbar.</param>
    void AddTab([In] IntPtr hWnd);

    /// <summary>
    /// Deletes an item from the taskbar.
    /// </summary>
    /// <param name="hWnd">A handle to the window to be deleted from the taskbar.</param>
    void DeleteTab([In] IntPtr hWnd);

    /// <summary>
    /// Activates an item on the taskbar. The window is not actually activated; the window's item on the taskbar is merely displayed as active.
    /// </summary>
    /// <param name="hWnd">A handle to the window on the taskbar to be displayed as active.</param>
    void ActivateTab([In] IntPtr hWnd);

    /// <summary>
    /// Marks a taskbar item as active but does not visually activate it.
    /// </summary>
    /// <param name="hWnd">A handle to the window to be marked as active.</param>
    void SetActiveAlt([In] IntPtr hWnd);
}
