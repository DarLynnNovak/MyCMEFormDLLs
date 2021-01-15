The following files were created to assist in the recycling of Aptify for DLL building.

To use the files download the entire set to your program files directory.

Open the files title Start... and ensure that the path to each Aptify instance matches your computers path to the Aptify Instances.

You will need to configure your visual studio to use the files by:

In VS Choose the Build menu and then Configuration Manager and create a configuration for each Aptify instance you wish to use.

## https://dev.azure.com/facs-teamservices/Aptify/_git/ACSMyCMEFormDLLs?path=%2FKillAptify%2FConfigManager.jpg

Then you need to go to your project properties and the 'build events' tab and add this to the pre-build:
call "C:\Program Files\KillAptify\KillAptify.bat" (change to the directory you put the kill aptify files in)

and in the post-build:  (One for each instance of aptify you use.).

if "$(ConfigurationName)" == "Aptify Test" (call "C:\Program Files\KillAptify\StartAptifyTest.bat")
if "$(ConfigurationName)" == "Aptify Staging" (call "C:\Program Files\KillAptify\StartAptifyStage.bat")
if "$(ConfigurationName)" == "Aptify Prod" (call "C:\Program Files\KillAptify\StartAptifyProd.bat")
if "$(ConfigurationName)" == "Aptify Test 6" (call "C:\Program Files\KillAptify\StartAptifyTest60.bat")
if "$(ConfigurationName)" == "Aptify Staging 6" (call "C:\Program Files\KillAptify\StartAptifyStage60.bat")
if "$(ConfigurationName)" == "Aptify Prod 6" (call "C:\Program Files\KillAptify\StartAptifyProd60.bat")

*Note you will want to ensure each of the files above have the correct path to that aptify instance.

While still in the project properties, you will want to go to the build tab and change the output path for each of the Aptify instance to match your Aptify Instance directory path.

