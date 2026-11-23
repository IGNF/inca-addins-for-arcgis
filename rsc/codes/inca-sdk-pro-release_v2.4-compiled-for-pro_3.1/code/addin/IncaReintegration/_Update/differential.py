# coding: utf-8

"""
Différentiel entre deux gdb fichier
===========================

Module de calcul d'un différentiel entre deux géodatabases fichier.

Les fonctions permettent d'obtenir les objets créés, supprimés ou modifiés d'une géodatabase à une autre

:Module: :mod:`differential`
:Auteur: Projet INCA
:Date: 23/07/2025
:Copyright: \(c\) IGN France 2017

"""

import arcpy
from datetime import datetime
import ast
import copy


def check_dataset(gdb_path):
    """Retourne le nombre de dataset d'une gdb

    Fonction qui permet de compter le nombre de dataset d'une gdb. Si 1 dataset, renvoie le nom de la gdb.
    Si plusieurs datasets renvoie un message d'erreur

    :param gdb_path: chemin absolu vers la géodatabase fichier

    :type gdb_path: str.

    :return0: la nombre de dataset présents dans la gdb
    :rtype0: int
    
    :return1: nom du dataset ou message inquidant qu'il n'y a pas de dataset, ou plusieurs datasets
    :rtype1: str
    """
    arcpy.env.workspace = gdb_path
    datasets = arcpy.ListDatasets()
    if datasets:
        if len(datasets) > 1:
            return 2, f"Erreur : La geodatabase {gdb_path} contient {len(datasets)} datasets"
        else:
            return 1, datasets[0]
    else:
        return 0, f"La geodatabase {gdb_path} ne contient aucun dataset"


#compare la structure des 3 gdb pour vérifier qu'elle ont toutes un dataset du même nom ou pas de dataset
def dataset_compare(gdb_origin_path, gdb1_path, gdb2_path):
    """Compare la structure de 3 gdb : présence d'un dataset + nom du dataset

    Fonction qui permet de compter les datasets présents dans 3 gdb. Retourne une erreur s'il
    y a plusieurs datasets dans une seule gdb ou s'il n'y a pas le même nombre de dataset dans
    les 3 gdb ou si le dataset n'a pas le même nom dans les 3 gdb.

    :param gdb_origin_path: chemin absolu vers la géodatabase fichier d'origine
    :param gdb1_path: chemin absolu vers la géodatabase fichier d'édition (gdb1)
    :param gdb2_path: chemin absolu vers la géodatabase fichier d'update (gdb2)

    :type gdb_path: str.

    :return0: Indique si les 3 gdb ont une structure de gdb valide
    :rtype0: bool
    
    :return1: nom du dataset ou message d'erreur si la structure des gdb est invalide
    :rtype1: str
    """
    dataset_origin = check_dataset(gdb_origin_path)
    if dataset_origin[0] == 2: #si plusieurs datasets dans gdb origin
        return False, dataset_origin[1]
    else:
        dataset1 = check_dataset(gdb1_path) #si plusieurs datasets dans gdb1
        if dataset1[0] == 2:
            return False, dataset1[1]
        else:
            dataset2 = check_dataset(gdb2_path) #si plusieurs datasets dans gdb2
            if dataset2[0] == 2:
                return False, dataset2[1]
            else:
                if all(x[0] == 0 for x in (dataset_origin, dataset1, dataset2)): #si les 3 gdb n'ont pas de dataset
                    return True, None
                elif dataset_origin[1] == dataset1[1] == dataset2[1].split(".")[-1]: #si les 3 gdb ont le même nom de dataset
                    return True, dataset_origin[1]
                else:
                    return False, "Les gdb n'ont pas la même structure de datasets (nom ou nombre différent)"


