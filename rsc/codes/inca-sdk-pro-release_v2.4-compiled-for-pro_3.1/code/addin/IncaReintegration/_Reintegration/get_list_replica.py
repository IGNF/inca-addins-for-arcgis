import arcpy


try:    
    workspace = arcpy.GetParameterAsText(0)
    replicas = arcpy.da.ListReplicas(workspace)
    liste_nom_replica = []
    if len(replicas)==0:
        print ("ERROR_None")
    else:
        for i in range (len(replicas)):
            liste_nom_replica.append(replicas[i].replicaID)
        print (liste_nom_replica)
    
except Exception as ex:
    
    print("Exception occurred!")
 