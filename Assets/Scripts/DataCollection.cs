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
    public int numSims = 1;
    public int numTimeUnits;

    // ===============================
    private string rootPath; 
    private string dataPath;
        
    // =============================== DATA ======================
    public struct RecordedData {
        public SimManager SimParams;
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
        Add(SimManager.instance);

        // string outputFilePath = $"{dataPath}/{saveNumber()}.json";
        string date = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm");
        string fileName = $"{scenario}{simNum}_{date}";
        string outputFilePath = Path.Combine(dataPath, $"{fileName}.json");

        // convert and save
        string json = JsonConvert.SerializeObject(Data, Formatting.Indented);
        System.IO.File.WriteAllText(outputFilePath, json);
    }


 
    // SimManager.instance.nextStep.AddListener(SaveDataPoint);
    // public void SaveDataPoint() {
    //     MovingHistory history = NewDataPoint(MovingManager.instance);
    //     Add(history);
    // }

    // public void SaveDataPoint<T>(T point) {
    //     var history = NewDataPoint(point);
    //     Add(history);
    // }


    // ========================= ADD DATA FUNCTIONS ============================
    void Add(SimManager data) {
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

    public MovingHistory NewDataPoint(MovingManager point) {
        MovingHistory output = new();
        output.moneyInHousingMarket = point.moneyInHousingMarket;
        output.averageHousePrice = point.averageHousePrice;
        output.medianHousePrice = point.medianHousePrice;
        output.averageOwnedHousePrice = point.averageOwnedHousePrice;
        output.medianOwnedHousePrice = point.medianOwnedHousePrice;
        return output;
    }

    public PlayerHistory NewDataPoint(Player point) {
        PlayerHistory output = new();
        output.income = point.income;
        output.expense = point.expense;
        output.costliness = point.costliness;
        output.attractiveness = point.currentLot.attractiveness;
        output.numMoves = point.numMoves;
        output.qualityGoal = point.qualityGoal;
        output.quality = point.quality;
        output.interestedIn = point._InterestedIn.Length;
        return output;
    }
    public LotHistory NewDataPoint(Lot point) {
        LotHistory output = new();
        output.ownerIncome = point.owner == null ? -1 : point.owner.income;
        output.currentPrice = point.currentPrice;
        output.attractiveness = point.attractiveness;
        output.PotentialBuyers = point.PotentialBuyers.Count;
        return output;
    }

}
