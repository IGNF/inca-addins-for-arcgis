# coding: utf-8

"""
Identification de conflits lors de l'édition de deux géodatabases fichier
===========================

Module de calcul des conflits lors de l'édition de deux géodatabases fichier
à partir des différentiels entre ces deux géodatabases fichier et une géodatabase de référence.

Les fonctions permettent d'obtenir les objets en conflits et de les inscrire dans une table
de la géodatabase fichier dédiée aux conflits

:Module: :mod:`conflict`
:Auteur: Projet INCA
:Date: 24/07/2025
:Copyright: (c) IGN France 2017

"""

import arcpy
import differential
import os


def generate_empty_gdb(folder_path, gdb_name):
    """Génère la géodatabase fichier vide

    Fonction qui permet de générer la géodatabase fichiée vide (utilisée pour générer la gdb des conflits et
    la gdb du réplica d'update

    :param folder_path: chemin absolu vers le répertoire qui va contenir la géodatabase fichier
    :param gdb_name: nom de la géodatabase fichier à créer

    :type folder_path: str
    :type gdb_name: str

    :return: True en cas de succès
    :rtype: bool
    """
    try:
        arcpy.CreateFileGDB_management(folder_path, gdb_name)
        return True
    except Exception as ex:
        return ex

 
def get_conflict(dico_deleted1, dico_updated1, dico_deleted2, dico_updated2):
    """Identifier les conflits

    Fonction qui permet d'identifierles différents types de conflits à partir des dictionnaires
    d'édition et de suppression des deux géodatabases/datasets d'édition

    :param dico_deleted1: chemin absolu vers le répertoire qui va contenir la géodatabase fichier des conflits
    :param dico_updated1: chemin absolu vers le répertoire qui va contenir la géodatabase fichier des conflits
    :param dico_deleted2: chemin absolu vers le répertoire qui va contenir la géodatabase fichier des conflits
    :param dico_updated2: chemin absolu vers le répertoire qui va contenir la géodatabase fichier des conflits

    :type dico_deleted1: dict
    :type dico_updated1: dict
    :type dico_deleted2: dict
    :type dico_updated2: dict

    :return0: dictionnaire des objets en "double update" (mis à jour dans les deux gdb)
    :rtype0: dict
    
    :return1: dictionnaire des objets supprimés dans la gdb1 et mis à jour dans la gdb2
    :rtype1: dict
    
    :return2: dictionnaire des objets supprimés dans la gdb2 et mis à jour dans la gdb1
    :rtype2: dict
    
    :return3: dictionnaire des objets en "double suppression" (supprimés dans les deux gdb donc pas en conflits)
    :rtype3: dict
    """
    dico_double_update = {}
    dico_deleted1_updated2 = {}
    dico_updated1_deleted2 = {}
    dico_double_delete = {}

    #on récupère toutes les classes d'entités qui sont présentes au moins dans 1 des 4 dico de suppression/update
    classes = set(dico_deleted1.keys()) | set(dico_updated1.keys()) | set(dico_deleted2.keys()) | set(
        dico_updated2.keys())

    for cls in classes:
        deleted1 = set(dico_deleted1.get(cls, []))
        updated1 = set(dico_updated1.get(cls, {}).keys())
        deleted2 = set(dico_deleted2.get(cls, []))
        updated2 = set(dico_updated2.get(cls, {}).keys())

        # Objets modifiés dans les deux GDB
        double_update = updated1 & updated2
        if double_update:
            dico_double_update[cls] = list(double_update)

        # Objets supprimés dans GDB1 et modifiés dans GDB2
        del1_upd2 = deleted1 & updated2
        if del1_upd2:
            dico_deleted1_updated2[cls] = list(del1_upd2)

        # Objets modifiés dans GDB1 et supprimés dans GDB2
        upd1_del2 = updated1 & deleted2
        if upd1_del2:
            dico_updated1_deleted2[cls] = list(upd1_del2)

        # Objets supprimés dans les deux GDB
        double_delete = deleted1 & deleted2
        if double_delete:
            dico_double_delete[cls] = list(double_delete)

    return dico_double_update, dico_deleted1_updated2, dico_updated1_deleted2, dico_double_delete


