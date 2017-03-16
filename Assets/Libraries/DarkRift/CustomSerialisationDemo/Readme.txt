This is a more complex version of the CubeDemo. It demonstrates how to use Custom Serialisation within
DarkRift alowing you to chose exactly what data is sent and in what order. Custom serialisation has
many advantages including a much smaller bandwidth, faster serialisation and control over what actually
gets serialised.

To serialise an object yourself you simply need to create a DarkRiftWriter object, add any data you wish
and then send it as you would normally send an object. DarkRift will detect that it should not be 
serialised in the normal way and will instead simply send what you asked to be sent. 

To read an object that has been serialised manually you will recieve a DarkRiftReader object which you
should cast to a DarkRiftReader and then can read the data off. The data must be read off in EXACTLY the
same order you wrote it in.

Both the DarkRiftWriter and DarkRiftReader classes inherit from the .NET BinaryWriter and BinaryReader 
objects respectively meaning that you should always dispose of the objects correctly. In most cases as you
pass the DarkRiftWriter object into DarkRift you do not need to worry obout disposal as DarkRift will 
manage that for you however when using the DarkRiftReader it should be wrapped in a using block so that it
is cleaned up properly.

In the CSCubeMove.cs file you can see two methods (Serialise and Deserialise) showing this in use.