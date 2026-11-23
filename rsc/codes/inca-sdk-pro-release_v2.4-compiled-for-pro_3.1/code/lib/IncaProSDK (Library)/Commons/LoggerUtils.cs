using log4net.Appender;
using log4net.Repository.Hierarchy;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;
using log4net.Config;
using ArcGIS.Core.Data.UtilityNetwork.Trace;
using ArcGIS.Desktop.Framework;

namespace IGN.Inca.ArcGISPro.SDK.Commons
{
    /// <summary>
    /// Class helping the initialization of the "inca_logger_sdk" logger.
    /// 
    /// NOTA: This class was created as a workaround to fix the problem 
    ///       of automatic initialization of the logger in the class 
    ///       "AssemblyInfo.cs", the following line of code does not load 
    ///       correctly the logger configuration file, that's why this 
    ///       class has been created:
    /// 
    /// <code>([assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config", Watch = true)])</code>
    /// </summary>
    public sealed class LoggerUtils
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private LoggerUtils() { }

        private static LoggerUtils _instance;

        public static LoggerUtils GetInstance()
        {
            if (_instance == null)
            {
                _instance = new LoggerUtils();
            }
            return _instance;
        }

        /// <summary>
        /// Force the initialization of the logger
        /// </summary>
        public void InitializeLogger()
        {
            // Recherche de la config XML log4net, d'abord dans le répertoire d'exécution (Addin)...
            string executingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            // ... sinon dans le répertoire de l'application (.exe)
            if (string.IsNullOrEmpty(executingPath))
                executingPath = AppContext.BaseDirectory;
            var logConfig = new FileInfo(Path.Combine(executingPath, "log4net.config"));
            XmlConfigurator.ConfigureAndWatch(logConfig);

            // Recherche/création du dossier qui contiendra le fichier de log dans "Mes Documents", avec ou sans OneDrive
            string logFolderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ArcGIS", "Logs"
            );
            Directory.CreateDirectory(logFolderPath); // créer le dossier si absent

            // Définition du nom du fichier
            string logFilePath = Path.Combine(logFolderPath, "inca_logger_sdk.log");

            // Parcours des appenders pour remplacer le chemin du fichier
            // Remarque: la syntaxe %property{} ne semble pas fonctionner dans la v3.1 de log4net
            var hierarchy = (Hierarchy)LogManager.GetRepository();
            foreach (var appender in hierarchy.GetAppenders())
            {
                if (appender is FileAppender fileAppender)
                {
                    fileAppender.File = logFilePath;
                    fileAppender.ActivateOptions(); // IMPORTANT : applique la modification
                }
            }

            log.Debug("The logger 'inca_logger_sdk.log' has been configured.");
        }
    }
}