def get_conflict_attributes(dico_double_update, dico_deleted1_updated2, dico_updated1_deleted2,
                            all_features_class_data1, all_features_class_data2, fields_type, updated_result1,
                            updated_result2):
    """Identifier les attributs en conflits

    Fonction qui permet d'identifier les attributs en conflits pour les différents types de conflits. Avoir
    l'objectid des objets en conflits ne suffit pas, on a aussi besoin de savoir quels champs sont en conflit

    :param dico_double_update: dictionnaire des objets en "double update" (mis à jour dans les deux gdb)
    :param dico_deleted1_updated2: dictionnaire des objets supprimés dans la gdb1 et mis à jour dans la gdb2
    :param dico_updated1_deleted2: dictionnaire des objets supprimés dans la gdb2 et mis à jour dans la gdb1
    :param all_features_class_data1: dictionnaire de données de géodatabase/dataset éditée n°1
    :param all_features_class_data2: dictionnaire de données de géodatabase/dataset éditée n°2
    :param fields_type: dictionnaire des noms de champs + types en fonction des classes
    :param updated_result1: dictionnaire des objets mis à jour dans la gdb1
    :param updated_result2: dictionnaire des objets mis à jour dans la gdb2

    :type dico_double_update: dict
    :type dico_deleted1_updated2: dict
    :type dico_updated1_deleted2: dict
    :type all_features_class_data1: dict
    :type all_features_class_data2: dict
    :type fields_type: dict
    :type updated_result1: dict
    :type updated_result2: dict

    :return0: dictionnaire des objets en conflit de type "double update" (avec les attributs en conflit)
    :rtype0: dict
    
    :return1: dictionnaire des objets supprimés dans la gdb1 et mis à jour dans la gdb2 (avec les attributs modifiés)
    :rtype1: dict
    
    :return2: dictionnaire des objets supprimés dans la gdb2 et mis à jour dans la gdb1 (avec les attributs modifiés)
    :rtype2: dict
    """
    conflict_double_update = {}
    dico_deleted1_updated2_attr = {}
    dico_updated1_deleted2_attr = {}
    for classe in dico_double_update:
        conflicts = {}
        for key in dico_double_update[classe]:
            val1 = all_features_class_data1[classe][key]
            val2 = all_features_class_data2[classe][key]
            if val1 != val2:
                fields_type_classe = fields_type[classe]
                conflicts[key] = differential.get_different_fields_list(val1, val2, fields_type_classe)
        if conflicts:
            conflict_double_update[classe] = conflicts
    for classe in dico_deleted1_updated2:
        attr = {}
        for key in dico_deleted1_updated2[classe]:
            attr[key] = updated_result2[classe][key]
        dico_deleted1_updated2_attr[classe] = attr
    for classe in dico_updated1_deleted2:
        attr = {}
        for key in dico_updated1_deleted2[classe]:
            attr[key] = updated_result1[classe][key]
        dico_updated1_deleted2_attr[classe] = attr

    return conflict_double_update, dico_deleted1_updated2_attr, dico_updated1_deleted2_attr


def write_logs(fichier,conflict_double_update, dico_deleted1_updated2_attr, dico_updated1_deleted2_attr, type_ecriture):
    """Ecrire les conflits dans le fichier de log

    Fonction écrivant les objets en conflit dans le fichier de log du traitement

    :param fichier: chemin absolu vers le fichier de logs
    :param conflict_double_update: dictionnaire des objets en conflit de type "double update"
    :param dico_deleted1_updated2_attr: dictionnaire des objets créés
    :param dico_updated1_deleted2_attr: dictionnaire des objets modifiés

    :type fichier: str
    :type conflict_double_update: dict
    :type dico_deleted1_updated2_attr: dict
    :type dico_updated1_deleted2_attr: dict

    :return: pas de return
    """
    with open(fichier, type_ecriture, encoding="utf-8") as f:
        f.write("\n")
        f.write("*******************************************\n")
        f.write("****************CONFLICTS******************\n")
        f.write("*******************************************\n")
        f.write("-------CONFLICT DELETED 1 UPDATED 2--------\n")
        f.write("\n")
        for classe, valeurs in dico_deleted1_updated2_attr.items():
            f.write(f"{classe}:\n")
            f.write(f"{valeurs}\n")
            f.write("\n")
        f.write("-------CONFLICT UPDATED 1 DELETED 2--------\n")
        f.write("\n")
        for classe, valeurs in dico_updated1_deleted2_attr.items():
            f.write(f"{classe}:\n")
            f.write(f"{valeurs}\n")
            f.write("\n")
        f.write("-------CONFLICT DOUBLE UPDATE--------------\n")
        f.write("\n")
        for classe, valeurs in conflict_double_update.items():
            f.write(f"{classe}:\n")
            f.write(f"{valeurs}\n")
            f.write("\n")


