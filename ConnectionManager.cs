using FishNet;
using UnityEngine;

public class ConnectionManager : MonoBehaviour
{
    public bool isServer;

    void Start()
    {
        if (isServer) InstanceFinder.ServerManager.StartConnection();
        else InstanceFinder.ClientManager.StartConnection();
    }
}
