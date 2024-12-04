using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System;
using UnityEngine.Events;
using UnityEditor;
using System.Linq;

public class SimManager : MonoBehaviour
{

    [Header("LOT PARAMS")]
    // lots
    public GameObject lotManager;
    public int rows;
    public int cols;
    public GameObject lot;
    public float spacing = 1.0f;
    public float lotSize = 2.0f; 
    public bool randomizeAttractiveness = true;
    public float deteriorationChance = 0.5f;

    [Header("PLAYER PARAMS")]
    // players
    public GameObject playerManager;
    public int numLow = 10;
    public int numMid = 4;
    public int numHigh = 1;
    [HideInInspector] public int numPeople;
    public GameObject player;
    public float housingChance = 0.75f; // at the start
    public float qualityGoalDeterioration = 0.1f; // longer u're homeless for, reduce 10% your standards

    [Header("CLASS PARAMS")]
    public float MoneyInCirculation = 1000; // per time unit, generated from the nether
    [Serializable] public struct PercentsOwned {
        public float Low;
        public float Mid;
        public float High;
    }
    public PercentsOwned percentsOwned;

    // public float percentLowOwn = 0.13f;
    // public float percentMidOwn = 0.6f;
    // public float percentHighOwn = 0.27f;
    public float highestIncome = 0f;
    public float medianIncome = 0f;
    public List<float> incomeDistribution = new();

    [Header("GAME")]
    public bool sellToHighest = true;
    public float timeUnit;
    public float secsPerUnit;
    public TMP_Text displayText;
    private float timer;
    DateTime startTime;
    public UnityEvent nextStep;


    [Header("MATH")]
    // 𝑅 = chance of buying
    public float alpha = 0.1f; //α is a constant that controls the rate at which 𝑅 increases with distance.
    public float beta = 0.1f; //β is a constant that controls the rate at which 𝑅 R decreases with distance.
    public float kappa = 5f; // how sharp the sigmoid for care of cost vs attractiveness | high kappa = sharper care for attractiveness at higher incomes
    public float dynamicPricingPercent = 0.5f;  // 0: does not factor individual income | 1: max factoring
        // λ: This parameter controls how much the individual's income influences the price. If λ = 1 λ=1, the price is adjusted directly
        // by the percentage difference in income. If λ = 0 λ=0, the income difference has no effect on the price.
    public float costPenalty = 5f;

    //====================
    [HideInInspector] public static SimManager instance;
    private bool initialized = false;

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
        numPeople = numLow + numHigh + numMid;
        InstantiateGrid();
        InstatiatePlayers();
        medianIncome = MyUtils.Median(incomeDistribution.ToArray());
        InitializePlayers();

        initialized = true;
        startTime = DateTime.Now;
        Debug.Log("intialized done");

    }

    // Update is called once per frame
    void Update()
    {
        if (initialized) {
            timer += Time.deltaTime;
            if (timer >= secsPerUnit) {
                timer = 0;
                nextStep.Invoke();
                timeUnit++;
            }   

            displayText.text =  $"Time Unit: {timeUnit}" + 
                            $"\nTime Elapsed: {(DateTime.Now - startTime).ToString("hh\\:mm\\:ss\\.fff")}" +
                            $"\nSecs per Unit: {secsPerUnit}" ; 
        }
    }


    void InstantiateGrid()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                // Calculate the position for each prefab
                // Vector3 position = new Vector3(col * spacing, row * spacing, 0);
                // centered positioning
                Vector3 position = new Vector3(
                    col * spacing - (cols - 1) * spacing / 2,
                    row * spacing - (rows - 1) * spacing / 2,
                    0
                );
                // position += lotManager.transform.position; // centered on lot manager

                // Instantiate the prefab at the calculated position
                GameObject instance = Instantiate(lot, position, Quaternion.identity);
                instance.transform.parent = lotManager.transform; // move to lot's child
                instance.transform.localScale = Vector3.one * lotSize;
                instance.name = $"lot({row},{col})";

                // intialize the lot
                Lot settings = instance.GetComponent<Lot>();
                MovingManager.instance.Lots.Add(instance.name, settings);
                MovingManager.instance.AvailableLots.Add(instance.name, true); // lot is available! ^__^
                settings.deteriorationChance = deteriorationChance;
                if (randomizeAttractiveness) {
                    settings.attractiveness = UnityEngine.Random.Range(-10,11); // [-10,10]
                }

            }
        }
    }

    void InstatiatePlayers() 
    {
        void instantiateClass(int num, SocioClass socio, float percentOwned) {
            // List<float> incomes = new();
            float pie = percentOwned*MoneyInCirculation;
            List<float> pieDistribution = MyUtils.SplitPie(pie, num);
            incomeDistribution.AddRange(pieDistribution);

            for (int i = 0; i < num; i++) {
                GameObject instance = Instantiate(player);
                instance.transform.parent = playerManager.transform; // move to lot's child
                instance.name = $"player({socio}{i})";

                Player settings = instance.GetComponent<Player>();
                MovingManager.instance.Players.Add(instance.name, settings);
                MovingManager.instance.MovingPlayers.Add(instance.name, false);

                settings.socioClass = socio;
                settings.income = pieDistribution[i];
                if (pieDistribution[i] > highestIncome) highestIncome = pieDistribution[i];

                float randValue = UnityEngine.Random.value;
                if (randValue < housingChance)
                {
                    // give em a random house
                    Lot lot = MovingManager.instance.GetRandomAvailable();
                    MovingManager.instance.BuyHouse(settings, lot);
                } else {
                    // you are unhoused
                    MovingManager.instance.MoveOut(settings);
                }
                settings.UpdatePosition();
            }
        }
        instantiateClass(numLow, SocioClass.LOW, percentsOwned.Low);
        instantiateClass(numMid, SocioClass.MID, percentsOwned.Mid);
        instantiateClass(numHigh, SocioClass.HIGH, percentsOwned.High);
    }
    void InitializePlayers() {
        foreach ((string name, Player player) in MovingManager.instance.Players) {
            (player.maxQuality, player.qualityGoal) = Calculate.QualityOnMarket(player);
            (player.WeightCost, player.WeightAttr) = IncomeManagement.Weights(player);
        }
    }

}
