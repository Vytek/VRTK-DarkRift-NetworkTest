This is a more complex example that uses players specific to one client, this means creating, destroying them
and syncronising them at the start. If you're brand new to DarkRift have a look at the CubeDemo first.


#############################
Running
#############################

Run a server (go to DarkRiftServer and double click DarkRiftServer.exe).
Open the PlayerDemo scene.
Build and run the PlayerDemo scene to create the first client.
Hit play in the editor to create a second client.

You should now have 2 players on screen, run more clients to get more players (they spawn on top of each other
so use WASD tto move them around!)


############################
Workings
############################

Firstly, as with the CubeDemo, the demo has a DarkRiftReciever.cs script on the main camera, this simply 
repeatedly tells the API to send out the latest data. You'll also notice that each script that uses the 
API has the line "using DarkRift;" this tells the compiler that you want to be able to use the DarkRift 
API.


TagIndex.cs

Have a look at the TagIndex.cs file. Because this is a more complex example and may get complicated, we need a 
nice way of organising the Tags and Subjects for when you start getting a lot of them, this can be used both 
on the server plugins and clients to make life easier. We seriously recommend you do this for every project!


NetworkManager.cs:

As you should guess the NetworkManager script handles the connection, in this case though it also does a little
more. In this example it is responsible for creating players when new people connect and also telling them about 
our player.

As usual the first thing to do is connect to the Server using DarkRift.Connect(IP) but as we need to have data 
sent to us we attach to the onDetailedData event. The only difference to the onData event is that this one passes
the ID of the sender and is called second; we'll use the senders ID to work out which player needs moving. 

The final part of Start() is to talk to tell everyone else we're here! Firstly we send a JoinMessage which will 
trigger everyone to tell us to spawn a player and then we teel everyone to spawn us a player using SpawnPlayer.
As all these are dealt with by the NetworkManagers we attach controller tags to them.

In RecieveData(sender, tag, subject, data) as usual we first check to see if it's for us and then the subject.
For the JoinMessage all we really need to do is tell the sender to spawn a player to represent us so we use the 
SpawnPlayer subject in a reply and pass our position as data.

The SpawnPlayer subject is a litle more complex, we simply instantiate a player object in the position sent to us 
as data and then set it's values. Firstly the player needs to know who it belongs to so we set it's networkID to
the sender's ID so it will follow data sent by that client. Then, because we're spawning our player's object in 
the same way, we need to set it to be controllable if it's ours. To check if a message was sent by us we just 
compare the sender's ID to our ID in DarkRiftAPI.id and then we can allow control or not.


NetworkPlayer.cs

You should recognise most of the Start() routine, the only new thing is the onPlayerDisconnect event; as you 
might expect this is called whenever a player disconnects from the server so is used by us to delete their player 
as it's no longer needed.

The Update() routine is simply used to transmit our position and rotation to the server if we've moved. Firstly 
we need to check if we need to send data by checking if our ID is the same as the player we're watching.
Then if things have changed we send a PlayerUpdate tag with a subject of what's changed and the data of it's new
value.

Our RecieveData(sender, tag, subject, data) method is basicly the same, we check if the data is for our cube then
we update what is needed.

Finally the PlayerDisconnected simply deletes our object if the owning client has disconnected.