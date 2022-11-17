using System;
using System.Collections;
using System.Collections.Generic;
using FirstGearGames.LobbyAndWorld.Lobbies.JoinCreateRoomCanvases;
using UnityEngine;
using FishNet.Connection;
using FishNet.Object;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;
using FishNet;

public class ClientServerManager : NetworkBehaviour
{

    public GameObject botPrefab;

    public double serverTime = 0;


    public static ClientServerManager Instance;
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {

    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        GameManager.IsOwner = IsOwner;
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if (IsOwner)
        {
            GameManager.IsOwner = false;
            GameManager.Instance.hasGameStarted = false;
        }
    }


    [TargetRpc]
    public void SetDataForUser(NetworkConnection connection, RoomDetails _room, LeaderBoardItem _leaderBoard)
    {
        print("SetRoomAndDishDetailsForUser");

        //RoomData roomData = new RoomData()
        //{
        //    Name = _room.Name,
        //    IsSimulationStarted = _room.IsSimulationStarted,
        //    IsEnded = _room.IsEnded,
        //};

        // SaveSystem.Save(roomData);

        //PlayerPrefs.SetString(UserMetaData.myAddress, JsonConvert.SerializeObject(roomData));
        //PlayerPrefs.Save();

        //Debug.Log("Room Details Saved :   " + roomData.Name);

        //UserMetaData.currentRoom = _room;

        //if (!IsOwner) return;

        GameManager.Instance.SetClientId(connection.ClientId.ToString());
        GameManager.Instance.SetRoomDetails(_room);
        GameManager.Instance.SetDishData(_room.dishData);
        GameManager.Instance.SetLeaderboardData(_leaderBoard);
        GameManager.Instance.cluesManager.Setup(_room.dishData);

        //Debug.Log("DIsh COunt For USer:   " + _room.userData[UserMetaData.myAddress].userDishes.selectedDish.Count);
    }


    [TargetRpc]
    public void ActivatePlayerScripts(NetworkConnection connection, GameObject _player)
    {
        _player.GetComponent<Player_Controller>().enabled = true;
        _player.GetComponent<Player_Interaction>().enabled = true;
    }


    [ContextMenu("GetLeaderboard")]
    public void GetLeaderboard()
    {
        //if (!IsOwner) return;
        ServerInstancing.Instance.GetLeaderboard(GameManager.Instance.GetUserData().userDataServer.uid, GameManager.Instance.GetUserData().userDataServer.roomId);
    }


    [TargetRpc]
    public void SetLeaderboard(NetworkConnection networkConnection = null, Dictionary<string, LeaderBoardItem> _leaderBoard = null)
    {
        print("Set leaderBoard Target RPC");
        ScreenManager.Instance.leaderboardScreen.SetLeaderboard(_leaderBoard);
    }


    [TargetRpc]
    public void StartGamePlay(NetworkConnection networkConnection)
    {
        print("Gameplay Started " + gameObject.name);
        PopUpManager.Instance.HideWaitingForOtherPlayers();
        ScreenManager.Instance.SwitchScreen(ScreenManager.Instance.menuScreen.gameObject, ScreenManager.Instance.gameplayScreen.gameObject);
        ScreenManager.Instance.gameplayScreen.StartGamePlay();
        GameManager.Instance.hasGameStarted = true;
    }

    [TargetRpc]
    public void UpdateWaitTime(NetworkConnection networkConnection, float _timeLeft, string _onfinishMsg = null)
    {
        PopUpManager.Instance.UpdateWaitingTime(_timeLeft);
    }

    [TargetRpc]
    public void UpdateGamePlayTime(NetworkConnection networkConnection, float _timeGone)
    {
        ScreenManager.Instance.gameplayScreen.UpdateGamePlayTime(_timeGone);
    }

    [TargetRpc]
    public void GenerateBot(NetworkConnection _connection, UserData _botData)
    {
        AI aI = Instantiate(botPrefab).GetComponent<AI>();
        GameManager.Instance.AddBot(_botData);
    }



