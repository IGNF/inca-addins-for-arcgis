# coding: utf-8

"""
Copie des évolutions sans conflit d'une géodatabase fichier vers une autre lors d'un update
===========================

Fonctions permettant d'obtenir la copie/suppression/modification d'objets d'une géodatabase fichier
vers une autre en l'absence de conflits.

:Module: :mod:`apply_modif`
:Auteur: Projet INCA
:Date: 30/09/2025
:Copyright: (c) IGN France 2017

"""

import arcpy
from datetime import datetime
import ast
import copy


def to_delete(dico_deleted1, dico_deleted2, dico_updated2):
    """Retourne une dictionnaire contenant la liste des objets que l'on peut supprimer dans la gdb2

    Fonction qui lit la liste des objets supprimés dans la gdb et la compare aux objets supprimés ou modifiés
    dans la gdb2. Si l'objet a déjà été supprimé dans la gdb2 (nouvelle gdb de référence) il n'y a pas besoin
    de le "re-supprimer". Si l'objet a été modifié dans la gdb2 on ets dans une situation de conflit, on ne doit
    pas supprimer l'objet. Mais si l'objet n'est ni supprimé, ni modifié dans gdb2, on peut le supprimer
    
    Cette fonction peut aussi être utilisée pour obtenir la lisste des objets que l'on peut modifier dans la gdb2
    en remplaçant dico_deleted1 par dico_updated1 (dictionnaire des objets modifiés dans la gdb1)

    :param dico_deleted1: dictionnaire des objets supprimés dans la gdb1
    :param dico_deleted2: dictionnaire des objets supprimés dans la gdb2
    :param dico_updated2: dictionnaire des objets modifiés dans la gdb2

    :type dico_deleted1: dict.
    :type dico_deleted2: dict.
    :type dico_updated2: dict.

    :return: le dictionnaire des objets à supprimer dans la gdb2
    :rtype: dict
    """
    dico_to_delete = {}
    classes = set(dico_deleted1.keys())
    for cls in classes:
        deleted1 = set(dico_deleted1.get(cls, []))
        if cls in dico_deleted2:
            deleted2 = set(dico_deleted2.get(cls, []))
            deleted1 = deleted1 - deleted2
        if cls in dico_updated2:
            updated2 = set(dico_updated2.get(cls, []))
            deleted1 = deleted1 - updated2
        if deleted1:
            dico_to_delete[cls] = list(deleted1)
    return dico_to_delete


def delete(dico_to_delete, gdb, dataset):
    """Applique les suppressions dans la gdb d'update

    Fonction qui supprime les objets listés en entrée dans la géodatabase fichier générée lors de l'update

    :param dico_to_delete: dictionnaire des objets à supprimer dans la gdb d'update (obtenu avec to_delete())
    :param gdb: géodatabase dans laquelle il faut supprimer des objets
    :param dataset: dataset de la géodatabase dans lequel il faut supprimer des objets (si pas de dataset : "")

    :type dico_to_delete: dict.
    :type gdb: string.
    :type dataset: string.

    :return: pas de return
    """
    arcpy.env.workspace = gdb + dataset
    classes = set(dico_to_delete.keys())
    
    edit = arcpy.da.Editor(gdb)
    edit.startEditing(False, True)
    
    for feature_class in classes:
        object_ids = dico_to_delete[feature_class]
        sql_query = f"OBJECTID IN ({', '.join(map(str, object_ids))})"
        with arcpy.da.UpdateCursor(feature_class, ['OBJECTID'], sql_query) as cursor:
            for row in cursor:
                edit.startOperation()
                cursor.deleteRow()
                edit.stopOperation()
                
    edit.stopEditing(True)


def copy(dico_to_copy, source_gdb, destination_gdb, dataset):
    """Applique les créations dans la gdb d'update

    Fonction qui copie les objets listés en entrée de la gdb d'édition vers 
    la géodatabase fichier générée lors de l'update

    :param dico_to_copy: dictionnaire des objets à copier dans la gdb d'update
    :param source_gdb: géodatabase depuis laquelle il faut copier des objets
    :param destination_gdb: géodatabase dans laquelle il faut copier des objets
    :param dataset: dataset de la géodatabase dans lequel il faut copier des objets (si pas de dataset : "")

    :type dico_to_copy: dict.
    :type source_gdb: string.
    :type destination_gdb: string.
    :type dataset: string.

    :return: pas de return
    """
    arcpy.env.workspace = destination_gdb + dataset
    classes = set(dico_to_copy.keys())

    edit = arcpy.da.Editor(destination_gdb)
    edit.startEditing(False, True)
    for feature_class in classes:
        # Chemin de la classe d'entité source
        source_fc = source_gdb + dataset + rf"\{feature_class}"
        # Chemin de la classe d'entité cible
        destination_fc = destination_gdb + dataset + rf"\{feature_class}"
        
        object_ids = dico_to_copy[feature_class]
        sql_query = f"OBJECTID IN ({', '.join(map(str, object_ids))})"
        # Utiliser l'outil Select pour extraire les entités
        source_fc_selec = arcpy.management.SelectLayerByAttribute(source_fc, "NEW_SELECTION", sql_query)
        # Copier les entités sélectionnées dans la classe d'entités de destination
        arcpy.management.Append(source_fc_selec, destination_fc, "NO_TEST")

    edit.stopEditing(True)


