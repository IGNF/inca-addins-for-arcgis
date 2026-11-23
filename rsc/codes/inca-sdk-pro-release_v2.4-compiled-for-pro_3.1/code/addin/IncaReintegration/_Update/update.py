# coding: utf-8

"""
Traitement d'update
===========================

Traitement d'update sur une géodatabase. 

:Module: :mod:`update`
:Auteur: Projet INCA
:Date: 24/07/2025
:Copyright: \(c\) IGN France 2017

"""

import arcpy
import os
from datetime import datetime
import sys

import differential
import conflict
import apply_modif
import create_replica


try:
    t_deb = datetime.now()
    timestamp = t_deb.strftime("%Y%m%d_%H%M%S")
    
    gdb_origin = arcpy.GetParameterAsText(0)
    gdb_edited1 = arcpy.GetParameterAsText(1)
    sde = arcpy.GetParameterAsText(2)

    fichier_log = arcpy.GetParameterAsText(3)+fr"\evolutions_log_{timestamp}.txt"
    update_folder = arcpy.GetParameterAsText(3)+fr"\Update_{timestamp}"
    bkp_folder = arcpy.GetParameterAsText(3)+fr"\Update_{timestamp}"+fr"\BKP_Update_{timestamp}"
    project_folder = arcpy.GetParameterAsText(4)
    
    check_replica1 = create_replica.check_replica(gdb_edited1)
    if check_replica1[0]:

        check_dataset_sde = differential.check_dataset(sde)
        verif_dataset = differential.dataset_compare(gdb_origin, gdb_edited1, sde)
        # si nom ou nombre de dataset différents dans le sgdb on stoppe
        if not verif_dataset[0]:
            print(verif_dataset[1])
            sys.exit()

        if verif_dataset[1]:
            dataset = "\\" + verif_dataset[1]
        else:
            dataset = ""

        featureclasses1 = create_replica.list_feature_class(gdb_edited1, dataset)
        featureclasse_sde = create_replica.list_feature_class_sde(sde)
        list_extraction = create_replica.compare_list_feature_class(featureclasses1, featureclasse_sde)
        if list_extraction[0]:
            if create_replica.check_zone_interet(gdb_edited1):
                # Créer le dossier qui contiendra la gdb d'update
                if not os.path.exists(update_folder):
                    os.makedirs(update_folder)
                new_replica_name = create_replica.generate_replica_name(check_replica1[1])
                gdb_edited2 = create_replica.create_replica(list_extraction[1], sde, update_folder, new_replica_name, gdb_edited1)
                # Une fois le réplica créé
                # dictionnaire contenant toutes les entités des classes  de la gdb fichier d'origine
                all_feature_class_origin = differential.gdb2dico(gdb_origin + dataset)
                all_features_class_data_origin = all_feature_class_origin[0]
                all_features_class_structure_origin = all_feature_class_origin[1]

                # dictionnaire contenant toutes les entités des classes  de la gdb fichier éditée n°1
                all_feature_class1 = differential.gdb2dico(gdb_edited1 + dataset)
                all_features_class_data1 = all_feature_class1[0]
                all_features_class_structure1 = all_feature_class1[1]

                # dictionnaire contenant toutes les entités des classes  de la gdb fichier d'update
                all_feature_class2 = differential.gdb2dico(gdb_edited2 + dataset)
                all_features_class_data2 = all_feature_class2[0]
                all_features_class_structure2 = all_feature_class2[1]

                # calcul des différences de structure entre la géodatabase de référence et la gdb fichier éditée n°1
                diff_structure_origin_edited1 = differential.structureCompare(all_features_class_structure_origin,
                                                                              all_features_class_structure1)

                # calcul des différences de structure entre la géodatabase de référence et la gdb fichier d'update
                diff_structure_origin_edited2 = differential.structureCompare(all_features_class_structure_origin,
                                                                              all_features_class_structure2)

                # si les 3 gdb ont la même structure : on peut commencer les calculs de différentiel
                if diff_structure_origin_edited1[0] and diff_structure_origin_edited2[0]:

                    # calcul des objets supprimés dans la gdb n°1
                    deleted_result1 = differential.get_deleted(all_features_class_data_origin, all_features_class_data1)
                    dictionnary_deleted1 = deleted_result1[0]
                    dictionnary_origin_cleaned1 = deleted_result1[1]

                    # calcul des objets créés dans la gdb n°1
                    added_result1 = differential.get_deleted(all_features_class_data1, all_features_class_data_origin)
                    dictionnary_added1 = added_result1[0]
                    dictionnary_added_cleaned1 = added_result1[1]

                    # calcul des objets mis à jour dans la gdb n°1
                    updated_result1 = differential.get_updated(dictionnary_origin_cleaned1, dictionnary_added_cleaned1,
                                                               all_features_class_structure_origin)

                    differential.write_logs(fichier_log, dictionnary_deleted1, dictionnary_added1, updated_result1, "w")

                    # calcul des objets supprimés dans la gdb n°2
                    deleted_result2 = differential.get_deleted(all_features_class_data_origin, all_features_class_data2)
                    dictionnary_deleted2 = deleted_result2[0]
                    dictionnary_origin_cleaned2 = deleted_result2[1]

                    # calcul des objets créés dans la gdb n°2
                    added_result2 = differential.get_deleted(all_features_class_data2, all_features_class_data_origin)
                    dictionnary_added2 = added_result2[0]
                    dictionnary_added_cleaned2 = added_result2[1]

                    # calcul des objets mis à jour dans la gdb n°2
                    updated_result2 = differential.get_updated(dictionnary_origin_cleaned2, dictionnary_added_cleaned2,
                                                               all_features_class_structure_origin)

                    differential.write_logs(fichier_log, dictionnary_deleted2, dictionnary_added2, updated_result2, "a")

                    # en l'absence de modification de la gdb patagée
                    if dictionnary_deleted2 == {} and dictionnary_added2 == {} and updated_result2 == {}:
                        arcpy.management.DeleteVersion(sde, new_replica_name)
                        # data_editor.nom_replica
                        desc = arcpy.Describe(sde)
                        arcpy.management.UnregisterReplica(sde, desc.connectionProperties.user + "." + new_replica_name)
                        arcpy.management.Delete(update_folder + "\\france25_extraction.gdb")
                        differential.clean_log_file(fichier_log)
                        print("END pas de modif sur la gdb partagée")
                    else:
                        create_replica.copy_zone_interet(gdb_edited1, gdb_edited2)
                        # il y a eu des modifications/suppressions/creations sur la gdb2.
                        # Il faut copier les modification de la gdb1 vers la gdb2 (gdb d'update)
                        # Créer le dossier qui contiendra la gdb d'update
                        if not os.path.exists(update_folder):
                            os.makedirs(update_folder)
                        # Créer le dossier de sauvegarde
                        if not os.path.exists(bkp_folder):
                            os.makedirs(bkp_folder)
                        # Copier les géodatabases dans le dossier de sauvegarde
                        bkp_ori = os.path.join(bkp_folder, f"ori_{timestamp}.gdb")
                        bkp_gdb1 = os.path.join(bkp_folder, f"gdb1_{timestamp}.gdb")
                        bkp_gdb2 = os.path.join(bkp_folder, f"gdb2_{timestamp}.gdb")

                        arcpy.management.Copy(gdb_origin, bkp_ori)
                        arcpy.management.Copy(gdb_edited1, bkp_gdb1)
                        arcpy.management.Copy(gdb_edited2, bkp_gdb2)

                        # Copier la gdb2 (gdb d'update) à la racine du dossier d'update
                        update_gdb = os.path.join(update_folder, f"update_gdb_{timestamp}.gdb")

                        arcpy.management.Copy(gdb_edited2, update_gdb)

                        # Appliquer les créations de la gdb1 vers la gdb d'update
                        apply_modif.copy(dictionnary_added1, bkp_gdb1, update_gdb, dataset)
                        # Appliquer les modifications sans conflit de la gdb1 vers la gdb d'update
                        dico_to_update = apply_modif.to_delete(updated_result1, dictionnary_deleted2, updated_result2)
                        apply_modif.update(dico_to_update, bkp_gdb1, update_gdb, dataset)
                        # Appliquer les suppressions sans conflit de la gdb1 vers la gdb d'update
                        dico_to_delete = apply_modif.to_delete(dictionnary_deleted1, dictionnary_deleted2,
                                                               updated_result2)
                        apply_modif.delete(dico_to_delete, update_gdb, dataset)

                        # calcul des 3 types de conflits
                        conflicts = conflict.get_conflict(dictionnary_deleted1, updated_result1, dictionnary_deleted2,
                                                          updated_result2)
                        if not conflicts[0] and not conflicts[1] and not conflicts[2]:
                            print(["pas de conflits",f"update_gdb_{timestamp}.gdb"])
                        else:
                            # il y a peut etre conflits (dépend du double update)
                            conflicts_attr = conflict.get_conflict_attributes(conflicts[0], conflicts[1], conflicts[2],
                                                                              all_features_class_data1,
                                                                              all_features_class_data2,
                                                                              all_features_class_structure_origin,
                                                                              updated_result1, updated_result2)
                            if not conflicts_attr[0] and not conflicts_attr[1] and not conflicts_attr[2]:
                                print(["pas de conflits (double update sans conflits)",f"update_gdb_{timestamp}.gdb"])
                            else:
                                # il y a des conflits
                                conflict.write_logs(fichier_log, conflicts_attr[0], conflicts_attr[1],
                                                    conflicts_attr[2], "a")
                                conflict_gdb_path = update_folder
                                # création gdb conflits
                                result_generate_conflict_gdb = conflict.generate_empty_gdb(conflict_gdb_path,
                                                                                           'Conflict.gdb')
                                if result_generate_conflict_gdb is not True:
                                    print(result_generate_conflict_gdb)
                                else:
                                    conflict.copy_all_class_conflict(gdb_origin + dataset, gdb_edited1 + dataset,
                                                            gdb_edited2 + dataset, conflict_gdb_path + r"\Conflict.gdb",
                                                            conflicts_attr[0], conflicts_attr[1], conflicts_attr[2])
                                    result_create_conflict_class = conflict.create_conflict_class(conflict_gdb_path)
                                    
                                    # écraser la gdb de contexte par le replica de la gdb partagée
                                    conflict.update_context_data(project_folder, update_gdb)
                                    
                                    if result_create_conflict_class:
                                        dico_centroid = conflict.get_centroid_conflict(conflict_gdb_path + r"\Conflict.gdb")
                                        result_insert_conflict_class = conflict.insert_conflict_class(conflict_gdb_path,
                                                                            conflicts_attr[0], conflicts_attr[1],
                                                                            conflicts_attr[2], dico_centroid)
                                        if result_insert_conflict_class is not True:
                                            print(result_insert_conflict_class)
                                        else: 
                                            print(["OK",conflict_gdb_path + r"\Conflict.gdb"])
                                    else:
                                        print(result_create_conflict_class)
                        
                        arcpy.management.DeleteVersion(sde, check_replica1[1])
                        # data_editor.nom_replica
                        desc = arcpy.Describe(sde)                    
                        arcpy.management.UnregisterReplica(sde, desc.connectionProperties.user + "." + check_replica1[1])
                        arcpy.management.Delete(update_folder + "\\france25_extraction.gdb")
                        # changement de la syntaxe du fichier de logs                       
                        differential.clean_log_file(fichier_log)
                        
                else:
                    if diff_structure_origin_edited1[0]:
                        print("problème structure différentes avec la gdb partagée'. " + diff_structure_origin_edited2[1])
                    else:
                        print("problème structure différentes avec la gdb d'édition'. " + diff_structure_origin_edited1[1])

            else:
                print("Erreur la couche zone_interet_large n'est pas présente à la racine de la gdb fichier")
        else:
            print("Erreur, ces couches ne sont pas présentes sur la gdb partagée : " + str(list_extraction[1]))

    else:  # Pas de replica dans la gdb éditée donc pas d'update
        print(check_replica1[1])

    
except Exception as ex:
    
    print(ex)