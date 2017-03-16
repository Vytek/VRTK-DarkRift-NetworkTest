HeavySyncDemo is designed to show an example of DarkRift in an RTS game where there are lots of units in play at once. It also shows DarkRift in use as an authoritative server rather then a passive server as the other demos show.

Each new player is allocated 100 units which it randomly moves around the scene, the server validates each movement update and then distributes only valid updates.

To run the demo you'll need to place the plugin DLL file (in the zipped folder under bin/Release) in the server's Plugins directory and then you can start the server. This demo isn't interactive.