def update(dico_to_update, source_gdb, target_gdb, dataset):
    """Applique les modifications dans la gdb d'update

    Fonction qui applique les modifications d'objets listés en entrée de la gdb d'édition vers 
    la géodatabase fichier générée lors de l'update

    :param dico_to_update: dictionnaire des objets à modifier dans la gdb d'update
    :param source_gdb: géodatabase qui contient les objets modifiés
    :param target_gdb: géodatabase dans laquelle il faut modifier des objets
    :param dataset: dataset de la géodatabase dans lequel il faut modifier des objets (si pas de dataset : "")

    :type dico_to_copy: dict.
    :type source_gdb: string.
    :type target_gdb: string.
    :type dataset: string.

    :return: pas de return
    """
    arcpy.env.workspace = target_gdb + dataset
    classes = set(dico_to_update.keys())

    for feature_class in classes:
        # Chemin de la classe d'entité source
        source_fc = source_gdb + dataset + rf"\{feature_class}"
        # Chemin de la classe d'entité cible
        target_fc = target_gdb + dataset + rf"\{feature_class}"

        # Liste des OBJECTID à supprimer
        object_ids = dico_to_update[feature_class]

        # pas possible de mettre tous les champs à jour d'un seul coup (champ Angle pose problème pour les couches d'annotations)
        if feature_class != 'e_toponyme_anno_25000':
            edit = arcpy.da.Editor(target_gdb)
            edit.startEditing(False, True)
            
            # Construire une clause SQL pour filtrer les OBJECTID
            sql_clause = f"OBJECTID IN ({', '.join(map(str, object_ids))})"
            fields = [f.name for f in arcpy.ListFields(source_fc) if
                      f.name not in ('shape', 'shape_Length', 'shape_Area', 'SUBST_SRC', 'oid_ref', 'OID_REF') and f.type not in ('Blob')]
            fields.append("SHAPE@")

            # Créer un curseur de recherche pour la classe d'entité source avec le filtre
            with arcpy.da.SearchCursor(source_fc, fields, sql_clause) as search_cursor:
                # Créer un curseur de mise à jour pour la classe d'entité cible
                with arcpy.da.UpdateCursor(target_fc, fields, sql_clause) as update_cursor:
                    for source_row in search_cursor:
                        source_objectid = source_row[0]  # OBJECTID de la source
                        for target_row in update_cursor:
                            target_objectid = target_row[0]  # OBJECTID de la cible
                            # Si les OBJECTID correspondent, mettre à jour les données
                            if source_objectid == target_objectid:
                                edit.startOperation()
                                update_cursor.updateRow(source_row)
                                edit.stopOperation()
                                break  # Passer au prochain OBJECTID source
            # Arrêter la session d'édition
            edit.stopEditing(True)

        else:
            edit = arcpy.da.Editor(target_gdb)
            edit.startEditing(False, True)

            # Construire une clause SQL pour filtrer les OBJECTID
            sql_clause = f"OBJECTID IN ({', '.join(map(str, object_ids))})"
            fields = [f.name for f in arcpy.ListFields(source_fc) if
                        f.name not in ('shape', 'shape_Length', 'shape_Area', 'angle', 'SUBST_SRC')]
            fields.append("SHAPE@")
            # Créer un curseur de recherche pour la classe d'entité source avec le filtre
            with arcpy.da.SearchCursor(source_fc, fields, sql_clause) as search_cursor:
                # Créer un curseur de mise à jour pour la classe d'entité cible
                with arcpy.da.UpdateCursor(target_fc, fields, sql_clause) as update_cursor:
                    for source_row in search_cursor:
                        source_objectid = source_row[0]  # OBJECTID de la source
                        for target_row in update_cursor:
                            target_objectid = target_row[0]  # OBJECTID de la cible
                            # Si les OBJECTID correspondent, mettre à jour les données
                            if source_objectid == target_objectid:
                                edit.startOperation()
                                update_cursor.updateRow(source_row)
                                edit.stopOperation()
                                break  # Passer au prochain OBJECTID source

            # Arrêter la session d'édition           
            edit.stopEditing(True)

