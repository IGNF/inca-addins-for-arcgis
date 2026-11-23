# coding: utf-8

"""
Création du réplica lors de l'Update
===========================

Module de création d'un réplica de géodatabase à jour. Utilisé pour récupérer
les dernières évolutions sur la géodatabase partagée

Les fonctions permettent de vérifier si les conditions sont bien réunies pour lancer
la création du réplica. Elles permettents aussi la création du réplica.

:Module: :mod:`conflict`
:Auteur: Projet INCA
:Date: 30/10/2025
:Copyright: (c) IGN France 2017

"""

import arcpy
import conflict


def check_replica(gdb):
    """Vérifie la présence d'un réplica dans la gdb (ou dans le dataset)

    Fonction qui permet de vérifier que la géodatabase d'édition contienne bien un réplica.
    Attention ListReplicas prend en entrée une gdb, pas un dataset

    :param gdb: chemin vers la géodatabase fichier

    :type gdb: string.

    :return0: True s'il y a un replica dans la gdb, False si 0 ou plusieurs
    :rtype0: bool

    :return1: Nom du replica ou message d'erreur
    :rtype1: string
    """
    try:
        replicas = arcpy.da.ListReplicas(gdb)
        if len(replicas) == 0:
            return False, "Erreur la géodatabase fichier d'édition ne contient pas de réplica"
        elif len(replicas) > 1:
            return False, "Erreur la géodatabase fichier d'édition contient plusieurs réplicas"
        elif len(replicas) == 1:
            return True, replicas[0].name
    except Exception as ex:
        print("exeception")
        return ex


def list_feature_class(gdb, dataset):
    """Retourne la liste des classes d'entités d'un dataset de gdb fichier

    Fonction qui permet de retourner la liste des classes d'entités d'un dataset de gdb fichier
    Si on veut la liste des classes d'entités à la racine la gdb (hors dataset), mettre dataset=""

    :param gdb: chemin vers la géodatabase fichier
    :param dataset: dataset de la géodatabase fichier (pour chercher à la racine : "")

    :type gdb: string.
    :type dataset: string.

    :return: Liste des classes d'entités
    :rtype: list
    """

    arcpy.env.workspace = gdb + dataset
    featureclasses = arcpy.ListFeatureClasses()
    return featureclasses


def list_feature_class_sde(sde_path):
    """Retourne la liste des classes d'entités d'une géodatabase partagés

    Fonction qui permet de retourner la liste des classes d'entités d'une géodatabase partagée
    Cette fonction fonctionne pour le cas où la géodatabase possède 1 ou 0 dataset (données à la racine)
    Si la gdb partagée contient plusieurs datasets, on ne travaille que sur le premier.

    :param sde_path: chemin vers la géodatabase partagée

    :type sde_path: string.

    :return: Liste des classes d'entités
    :rtype: list
    """

    arcpy.env.workspace = sde_path
    datasets = arcpy.ListDatasets(feature_type='Feature')
    if len(datasets) == 0:
        dataset = ""
    else:
        dataset = datasets[0]
    featureclasses = arcpy.ListFeatureClasses(feature_dataset=dataset)
    return featureclasses


def compare_list_feature_class(liste_gdb, liste_sde):
    """Retourne la liste des classes d'entités de la gdb partager à extraire

    Fonction qui permet de retourner la liste des classes d'entités de la gdb partagée qui sont
    aussi présentes dans la gdb d'édition. Ce sont les classes d'entités à extraire

    :param liste_gdb: liste contenant les classes d'entités de la gdb d'édition (ex: 'b_bati_surf_n3')
    :param liste_sde: liste contenant les classes d'entités de la gdb partagée
    (ex: 'france25_edition_sprint22_glp.data_owner.b_bati_surf_n3')

    :type liste_gdb: list
    :type liste_sde: list

    :return0: True si toutes les classes de la gdb de réplica sont sur la gdb partagée, False sinon
    :rtype0: bool

    :return1: En cas de succès : Liste des classes d'entités de la gdb partagée présentes dans la gdb fichier d'édition.
              En cas d'erreur : La liste des classes d'entités de la gdb d'édition manquantes dans la gdb partagée
    :rtype1: list
    """

    list_extraction = []
    for i in range(len(liste_sde)):
        for j in range(len(liste_gdb)):
            if liste_gdb[j] in liste_sde[i]:
                list_extraction.append(liste_sde[i])
                break
    if len(liste_gdb) > len(list_extraction):
        list_present = []
        for i in range(len(liste_gdb)):
            for j in range(len(liste_sde)):
                if liste_gdb[i] in liste_sde[j]:
                    list_present.append(liste_gdb[i])
                    break
        return False, list(set(liste_gdb) - set(list_present))
    else:
        return True, list_extraction


