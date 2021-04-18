using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Normal.Realtime;
using UnityEngine;
using UnityEngine.PlayerLoop;

[RequireComponent(typeof(Realtime))]
public class RealTimeInstance : MonoBehaviour {
    private static RealTimeInstance _instance;
    public static RealTimeInstance Instance => _instance;
    
    private Realtime _realtime;
    [SerializeField] private GameObject networkManagerPrefab;
    public GameObject networkManager;
    private NetworkManagerSync _networkManagerSync;
    public bool isConnected;
    public int numberPlayers;
    public bool isSoloMode = false;

    [SerializeField] private Transform realtimeInstancesHolder;

        private void Awake() {
        _instance = this;
        _realtime = GetComponent<Realtime>();
        RegisterToEvents();
    }

    private void RegisterToEvents() {
        // Notify us when Realtime connects to or disconnects from the room
        _realtime.didConnectToRoom += DidConnectToRoom;
        _realtime.didDisconnectFromRoom += DidDisconnectFromRoom;
    }

    private void LateUpdate()
    {
        if (isSoloMode) return;
        numberPlayers = realtimeInstancesHolder.childCount - 1;
    }

    private void DidConnectToRoom(Realtime realtime) {
        networkManager = Realtime.Instantiate(networkManagerPrefab.name, true);
        networkManager.transform.SetParent(realtimeInstancesHolder);
        _networkManagerSync = networkManager.GetComponent<NetworkManagerSync>();
        _networkManagerSync.PlayerConnected();
        isConnected = true;
        MasterManager.Instance.localPlayerNumber = numberPlayers; // set this local player's player number to the current player number (index value)
    }
    
    private void DidDisconnectFromRoom(Realtime realtime) {
        _networkManagerSync.PlayerDisconnected();
        _networkManagerSync = networkManager.GetComponent<NetworkManagerSync>();
        isConnected = false;
    }

    private void OnDisable() {
        _realtime.didConnectToRoom -= DidConnectToRoom;
        _realtime.didDisconnectFromRoom -= DidDisconnectFromRoom;
    }
}