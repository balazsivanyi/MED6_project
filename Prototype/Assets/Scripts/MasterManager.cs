using Normal.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using UnityEngine;
using UnityEngine.UI;

public class MasterManager : MonoBehaviour {
    private static MasterManager _instance;

    public static MasterManager Instance {
        get {
            if (_instance != null) return _instance;
            var masterManagerGameObject = new GameObject();
            _instance = masterManagerGameObject.AddComponent<MasterManager>();
            masterManagerGameObject.name = typeof(MasterManager).ToString();
            return _instance;
        }
    }

    [Header("Drums")] public int numberInstruments = 5;
    public int numberOfNodes = 16;
    [SerializeField] private GameObject nodesPanelPrefab;
    [SerializeField] private GameObject effectsPanelPrefab;

    public GameObject[] nodesPanels;
    public GameObject[] effectsPanels;

    public Color[] drumColors;
    public Color[] defaultNodeColors;

    [SerializeField] private AK.Wwise.Event[] classicDrums = new AK.Wwise.Event[5];
    [SerializeField] private AK.Wwise.Event[] djembeDrums = new AK.Wwise.Event[5];
    [SerializeField] private AK.Wwise.Event[] edmDrums = new AK.Wwise.Event[5];
    [SerializeField] private AK.Wwise.Event[] hangDrums = new AK.Wwise.Event[5];
    [SerializeField] private AK.Wwise.Event[] ambientDrums = new AK.Wwise.Event[5];

    public AK.Wwise.Event[] currentDrums = new AK.Wwise.Event[5];

    [SerializeField] private Dictionary<int, AK.Wwise.Event[]> drumDictionary = new Dictionary<int, AK.Wwise.Event[]>();
    // nodes stuff
    public List<NodeManager> _nodeManagers = new List<NodeManager>();
    public float dwellTimeSpeed = 100.0f;

    public UserInterfaceManager userInterfaceManager;

    [SerializeField] private ScreenSync[] _screenSyncs;

    [Space] [Header("Timer")] public int bpm = 120;
    [SerializeField] private GameObject timerPrefab;
    private GameObject timerGameObject;
    public Timer timer;

    [Space] [Header("Player")] public Camera playerCamera;
    // public List<Player> Players = new List<Player>();
    public ObservableCollection<Player> Players = new ObservableCollection<Player>();
    
    private Transform _playerPosition;
    private float startTime, journeyLength;
    [SerializeField] private float positionSpeed = 10.0f;
    [SerializeField] private Transform playerStartPosition, playerPositionDestination;
    public int localPlayerNumber = 0;
    public bool gameSetUpFinished;
    public float currentRotationOfUI = 0.0f;
    public Player player;
    public bool isInPosition = false;
    public bool isWaitingInLobby = true;

    [Space] [Header("Signifiers")] [SerializeField]
    private GameObject topDownSignifier, mainSignifier;

    [SerializeField] private GameObject timerUI;
    public bool DwellSettingsActive;
    public GameObject dwellSettingsPrefab;
    public DataSync dataMaster;
    private void Start() {
        if (_instance == null) _instance = this;
        Players.CollectionChanged += OnPlayersChanged;
        drumDictionary.Add(0, classicDrums);
        drumDictionary.Add(1, djembeDrums);
        drumDictionary.Add(2, edmDrums);
        drumDictionary.Add(3, hangDrums);
        drumDictionary.Add(4, ambientDrums);
        
        currentDrums = classicDrums;

    }

    private void OnPlayersChanged(object sender, NotifyCollectionChangedEventArgs e) {
        print("Players changed");
        foreach (var p in Players) {
            print("We have this player: " + p);
        }

        RealTimeInstance.Instance.numberPlayers = Players.Count;
    }

    private void FixedUpdate() {
        if (!isWaitingInLobby && !isInPosition) {
            // Distance moved equals elapsed time times speed..
            float distCovered = (Time.time - startTime) * positionSpeed;
            // Fraction of journey completed equals current distance divided by total distance.
            float fractionOfJourney = distCovered / journeyLength;
            var playerCameraTransform = playerCamera.transform;
            playerCamera.transform.position = Vector3.Lerp(playerCameraTransform.position,
                playerPositionDestination.position, fractionOfJourney);
            playerCamera.transform.rotation = Quaternion.Lerp(playerCameraTransform.rotation,
                playerPositionDestination.rotation, fractionOfJourney);

            if (Vector3.Distance(playerCameraTransform.position, playerPositionDestination.position) < 0.1f) {
                isInPosition = true;
                mainSignifier.SetActive(true);
                StartCoroutine(userInterfaceManager.SwitchPanelRenderLayers());
                if (!RealTimeInstance.Instance.isSoloMode)
                {
                    timerUI.SetActive(true);
                    if (RealTimeInstance.Instance.numberPlayers >= 2)
                        timer.localTimer = timer.timer;
                    timer.ToggleTimer(true);
                    StartCoroutine(dataMaster.CalculateAverageTime());
                }
            }
        }
    }