    public void Update()
    {
        //serverTime = TimeManager.TicksToTime(TickType.Tick);
    }




    private bool AllUserAreReady(string roomName)
    {
        if (ServerInstancing.Instance.currentRoomsRunning[roomName].userData.Count != ServerInstancing.Instance.currentRoomsRunning[roomName].MaxPlayers)
            return false;


        foreach (var userData in ServerInstancing.Instance.currentRoomsRunning[roomName].userData.Values)
        {
            //if (userData.userDishes.selectedDish.Count != 2)
            //    return false;
        }

        return true;
    }

    [TargetRpc]
    public void OnMemberReady(NetworkConnection connection, RoomDetails roomDetails)
    {
        //if (_lobbyManager)
        //    _lobbyManager.OnMembeGetReady(roomDetails);
    }

    //IEnumerator StartGameAfterDelay(string roomName)
    //{
    //    yield return new WaitForSeconds(3f);
    //    ServerInstancing.instance.StartGame(roomName, "GamePlay");
    //}

    [TargetRpc]
    //public void GetCustomersData(NetworkConnection connection, Dictionary<string, UserData> userData, RoomDetails room, SimulationData simulationDatas)
    public void GetCustomersData(NetworkConnection connection, Dictionary<string, UserData> userData, RoomDetails room)
    {
        //UserMetaData.currentRoom = room;
        //UserMetaData.userData = new Dictionary<string, UserData>(userData);
        //UserMetaData.customersSimulation = simulationDatas;
    }

    public class Startplay
    {
        public List<string> addresses = new List<string>();
        public int fee;
    }

    IEnumerator StartPlay(RoomDetails room, Action callback)
    {
        Startplay startplay = new Startplay();


        startplay.fee = 10;
        foreach (var data in room.userData.Values)
        {
            //startplay.addresses.Add(data.address);
        }


        JObject obj = JObject.FromObject(new
        {
            startplay = startplay
        });


        string json = JsonConvert.SerializeObject(obj);


        //using (UnityWebRequest www = UnityWebRequest.Put(Constants.BASE_URL + "startPlay", json))
        using (UnityWebRequest www = UnityWebRequest.Put("startPlay", json))
        {
            www.method = UnityWebRequest.kHttpVerbPOST;
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Accept", "application/json");

            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                //Result awardObject = StartRewardDistributing(room);

                Debug.Log("Start Game Data:   " + json);
                Debug.Log("Game Started!    -     " + www.downloadHandler.text);

                //StartCoroutine(DistributeRewards(awardObject, room));
                callback();
            }
        }
    }

    //public Result StartRewardDistributing(RoomDetails room)
    //{
    //    Result result = new Result();

    //    int _score = 0;
    //    int index = 0;

    //    foreach (var data in room.userData.Values)
    //    {
    //        _score = 0;
    //        foreach (var dish in data.userDishes.selectedDish)
    //        {
    //            _score += dish.price / 2;
    //        }

    //        Address temp = new Address()
    //        {
    //            address = data.address,
    //            score = _score
    //        };
    //        result.addresses.Add(temp);
    //        index++;
    //    }
    //    return result;
    //}


    //IEnumerator DistributeRewards(Result result, RoomDetails room)

    IEnumerator DistributeRewards(RoomDetails room)
    {
        yield return new WaitForSeconds(90f);


        JObject obj = JObject.FromObject(new
        {
            //result = result
        });


        string json = JsonConvert.SerializeObject(obj);


        //using (UnityWebRequest www = UnityWebRequest.Put(Constants.BASE_URL + "distributeRewards", json))
        using (UnityWebRequest www = UnityWebRequest.Put("distributeRewards", json))
        {
            www.method = UnityWebRequest.kHttpVerbPOST;
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Accept", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                if (ServerInstancing.Instance.currentRoomsRunning.ContainsKey(room.Name))
                    ServerInstancing.Instance.currentRoomsRunning.Remove(room.Name);

                Debug.Log("Reward post Data!    -     " + json);
                Debug.Log("Reward Distributed!    -     " + www.downloadHandler.text);
            }
        }
    }
}