# Introduction 
This project contains various dlls for use with the MyCME in Aptify.  The file set contains 5 dlls and is written in c sharp.

MainFormLC

ACSCMECESendEvents

ACSCMEEventSetDeliveryMethod

ACSCMECEPersonBuildXML

ACSCMECEPersonSubmitXML



# Getting Started
Getting your code up and running on your own system:
1.	Clone this project to a folder on your drive.


# Build and Test
1.	Check the build events (if you are using KillAptify leave the build events as is, if not remove the Pre and Post Build Events)
2.	Make your changes and build the project.
3.	Copy the dll into your desired instance of Aptify
4.  Update the ORO (Object Repository Object) for your dlls
5.  If the project will be utilized from a web page you must ensure the dlls are also in the following libs on the server.
    Projects bin (project using the dll)
    Services bin (Aura Services bin)

