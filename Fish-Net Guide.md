# Fish-Net Guide

## Installation 
* Install fish-networking using the github link. https://github.com/FirstGearGames/FishNet
* Install ParrelSync for testing multiplayer directly from the editor from the link. https://github.com/VeriorPies/ParrelSync


## Basics of FisNnet
### Concepts of communcation between Server and Client.
> ServerRPC -> It will be implemented in the server side script and run only on the server. It will be called from the client side.

> TargetRPC -> It will be implemented in the client side script and run only on the specfic client passed by the connection param. It will be called from the server side.
 
> ObserverRPC -> It is the same as TargetRPC except all the observers will recieve the call from the Server.

> NetworkBehaviour -> All the scripts that implemen RPCs must inheririt from this instead of MonoBehaviour.

> NetworkObject -> Attach this component to sahre the object on the network.

> NetworkTransform -> Attach this component to Sync the transform component among all the clients.

> NetworkAnimation -> Attach this component to share animation among all the clients.

### Other Recommendations
* Make a script to run on the Server which will contain all the ServerRPCs. Write only the logic which will execute on the server side.
* Make a separate script to run on the Client side which  will contain all the TartgetRPCs
* Make sure to use IsOwner from the NetworkBehaviour to avoid executing a function on each client.