def copy_class_conflict(source_gdb, output_gdb, objectid_list, class_name, source_suffixe):
    """Copier une classe d'une gdb à une autre en filtrant les entités par objectid

    Fonction copiant une classe d'entité d'une géodatabase fichier vers une autre en copiant uniquement les
    éléments dont l'objectid est présent dans la liste en entrée. Cette fonction est utilisée pour copier les objets
    en conflit dans la gdb Conflict.gdb

    :param source_gdb: chemin absolu vers la gdb fichier contenant la classe à copier
    :param output_gdb: dchemin absolu vers la gdb fichier de destination
    :param objectid_list: liste des objectid à copier
    :param class_name: nom de la classe
    :param source_suffixe: '_gdb' si la source est la gdb d'édition, '_sde' pour la gdb partagée,
            '_ini' pour la gdb initiale

    :type source_gdb: str
    :type output_gdb: str
    :type objectid_list: list
    :type class_name: str
    :type source_suffixe: str

    :return: pas de return
    """

    try:
        # Définir le chemin de la classe d'entités source
        input_fc = source_gdb + '\\' + class_name
        output_fc = output_gdb + '\\' + class_name + source_suffixe
        objectid_list = list(objectid_list)
        objectid_list.sort()
        if len(objectid_list) == 0:
            sql_query = f"OBJECTID = 0"
        else:
            sql_query = f"OBJECTID IN ({','.join(map(str, objectid_list))})"
        # Création de la classe d'entité filtrée
        if class_name == "e_toponyme_anno_25000":
            arcpy.conversion.FeatureClassToFeatureClass(input_fc, output_gdb, class_name + source_suffixe, sql_query)
        else:
            arcpy.analysis.Select(input_fc, output_fc, sql_query)
        # Ajout d'un champ pour stocker l'ancien objectid
        arcpy.AddField_management(output_fc, "ORIGIN_OID", "LONG")
        # ATTENTION peut-être ajouter une session d'édition
        with arcpy.da.UpdateCursor(output_fc, ["ORIGIN_OID"]) as cur:
            for idx, row in enumerate(cur):
                row[0] = objectid_list[idx]
                cur.updateRow(row)
    except Exception as ex:
        return ex


def copy_all_class_conflict(gdb_ini, gdb_edited, gdb_sde, gdb_conflict, conflict_double_update, dico_deleted1_updated2,
                            dico_updated1_deleted2):
    """Copier les différentes versions (ini, edited, sde) des objets en conflits dans la gdb des conflits

    Fonction copiant les objets en conflits pour chaque classe contenant des conflits vers une nouvelle classe d'entité
    dans la géodatabase fichier des conflits. Chaque classe d'entité est copiée en triple dans la gdb Conflict.gdb car
    on a besoin d'avoir les 3 versions des objets en conflit (état d'origine, mise à jour en local par l'opérateur,
    et mise à jour sur la gdb partagée).

    :param gdb_ini: chemin absolu vers la gdb fichier contenant la classe à copier
    :param gdb_edited: dchemin absolu vers la gdb fichier de destination
    :param gdb_sde: chemin absolu vers la gdb fichier de replica créée lors de l'update
    :param gdb_conflict: chemin absolu vers la gdb fichier des conflits
    :param conflict_double_update: dictionnaire des objets en conflit de type "double update"
    :param dico_deleted1_updated2: dictionnaire des objets en conflit de type "delete update"
    :param dico_updated1_deleted2: dictionnaire des objets en conflit de type "update delete"

    :type gdb_ini: str
    :type gdb_edited: str
    :type gdb_sde: list
    :type gdb_conflict: str
    :type conflict_double_update: dict
    :type dico_deleted1_updated2: dict
    :type dico_updated1_deleted2: dict

    :return: pas de return
    """

    try:
        # Création d'un dictionnaire contenant les objectids des objets en conflits de la gdb ini pour chaque classe
        dico_objectid_conflict_ini = create_dico_objectid([conflict_double_update, dico_deleted1_updated2,
                                                           dico_updated1_deleted2])
        # Création d'un dictionnaire contenant les OID des objets en conflits de la gdb partagée pour chaque classe
        dico_objectid_conflict_sde = create_dico_objectid([conflict_double_update, dico_deleted1_updated2])
        # Création d'un dictionnaire contenant les OID des objets en conflits de la gdb d'édition pour chaque classe
        dico_objectid_conflict_gdb = create_dico_objectid([conflict_double_update, dico_updated1_deleted2])

        # Création des objets en conflit pour chaque classe contenant des conflits (une copie par source)
        for fc in dico_objectid_conflict_ini:
            copy_class_conflict(gdb_ini, gdb_conflict, dico_objectid_conflict_ini[fc], fc, '_ini')
            if fc in dico_objectid_conflict_gdb:
                copy_class_conflict(gdb_edited, gdb_conflict, dico_objectid_conflict_gdb[fc], fc, '_gdb')
            else:
                copy_class_conflict(gdb_edited, gdb_conflict, [], fc, '_gdb')
            if fc in dico_objectid_conflict_sde:
                copy_class_conflict(gdb_sde, gdb_conflict, dico_objectid_conflict_sde[fc], fc, '_sde')
            else:
                copy_class_conflict(gdb_sde, gdb_conflict, [], fc, '_sde')
    except Exception as ex:
        return ex


