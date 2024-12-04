using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Numerics;
using System;

public class Calculate
{
    // chat gptfied
    public static float StaticLotPrice(Lot lot)
    {
        // Normalize attractiveness ([-10, 10] to [0, 1]) : F(A)
        float attractivenessMultiplier = 1 + (lot.attractiveness / 10);

        // see below for G(I): median income
        var incomeDistribution = SimManager.instance.incomeDistribution.ToArray();
        float incomeMultiplier = IncomeManagement.CalculateIncomeMultiplier(incomeDistribution);

        // Calculate price
        float price = attractivenessMultiplier * incomeMultiplier;

        // Debug.Log($"static price: {price} | A{lot.attractiveness} Inc{incomeMultiplier}");
        return price;
    }

    public static float DynamicLotPrice(Lot lot) {
        // sigma: 1: totally dynamic price 2: totally static price
        float staticPrice = StaticLotPrice(lot);
        var (median, sigma) = (SimManager.instance.medianIncome, SimManager.instance.dynamicPricingPercent);
        
        int N = lot.PotentialBuyers.Count(); // num ppl interested
        if (N > 0) {
            float F_interest = (float)N / MovingManager.instance.N_0;
            float F_income = lot.PotentialBuyers.Average(player => player.income) / median;
            float dynamicPrice = staticPrice * F_interest * F_income;

            // Debug.Log($"dynamic price: {dynamicPrice} | static price: {staticPrice} | int{F_interest} inc{F_income}");
            return (1-sigma)*staticPrice + sigma*dynamicPrice;
        }
        return staticPrice;
    }

    // ================================================================================================

    public static float ChanceOfBuying(float quality, float qualityGoal) {
        var (alpha, beta) = (SimManager.instance.alpha, SimManager.instance.beta);
        float d = Math.Abs(quality-qualityGoal);

        if (quality >= qualityGoal) {
            return 1 - (float)Math.Exp(-alpha*d);
        } else {
            return (float)Math.Exp(-beta*d);
        }
    }

    public static float ChanceOfMoving(Player player) 
    {
        var (S_a, S_c) = LotSatisfaction(player);
        float P = 1 - (player.WeightAttr*S_a + player.WeightCost*S_c);
        // chance of movign out: how satisfied w/ attraction you are * how much do u care abt attractiveness
        // Debug.Log($"{player.gameObject.name} A{player.currentLot.attractiveness} C{player.costliness} | S_a{S_a} S_c{S_c} | move?{P}");
        return P;
    }


    // ================================================================================================

    public static float QualityOfLot(Lot lot, Player player) {
        float Q = player.WeightCost * lot.currentPrice + player.WeightAttr * lot.attractiveness;
        // Debug.Log($"C{player.WeightCost} A{player.WeightAttr} | cost{MovingManager.instance.CalculateExpense(lot, player)} attr{lot.attractiveness} | quality{Q}");
        return Q;
    }

    public static (float, float) LotSatisfaction(Player player)
    {
        // satisfaction attractiveness
        float S_a = (player.currentLot.attractiveness+10f)/20f;
        // satisfaction cost
        float S_c = 1 / (1+(float)Math.Exp(SimManager.instance.costPenalty*(player.expense-player.income))); // sigmoid satisfaction

        // Debug.Log($"{player.gameObject.name} | attr{S_a} cost{S_c} | costiness{player.costliness}");

        return (S_a, S_c);
    }

    public static (float, float) QualityOnMarket(Player player) {
        float maxQuality = 0f;
        List<float> qualities = new();
        foreach ((string name, bool avail) in MovingManager.instance.AvailableLots) {
            if (avail) {
                var q = QualityOfLot(MovingManager.instance.Lots[name], player);
                qualities.Add(q);

                if (q > maxQuality) maxQuality = q;
            }
        }

        return qualities.Count > 0 ? (maxQuality, MyUtils.Median(qualities.ToArray())) : (maxQuality, 0f);
    }
}

public class IncomeManagement // G(i)
{
        // Function to calculate the median of an array

    // Function to calculate g(I) with a more robust income measure (median, trimmed mean, or log-transformed)
    public static float CalculateIncomeMultiplier(float[] incomeDistribution)
    {
        float medianIncome = MyUtils.Median(incomeDistribution); // median
        // float medianIncome = TrimmedMean(incomeDistribution, 0.1f); // 10% trimmed // trimmed mean
        // float medianIncome = Math.Log(incomeDistribution.Average()); // Log-transformation // log-transformed income
        return medianIncome;
    }

    public static (float, float) Weights(Player player) {

        float WeightCost = 1/(1 + (float)Math.Exp(SimManager.instance.kappa * (player.income-SimManager.instance.medianIncome)));
        float WeightAttr = 1 - WeightCost;

        // Debug.Log($"{player.socioClass} diff{player.income-SimManager.instance.medianIncome} |wcost{WeightCost} wattr{WeightAttr}");

        return (WeightCost, WeightAttr);
    }
                

}