def check_zone_interet(gdb):
    """Vérifie la présence de la zone d'intérêt dans la gdb fichier d'édition

    Fonction qui permet de vérifier que la géodatabase d'édition contienne bien la couche zone_interet_large
    à sa racine.

    :param gdb: chemin vers la géodatabase fichier d'édition

    :type gdb: string.

    :return: True s'il y a la couche zone_interet_large à la racine de la gdb
    :rtype: bool
    """
    arcpy.env.workspace = gdb
    featureclasses = arcpy.ListFeatureClasses()
    zone_interet = False
    for fc in featureclasses:
        if fc == 'zone_interet_large':
            zone_interet = True
    return zone_interet


def generate_replica_name(origin_name):
    """Génère le nom du réplica qui va être créé

    Fonction qui permet de générer le nom du réplica qui va être créé. Le nom sera le nom du réplica d'origine
    suivi du suffixe _UpdateX avec X le nombre d'update effectués

    :param origin_name: nom du réplica précédent

    :type gdb: origin_name.

    :return: Nom du nouveau réplica
    :rtype: string
    """
    split_origin_name = origin_name.split('_Update')
    if len(split_origin_name) == 1:
        new_name = split_origin_name[0] + '_Update1'
    elif len(split_origin_name) == 2:
        new_name = split_origin_name[0] + '_Update' + str(int(split_origin_name[1])+1)
    else:
        new_name = "ERROR generating replica name"
    return new_name


def create_replica(in_data, sde_path, replica_gdb_path, new_replica_name, gdb_edited_path):
    """Créer le réplica d'update

    Fonction qui permet de lancer la création du réplica d'update : gdb fichier à jour avec les dernières mises à jour
    faites sur la gdb partagée et réintégrable après modifications.

    :param in_data: liste des feature class à exporter dans le replica
    :param sde_path: chemin vers la gdb partagée à partir de laquelle on va créer le réplica
    :param replica_gdb_path: chemin vers la gdb fichier qui contiendra le replica
    :param new_replica_name: nom du replica à créer
    :param gdb_edited_path: chemin vers la gdb d'édition (qui contient la zone d'intérêt large : filtre spatial de
                            l'extraction)

    :type in_data: list
    :type sde_path: str
    :type replica_gdb_path: str
    :type origin_replica_name: str
    :type gdb_edited_path: str

    :return: True s'il y a la couche zone_interet_large à la racine de la gdb
    :rtype: bool
    """
    arcpy.env.workspace = sde_path
    conflict.generate_empty_gdb(replica_gdb_path, 'france25_extraction.gdb')

    arcpy.management.CreateReplica(in_data=in_data, in_type="CHECK_OUT", out_geodatabase=replica_gdb_path + "\\france25_extraction.gdb",
                                   out_name=new_replica_name, geometry_features=gdb_edited_path + "\\zone_interet_large", out_type="GEODATABASE")
    return replica_gdb_path + "\\france25_extraction.gdb"


def copy_zone_interet(gdb_edited, gdb_update):
    """Copie la zone d'intérêt large dans la gdb d'update

    Fonction qui permet de copier la zone d'intérêt large présente dans la géodatabase d'édition vers la gdb d'update.
    Cela sera utile dans la persepective de nouveaux updates réalisés à partir de la gdb d'update

    :param gdb_edited: chemin vers la géodatabase fichier d'édition
    :param gdb_update: chemin vers la géodatabase fichier d'update

    :type gdb_edited: string.
    :type gdb_update: string.

    :return: None
    """
    try:
        arcpy.conversion.FeatureClassToFeatureClass(
            in_features=gdb_edited + "\\zone_interet_large",
            out_path=gdb_update,
            out_name="zone_interet_large",
        )
    except Exception as ex:
        print("Erreur copie zone d'interet")