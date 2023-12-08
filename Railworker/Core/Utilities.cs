﻿using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.WindowsAPICodePack.Dialogs;
using Railworker.Properties;
using RWLib.Exceptions;
using RWLib.RWBlueprints.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using System.Xml;

namespace Railworker.Core
{
    public static class Utilities
    {
        internal static App App { get => (App)Application.Current; }
        internal static Logger Logger { get => App.Logger; }

        public static Action Debounce(this Action func, int milliseconds = 300)
        {
            var last = 0;
            return () =>
            {
                var current = Interlocked.Increment(ref last);
                Task.Delay(milliseconds).ContinueWith(task =>
                {
                    if (current == last) func();
                    task.Dispose();
                });
            };
        }
        public static Action<T> Debounce<T>(this Action<T> func, int milliseconds = 300)
        {
            var last = 0;
            return arg =>
            {
                var current = Interlocked.Increment(ref last);
                Task.Delay(milliseconds).ContinueWith(task =>
                {
                    if (current == last) func(arg);
                    task.Dispose();
                });
            };
        }

        internal static bool ChangeTsPath()
        {
            var valid = false;
            while (!valid)
            {
                using (var dialog = new CommonOpenFileDialog())
                {
                    dialog.Title = Language.Resources.select_ts_path;
                    dialog.IsFolderPicker = true;
                    try
                    {
                        dialog.DefaultDirectory = RWLib.RWUtils.GetTSPathFromSteamAppInRegistry();
                    }
                    catch (TSPathInRegistryNotFoundException ex)
                    {
                        Logger.Debug(ex.Message!);
                    }

                    var result = dialog.ShowDialog();
                    if (result != CommonFileDialogResult.Ok)
                    {
                        return false;
                    }
                    var path = dialog.FileName;
                    if (path == null)
                    {
                        return false;
                    }
                    var tsExe = Path.Combine(path, "RailWorks.exe");
                    if (!File.Exists(tsExe))
                    {
                        MessageBox.Show(Language.Resources.msg_ts_path_invalid, Language.Resources.msg_error, MessageBoxButton.OK, MessageBoxImage.Warning);
                        continue;
                    }
                    Settings.Default.TsPath = path;
                    Settings.Default.Save();
                    valid = true;
                }
            }
            return true;
        }

        public static string DetermineDisplayName(RWDisplayName displayName)
        {
            var lang = Settings.Default.Language;
            if (lang == "") lang = Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName;
            var langConversionTable = new Dictionary<string, string>()
            {
                { "en", "English" },
                { "fr", "French" },
                { "it", "Italian" },
                { "de", "German" },
                { "es", "Spanish" },
                { "nl", "Dutch" },
                { "pl", "Polish" },
                { "ru", "Russian" }
            };
            var convertedLang = "en";
            if (langConversionTable.ContainsKey(lang))
            {
                convertedLang = langConversionTable[lang];
            }
            string? name = displayName.GetDisplayName(convertedLang);
            if (string.IsNullOrWhiteSpace(name) == false) return name;

            var listInOrderOfPriority = new string[] {
                displayName.En,
                displayName.Fr,
                displayName.It,
                displayName.De,
                displayName.Es,
                displayName.Nl,
                displayName.Pl,
                displayName.Ru,
                displayName.Other,
                displayName.Key,
                Language.Resources.unknown_name
            };

            return listInOrderOfPriority.Where(x => string.IsNullOrWhiteSpace(x) == false).First();
        }
    }
}