

# Add-Ins for ArcGIS Pro as part of the INCA project



```
Language:              C#
Subject:               INCA Add-Ins for ArcGIS Pro
Project:               INCA
Contributor:           INCA Project Team
Organization:          IGN France
Date de mise-à-jour:   21/09/2022
ArcGIS Pro:            2.9 (and 2.6 jusqu'à la version 1.0) 
.NET Target Framework: 4.8
```


## 1. Add-In *Maplex Engine Config*
This extension has been developed to help the user straightforwardly configure the Maplex Engine. The Add-In takes an Excel configuration file as input, browses the values defined in it, and executes the Maplex Engine automatically.

![Maplex Engine Config](https://github.com/IGNF/inca-addins-for-arcgis/blob/main/rsc/img/addin-mec-global-preview.jpg?raw=true)  

Resources
-----------
- [*User guide*](https://github.com/IGNF/inca-addins-for-arcgis/blob/main/rsc/doc/user_guide/Add-In_MEC_-_User_guide.pdf?raw=true)

- [*Maplex config file template*](https://github.com/IGNF/inca-addins-for-arcgis/blob/main/rsc/maplex_engine_config_file_template.xlsx?raw=true)


## 2. Add-In *Query Manager*
This extension is divided in 2 sections:
## 2.1. *Query*
The query manager allows you to create a table with references to objects from several layers, in order to store the result of a selection in memory. Either using pre-saved queries or keeping the current selection.
![Query](https://github.com/IGNF/inca-addins-for-arcgis/blob/main/rsc/img/addin-queryman-query-preview.jpg?raw=true)

## 2.2. *Edit Queue*
The edit queue is used to browse one by one the objects of a pre-recorded table that may contain references to several layers. The pointed object is selected. Ability to navigate to the first or last object in the list, and to jump to rank n/n_max.
![Edit Queue](https://github.com/IGNF/inca-addins-for-arcgis/blob/main/rsc/img/addin-queryman-edit-query-preview.jpg?raw=true)

Resources
-----------
- [*User guide*](https://github.com/IGNF/inca-addins-for-arcgis/blob/main/rsc/doc/user_guide/Add-In_QM_-_User_guide.pdf?raw=true)

---

> # **Releases**
> 
> ### *v0.9*
>
>  1. `Maplex Engine Config`
>	 - [Compiled for ArcGIS Pro 2.6](https://github.com/IGNF/inca-addins-for-arcgis/blob/main/releases/0.9/for-pro-2.6/MaplexEngineConfig.esriAddinX?raw=true)
>	 - [Compiled for ArcGIS Pro 2.9](https://github.com/IGNF/inca-addins-for-arcgis/blob/main/releases/0.9/for-pro-2.9/MaplexEngineConfig.esriAddinX?raw=true)
> ---
>  2. `Query Manager`
>	 - [Compiled for ArcGIS Pro 2.6](https://github.com/IGNF/inca-addins-for-arcgis/blob/main/releases/0.9/for-pro-2.6/QueryManager.esriAddinX?raw=true)
>	 - [Compiled for ArcGIS Pro 2.9](https://github.com/IGNF/inca-addins-for-arcgis/blob/main/releases/0.9/for-pro-2.9/QueryManager.esriAddinX?raw=true)
> ---
> 
> ### *v1.0*
>
>  1. `Maplex Engine Config`
>	 - [Compiled for ArcGIS Pro 2.6](https://github.com/IGNF/inca-addins-for-arcgis/blob/main/releases/1.0/for-pro-2.6/MaplexEngineConfig.esriAddinX?raw=true)
>	 - [Compiled for ArcGIS Pro 2.9](https://github.com/IGNF/inca-addins-for-arcgis/blob/main/releases/1.0/for-pro-2.9/MaplexEngineConfig.esriAddinX?raw=true)
> ---
>  2. `Query Manager`
>	 - [Compiled for ArcGIS Pro 2.6](https://github.com/IGNF/inca-addins-for-arcgis/blob/main/releases/1.0/for-pro-2.6/QueryManager.esriAddinX?raw=true)
>	 - [Compiled for ArcGIS Pro 2.9](https://github.com/IGNF/inca-addins-for-arcgis/blob/main/releases/1.0/for-pro-2.9/QueryManager.esriAddinX?raw=true)
> ---
>  
> ### *v1.1*
>
>  1. `Maplex Engine Config`
>	 - [Compiled for ArcGIS Pro 2.9](https://github.com/IGNF/inca-addins-for-arcgis/blob/main/releases/1.1/for-pro-2.9/MaplexEngineConfig.esriAddinX?raw=true)
> ---
>  2. `Query Manager`
>	 - [Compiled for ArcGIS Pro 2.9](https://github.com/IGNF/inca-addins-for-arcgis/blob/main/releases/1.1/for-pro-2.9/QueryManager.esriAddinX?raw=true)
>	