    public void Initialize() {
        nodesPanels = new GameObject[numberInstruments];
        effectsPanels = new GameObject[numberInstruments];

        // create the rotations for the panels -->  (360.0f / (numberInstruments * 2) * -1)  degrees difference between each other in the Y axis
        Vector3[] panelRotations = new Vector3[10]; // number of instruments * 2
        var rotationValue =
            360.0f / (numberInstruments * 2) * -1; // if 5 instruments --> 360.0f / (5 * 2) * -1 = -36.0f; 
        for (int i = 0; i < panelRotations.Length; i++) {
            panelRotations[i] = new Vector3(0, i * rotationValue, 0);
        }

        // TODO: Set the size of the panels' canvas (radius distance to the origin point)

        if (RealTimeInstance.Instance.isSoloMode) {
            InstantiatePanelsSoloMode();
            StartCoroutine(WaitToPositionCamera(0.5f));
            return;
        }

        // position player at the top view
        //playerCamera.transform.position = playerStartPosition.position;
        StartCoroutine(WaitUntilConnected());
    }

    private void InstantiatePanelsSoloMode() {
        Vector3 rotationValue = Vector3.zero;
        var nodesPanelsGo = new GameObject("Nodes panels");
        nodesPanelsGo.transform.SetParent(userInterfaceManager.transform);
        var effectsPanelsGo = new GameObject("Effects panels");
        effectsPanelsGo.transform.SetParent(userInterfaceManager.transform);

        for (int i = 0; i < nodesPanels.Length; i++) {
            nodesPanels[i] = Instantiate(nodesPanelPrefab, transform.position, Quaternion.Euler(rotationValue));
            nodesPanels[i].transform.SetParent(nodesPanelsGo.transform);
            nodesPanels[i].name = "NodesPanel_" + (DrumType) i;
            rotationValue += new Vector3(0, 360.0f / (numberInstruments * 2) * -1, 0);
            effectsPanels[i] = Instantiate(effectsPanelPrefab, transform.position, Quaternion.Euler(rotationValue));
            effectsPanels[i].transform.SetParent(effectsPanelsGo.transform);
            effectsPanels[i].name = "EffectsPanel_" + (DrumType) i;
            
            foreach(Transform child in transform.GetComponentsInChildren<Transform>())
            {
                if (child.CompareTag("UI_Drum_Colour"))
                {
                    child.GetComponent<Text>().color = drumColors[i];
                    break;
                }
            }

            userInterfaceManager.panels.Add(effectsPanels[i]);
            rotationValue += new Vector3(0, 360.0f / (numberInstruments * 2) * -1, 0);
            var nodeManager = nodesPanels[i].GetComponentInChildren<NodeManager>();
            if (nodeManager == null)
                Debug.LogError(_nodeManagers[i].name +
                               " has an error getting its NodeManager. Check that it has a NodeManager");
            _nodeManagers.Add(nodeManager);
            nodeManager._screenSync = _screenSyncs[i];
            nodeManager.drumType = (DrumType) i;
            nodeManager.subNodeIndex = i;
            nodeManager.defaultColor = defaultNodeColors[i];
            nodeManager.drumColor = drumColors[i];
            foreach (var incButton in nodeManager.incrementButtons) incButton.activeColor = drumColors[i];
            nodeManager.euclideanButton.activeColor = drumColors[i];
            
            // initialize the knob sliders for this current node manager
            nodeManager.bpmSlider = effectsPanels[i].GetComponentInChildren<SliderKnob>();
            var knobs = effectsPanels[i].GetComponentsInChildren<RadialSlider>();
            nodeManager.sliders = new RadialSlider[knobs.Length];
            for (int j = 0; j < knobs.Length; j++) {
                nodeManager.sliders[j] = knobs[j];
                knobs[j].activeColor = drumColors[i];
            }
            
            
            var nodesSoloButtons = nodesPanels[i].GetComponentsInChildren<UI_Gaze_Button>();
            foreach (var uigazeButton in nodesSoloButtons) uigazeButton.drumTypeIndex = i;
            var effectsSoloButtons = effectsPanels[i].GetComponentsInChildren<UI_Gaze_Button>();
            foreach (var uigazeButton in effectsSoloButtons) uigazeButton.drumTypeIndex = i;
            nodeManager.SetUpNode();

            userInterfaceManager.panels.Add(nodesPanels[i]);
        }
        
        userInterfaceManager.SetUpInterface();
        StartCoroutine(userInterfaceManager.SwitchPanelRenderLayers());
        gameSetUpFinished = true;
    }