def gdb2dico(gdb_path):
    """Retourne une géodatabase fichier ou un dataset sous la forme d'un dictionnaire

    Fonction qui permet de lire toutes les classes d'entités d'une gdb fichier ou d'un dataset 
    sous la forme d'un dictionnaire de dictionnaires avec pour clé le nom de la classe et pour valeur 
    le dictionnaire de la classe généré par la fonction featureclass2dico

    :param gdb_path: chemin absolu vers la géodatabase fichier (ou un dataset)

    :type gdb_path: str.

    :return0: la géodatabase fichier ou le dataset sous forme de dictionnaire
    :rtype0: dict
    
    :return1: la structure de la géodatabase fichier ou du dataset sous forme de dictionnaire
              (clé = nom_classe, valeur = return1 de featureclass2dico)
    :rtype1: dict
    """
    arcpy.env.workspace = gdb_path
    # Dictionnaire pour stocker les dictionnaires par feature class
    all_features_data = {}
    all_features_structure = {}
    # Liste les Feature Classes
    feature_classes = arcpy.ListFeatureClasses()
    for fc in feature_classes:
        # chemin vers la classe fc
        fc_path = f"{gdb_path}\\{fc}"       
        # Stocker le dictionnaire dans la liste globale
        if fc == 'e_toponyme_anno_25000':
            fc2dico = featureclass2dico(fc_path, ('shape', 'shape_length', 'shape_area', 'subst_src', 'cleabs', 'symbo'))
        else :
            fc2dico = featureclass2dico(fc_path, ('shape', 'shape_length', 'shape_area', 'subst_src', 'oid_ref'))
        all_features_data[fc] = fc2dico[0]
        all_features_structure[fc] = fc2dico[1]
    return all_features_data, all_features_structure


def featureclass2dico(fc_path, champs_exclus):
    """Retourne une classe d'entité sous la forme d'un dictionnaire

    Fonction qui permet de lire une classe d'entité sous la forme d'un dictionnaire avec 
    pour clé l'OBJECTID et pour valeur une chaîne de caractère contenant tous les attributs de l'objet
    dont sa géométrie sous format WKT

    :param fc_path: chemin absolu vers la classe d'entité

    :type fc_path: str.

    :return0: la classe d'entité sous forme de dictionnaire
    :rtype0: dict
    
    :return1: la liste des tuple (nom_champ, type) de la classe d'entité
    :rtype1: list
    """
    # Récupération de la liste des champs de la classe d'entité (nom et type) en excluant les champs non désirables
    fields_type = [(f.name, f.type) for f in arcpy.ListFields(fc_path) if f.name.lower() not in champs_exclus and f.type not in ('Blob')]

    # On garde seulement les nom des champs
    fields = [f for f, _ in fields_type]
    fields.append("SHAPE@")
    fields_type.append(('shape', 'geom'))

    # On récupère chaque objet de la classe d'entité on le transforme en objet du dictionnaire (avec shape en WKT)
    features_dict = {r[0]: str(list(r[0:-1]) + [r[-1].WKT] if r[-1] else None) for r in arcpy.da.SearchCursor(fc_path, fields)}
    return features_dict, fields_type