def create_dico_objectid(list_dico):
    """Créer le dictionnaire contenant les objectids des objets présents dans une liste de dictionnaires (par classe)

    Fonction créant un dictionnaire contenant pour chaque classe une liste des objectids présents pour cette classe
    dans la liste de dictionnaire en entrée du traitement.
    Permet à parti des 3 dictionnaires des objets en conflits (1 dictionnaire par type de conflit), de générer la liste
    des objectid à copier dans les classes d'entités de conflits de Conflict.gdb.
    Exemple : un objet en conflit (update gdb - delete sde) doit être depuis la gdb d'édition pour être affiché dans la
    carte des conflits, mais pas depuis la gdb partagée puisqu'il n'y existe plus.
    On pourrait utiliser la même liste de sélection pour les 3 sources de données (gdb d'édition, gdb partagée et
    gdb ini) mais cela poserait problème ensuite pour mettre à jour le champ Origin_OID dans les classes en conflit

    :param list_dico: liste contenant les dictionnaires de conflits (les 3 pour gdb_ini, et 2 pour les deux autres gdb)

    :type list_dico: list

    :return: dictionnaire des objets présents dans les dictionnaires en entrée
    :rtype: dict
    """

    dico_objectid = {}
    for dico in list_dico:
        for classe, valeurs in dico.items():
            if classe not in dico_objectid:
                dico_objectid[classe] = set()
            dico_objectid[classe].update(valeurs)
    return dico_objectid


def create_conflict_class(folder_path):
    """Génère la classe d'entité des conflits

    Fonction qui permet de créer la classe d'entité des conflits

    :param folder_path: chemin absolu vers le répertoire qui va contenir la géodatabase fichier des conflits

    :type folder_path: str

    :return: True en cas de succès
    :rtype: bool
    """
    gdb_name = r"\Conflict.gdb"
    table_name = "conflict_point"
    workspace = folder_path + gdb_name
    try:
        # Créer la table
        spatial_reference = arcpy.SpatialReference(2154)
        arcpy.management.CreateFeatureclass(workspace, table_name, "POINT", spatial_reference=spatial_reference)
        arcpy.management.AddFields(
            in_table=f"{workspace}/{table_name}",
            field_description="ORIGIN_OID LONG # # # #;classe TEXT # 255 # #;type_conflit TEXT # 255 # #;"
                              "champs_modifies TEXT # 255 # #;resolution TEXT # 255 # #",
        )
        return True
    except Exception as ex:
        return ex


