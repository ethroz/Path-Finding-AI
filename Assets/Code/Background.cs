using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public static class Background
{
    public static string Mats = Application.dataPath + @"/Materials";
    public static List<Material> Materials;
    public static string Saves = Application.dataPath + @"/Saves";
    public static List<string> Directories;
    public static List<string> DeepDocuments;
    public static List<string> NaturalDocuments;
    public static List<NeuralNetwork> DeepNets = new List<NeuralNetwork>();
    public static List<PopulationNetwork> NaturalNets = new List<PopulationNetwork>();
    public static List<float[][][]> DeepWeights;
    public static List<float[][][]> NaturalWeights;

    public static void Test()
    {
        //Write Code Here
        SaveNewNetwork(new float[1][][] { new float[1][] { new float[1] { 0 } } }, 0, 0.123f, "1");
        SaveNewNetwork(new float[1][][] { new float[1][] { new float[1] { 0 } } }, 1, 0.456f, "01101010");
    }

    public static void FetchAllMaterials()
    {
        Materials = new List<Material>();
        try
        {
                foreach (string d in Directory.GetDirectories(Mats))
            {
                if (!d.Contains(".meta"))
                    Materials.Add(Resources.Load(d, typeof(Material)) as Material);
            }
        }
        catch (Exception excpt)
        {
            Debug.LogError(excpt.Message);
        }
    }

    public static void FetchAllDocuments()
    {
        Directories = new List<string>();
        DeepDocuments = new List<string>();
        NaturalDocuments = new List<string>();
        try
        {
            foreach (string d in Directory.GetDirectories(Saves))
            {
                if (!d.Contains(".meta"))
                {
                    Directories.Add(d);
                    foreach (string p in Directory.GetFiles(d))
                    {
                        if (!p.Contains(".meta"))
                        {
                            if (d.Contains("Deep"))
                                DeepDocuments.Add(p);
                            else
                                NaturalDocuments.Add(p);
                        }
                    }
                    
                }
            }
        }
        catch (Exception excpt)
        {
            Debug.LogError(excpt.Message);
        }
    }

    public static void FetchAllWeights()
    {
        DeepNets = new List<NeuralNetwork>();
        DeepWeights = new List<float[][][]>();
        NaturalNets = new List<PopulationNetwork>();
        NaturalWeights = new List<float[][][]>();
        for (int i = 0; i < Directories.Count; i++)
        {
            List<string> documents = new List<string>();
            if (i == 0)
                documents = DeepDocuments;
            else if (i == 1)
                documents = NaturalDocuments;
            for (int j = 0; j < documents.Count; j++)
            {
                if (IsDocument(documents[j]))
                {
                    if (Directories[i].Contains("Deep"))
                    {
                        DeepNets.Add(null);
                        DeepWeights.Add(null);
                    }
                    else
                    {
                        NaturalNets.Add(null);
                        NaturalWeights.Add(null);
                    }
                    continue;
                }
                string[] bestWeights = GetDocument(documents[j]);
                List<float[][]> bigWeightsList = new List<float[][]>();
                List<float[]> arrayWeightsList = new List<float[]>();
                for (int k = 1; k < bestWeights.Length - 2; k++)
                {
                    int increment;
                    List<float> weightsList = new List<float>();
                    if (bestWeights[k] == "#")
                    {
                        bigWeightsList.Add(arrayWeightsList.ToArray());
                        arrayWeightsList = new List<float[]>();
                        continue;
                    }
                    for (int m = 0; m < bestWeights[k].Length; m += increment + 1)
                    {
                        increment = 0;
                        for (int n = 0; n < 24; n++)
                        {
                            if (bestWeights[k][m + n].ToString() == " ")
                                break;
                            else
                                increment++;
                        }
                        weightsList.Add(float.Parse(bestWeights[k].Substring(m, increment).ToString()));
                    }
                    arrayWeightsList.Add(weightsList.ToArray());
                }
                if (Directories[i].Contains("Deep"))
                {
                    DeepNets.Add(new NeuralNetwork(bigWeightsList.ToArray(), PlayerScript.Type));
                    DeepWeights.Add(bigWeightsList.ToArray());
                }
                else
                {
                    NaturalNets.Add(new PopulationNetwork(bigWeightsList.ToArray()));
                    NaturalWeights.Add(bigWeightsList.ToArray());
                }
            }
        }
    }

    public static void SaveNewNetwork(float[][][] weights, int directory, float time, string level)
    {
        string type;
        switch (PlayerScript.Type)
        {
            case 0:
                type = "Tanh";
                break;
            default:
                type = "Other";
                break;
        }
        string currentTime = DateTime.Now.Year + "/" + DateTime.Now.Month + "/" + DateTime.Now.Day + " " + DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second;
        string text = "";
        text += "##### " + currentTime + " ##### Type: " + type + Environment.NewLine;
        for (int i = 0; i < weights.Length; i++)
        {
            for (int j = 0; j < weights[i].Length; j++)
            {
                for (int k = 0; k < weights[i][j].Length; k++)
                {
                    text += weights[i][j][k] + " ";
                }
                text += Environment.NewLine;
            }
            text += "#" + Environment.NewLine;
        }
        text += "##### Level: " + level + "  Time: " + time + " #####" + Environment.NewLine;
        text += "##### " + currentTime + " ##### Type: " + type + Environment.NewLine;
        string path;
        if (Directories[directory].Contains("Deep"))
        {
            path = Directories[directory] + @"\" + type + (DeepNets.Count + 1) + @".txt";
            DeepWeights.Add(weights);
            DeepNets.Add(new NeuralNetwork(weights, PlayerScript.Type));
            DeepDocuments.Add(path);
        }
        else
        {
            path = Directories[directory] + @"\" + type + (NaturalNets.Count + 1) + @".txt";
            NaturalWeights.Add(weights);
            NaturalNets.Add(new PopulationNetwork(weights));
            NaturalDocuments.Add(path);
        }
        using (FileStream fs = File.Create(path))
        {
            Byte[] info = new UTF8Encoding(true).GetBytes(text);
            fs.Write(info, 0, info.Length);
        }
    }

    public static bool IsDocument(string path)
    {
        return File.ReadAllText(path) == "";
    }

    public static NeuralNetwork BestDeepNet()
    {
        if (DeepNets.Count == 0)
            return null;
        return DeepNets[0];
    }

    public static string[] GetDocument(string path)
    {
        return File.ReadAllLines(path);
    }

    public static void ClearDocument(string path)
    {
        File.WriteAllText(path, "");
    }

    public static float[] NetEmulation(float[] inputs)
    {
        return NaturalNets[NaturalNets.Count - 1].FeedForward(inputs);
    }
}