def structureCompare(s1, s2):
    """Compare la structure de deux géodatabases

    Fonction qui permet de comparer les dictionnaires de structure de deux géodatabase (return1 de gdb2dico).
    Indique si les deux gdb ont la même structure ou s'il y a une différence (au niveau de la classe ou de ses attributs :
    noms, types...)

    :param s1: dictionnaire de structure de la première géodatabase/dataset
    :param s2: dictionnaire de structure de la deuxième géodatabase/dataset

    :type s1: dict.
    :type s2: dict.

    :return0: True si la structure ets identique False sinon
    :rtype0: bool
    
    :return1: Message précisant la différence
    :rtype1: str
    """
    #si les deux dictionnaires sont identiques
    if s1 == s2:
        return True, "ok"
    else:
        if len(s1) != len(s2):
            return False, "Nombre de classes d'entités différent"
        else:
            cles_s1 = set(s1.keys())
            cles_s2 = set(s2.keys())
            if cles_s1 != cles_s2:
                return False, f"Noms des classes d'entités différents. Les classes suivantes " \
                              f"n'ont pas de correspondance dans la gdb : {cles_s1 - cles_s2}. Les" \
                              f" classes suivantes n'ont pas de correspondance dans la gdb d'origine" \
                              f" : {cles_s2 - cles_s1}"
            #Différence au niveau attributaire
            else:
                for key in s1.keys() & s2.keys():
                    set_s1 = set(s1[key])
                    set_s2 = set(s2[key])
                    if set_s1 != set_s2:
                        diff1 = set(s1[key])-set(s2[key])
                        diff2 = set(s2[key])-set(s1[key])
                        txt = ""
                        if diff1:
                            txt += f" Les champs suivants " \
                              f"n'ont pas de correspondance dans la gdb : {diff1}."
                        if diff2:
                            txt += f" Les champs suivants " \
                              f"n'ont pas de correspondance dans la gdb d'origine : {diff2}."
                        return False, f"Champs différents pour la classe d'entités {key}." + txt
                return False, "autre"


def get_deleted(dico_origin, dico_modif):
    """Identifier les objets supprimés

    Fonction générant un dictionnaire contenant les objectid des objets supprimés pour chaque classe
    en comparant les dictionnaires de deux géodatabases/dataset (dico d'origine et dico modifié)

    :param dico_origin: dictionnaire de données de géodatabase/dataset de référence
    :param dico_modif: dictionnaire de données de géodatabase/dataset éditée (sur laquelle des objets ont peut-être été supprimés)

    :type dico_origin: dict.
    :type dico_modif: dict.

    :return0: dictionnaire contenant les objectid des objets supprimés (clé = nom_classe, valeur = liste des objectid)
    :rtype0: dict
    
    :return1: le dictionnaire de la géodatabase/datatset d'origine sans les objets supprimés
    :rtype1: dict
    """
    deleted_keys = {}
    dico_copy = copy.deepcopy(dico_origin)  # Copie indépendante du dico d'origine
    for classe in dico_copy:
        orig_keys = set(dico_copy[classe].keys())
        modif_keys = set(dico_modif[classe].keys())
        deleted = list(orig_keys - modif_keys)
        if deleted:
            deleted_keys[classe] = deleted
            for k in deleted:
                del dico_copy[classe][k]
    return deleted_keys, dico_copy


def get_updated(dico_origin, dico_modif, fields_type):
    """Identifier les objets modifiés

    Fonction générant un dictionnaire contenant les objectid des objets modifiés + les champs modifiés pour chaque classe
    en comparant les dictionnaires de deux géodatabases/dataset (dico d'origine et dico modifié)

    :param dico_origin: dictionnaire de données de géodatabase/dataset de référence
    :param dico_modif: dictionnaire de données de géodatabase/dataset éditée (sur laquelle des objets ont peut-être été modifiés)
    :param fields_type: la structure de la géodatabase fichier ou du dataset (return1 de gdb2dico)

    :type dico_origin: dict.
    :type dico_modif: dict.
    :type fields_type: dict.

    :return: dictionnaire contenant les objectid des objets modifiés (clé = nom_classe, valeur = dictionnaire des modifs de la classe)
    :rtype: dict
    """
    updated_keys = {}
    for classe in dico_origin:
        diffs = {}
        for key in dico_origin[classe]:
            val1 = dico_origin[classe][key]
            val2 = dico_modif[classe][key]
            if val1 != val2:
                fields_type_classe = fields_type[classe]
                # on récupère les noms des champs pour lesquels les valeurs sont différentes
                diffs[key] = get_different_fields_list(val1, val2, fields_type_classe)
        if diffs:
            updated_keys[classe] = diffs
    return updated_keys


