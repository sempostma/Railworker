using Microsoft.VisualBasic.Logging;
using Railworker.Core;
using Railworker.DataRepository;
using Railworker.Properties;
using RWLib;
using RWLib.Exceptions;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;

namespace Railworker
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        internal AppGlobals AppGlobals { get; } = new AppGlobals(new Logger());

        public Logger Logger { get => AppGlobals.Logger!; }

        public RWLibrary? RWLib { get; private set; }
        public IDataRepository? DataRepo { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                if (File.Exists("debug.log"))
                {
                    File.Delete("debug.log");
                }
            }
            catch (Exception)
            {
                Debug.Print("Unable to remove log file");
            }

            if (Settings.Default.UpgradeRequired)
            {
                Logger.Debug("Upgrade detected, migrating settings..");
                try
                {
                    Settings.Default.Upgrade();
                    Settings.Default.UpgradeRequired = false;
                }
                catch (Exception)
                {
                    Logger.Debug("Could not migrate settings!");
                }
            }

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;

            SetLanguageDictionary();

            if (Settings.Default.ReplacementRules == null) Settings.Default.ReplacementRules = new ReplacementRules();
            if (Settings.Default.VehicleVariations == null) Settings.Default.VehicleVariations = new VehicleVariations();
            if (Settings.Default.FavoriteRoutes == null) Settings.Default.FavoriteRoutes = new System.Collections.Specialized.StringCollection();

            while (Settings.Default.TsPath == "")
            {
                try
                {
                    var pathFromReg = RWUtils.GetTSPathFromSteamAppInRegistry();
                    Settings.Default.TsPath = pathFromReg;
                    Settings.Default.Save();
                    break;
                }
                catch (TSPathInRegistryNotFoundException ex)
                {
                    Logger.Debug(ex.Message!);
                }

                MessageBox.Show(Language.Resources.msg_first_time, Language.Resources.msg_message, MessageBoxButton.OK, MessageBoxImage.Information);
                var selected = Utilities.ChangeTsPath();
                if (!selected)
                {
                    MessageBox.Show(Language.Resources.msg_ts_path_required, Language.Resources.msg_message, MessageBoxButton.OK, MessageBoxImage.Information);
                    Current.Shutdown();
                    return;
                }
            }

            Logger.Debug($"ReplacementRules has {Settings.Default.ReplacementRules.List.Count} items");

            RWLib = new RWLibrary(new RWLibOptions { Logger = new Logger(), TSPath = Settings.Default.TsPath, UseCustomSerz = false });
            DataRepo = new LocalDataRepository();
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            Logger.Warning($"Uncaught exception: {e.Exception.GetType()} - {e.Exception.Message}\n{e.Exception.StackTrace}");
            Current.Shutdown();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception exception = (Exception)e.ExceptionObject;
            Logger.Warning($"Uncaught exception: {exception.Message}");
            Current.Shutdown();
        }

        internal void SetLanguageDictionary()
        {
            var lang = Settings.Default.Language;
            if (lang == "") lang = Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName;
            Logger.Debug($"Set language to {lang}");
            switch (lang)
            {
                //case "de":
                //    Language.Resources.Culture = new System.Globalization.CultureInfo("de-DE");
                //    break;
                //case "ru":
                //    Language.Resources.Culture = new System.Globalization.CultureInfo("ru-RU");
                //    break;
                //case "nl":
                //    Language.Resources.Culture = new System.Globalization.CultureInfo("nl-NL");
                //    break;
                case "en":
                default:
                    Language.Resources.Culture = new System.Globalization.CultureInfo("en-US");
                    break;
            }
        }
    }
}
