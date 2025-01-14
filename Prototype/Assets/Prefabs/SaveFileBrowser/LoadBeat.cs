using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DidStuffLab.Scripts.Custom_Buttons.Did_Stuff_Buttons.Buttons;
using DidStuffLab.Scripts.Managers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadBeat : MonoBehaviour
{
    public GameObject template;
    public Transform listWindow;
    private Transform savedListWindow;
    private SaveFileButton _currentSelectedSaveFileButton;
    public string currentlySelectedSaveFile;
    public ToggleGroup toggleGroup;

    public List<GameObject> retrievedBeats = new List<GameObject>();

    public ScrollRect scrollRect;
    public Transform layoutGroupTransform;
    public GameObject panelTemplate;

    public GameObject arrowRight;
    public GameObject arrowLeft;

    public float[] positions; 


    public List<string> fileList = new List<string>();
    [SerializeField] private MasterManagerData _loadedManagerData = new MasterManagerData();

    private float numPanels = 1;
    private int CurrentPanel = 0;
    
    private float yVelocity = 0.0f;
    private float targetPos;
    private float currentPos;
    private bool moving;

    private List<GameObject> listPanels = new List<GameObject>();

    private List<GameObject> signifiers = new List<GameObject>();
    public GameObject signifierTemplate;

    private bool hasRunOnce;

    private void Start()
    {
        savedListWindow = listWindow;
        Initialize();
        StartCoroutine(SetCorrectPanel());
    }

    private void Initialize()
    {
        
        CurrentPanel = 0;
        currentPos = 0;
        targetPos = 0;
        scrollRect.horizontalNormalizedPosition = 0;
        GetFileList();
        if (fileList.Count < 1)
        {
            panelTemplate.SetActive(false);
            template.SetActive(false);
            signifierTemplate.SetActive(false);
            arrowLeft.gameObject.SetActive(false);
            arrowRight.gameObject.SetActive(false);
            return;
        }
        else if (listPanels.Count < 2)
        {
            arrowLeft.gameObject.SetActive(false);
            arrowRight.gameObject.SetActive(false);
        }
        GoToPreviousPanel();
        ChangeCurrentlySelectedFile();
        positions = linspace(0, 1, (int)numPanels);

        signifiers[0].GetComponent<Image>().color = new Color(1, 1, 1, 1);
        
        arrowLeft.gameObject.SetActive(false);
        
        FixToggles();
    }


    // Get the list of files from persistent data path and create toggles for them
    private void GetFileList()
    {
        //Clear the file list so we dont have any duplicates
        fileList.Clear();
        
        // Get all files that have any number of characters infront of .json
        string[] files = Directory.GetFiles(Application.persistentDataPath, "*.json");

        if (files.Length < 1)
        {
            MainMenuManager.Instance.SpawnErrorToast("Start a jam session and save your beats to see them here.", 0.5f);
            return;
        }
        GameObject firstSignifier = Instantiate(signifierTemplate, signifierTemplate.transform.parent.transform);
        signifiers.Add(firstSignifier);

        //Add each file to the fileList
        for (int i = 0; i < files.Length; i++)
        {
            if ((i % 6) == 0 && i >= 5)
            {
                numPanels++;
                AddNewPanel();
                GameObject newSignifier = Instantiate(signifierTemplate, signifierTemplate.transform.parent.transform);
                signifiers.Add(newSignifier);
            }
            fileList.Add(files[i]);
            Debug.Log(files[i]);
            CreateNewToggle(files[i]);
        }
        panelTemplate.SetActive(false);
        template.SetActive(false);
        signifierTemplate.SetActive(false);
    }

    // The process of creating toggles
    private void CreateNewToggle(string saveFileName)
    {
        GameObject beat = Instantiate(template, listWindow);
        ToggleSaveData saveData = beat.GetComponent<ToggleSaveData>();
        saveData.fileName = saveFileName;

        // Get the saved screenshot from the save file
        byte[] bytes = File.ReadAllBytes(saveFileName.Substring(0, saveFileName.IndexOf('.')) + ".png");
        Texture2D texture = new Texture2D(480, 270);
        texture.filterMode = FilterMode.Trilinear;
        texture.LoadImage(bytes);
        Sprite sprite = Sprite.Create(texture, new Rect(0,0,480, 270), new Vector2(0.5f,0.0f), 1.0f);
        saveData.image.sprite = sprite;

        DateTime dateTime = File.GetCreationTime(saveFileName);
        string saveFileCreationDate = dateTime.ToString("ddd d MMM HH:mm");
        
        // Format the save file name so it looks nicer
        string formattedFileName = saveFileName.Substring(saveFileName.IndexOf("DuoRhythmo")+10);
        formattedFileName = formattedFileName.Replace(@"\","");
        formattedFileName = formattedFileName.Replace("_", ":");
        formattedFileName = formattedFileName.Substring(0,formattedFileName.IndexOf('.'));
        formattedFileName = formattedFileName.Substring(0, formattedFileName.IndexOf(':') - 2);

        saveData.dateCreated.text = saveFileCreationDate;
        saveData.label.text = formattedFileName;
        
        /*SaveFileButton SB = beat.AddComponent(typeof(SaveFileButton)) as SaveFileButton;
        
        
        beat.GetComponent<SaveFileButton>().onClicked.AddListener(SetToggleOn);*/
        
        retrievedBeats.Add(beat);
    }

    private void FixToggles()
    {
        foreach (var beat in retrievedBeats)
        {
            beat.SetActive(false);
            beat.GetComponent<SaveFileButton>().enabled = true;
            beat.SetActive(true);
        }
    }
    
    // Somewhat self-explanitory
    public void LoadData()
    {
        _loadedManagerData = GetData(currentlySelectedSaveFile);
        JamSessionDetails.Instance.loadingBeat = true;
        JamSessionDetails.Instance.loadedBeatData = _loadedManagerData;
        JamSessionDetails.Instance.DrumTypeIndex = _loadedManagerData.drumIndex;
        StartCoroutine(SetValues());
        JamSessionDetails.Instance.isSoloMode = true;
        SceneManager.LoadScene(1); 
    }
    
    //interactionMethod that returns a master manager data structure
    public MasterManagerData GetData(string file)
    {
        string json = System.IO.File.ReadAllText(file);
        MasterManagerData data = JsonUtility.FromJson<MasterManagerData>(json);
        //Debug.Log(data.nodeManagersData[0].drumType);

        return data;
    }

    // Set all the values loaded to the canvas 
    public IEnumerator SetValues()
    {
        /*// Wait for duorythmo to fully load (strange things will occur if you don't wait)
        yield return new WaitForSeconds(1);
        MasterManager masterManager = MasterManager.Instance;

        List<NodeManager> nodeManagers = new List<NodeManager>();
        List<EffectsManager> effectsManagers = new List<EffectsManager>();
        masterManager.bpm = _loadedManagerData.BPM;
        
        // Same as in SaveIntoJson just in reverse
        for (int i = 0; i < MasterManager.Instance.nodeManagers.Count; i++)
        {
            nodeManagers.Add(MasterManager.Instance.nodeManagers[i]);
        }

        for (int i = 0; i < nodeManagers.Count; i++)
        {
            for (int j = 0; j < nodeManagers[i].nodes.Count; j++)
            {
                if (_loadedManagerData.nodeManagersData[i].nodesData[j].isActive)
                {
                    nodeManagers[i].nodes[j].ToggleState();
                }
            }

            for (int j = 1; j < effectsManagers[i].sliders.Count; j++)
            {
                effectsManagers[i].sliders[j].SetCurrentValue(_loadedManagerData.nodeManagersData[i].sliderValues[j]);
            }
        }
*/
        yield return null;
    }

    // interactionMethod for saving the currently selected save file string
    public void ChangeCurrentlySelectedFile()
    {
        if(_currentSelectedSaveFileButton != null) _currentSelectedSaveFileButton.SetCanHover(true);
        currentlySelectedSaveFile =
            toggleGroup.GetFirstActiveToggle().gameObject.GetComponent<ToggleSaveData>().fileName;
        _currentSelectedSaveFileButton = toggleGroup.GetFirstActiveToggle().gameObject.GetComponent<SaveFileButton>();
    }

    public void AddNewPanel()
    {
        GameObject newPanel = Instantiate(panelTemplate, layoutGroupTransform);
        listPanels.Add(newPanel);
        listWindow = newPanel.transform;
    }

    public void GoToNextPanel()
    {
        currentPos = scrollRect.horizontalNormalizedPosition;

        if (CurrentPanel <= positions.Length - 1)
        {
            targetPos = positions[CurrentPanel + 1];
            CurrentPanel++;
            signifiers[CurrentPanel].GetComponent<Image>().color = new Color(1, 1, 1, 1);
            if (signifiers.Count > 0)
            {
                signifiers[CurrentPanel-1].GetComponent<Image>().color = new Color(1,1,1,0.2f);
            }
        }
        
        
        moving = true;
    }
    
    public void GoToPreviousPanel()
    {
        currentPos = scrollRect.horizontalNormalizedPosition;
        if (CurrentPanel > 0)
        {
            targetPos = positions[CurrentPanel - 1];
            CurrentPanel--;
            signifiers[CurrentPanel].GetComponent<Image>().color = new Color(1, 1, 1, 1);
            if (signifiers.Count > 0)
            {
                signifiers[CurrentPanel+1].GetComponent<Image>().color = new Color(1,1,1,0.2f);
            }
            
        }
        
        moving = true;
    }

    public void DeleteSaveFile()
    {
        File.Delete(currentlySelectedSaveFile);
        File.Delete(currentlySelectedSaveFile.Substring(0, currentlySelectedSaveFile.IndexOf('.')) + ".png");
        Destroy(toggleGroup.GetFirstActiveToggle().gameObject);
        print("Deleting");
        ResetSaveFileBrowser();
    }



    private void Update()
    {
        if (fileList.Count < 1) return;
        if (moving)
        {
            currentPos = scrollRect.horizontalNormalizedPosition;
            float newPos =
                Mathf.SmoothDamp(currentPos, targetPos, ref yVelocity, 0.1f);
            scrollRect.horizontalNormalizedPosition = newPos;
        }

        if (scrollRect.horizontalNormalizedPosition >= 1.0001 || scrollRect.horizontalNormalizedPosition <= -0.0001)
        {
            moving = false;
        }

        if (targetPos == positions[positions.Length-1])
        {
            arrowRight.gameObject.SetActive(false);
        }
        else
        {
            arrowRight.gameObject.SetActive(true);
        }

        if (targetPos == positions[0])
        {
            arrowLeft.gameObject.SetActive(false);
        }
        else
        {
            arrowLeft.gameObject.SetActive(true);
        }

        if (currentPos == targetPos)
        {
            moving = false;
        }
        if (numPanels == 1)
        {
            arrowLeft.gameObject.SetActive(false);
            arrowRight.gameObject.SetActive(false);
        }

        /*if (!hasRunOnce)
        {
            scrollRect.horizontalNormalizedPosition = 0;
        }*/
        

    }

    void ResetSaveFileBrowser()
    {
        
        transform.GetComponentInParent<ReloadSaveFileBrowser>().ReinitializeBrowser();
        /*foreach (var beat in retrievedBeats)
        {
            Destroy(beat);
        }

        foreach (var panel in listPanels)
        {
            Destroy(panel);   
        }

        foreach (var signifier in signifiers)
        {
            Destroy(signifier);
        }
        
        retrievedBeats.Clear();
        listPanels.Clear();
        fileList.Clear();
        signifiers.Clear();
        signifierTemplate.SetActive(true);
        panelTemplate.SetActive(true);
        template.SetActive(true);
        listWindow = savedListWindow;
        numPanels = 1;

        Initialize();*/

    }

    public IEnumerator SetCorrectPanel()
    {
        yield return new WaitForEndOfFrame();
        scrollRect.horizontalNormalizedPosition = 0;
    }


    
    public static float[] linspace(float startval, float endval, int steps)
    {
        float interval = (endval / Mathf.Abs(endval)) * Mathf.Abs(endval - startval) / (steps - 1);
        return (from val in Enumerable.Range(0,steps)
            select startval + (val * interval)).ToArray(); 
    }
}

// <3