def get_different_fields_list(str1, str2, fields_type):
    """Identifier champs modifiés

    Fonction comparant deux chaînes de caractères contenant tous les attributs de deux objets (ayant la même structure)
    et retournant la liste des champs modifiés

    :param str1: attributs de l'objet 1 concaténés sous la forme d'une chaîne de caractères
    :param str2: attributs de l'objet 2 concaténés sous la forme d'une chaîne de caractères
    :param fields_type: la structure classe d'entité liste des champs: (nom champ, type)

    :type str1: str
    :type str2: str
    :type fields_type: list

    :return: liste des champs différents entre les deux chaînes de caractères (champs modifiés)
    :rtype: list
    """
    str1_fixed = str1.replace(", nan,", ", 'nan',")
    str2_fixed = str2.replace(", nan,", ", 'nan',")
    l1 = ast.literal_eval(str1_fixed)
    l2 = ast.literal_eval(str2_fixed)
    champs_modifs = []
    for i in range(len(l1)):
        if str(l1[i]) != str(l2[i]):
            champs_modifs.append(fields_type[i][0])
    return champs_modifs


def write_logs(fichier,list_deleted, list_added, list_updated, type_ecriture):
    """Ecrire les résultats du traitement de comparaison

    Fonction écrivant les logs du traitement de comparaison des géodatases (objets supprimés, créés, modifiés
    par classe d'entités)

    :param fichier: chemin absolu vers le fichier de logs
    :param list_deleted: dictionnaire des objets supprimés
    :param list_added: dictionnaire des objets créés
    :param list_updated: dictionnaire des objets modifiés
    :param type_ecriture: "w" pour créer le fichier, "a" pour écrire sur un fichier existant

    :type fichier: str
    :type list_deleted: dict
    :type list_added: dict
    :type list_updated: dict
    :type type_ecriture: str

    :return: pas de return
    """
    gdb = "GDB 1"
    # type "a" signifie qu'on ajoute du texte au fichier de log déjà existant
    # il s'agit donc de la deuxième comparaison (GDB2)
    if type_ecriture == "a":
        gdb = "GDB 2"
    with open(fichier, type_ecriture, encoding="utf-8") as f:
        if type_ecriture == "a":
            f.write("\n")
            f.write("*********************************\n")
        f.write("-------SUPPRESSIONS "+gdb+"--------\n")
        f.write("\n")
        for classe, valeurs in list_deleted.items():
            f.write(f"{classe}:\n")
            f.write(f"{valeurs}\n")
            f.write("\n")
        f.write("-------CREATIONS "+gdb+"-----------\n")
        f.write("\n")
        for classe, valeurs in list_added.items():
            f.write(f"{classe}:\n")
            f.write(f"{valeurs}\n")
            f.write("\n")
        f.write("-------UPDATES "+gdb+"-------------\n")
        f.write("\n")
        for classe, valeurs in list_updated.items():
            f.write(f"{classe}:\n")
            f.write(f"{valeurs}\n")
            f.write("\n")


def clean_log_file(fichier_log):
    """Modification de la syntaxe du fichier de log

    Fonction modifiant la syntaxe du fichier de log pour répondre au besoin SVRP (parenthèse au lieu d'accolades...)

    :param fichier_log: chemin absolu vers le fichier de logs
    
    :type fichier: str
   
    :return: pas de return
    """
    # Lire le contenu du fichier
    with open(fichier_log, 'r', encoding='utf-8') as f:
        contenu = f.read()
    # Effectuer les remplacements
    contenu = contenu.replace('[', '(')
    contenu = contenu.replace(']', ')')
    contenu = contenu.replace('{', '(')
    contenu = contenu.replace('}', ')')
    contenu = contenu.replace("('", "['")
    contenu = contenu.replace("')", "']")
    # Sauvegarder le fichier modifié (en écrasant l'ancien fichier)
    with open(fichier_log, 'w', encoding='utf-8') as f:
        f.write(contenu)