using FishNet;
using FishNet.Managing;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Managing.Object;
using FishNet.Object;
using FishNet.Transporting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FirstGearGames.LobbyAndWorld.Lobbies;
using FirstGearGames.LobbyAndWorld.Lobbies.JoinCreateRoomCanvases;
using FishNet.Managing.Timing;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class ServerInstancing : NetworkBehaviour
{

    [SerializeField] private GameObject playerNetworkPrefab;
    [SerializeField] private Vector3 playerPosition;
    public Dictionary<string, RoomDetails> currentRoomsRunning = new Dictionary<string, RoomDetails>();
    RoomDetails currentRoom;

    public int localSpawnIndex = 0;

    private const int MAX_PLAYERS = 5;
    private const int WAITING_FOR_PLAYERS_DURATION = 5;


    private enum ParamsTypes
    {
        ServerLoad,
        MemberLeft
    }

    public static ServerInstancing Instance;
    private NetworkManager _networkManager;

    protected void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        _networkManager = FindObjectOfType<NetworkManager>();

        if (_networkManager == null)
        {
            Debug.LogError("NetworkManager not found, HUD will not function.");
            return;
        }
    }


    private void Start()
    {
        InstanceFinder.SceneManager.OnLoadEnd += OnSceneLoadEnd;
    }



    [ServerRpc(RequireOwnership = false)]
    public void QuickRaceConnect(UserData userData, NetworkConnection connection = null, bool isConnectingToLastGame = false)
    {
        if (currentRoom != null)
        {
            Debug.Log("current room is not null");
            JoinRoom(connection, currentRoom, userData);
        }
        else
        {
            Debug.Log("current room is null");
            CreateRoom(() => { JoinRoom(connection, currentRoom, userData); });
        }
    }


    private RoomDetails FindRoomInUserAlreadyExists(UserData _userData)
    {
        return null;
    }



    private void CreateRoom(Action callback = null)
    {
        RoomDetails room = new RoomDetails();
        room.ID = Guid.NewGuid().ToString();
        room.Name = currentRoomsRunning.Count.ToString() + "FarziCafe" + Random.Range(1000, 10000).ToString();
        room.sceneHandle = currentRoomsRunning.Count + 1;
        room.MaxPlayers = MAX_PLAYERS;
        room.dishData = GameManager.Instance.cluesManager.SelectRandomDish();

        if (currentRoomsRunning.ContainsKey(room.ID))
            currentRoomsRunning[room.ID] = room;
        else
            currentRoomsRunning.Add(room.ID, room);

        currentRoom = room;
        callback?.Invoke();
    }

    Coroutine waitForOtherplayers;
    private void JoinRoom(NetworkConnection connection, RoomDetails room, UserData userData)
    {
        Debug.Log(room.Name + " room is joined by:   " + connection.ClientId + "   " +
                  room.currentConnections.Count);

        // Join new or returning user
        if (room.userData.ContainsKey(userData.userDataServer.uid))
        {
            Debug.Log("User is replaced");
            room.userData[userData.userDataServer.uid] = userData;

            // Current Connections
            if (room.currentConnections.ContainsKey(userData.userDataServer.uid))
                room.currentConnections[userData.userDataServer.uid] = connection;
            else
                room.currentConnections.Add(userData.userDataServer.uid, connection);

            // Players
            //if (room.players.ContainsKey(userData.uid))
            //    room.players[userData.uid] = connection.FirstObject.gameObject;
            //else
            //    room.players.Add(userData.uid, connection.FirstObject.gameObject);
        }
        else
        {
            Debug.Log("User is added");
            room.userData.Add(userData.userDataServer.uid, userData);
            room.currentConnections.Add(userData.userDataServer.uid, connection);
            //room.players.Add(userData.uid, connection.FirstObject.gameObject);
        }

        // Add User to Leaderboard on Server
        UserData _userDataRoom = room.userData[userData.userDataServer.uid];
        _userDataRoom.dishData = room.dishData;
        LeaderBoardItem _leaderBoardUser = new LeaderBoardItem() { id = _userDataRoom.userDataServer.uid, userName = _userDataRoom.userDataServer.userName, rank = "0", dishName = _userDataRoom.dishData.Dish_Name, isBot = false };

        if (room.leaderBoard.ContainsKey(userData.userDataServer.uid))
            room.leaderBoard[userData.userDataServer.uid] = _leaderBoardUser;
        else
            room.leaderBoard.Add(_userDataRoom.userDataServer.uid, _leaderBoardUser);


        // Set Room and Dish For Joined User
        ClientServerManager.Instance.SetDataForUser(connection, room, _leaderBoardUser);


        // Start countdown when first user joins the room to wait for other players to join
        if (room.userData.Count == 1)
        {
            print("First user only");
            //room.startTime = (float)ReferenceManager.Instance.timeManager.TicksToTime(TickType.Tick);

            waitForOtherplayers = GameManager.Instance.timer.StartCountdown(WAITING_FOR_PLAYERS_DURATION, 1, room.ID, UpdateWaitingTimeForAllPlayers, () =>
             {
                 // This runs when Wait time finishes


                 // Start Game
                 UpdateWaitingTimeForAllPlayers(0, room.ID);
                 currentRoom = null;
                 StartGame(room.ID, "GamePlay", connection);
             });
        }

        currentRoomsRunning[room.ID] = room;

        if (room.userData.Count == room.MaxPlayers)
        {
            Debug.Log("Room is Full - Start Game");
            currentRoom = null;
            foreach (var conn in room.currentConnections)
            {
                currentRoom = null;
                GameManager.Instance.timer.StopTimer(waitForOtherplayers);
                StartGame(room.ID, "GamePlay", connection);
            }
        }
    }

    public void StartGame(string roomID, string sceneName, NetworkConnection conn, bool isSingleConnection = false, NetworkObject networkObject = null, int _botsCount = 0)
    {
        RoomDetails _room = currentRoomsRunning[roomID];
        List<NetworkObject> networkObjects = new List<NetworkObject>();

        // Bots Setup
        int botsCount = _room.MaxPlayers - _room.userData.Count;
        for (int i = 0; i < botsCount; i++)
        {
            UserData _botData = new UserData().CreateBotData();
            LeaderBoardItem _leaderBoardBot = new LeaderBoardItem()
            {
                id = _botData.userDataServer.uid,
                userName = _botData.userDataServer.userName,
                rank = "5",
                dishName = _room.dishData.Dish_Name,
                isBot = true
            };

            // Set Details For User
            _botData.SetRoomDetails(_room);
            _botData.SetDishData(_room.dishData);
            _botData.SetLeaderboardData(_leaderBoardBot);


            // Add Details to Room
            _room.userData.Add(_botData.userDataServer.uid, _botData);
            //room.currentConnections.Add(userData.uid, connection);
            _room.leaderBoard.Add(_botData.userDataServer.uid, _leaderBoardBot);
            print("Bot Created - " + _botData.userDataServer.uid);

            foreach (var item in _room.currentConnections)
            {
                ClientServerManager.Instance.GenerateBot(item.Value, _botData);
            }
        }

        // Spawn
        foreach (var item in _room.currentConnections)
        {
            networkObjects.Add(SpawnNetworkPlayer(playerNetworkPrefab, _room, item.Value));
        }

        SceneLookupData lookup = new SceneLookupData(_room.sceneHandle, sceneName);
        SceneLoadData sld = new SceneLoadData(lookup);
        sld.MovedNetworkObjects = networkObjects.ToArray();

        LoadParams loadParams = new LoadParams
        {
            ServerParams = new object[]
            {
                ParamsTypes.ServerLoad,
                _room,
                sld
            }
        };

        sld.Options.AllowStacking = true;
        sld.ReplaceScenes = ReplaceOption.None;
        sld.Params = loadParams;
        sld.Options.AutomaticallyUnload = true;

        if (!isSingleConnection)
            InstanceFinder.SceneManager.LoadConnectionScenes(_room.currentConnections.Values.ToArray(), sld);
        else
            InstanceFinder.SceneManager.LoadConnectionScenes(sld);
        _room.hasGameStarted = true;
    }

    private NetworkObject SpawnNetworkPlayer(GameObject _playerPrefab, RoomDetails _room = null, NetworkConnection connection = null)
    {
        Vector3 _position = playerPosition;
        float _multiplier = (connection.ClientId + 1) * 0.04f;
        _position.x = _position.x + playerPosition.x * _multiplier;
        _position.z = _position.z + playerPosition.z * _multiplier;
        print("Player Position - " + _position);

        GameObject _player = Instantiate(_playerPrefab, _position, Quaternion.identity);

        if (connection == null) Spawn(_player);
        else Spawn(_player, connection);

        //_room.players.Add(, go.GetComponent<Player>());

        return _player.GetComponent<NetworkObject>();
    }

    #region Update Time
    private void UpdateWaitingTimeForAllPlayers(float _timeLeft, string _roomID)
    {
        foreach (var item in currentRoom.currentConnections)
        {
            ClientServerManager.Instance.UpdateWaitTime(item.Value, _timeLeft);
        }
    }

    private void UpdateGamePlayTimeForAllPlayers(float _timeGone)
    {
        foreach (var item in currentRoom.currentConnections)
        {
            ClientServerManager.Instance.UpdateGamePlayTime(item.Value, _timeGone);
        }
    }

    private void UpdateRoomTime(float _time, string _roomID)
    {
        if (currentRoomsRunning.ContainsKey(_roomID))
        {
            currentRoomsRunning[_roomID].roomTime = _time;
        }
    }
    #endregion



    [ServerRpc(RequireOwnership = false)]
    public void GetLeaderboard(string _uid, string _roomId)
    {
        if (currentRoomsRunning.ContainsKey(_roomId))
        {
            if (currentRoomsRunning[_roomId].userData.ContainsKey(_uid))
            {
                float _userTime = (float)TimeManager.TicksToTime(TickType.Tick) - currentRoomsRunning[_roomId].startTime;
                //if (currentRoomsRunning[_roomId].userData[_uid].hasCompleted)
                //_userTime = float.Parse(currentRoomsRunning[_roomId].userData[_uid].userDataServer.time);
                //else
                foreach (var item in currentRoomsRunning[_roomId].leaderBoard)
                {
                    if (currentRoomsRunning[_roomId].userData[item.Value.id].hasCompleted && item.Value.id == _uid)
                        continue;

                    item.Value.time = _userTime;
                }
                currentRoomsRunning[_roomId].leaderBoard[_uid].time = _userTime;

                print($"GetLeaderboard: UID - {_uid} | RoomID - {_roomId} | UserTime - {(float)TimeManager.TicksToTime(TickType.Tick) - currentRoomsRunning[_roomId].startTime}");
                print($"GetLeaderboard: UID - {_uid} | RoomID - {_roomId} | UserTime - {currentRoomsRunning[_roomId].userData[_uid].userDataServer.time}");
                ClientServerManager.Instance.SetLeaderboard(currentRoomsRunning[_roomId].currentConnections[_uid], currentRoomsRunning[_roomId].leaderBoard);
            }
        }
    }



    #region Scene
    [ServerRpc(RequireOwnership = false)]
    private void Server_UnLoadScene(NetworkConnection connection, string sceneName, RoomDetails room)
    {
        SceneLookupData lookup = new SceneLookupData(room.sceneHandle, sceneName);
        SceneUnloadData sld = new SceneUnloadData(lookup.Handle);

        UnloadParams unloadParams = new UnloadParams
        {
            ServerParams = new object[]
            {
                ParamsTypes.ServerLoad,
                room,
                sld
            }
        };

        sld.SceneLookupDatas = new[] { lookup };
        sld.Params = unloadParams;
        sld.Options.Mode = UnloadOptions.ServerUnloadMode.UnloadUnused;
        InstanceFinder.SceneManager.UnloadConnectionScenes(connection, sld);
    }

    public void UnLoadSceneViaServer(NetworkConnection connection, string sceneName, RoomDetails room)
    {
        Server_UnLoadScene(connection, sceneName, room);
    }

    public void OnSceneLoadEnd(SceneLoadEndEventArgs obj)
    {
        if (base.IsClient)
        {
            //RoomData roomData = SaveSystem.Load();
            //roomData.IsEnded = true;
            //roomData.IsSimulationStarted = true;
            //UserMetaData.currentRoom.IsSimulationStarted = true;
            //SaveSystem.Save(roomData);
        }

        if (!base.IsServer)
            return;

        object[] temp_ServerParams = obj.QueueData.SceneLoadData.Params.ServerParams;

        if (temp_ServerParams.Length == 0)
            return;


        RoomDetails temp = temp_ServerParams[1] as RoomDetails;

        foreach (var item in temp.currentConnections)
        {
            ClientServerManager.Instance.StartGamePlay(item.Value);
            //ClientServerManager.Instance.ActivatePlayerScripts(item.Value, item.Key);
        }


        // Start Timer
        temp.startTime = (float)TimeManager.TicksToTime(TickType.Tick);
        //GameManager.Instance.timer.StartTimer(0, 1, false, temp.ID, UpdateRoomTime, null);


        Debug.Log("Sceneloaded and even argument Room name" + " " + temp.Name);

        Debug.Log("Loaded scenes count" + " " + obj.LoadedScenes.Length);

        int handle = obj.QueueData.SceneLoadData.SceneLookupDatas[0].Handle;
        foreach (Scene scene in obj.LoadedScenes)
        {
            //Debug.Log("loaded scene handle" + " " + scene.handle);
        }

        //Debug.Log("Handle data on scene load end" + " " + handle);


        foreach (NetworkConnection connection in obj.QueueData.Connections)
        {

        }
    }
    #endregion



    [ServerRpc(RequireOwnership = false)]
    public void StopGame(UserData _userData, string _roomId, string _sceneName, bool _unloadForAll = false, NetworkConnection connection = null)
    {
        if (currentRoomsRunning.ContainsKey(_roomId))
        {
            print(_userData.userDataServer.userName + " - Unloading Scene For Room - " + currentRoomsRunning[_roomId].Name);
            currentRoomsRunning[_roomId].userData[_userData.userDataServer.uid].userDataServer.score = _userData.userDataServer.score;

            SceneUnloadData sud = new SceneUnloadData(_sceneName);
            NetworkConnection[] conns;
            if (_unloadForAll)
                conns = currentRoomsRunning[_roomId].currentConnections.Values.ToArray();
            else
                conns = new NetworkConnection[] { connection };

            NetworkManager.SceneManager.UnloadConnectionScenes(conns, sud);

            if (currentRoomsRunning[_roomId].IsGameCompleted())
                currentRoomsRunning.Remove(_roomId);
        }
    }






    private void ClienDisConnected(NetworkConnection connection, RoomDetails room)
    {
        // if client is in a temp room waiting
        if (room.MemberIds.Count < room.MaxPlayers)
            DisconnectedInWait(connection);
    }

    [ServerRpc(RequireOwnership = false)]
    public void OnClientDisConnectedRpc(NetworkConnection connection = null)
    {
        Debug.Log("Client disconnected with id" + "  " + connection.ClientId);
        Debug.Log("Client disconnected with address" + "  " + connection.GetAddress());
    }

    private void DisconnectedInWait(NetworkConnection connection)
    {
    }

    private void DisconnectedGameRunning(NetworkConnection connection)
    {
    }

    public void OnRemoteConnectionStateChanged(NetworkConnection connection)
    {

    }

    private void OnDestroy()
    {
        if (InstanceFinder.SceneManager)
            InstanceFinder.SceneManager.OnLoadEnd -= OnSceneLoadEnd;
    }




    [ContextMenu("PrintRoomDetails")]
    private void PrintRoomDetails()
    {
        if (currentRoom != null) print(JsonConvert.SerializeObject(currentRoom.leaderBoard));
        else print("Current room is NULL");
    }


}