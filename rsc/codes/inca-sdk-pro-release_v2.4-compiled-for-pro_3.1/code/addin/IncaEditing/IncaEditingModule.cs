using System;
using ArcGIS.Core.Events;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;

namespace IncaEditing
{
    internal class IncaEditingModule : Module
    {
        /// <summary>
        /// Module de l'Addin IncaEditing
        /// </summary>
        private static IncaEditingModule _this = null;

        /// <summary>
        /// Retourne une instance unique du module
        /// </summary>
        public static IncaEditingModule Current
        {
            get
            {
                return _this ?? (_this = (IncaEditingModule)FrameworkApplication.FindModule("IncaEditing_Module"));
            }
        }

        #region Overrides
        /// <summary>
        /// Called by Framework when ArcGIS Pro is closing
        /// </summary>
        /// <returns>False to prevent Pro from closing, otherwise True</returns>
        protected override bool CanUnload()
        {
            //TODO - add your business logic
            //return false to ~cancel~ Application close
            return true;
        }

        #endregion Overrides

    }

    #region Events
    /// <summary>
    /// Arguments de l'événement FileNameChangedEvent
    /// </summary>
    internal sealed class FileNameChangedEventArgs : EventArgs
    {
        internal string _value;
        internal FileNameChangedEventArgs(string value)
        {
            _value = value;
        }
    }

    /// <summary>
    /// Evénement déclenché par le changement de nom du fichier Excel
    /// </summary>
    internal class FileNameChangedEvent : CompositePresentationEvent<FileNameChangedEventArgs>
    {
        public static SubscriptionToken Subscribe(Action<FileNameChangedEventArgs> action, bool keepSubscriberAlive = false)
        {
            return FrameworkApplication.EventAggregator.GetEvent<FileNameChangedEvent>().Register(action, keepSubscriberAlive);
        }

        public static void Unsubscribe(Action<FileNameChangedEventArgs> action)
        {
            FrameworkApplication.EventAggregator.GetEvent<FileNameChangedEvent>().Unregister(action);
        }

        public static void Unsubscribe(SubscriptionToken token)
        {
            FrameworkApplication.EventAggregator.GetEvent<FileNameChangedEvent>().Unregister(token);
        }

        internal static void Publish(FileNameChangedEventArgs eventArgs)
        {
            FrameworkApplication.EventAggregator.GetEvent<FileNameChangedEvent>().Broadcast(eventArgs);
        }
    }
    #endregion Events
}
