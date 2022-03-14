# NetworkBehavior
Low level library of TCP and UDP communications for desktop apps.

## Overview
Easy to setup library that will allow you to create a server and clients communication, using it
all network objects will inherit from `NetworkIdentity` and will have those features:
  1. __Sync variables through all clients and server__, with one line of code you can update a varibale in all clients
  1. __Broadcast method__, with one line of code you can broadcast methods with supported parameters
  1. __Sync all cliens automatically__, new clients will automatically be sync with all the relevent object and object's variables


## Usage
### Referencing the library
First referance the dll, keep in mind that this lib uses other libs

### Runing a server
1. Create a `ServerBehavior` object with the wanted port, for instance:  
`ServerBehavior serverBehavior = new ServerBehavior(1331);`
1. Run the server:  
`serverBehavior.Run();`

### Creating a client and connecting
1. Create a `ClientBehavior` object with the wanted port and the server's ip address, for instance:  
`ClientBehavior client = new ClientBehavior(1331, "ServerHostName");`
2.  Connect the client:  
`client.Connect();`

### Network objects
_All of the examples that are shown here use MonoGame Lib, network features have nothig to do with that lib it's just examples from 
a real project_

#### Creating and initilazing 

1. First we need to define our objects, this object will need to inherate from `NetworkIdentity`.  
So lets say we are building a game and we have this object:
    <details>
      <summary>GameObject</summary>

      ```c#
       public class GameObject : NetworkIdentity  
            {
                public virtual Vector2 DrawLocation { get; set; }
                public virtual float SyncX
                {
                    get => syncX; set
                    {
                        syncX = value;
                        InvokeSyncVarNetworkly(nameof(SyncX), value, NetworkInterfaceType.UDP);
                        OnXSet();
                    }
                }

                public virtual float SyncY
                {
                    get => syncY; set
                    {
                        syncY = value;
                        InvokeSyncVarNetworkly(nameof(SyncY), value, NetworkInterfaceType.UDP);
                        OnYSet();
                    }
                }

                protected readonly float DEFAULT_MIN_DISTANCE_TO_UPDATE = 5;
                private float syncX;
                private float syncY;

                public GameObject()
                {
                    SyncX = -9999;
                    SyncY = -9999;
                    OnNetworkInitializeEvent += OnNetworkInitialize;
                    OnDestroyEvent += OnDestroyed;
                }

                public virtual void OnNetworkInitialize()
                {
                    DrawLocation = new Vector2(SyncX, SyncY);
                }

                public virtual void OnXSet()
                {
                    if (MathHelper.Distance(DrawLocation.X, SyncX) >= DEFAULT_MIN_DISTANCE_TO_UPDATE)
                    {
                        DrawLocation = new Vector2(SyncX, DrawLocation.Y);
                    }
                }

                public virtual void OnYSet()
                {
                    if (MathHelper.Distance(DrawLocation.Y, SyncY) >= DEFAULT_MIN_DISTANCE_TO_UPDATE)
                    {
                        DrawLocation = new Vector2(DrawLocation.X, SyncY);
                    }
                }

                public abstract void OnDestroyed(NetworkIdentity identity);
            }
      ```
    </details>  

    Right now you dont really need to understand what is going on there just remember that we have a network object class called `GameObject`
1. Before creating the object we need to _register_ it in the server and all clients, registering is done by simply creating an instance 
_(Do not use this object)_:  
`new GameObject();`  
You must register all network object __before__ connecting to a server, the server also needs to register all network objects.

1. Then we want to create an instance in the sever and all clients, __only the server can create new network objects:__
    1. If the object is a server's object (meaning only the server should control it):  
    `SpawnWithServerAuthority(typeof(GameObject));`
    1. If the object is belong to a client you need the client id, then simply:  
    `SpawnWithClientAuthority(typeof(GameObject), clientId);`
1. _TBC_
#### Communicating
_TBC..._
