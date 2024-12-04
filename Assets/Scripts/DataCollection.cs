using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

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
    public int numSims;
    public int numTimeUnits;


    // ===============================
    [HideInInspector] public static Dictionary<string, object> Data;
    [HideInInspector] public static SimParams SimParams;
    [HideInInspector] public static List<MovingHistory> MovingHistory;
    [HideInInspector] public static Dictionary<float, List<PlayerHistory>> PlayerHistories; // index: income
    [HideInInspector] public static Dictionary<int, List<LotHistory>> LotHistories; // index: attractive

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
        Initialize();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void Initialize() 
    {
        Data = new();
        SimParams = new();
        MovingHistory = new();
        PlayerHistories = new();
        LotHistories = new();
    }
    void SaveFile() 
    {
        Data.Add("scenario", scenario);
        Data.Add("sim_params", SimParams);
        Data.Add("moving_history", MovingHistory);
        Data.Add("player_histories", PlayerHistories); // indexed by income
        Data.Add("lot_histories", LotHistories);   
    }

    void Add(SimParams data) {
        SimParams = data;
    }

    void Add(MovingHistory data) {
        MovingHistory.Add(data);
    }

    void Add(float key, PlayerHistory data) {
        if (!PlayerHistories.ContainsKey(key)) {
            PlayerHistories.Add(key, new List<PlayerHistory>(){data});
        } else {
            PlayerHistories[key].Add(data);
        }
    }
    void Add(int key, LotHistory data) {
        if (!LotHistories.ContainsKey(key)) {
            LotHistories.Add(key, new List<LotHistory>(){data});
        } else {
            LotHistories[key].Add(data);
        }
    }
}
