This demo demonstrates how to run a DarkRift server from Unity allowing you to access Unity's physics, navmesh 
and other features. Running a server from Unity can be very useful but can also impact performance and make
other parts of your code more complicated.

EmbeddedCubeMove is a similar script to that in CubeDemo except that it sends updates using SentToServer instead
of SendToOthers so that only the server receives it. EmbeddedCubeManager is the server script that sends constant
updates to all of the clients.

To get a full understanding of how to use Unity based DarkRift servers look in the documentation folder.

#################
Running
#################

To run this demo make a build of the server scene and another of the client scene. You can run the server on 
your PC and then any clients you run after will connect to it and show the same cube layout across both the 
server and cients.
