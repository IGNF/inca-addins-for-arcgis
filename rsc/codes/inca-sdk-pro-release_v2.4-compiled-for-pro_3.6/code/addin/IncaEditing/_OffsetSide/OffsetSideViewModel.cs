//   Copyright 2019 Esri
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at

//       https://www.apache.org/licenses/LICENSE-2.0

//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

using System;
using System.ComponentModel;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Controls;

using System.Resources;
using System.Text;

namespace IncaEditing._OffsetSide
{
  /// <summary>
  /// Classe permettant de gérer le panneau imbriqué de l'outil de changement de côté du décalage
  /// </summary>
  internal class OffsetSideViewModel : EmbeddableControl, INotifyPropertyChanged
  {
    public OffsetSideViewModel(XElement options, bool canChangeOptions) : base(options, canChangeOptions)
    {
        // Dictionnaire de la langue utilisée dans l'IHM ("CurrentUICulture")
        Translator = CultureResourcesManager.GetInstance().dictLanguage;
    }

    #region Propriétés

    // Valeurs par défaut
    internal const bool DefaultRepairTopology = false;
    internal const bool DefaultRealign = false;
  
    /// <summary>
    /// Traducteur de langue
    /// </summary>
    private ResourceSet Translator { get; set; }

    /// <summary>
    /// Texte de la zone d'information
    /// </summary>
    private string _instructionInfo;
    public string InstructionInfo
    {
        get { return _instructionInfo; }
        set
        {
            SetProperty(ref _instructionInfo, value, () => InstructionInfo);
        }
    }

    /// <summary>
    /// Statut de la zone d'information (Info, réussite, erreur) qui détermine le choix de l'icône et de la couleur
    /// </summary>
    private string _statusInfo;
    public string StatusInfo
    {
        get { return _statusInfo; }
        set
        {
            if (SetProperty(ref _statusInfo, value, () => StatusInfo))
            {
                if (_statusInfo == "Info")
                {
                    InstructionInfoIcon = "/IncaEditing;component/Images/GenericInformation16.png";
                    InstructionInfoColor = "#FFDFEFFA";
                }
                else if (_statusInfo == "Complete")
                {
                    InstructionInfoIcon = "/IncaEditing;component/Images/GenericComplete16.png";
                    InstructionInfoColor = "#FFE4F0E4";
                }
                else if (_statusInfo == "Error")
                {
                    InstructionInfoIcon = "/IncaEditing;component/Images/GenericError16.png";
                    InstructionInfoColor = "#FFF6CBC2";
                }
                    NotifyPropertyChanged(() => InstructionInfoIcon);
                    NotifyPropertyChanged(() => InstructionInfoColor);
            }
        }
    }

    /// <summary>
    /// Icône affichée dans la zone d'information (Info, réussite, erreur)
    /// </summary>
    private string _instructionInfoIcon;
    public string InstructionInfoIcon
    {
        get { return _instructionInfoIcon; }
        set { SetProperty(ref _instructionInfoIcon, value, () => InstructionInfoIcon); }
    }

    /// <summary>
    /// Couleur de la zone d'information (bleu, vert, rouge)
    /// </summary>
    private string _instructionInfoColor;
    public string InstructionInfoColor
    {
        get { return _instructionInfoColor; }
        set { SetProperty(ref _instructionInfoColor, value, () => InstructionInfoColor);}
    }

    /// <summary>
    /// Mode de fonctionnement par sélection actif
    /// </summary>
    private bool _bySelectionActive;
    public bool BySelectionActive
    {
        get { return _bySelectionActive; }
        set
        {
            if (SetProperty(ref _bySelectionActive, value, () => BySelectionActive))
            {
                NotifyPropertyChanged(() => RelativeToGeometryActive);
                OnMethodChanged(null);
            }
        }
    }

    /// <summary>
    /// Mode de fonctionnement par saisie actif
    /// </summary>
    public bool RelativeToGeometryActive
    {
    get { return !_bySelectionActive; }
    set { BySelectionActive = !value; }
    }

    /// <summary>
    /// Option de correction topologique du résultat
    /// </summary>
    private bool _repairTopology;
    public bool RepairTopology
    {
        get { return _repairTopology; }
        set
        {
            SetProperty(ref _repairTopology, value);
        }
    }

    /// <summary>
    /// Option de réalignement du tronçon décalé
    /// </summary>
    private bool _realign;
    public bool Realign
    {
        get { return _realign; }
        set
        {
            SetProperty(ref _realign, value);
        }
    }
    /// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ START ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    ///     One-Way Data Binding Properties, used specially for internationalization of the UI
    /// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    public string TopologyText { get { return Translator.GetString("Topology"); } }
    public string MethodText { get { return Translator.GetString("Method"); } }
    public string SnapEndPointsText { get { return Translator.GetString("SnapEndPoints"); } }
    public string RealignmentText { get { return Translator.GetString("Realign"); } }
    public string BySelectionMethodText { get { return new StringBuilder(Translator.GetString("BySelectionMethod")).ToString(); } }
    public string RelativeToReferenceGeometryMethodText { get { return Translator.GetString("RelativeToReferenceGeometryMethod"); } }
    /// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ END ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    #endregion

    #region Commande

    /// <summary>
    /// Commande permettant de lancer un calcul sur la sélection active
    /// </summary>
    private RelayCommand _bySelectionCmd;
    public ICommand BySelectionCmd { get { return _bySelectionCmd; } }
        
    /// <summary>
    /// Commande permettant d'activer le fonctionnement par saisie quand l'option d'alignement
    /// est modifiée
    /// </summary>
    public ICommand RealignmentCmd => new RelayCommand(() =>
    { RelativeToGeometryActive = true; });

    #endregion

    #region Gestion des événements

    public event EventHandler MethodChanged;
    /// <summary>
    /// Envoi d'un message pour signaler une demande de changement du mode de fonctionnement de l'outil
    /// </summary>
    protected virtual void OnMethodChanged(EventArgs e)
    {
        EventHandler eh = MethodChanged;
        if (eh != null)
            eh(this, e);
    }

    public event EventHandler BySelectionCalled;
    /// <summary>
    /// Envoi d'un message pour signaler une demande de calcul par sélection
    /// </summary>
    protected virtual void OnBySelectionCalled(EventArgs e)
    {
        EventHandler eh = BySelectionCalled;
        if (eh != null)
            eh(this, e);
    }
        #endregion

    /// <summary>
    /// Initialisation de la classe à l'ouverture
    /// </summary>
    public override Task OpenAsync()
    {
        // Valeurs des paramètres à l'ouverture
        BySelectionActive = true;
        RepairTopology = DefaultRepairTopology;
        Realign = DefaultRealign;

        // Affectation d'une fonction à la commande
        _bySelectionCmd = new RelayCommand(() => OnBySelectionCalled(null), true);
        NotifyPropertyChanged(() => BySelectionCmd);

        return base.OpenAsync();
    }
  }
}
