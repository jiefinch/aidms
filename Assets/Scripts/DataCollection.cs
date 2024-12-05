using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using System;
using System.Threading.Tasks;
using System.Linq;


public class DataCollection : MonoBehaviour
{
    /*
    1. dynamic pricing #: ...
    2. sim params #: ....

    1. player: time spent moving, # moves, quality history (normalized according to max quality on the board)
    2. market: lots available, avg lot price, money in housing circulation

    dynamic pricing: money in housing | avg costliness (housed) | %people housed at a time
    0: 160-200
    0.5: 200-600
    1: 200-900

    */

    public bool saveData;
    public string scenario;
    public int numSims = 1;
    public int numTimeUnits;

    // ===============================
    private string rootPath; 
    private string dataPath;
        
    // =============================== DATA ======================
    public struct RecordedData {
        public SimParams SimParams;
        public List<MovingHistory> MovingHistory;
        public Dictionary<float, List<PlayerHistory>> PlayerHistories;
        public Dictionary<int, List<LotHistory>> LotHistories;
    }  
    // [HideInInspector] public static Dictionary<string, object> Data;
    // [HideInInspector] public static SimParams SimParams;
    // [HideInInspector] public static List<MovingHistory> MovingHistory;
    // [HideInInspector] public static Dictionary<float, List<PlayerHistory>> PlayerHistories; // index: income
    // [HideInInspector] public static Dictionary<int, List<LotHistory>> LotHistories; // index: attractive

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
        // SimManager.instance.nextStep.AddListener(SaveDataPoint);
        if (saveData) RunSimulation(); 
    }

    public async void RunSimulation()
    {
        for (int sim = 0; sim < numSims; sim++)
        {
            NewRecording();
            SimManager.instance.InitializeSimulation(); // begins running the simulation

            // Wait until SimManager.timeUnit reaches 100
            await WaitForTimeUnit(numTimeUnits);

            // Once timeUnit reaches XXX, call endSim
            SimManager.instance.DestroySimulation();
            SaveRecording(sim, Data);
        }
    }

    private async Task WaitForTimeUnit(int targetTimeUnit)
    {
        // Wait until SimManager's timeUnit reaches the target value
        while (SimManager.instance.timeUnit < targetTimeUnit)
        {
            await Task.Delay(100); // Delay for 100 ms before checking again
        }
    }

    void NewRecording() 
    {
        Data = new();
        Data.SimParams = new();
        Data.MovingHistory = new();
        Data.PlayerHistories = new();
        Data.LotHistories = new();
    }

    public void SaveRecording(int simNum, RecordedData Data) {
        // SaveParameters(Data);

        // string outputFilePath = $"{dataPath}/{saveNumber()}.json";
        string date = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm");
        string fileName = $"{scenario}{simNum}_{date}";
        string outputFilePath = Path.Combine(dataPath, $"{fileName}.json");

        // convert and save
        string json = JsonConvert.SerializeObject(Data, Formatting.Indented);
        System.IO.File.WriteAllText(outputFilePath, json);
    }


    // ========================= ADD DATA FUNCTIONS ============================
    void Add(SimParams data) {
        Data.SimParams = data;
    }

    void Add(MovingHistory data) {
        Data.MovingHistory.Add(data);
    }

    void Add(float key, PlayerHistory data) {
        if (!Data.PlayerHistories.ContainsKey(key)) {
            Data.PlayerHistories.Add(key, new List<PlayerHistory>(){data});
        } else {
            Data.PlayerHistories[key].Add(data);
        }
    }
    void Add(int key, LotHistory data) {
        if (!Data.LotHistories.ContainsKey(key)) {
            Data.LotHistories.Add(key, new List<LotHistory>(){data});
        } else {
            Data.LotHistories[key].Add(data);
        }
    }

    // ========================= ADD POINT FUNCTIONS ============================
    public SimParams NewDataPoint(SimManager point) {
        SimParams output = new();
        output.numLots = point.rows * point.cols;
        output.numPeople = point.numPeople;
        output.timeUnits = (int)point.timeUnit;
        output.incomeDistribution = point.incomeDistribution;
        output.dynamicPricingPercent = point.dynamicPricingPercent;
        return output;
    }

    public MovingHistory NewDataPoint(MovingManager point) {
        MovingHistory output = new();
        output.moneyInHousingMarket = point.moneyInHousingMarket;
        output.averageHousePrice = point.averageHousePrice;
        output.medianHousePrice = point.medianHousePrice;
        output.averageOwnedHousePrice = point.averageOwnedHousePrice;
        output.medianOwnedHousePrice = point.medianOwnedHousePrice;
        output.movingPlayers = point._MovingPlayers.Count;
        output.housedRate = (float) output.movingPlayers / SimManager.instance.numPeople;
        // output.movingPlayersIncomeDistribution = point._MovingPlayers.Select(player => player.income).ToList();
        // do that later
        return output;
    }
}