    public void PlayDrum(DrumType drum)
    {
        switch (drum)
        {
            case DrumType.Kick:
                currentDrums[0].Post(gameObject);
                break;
            case DrumType.Snare:
                currentDrums[1].Post(gameObject);
                break;
            case DrumType.HiHat:
                currentDrums[2].Post(gameObject);
                break;
            case DrumType.TomTom:
                currentDrums[3].Post(gameObject);
                break;
            case DrumType.Cymbal:
                currentDrums[4].Post(gameObject);
                break;
        }
    }

    public void SwitchDrumKits(int drumKitIndex) => currentDrums = drumDictionary[drumKitIndex];

    private IEnumerator WaitUntilConnected() {
        while (true) {
            // if (RealTimeInstance.Instance.isConnected && RealTimeInstance.Instance.numberPlayers > 1) break;
            if (RealTimeInstance.Instance.isSoloMode && RealTimeInstance.Instance.isConnected) break;
            if (RealTimeInstance.Instance.isConnected && RealTimeInstance.Instance.numberPlayers > 1) break;
            yield return new WaitForEndOfFrame();
        }

        print("local player number: " + localPlayerNumber);

        InstantiatePanels();
        StartCoroutine(WaitToPositionCamera(3.0f));
    }

    public void SetPlayerPosition() {
        journeyLength = Vector3.Distance(playerStartPosition.position, playerPositionDestination.position);

        // rotate the parent of the camera around the degrees dependant on the number of players and number of instruments
        // players should be opposite to each other --> so differentiate between even and uneven numbers --> 180 degrees difference between them
        float degrees = 0;
        float degreesPerPlayer = 360.0f / (numberInstruments * 2);
        // if even player --> degrees = -36 * evenPlayerNumberCounter
        if (localPlayerNumber % 2 == 0) degrees = -degreesPerPlayer * (localPlayerNumber / 2.0f);
        // if uneven player --> degrees = 180 - 36 * unevenPlayerNumber
        else degrees = 180 - (degreesPerPlayer * ((localPlayerNumber - 1) / 2.0f));
        // rotate the parent of the camera by the number of degrees around the y axis
        // Transform playerParent = playerCamera.transform.parent;
        // Vector3 playerParentPos = playerParent.position;
        // Quaternion playerParentRot = playerParent.rotation;

        playerCamera.transform.parent.Rotate(0, degrees, 0);
    }


