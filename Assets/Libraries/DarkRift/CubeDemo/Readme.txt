This is a simple example to show the how objects can be synced across clients in a peer-to-peer system (no one
has overall control).

The example has 2 scripts: CubeNetworkManager.cs which is responsible for connecting and disconnecting to the 
server, and CubeMove.cs which is responsible for telling the other clients when the cube has moved and for 
moving the cube to a new location when another client has moved it.


#############################
Running
#############################

Run a server (go to DarkRiftServer and double click DarkRiftConsole.exe).
Open the CubeDemo scene.
Build and run the CubeDemo scene to create the first client.
Hit play in the editor to create a second client.

You should now be able to drag the 3 cubes around and they will move on both clients!


############################
Workings
############################

Firstly, the demo has a DarkRiftReciever.cs script on the main camera, this simply repeatedly tells the API to
send out the latest data. You'll also notice that each script that uses the API has the line "using DarkRift;"
this tells the compiler that you want to be able to use the DarkRift API.

Lets first have a look a CubeNetworkManager.cs. CubeNetworkManager is responsible for creating the connection 
in Start() and disconnecting when the game is stopped in OnApplicationQuit(). To connect it simply calls 
DarkRiftAPI.Connect(IP) and to disconnect it simply calls DarkRiftAPI.Disconnect().


CubeMove.cs is slightly more complicated; each cube has this script attached and is assigned an ID (the cubeID
field) in the inspector, this is then used to identify which cube is being moved later.

Have a look at the start routine, here we attach the OnDataRecieved method to the onData event. This event is 
fired everytime data is recieved and so all methods attached to this event will be called. The event passes 3 
arguments to the OnDataRecieved routine: the tag, the subject and the data. For more information on what these
should be used for and what the events do have a look at the Documentation.

OnDataRecieved is called everytime data is sent, so we need to make sure that the data is for the cube we are 
attached to. To do this we check if the subject is the same as our ID, if it is we want to update the position,
if it's not we ignore it. You'll usually need to cast the data variable to the type you need as your script 
reads in as a generic 'object' type, here we need a Vector3 so simply cast data to it.

When an object in Unity is dragged with the mouse OnMouseDrag is called so we use this to detect when we need 
to send data. Firstly we will get an error if we are not connected to a server so we use 
DarkRiftAPI.isConnected to test this; if we are connected then we need to send the new position using 
DarkRiftAPI.SendToOthers(tag, subject, data). To avoid too much data being sent we only send when we've 
moved further than 0.05  The first parameter is the tag that we send with the object, this should state what 
topic the message describes eg, a change on the player, a change in environment, a chat message etc - as the 
only data we ever send is data about the cube moving we always use tag 0. The subject is a more refined topic,
for instance whether the player moved or turned - in our case we are using it to tell which cube was moved so
we send the cubeID. Finally the data is of any type and tells us the majority of what happened like where 
the player moved to or which direction they are now facing in, in this case it's the cube's new position.


This is a very simple example in reality as it has no authorative parts to it, no plugins to talk with and 
only ever uses 1 tag. You project is likely to be much more complex so have a look at the Player Demo.


