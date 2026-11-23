//   Copyright 2019 Esri
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at

//       http://www.apache.org/licenses/LICENSE-2.0

//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License. 



using ArcGIS.Desktop.Framework;
using IncaDisplay._AttributeSets;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace IncaDisplay
{
    public partial class AttributeSetsView : UserControl
    {
        public AttributeSetsView()
        {
            InitializeComponent();
            DataContext = new AttributeSetsViewModel();
        }

        /// <summary>
        /// Action lors du clic droit sur un bouton du panneau. 
        /// Ouverture du panneau d'édition du bouton
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button btn)
            {
                if (btn == null) return;
                if (btn.DataContext is AttributeSetsRule rule)
                {
                    if (rule is null || rule.BtnName == "" || rule.BtnName is null) return;

                    // Ouvre une fenêtre modale, bloquant le reste
                    var wnd = new InfoButtonWindow(rule)
                    {
                        Owner = FrameworkApplication.Current.MainWindow
                    };
                    wnd.ShowDialog();
                }
            }
        }

             
    }
}