    // call this method when the players have connected
    private void InstantiatePanels() {
        int panelCounter = 1; // to keep track of the panels for the userInterfaceManager
        // instantiate and set up effects panels
        var effectsPanelsGo = new GameObject("Effects panels");
        effectsPanelsGo.transform.SetParent(userInterfaceManager.transform);
        
        
        Vector3 rotationValue = new Vector3(0, 180.0f, 0);
        for (int i = 0; i < numberInstruments; i++) {
            effectsPanels[i] = Instantiate(effectsPanelPrefab, transform.position, Quaternion.Euler(rotationValue));
            effectsPanels[i].transform.SetParent(effectsPanelsGo.transform);
            effectsPanels[i].name = "EffectsPanel_" + (DrumType) i;
            rotationValue += new Vector3(0, 360.0f / (numberInstruments * 2) * -1 * 2, 0);
            userInterfaceManager.panels.Add(effectsPanels[i]);
            var effectsSoloButtons = effectsPanels[i].GetComponentsInChildren<UI_Gaze_Button>();
            foreach (var uigazeButton in effectsSoloButtons) uigazeButton.drumTypeIndex = i;
            foreach(Transform child in transform)
            {
                if (child.CompareTag("UI_Drum_Colour"))
                {
                    child.GetComponent<Text>().color = drumColors[i];
                    break;
                }
            }
            // userInterfaceManager.panels[panelCounter] = effectsPanels[i];
            panelCounter += 2;

        }

        panelCounter = 0;

        // instantiate and set up nodes panels
        rotationValue = Vector3.zero;

        var nodesPanelsGo = new GameObject("Nodes panels");
        nodesPanelsGo.transform.SetParent(userInterfaceManager.transform);
        for (int i = 0; i < numberInstruments; i++) {
            nodesPanels[i] = Instantiate(nodesPanelPrefab, transform.position, Quaternion.Euler(rotationValue));
            nodesPanels[i].transform.SetParent(nodesPanelsGo.transform);
            nodesPanels[i].name = "NodesPanel_" + (DrumType) i;
            rotationValue += new Vector3(0, 360.0f / (numberInstruments * 2) * -1 * 2, 0);

            // set up nodes managers
            // set up the drum type, drum color, and default color of each nodeManager
            var nodeManager = nodesPanels[i].transform.GetComponentInChildren<NodeManager>();

            if (nodeManager == null)
                Debug.LogError(_nodeManagers[i].name +
                               " has an error getting its NodeManager. Check that it has a NodeManager");
            _nodeManagers.Add(nodeManager);
            nodeManager._screenSync = _screenSyncs[i];
            nodeManager.drumType = (DrumType) i;
            nodeManager.subNodeIndex = i;
            nodeManager.defaultColor = defaultNodeColors[i];
            nodeManager.drumColor = drumColors[i];
            foreach (var incButton in nodeManager.incrementButtons) incButton.activeColor = drumColors[i];
            foreach (var navigationButton in nodeManager.navigationButtons) navigationButton.gameObject.SetActive(false);
            
            nodeManager.euclideanButton.activeColor = drumColors[i];
          
            
            
            // initialize the knob sliders for this current node manager
            nodeManager.bpmSlider = effectsPanels[i].GetComponentInChildren<SliderKnob>();
            var knobs = effectsPanels[i].GetComponentsInChildren<RadialSlider>();
            nodeManager.sliders = new RadialSlider[knobs.Length];
            for (int j = 0; j < knobs.Length; j++) {
                nodeManager.sliders[j] = knobs[j];
                knobs[j].activeColor = drumColors[i];
            }
            
            foreach (var incButton in effectsPanels[i].GetComponentsInChildren<IncrementButton>())
                if (incButton.transform.CompareTag("NavigationButtons")) incButton.gameObject.SetActive(false);

            nodeManager.SetUpNode();

            userInterfaceManager.panels.Add(nodesPanels[i]);
            // userInterfaceManager.panels[panelCounter] = nodesPanels[i];
            panelCounter += 2;

            var nodesUigazeButtons = nodesPanels[i].GetComponentsInChildren<UI_Gaze_Button>();
            foreach (var uigazeButton in nodesUigazeButtons) uigazeButton.drumTypeIndex = i;
        }


        // only the first player that connects to the room should start the timer - and as Timer is a RealTime instance object, it updates in all clients
        if (localPlayerNumber == 0) {
            userInterfaceManager.SetUpInterface();
            gameSetUpFinished = true;
            StartCoroutine(RealTimeInstance.Instance.CheckNumberOfPlayers());
        }
        RealTimeInstance.Instance._testStringSync.SetMessage(TestStringSync.MessageTypes.NEW_PLAYER_CONNECTED);
        
    }

    // whenever a nodes is activated / deactivated on any panel, call this method to update the corresponding subNode in the other NodeManagers
    public void UpdateSubNodes(int node, bool activated, int nodeManagerSubNodeIndex) {
        foreach (var nodeManager in _nodeManagers) {
            nodeManager.SetSubNode(node, activated, nodeManagerSubNodeIndex);
        }
    }

    public void SetBpm(int value) {
        bpm = value;
        foreach (var nodeManager in _nodeManagers) {
            nodeManager.bpm = bpm;
            nodeManager._screenSync.SetBpm(bpm);
        }
    }
    
    public void ResetLocalPlayerNumber(int disconnectedPlayerNumber) { }

    public void DrumNodeChangedOnServer(int drumIndex, int nodeIndex, bool activate) {
        _nodeManagers[drumIndex]._nodes[nodeIndex].SetNodeFromServer(activate);
    }


    private IEnumerator WaitToPositionCamera(float time) {
        SetPlayerPosition();
        topDownSignifier.SetActive(false);
        yield return new WaitForSeconds(time);
        isWaitingInLobby = false;
        print("Lerp to position");
    }

    public void SetDwellSettingsActive(bool set)
    {
        dwellSettingsPrefab.SetActive(set);
    }
    /// <summary>
    /// Map a value from one interval to another interval.
    /// </summary>
    /// <param name="value">Value to map</param>
    /// <param name="min1">Minimum value of the first interval</param>
    /// <param name="max1">Maximum value of the second interval</param>
    /// <param name="min2">Minimum value of the second interval</param>
    /// <param name="max2">Maximum value of the second interval</param>
    /// <returns>Mapped value</returns>
    public float Map(float value, float min1, float max1, float min2, float max2) {
        return min2 + (max2 - min2) * ((value - min1) / (max1 - min1));
    }
}