using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Linq;
using System.Numerics;
using System;

public static class ListExtensions
{
    public static T PopRandom<T>(this List<T> list)
    {
        if (list == null || list.Count == 0)
        {
            Debug.LogWarning("List is empty or null. Returning default value.");
            return default;
        }

        // Generate a random index
        int randomIndex = UnityEngine.Random.Range(0, list.Count);

        // Get the element at the random index
        T element = list[randomIndex];

        // Remove the element from the list
        list.RemoveAt(randomIndex);

        // Return the popped element
        return element;
    }

    public static List<Player> InsertInOrder(this List<Player> list, Player player)
    {
        int index = list.FindIndex(p => p.income < player.income);
        if (index == -1)
            list.Add(player); // Add at the end if no one has a smaller income
        else
            list.Insert(index, player); // Insert in the correct position
        return list;
    }

    public static List<int> GetIndicesFromValue(this List<bool> list, bool value)
    {
        List<int> indices = new List<int>();

        for (int i = 0; i < list.Count; i++)
        {
            if (value == list[i]) // Check if the value is false
            {
                indices.Add(i);
            }
        }
        return indices;
    }

    public static int GetRandomIndexFromValue(this List<bool> list, bool value)
    {
        // Filter the indices that match the value (true or false)
        List<int> matchingIndices = list.GetIndicesFromValue(value);
        // If no matching indices were found, return -1 or handle as needed
        if (matchingIndices.Count == 0)
        {
            return -1; // No match found
        }
        // Return a random index from the filtered list
        int randomIndex = UnityEngine.Random.Range(0, matchingIndices.Count);
        return matchingIndices[randomIndex];
    }

}

public static class MyUtils {
    public static List<float> SplitPie(float pie, int numPeople)
    {
                List<float> distributions = new List<float>();
                float totalRandomValue = 0f;

                // Generate random values for each person
                for (int i = 0; i < numPeople; i++)
                {
                    float randValue = UnityEngine.Random.value;
                    distributions.Add(randValue);
                    totalRandomValue += randValue;
                }
                // Normalize the values so that they sum to 'pie'
                for (int i = 0; i < numPeople; i++) distributions[i] = distributions[i] / totalRandomValue * pie;
                return distributions;
            }
        public static float Median(float[] values)
    {
        var sortedValues = values.OrderBy(v => v).ToArray();
        int middle = sortedValues.Length / 2;
        
        // If the length is odd, return the middle value, otherwise, return the average of the two middle values
        return (sortedValues.Length % 2 == 0) ? (sortedValues[middle - 1] + sortedValues[middle]) / 2 : sortedValues[middle];
    }

    // Function to calculate the trimmed mean
    public static float TrimmedMean(float[] values, float trimPercentage)
    {
        var sortedValues = values.OrderBy(v => v).ToArray();
        int trimCount = (int)(sortedValues.Length * trimPercentage);

        // Trim the extremes based on the given percentage and calculate the mean
        var trimmedValues = sortedValues.Skip(trimCount).Take(sortedValues.Length - 2 * trimCount).ToArray();
        return trimmedValues.Average();
    }



}