def get_centroid_conflict(gdb_path):
    """Retourne un dictionnaire des centroides des conflits

    Fonction qui permet de récupérer tous les centroïdes des objets en conflits (récupère les centroïdes des objets
    dans leur version d'origine : avant modifications)

    :param gdb_path: chemin absolu vers la géodatabase fichier des Conflits

    :type gdb_path: str.

    :return: les conflits sous forme de dictionnaire (clés : classes / sous-clés : objectid - valeurs : Point (geom))
    :rtype: dict
    """
    arcpy.env.workspace = gdb_path
    # Dictionnaire pour stocker les dictionnaires par feature class
    all_features_data = {}
    fields = ["ORIGIN_OID", "SHAPE@"]
    feature_classes = arcpy.ListFeatureClasses()
    for fc in feature_classes:
        # chemin vers la classe fc
        if fc[-4:] == '_ini':
            fc_path = f"{gdb_path}\\{fc}"
            # Stocker le dictionnaire dans la liste globale
            fc2dico = {r[0]: r[-1].centroid if r[-1] else None for r in
                             arcpy.da.SearchCursor(fc_path, fields)}
            all_features_data[fc[:-4]] = fc2dico
    return all_features_data


def insert_conflict_class(folder_path, conflict_double_update, dico_deleted1_updated2_attr,
                          dico_updated1_deleted2_attr, dico_centroid):
    """Insérer les conflits dans la classe d'entité des conflits

    Fonction insérant les objets en conflit dans la classe d'entité des conflits

    :param folder_path: chemin absolu vers le dossier contenant la gdb des conflits
    :param conflict_double_update: dictionnaire des objets en conflit de type "double update"
    :param dico_deleted1_updated2_attr: dictionnaire des objets en conflit de type "delete gdb - update sde"
    :param dico_updated1_deleted2_attr: dictionnaire des objets en conflit de type "update gdb - delete sde"
    :param dico_centroid: dictionnaire des centroïdes des conflits

    :type folder_path: str
    :type conflict_double_update: dict
    :type dico_deleted1_updated2_attr: dict
    :type dico_updated1_deleted2_attr: dict
    :type dico_centroid: dict

    :return: True en cas de succès
    :rtype: bool
    """
    gdb_name = r"\Conflict.gdb"
    table_name = r"\conflict_point"
    workspace = folder_path + gdb_name
    fields = ['ORIGIN_OID', 'classe', 'type_conflit', 'champs_modifies', "SHAPE@"]
    try:
        # Insérer dans la table
        with arcpy.da.InsertCursor(f"{workspace}/{table_name}", fields) as cursor:
            for classe in conflict_double_update:
                for key in conflict_double_update[classe]:
                    cursor.insertRow((key, classe, 'Double mise à jour', str(conflict_double_update[classe][key]),
                                      dico_centroid[classe][key]))
            for classe in dico_deleted1_updated2_attr:
                for key in dico_deleted1_updated2_attr[classe]:
                    cursor.insertRow((key, classe, 'Supprimé gdb - Mis à jour SDE',
                                      str(dico_deleted1_updated2_attr[classe][key]), dico_centroid[classe][key]))
            for classe in dico_updated1_deleted2_attr:
                for key in dico_updated1_deleted2_attr[classe]:
                    cursor.insertRow((key, classe, 'Mis à jour gdb - Supprimé SDE',
                                      str(dico_updated1_deleted2_attr[classe][key]), dico_centroid[classe][key]))
        return True
    except Exception as ex:
        return ex

   
def update_context_data(project_folder, gdb_edited):
    """Ecraser la gdb des données de contexte

    Fonction supprimant la gdb utilisée pour afficher les données de contexte sur la carte des conflits
    et la remplaçant par les gdb d'édition.

    :param project_folder: chemin absolu vers le dossier contenant le projet ArcGIS Pro

    :type project_folder: str

    :return: True en cas de succès
    :rtype: bool
    """

    try:
        # Insérer dans la table
        contexte_folder = project_folder + r"\DONNEES_CONTEXTE"
        # Créer le dossier qui contiendra la gdb d'update
        if not os.path.exists(contexte_folder):
            os.makedirs(contexte_folder)
        gdb_contexte = os.path.join(contexte_folder, f"france25_extraction.gdb")
        arcpy.management.Delete(gdb_contexte)
        if os.path.isdir(gdb_contexte):
            os.rmdir(gdb_contexte)
        arcpy.management.Copy(gdb_edited, gdb_contexte)
        return True
    except Exception as ex:
        return ex