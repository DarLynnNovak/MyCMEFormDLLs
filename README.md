# Introduction 
This project contains various dlls for use with the MyCME in Aptify.  The file set contains 5 dlls and is written in c sharp.

MainFormLC: (controls elements on the main form, ie tabs, form fields)

ACSCMECESendEvents: (Initiates and builds the xml for the events choosen in the grid after clicking the Send to CE Broker button)

ACSCMEEventSetDeliveryMethod: (Sets the delivery method for the event to coincide with the delivery methods provided by the CE Broker, this is done on the save)

ACSCMECEPersonBuildXML: (Builds the XML for submitting person cme to CE broker based on dates choosen from the web page:)

http://dev.facs.org/cme

http://qa.facs.org/cme

ACSCMECEPersonSubmitXML:  (Submits XML to CE Broker from above steps)



# Getting Started
Getting your code up and running on your own system:
1.	Clone this project to a folder on your drive.


# Build and Test
1.	Check the build events (if you are using KillAptify leave the build events as is, if not remove the Pre and Post Build Events)
2.	Make your changes and build the project.
3.	Copy the dll into your desired instance of Aptify (if using KillAptify this will be done autmatically, ignore this step)
4.  Update the ORO (Object Repository Object) for your dlls
5.  If the project will be utilized from a web page you must ensure the dlls are also in the following libs on the server.
    Projects bin (project using the dll)
    Services bin (Aura Services bin)

