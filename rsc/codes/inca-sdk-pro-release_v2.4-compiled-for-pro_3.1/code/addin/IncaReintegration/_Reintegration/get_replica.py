import arcpy

try:    
    workspace = arcpy.GetParameterAsText(0)
    replicas = arcpy.da.ListReplicas(workspace)
    if len(replicas)==0:
        print ("ERROR_None")
    elif len(replicas)>1:
        print ("ERROR_Several")
    elif len(replicas)==1:
        print ([replicas[0].name,replicas[0].replicaID])
    
except Exception as ex:
    
    print("Exception occurred!")