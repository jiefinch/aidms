using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using System;
using System.Threading.Tasks;
using System.Linq;
using Unity.VisualScripting;


public class DataCollection : MonoBehaviour
{

    public bool saveData;
    public string scenario;
    public int numTimeUnits;

    // ===============================
    private string rootPath; 
    private string dataPath;
        
    // =============================== DATA ======================
    public struct RecordedData {
        public SimParams SimParams;
        public List<MarketHistory> MarketHistory;
        public Dictionary<string, List<PlayerHistory>> PlayerHistories;
        public Dictionary<string, List<LotHistory>> LotHistories;
    }  

    public RecordedData Data;

    // ===============================
    [HideInInspector] public static DataCollection instance;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        rootPath = Application.dataPath;
        dataPath = Path.Combine(rootPath, "Data"); //rootPath + $"/Data/";
        
        // if (saveData) RunSimulation(); 
        Data = NewRecording();
    }

    void Update() {

        if (numTimeUnits <= SimManager.instance.timeUnit) {
            EndSimulation(NextFileNumber(), Data);
        }
    }

    public void EndSimulation(int i, RecordedData Data)
    {
        SaveRecording(i, Data);
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }


    RecordedData NewRecording() 
    {
        RecordedData Data = new();
        Data.SimParams = new();
        Data.MarketHistory = new();
        Data.PlayerHistories = new();
        Data.LotHistories = new();
        return Data;
    }

    public void SaveRecording(int i, RecordedData Data) {
        // SaveParameters(Data);
        Data.SimParams = NewDataPoint(SimManager.instance);

        // string outputFilePath = $"{dataPath}/{saveNumber()}.json";
        string fileName = $"{scenario} ({i})";
        string outputFilePath = Path.Combine(dataPath, $"{fileName}.json");

        // convert and save
        string json = JsonConvert.SerializeObject(Data, Formatting.Indented);
        System.IO.File.WriteAllText(outputFilePath, json);
    }

    // ========================= ADD POINT FUNCTIONS ============================

    public SimParams NewDataPoint(SimManager point) {
        SimParams output = new();
        output.numLots = point.cols * point.rows;
        output.numPeople = point.numPeople;
        output.incomeDistribution = point.incomeDistribution;
        output.timeUnits = numTimeUnits;
        output.dynamicPricingPercent = point.dynamicPricingPercent;
        output.lotAttractiveness = point.lotAttractiveness;
        output.playerSettings = point.playerSettings;
        return output;
    }
    public void NewDataPoint(MovingManager point) {
        MarketHistory output = new();
        output.moneyInHousingMarket = point.moneyInHousingMarket;
        output.averageHousePrice = point.averageHousePrice;
        output.medianHousePrice = point.medianHousePrice;
        output.averageOwnedHousePrice = point.averageOwnedHousePrice;
        output.medianOwnedHousePrice = point.medianOwnedHousePrice;
        Add(output);
    }

    public void NewDataPoint(Player point) {
        PlayerHistory output = new();
        output.income = point.income;
        output.expense = point.expense;
        output.costliness = point.costliness;
        output.attractiveness = point.currentLot.attractiveness;
        output.numMoves = point.numMoves;
        output.qualityGoal = point.qualityGoal;
        output.quality = point.quality;
        output.interestedIn = point._InterestedIn.Length;
        Add(point.gameObject.name, output);
    }
    public void NewDataPoint(Lot point) {
        LotHistory output = new();
        output.ownerIncome = point.owner == null ? -1 : point.owner.income;
        output.currentPrice = point.currentPrice;
        output.attractiveness = point.attractiveness;
        output.PotentialBuyers = point.PotentialBuyers.Count;
        Add(point.gameObject.name, output);
    }

    // ========================= ADD DATA FUNCTIONS ============================

    public void Add(MarketHistory data) {
        Data.MarketHistory.Add(data);
    }

    public void Add(string key, PlayerHistory data) {
        if (!Data.PlayerHistories.ContainsKey(key)) {
            Data.PlayerHistories.Add(key, new List<PlayerHistory>(){data});
        } else {
            Data.PlayerHistories[key].Add(data);
        }
    }
    public void Add(string key, LotHistory data) {
        if (!Data.LotHistories.ContainsKey(key)) {
            Data.LotHistories.Add(key, new List<LotHistory>(){data});
        } else {
            Data.LotHistories[key].Add(data);
        }
    }
    int NextFileNumber() {
        string[] existingFiles = Directory.GetFiles(dataPath, $"{scenario}*.json");
        // Determine the next available number
        int nextNumber = 0; // minimum number
        if (existingFiles.Length > 0)
        {
            // Extract numbers from existing file names and find the maximum
            var numbers = existingFiles.Select(file =>
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                int number;
                if (int.TryParse(fileName, out number))
                {
                    return number;
                }
                return 0;
            });
            nextNumber = numbers.Max() + 1;
        }
        return nextNumber;
    }

}
