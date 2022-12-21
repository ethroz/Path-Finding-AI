using System;
using System.Collections.Generic;
using UnityEngine;

public class NeuralNetwork : IComparable<NeuralNetwork>
{
    public float Time;
    public string Level;
    public static int Type = 0;
    public int[] Size;
    public Layer[] Layers;

    public NeuralNetwork(int[] size, int type)
    {
        if (size.Length < 2)
        {
            Debug.LogError("Neural Network must has at least 2 layers");
            return;
        }
        Type = type;
        Size = new int[size.Length];
        for (int i = 0; i < size.Length; i++)
            Size[i] = size[i];

        Layers = new Layer[Size.Length - 1];

        for (int i = 0; i < Layers.Length; i++)
        {
            bool last = false;
            if (i == Layers.Length - 1)
                last = true;
            Layers[i] = new Layer(Size[i], Size[i + 1], last, true);
        }
    }

    public NeuralNetwork(float[][][] weights, int type)
    {
        Type = type;
        Size = new int[weights.Length + 1];
        Size[0] = weights[0][0].Length;
        for (int i = 1; i < Size.Length; i++)
            Size[i] = weights[i - 1].Length;

        Layers = new Layer[weights.Length];

        for (int i = 0; i < Layers.Length; i++)
        {
            bool last = false;
            if (i == Layers.Length - 1)
                last = true;
            Layers[i] = new Layer(Size[i], Size[i + 1], last, false);
        }

        for (int i = 0; i < weights.Length; i++)
        {
            for (int j = 0; j < weights[i].Length; j++)
            {
                for (int k = 0; k < weights[i][j].Length; k++)
                {
                    Layers[i].Weights[j, k] = weights[i][j][k];
                }
            }
        }
    }

    public float[][][] GetWeights()
    {
        List<float[][]> arrArrWeights = new List<float[][]>();
        
        for (int i = 0; i < Layers.Length; i++)
        {
            List<float[]> arrWeights = new List<float[]>();

            for (int j = 0; j < Layers[i].NumberOfOutputs; j++)
            {
                float[] weights = new float[Layers[i].NumberOfInputs];

                for (int k = 0; k < Layers[i].NumberOfInputs; k++)
                {
                    weights[k] = Layers[i].Weights[j, k];
                }

                arrWeights.Add(weights);
            }

            arrArrWeights.Add(arrWeights.ToArray());
        }

        return arrArrWeights.ToArray();
    }

    public int CompareTo(NeuralNetwork other)
    {
        if (other == null)
            return 1;
        if (Time > other.Time)
            return 1;
        else if (other.Time > Time)
            return -1;
        else
            return 0;
    }

    public float[] FeedForward(float[] inputs)
    {
        Layers[0].FeedForward(inputs);

        for (int i = 1; i < Layers.Length; i++)
            Layers[i].FeedForward(Layers[i - 1].Outputs);

        return Layers[Layers.Length - 1].Outputs;
    }

    public void BackProp(float[] expected)
    {
        Layers[Layers.Length - 1].BackPropOutput(expected);
        for (int i = Layers.Length - 2; i > -1; i--)
            Layers[i].BackPropHidden(Layers[i + 1].Gamma, Layers[i + 1].Weights);

        for (int i = 0; i < Layers.Length; i++)
            Layers[i].UpdateWeights();
    }

    public class Layer
    {
        public int NumberOfInputs;
        public int NumberOfOutputs;

        public bool Last;
        public float[] Inputs;
        public float[] Outputs;
        public float[] NonActivatedOutputs;
        public float[,] Weights;
        public float[,] WeightsDelta;
        public float[] Gamma;
        public float[] Error;
        public static System.Random random = new System.Random();
        public static float LearningRate = 0.01f;

        public Layer(int numberOfInputs, int numberOfOutputs, bool last, bool init)
        {
            NumberOfInputs = numberOfInputs;
            NumberOfOutputs = numberOfOutputs;

            Last = last;
            Inputs = new float[numberOfInputs];
            Outputs = new float[numberOfOutputs];
            NonActivatedOutputs = new float[numberOfOutputs];
            Weights = new float[numberOfOutputs, numberOfInputs];
            WeightsDelta = new float[numberOfOutputs, numberOfInputs];
            Gamma = new float[numberOfOutputs];
            Error = new float[numberOfOutputs];
            if (init)
                InitializeWeights();
        }

        public void InitializeWeights()
        {
            for (int i = 0; i < NumberOfOutputs; i++)
            {
                for (int j = 0; j < NumberOfInputs; j++)
                {
                    Weights[i, j] = (float)random.NextDouble() - 0.5f;
                }
            }
        }

        public float[] FeedForward(float[] inputs)
        {
            Inputs = inputs;

            for (int i = 0; i < NumberOfOutputs; i++)
            {
                Outputs[i] = 0;
                for (int j = 0; j < NumberOfInputs; j++)
                    Outputs[i] += Inputs[j] * Weights[i, j];

                Outputs[i] = A.Activation(Outputs[i], Last);
                NonActivatedOutputs[i] = Outputs[i];
            }
            return Outputs;
        }

        public void LearningCertainty(float[] expected)
        {

        }

        public void BackProp(float[] expected)
        {
            for (int i = 0; i < NumberOfOutputs; i++)
                Error[i] = expected[i] - Outputs[i];

            for (int i = 0; i < NumberOfInputs; i++)
                Gamma[i] = 0;

            for (int i = 0; i < NumberOfOutputs; i++)
            {
                for (int j = 0; j < NumberOfInputs; j++)
                {
                    Gamma[j] += Weights[i, j] * Error[i];
                    WeightsDelta[i, j] = Error[i] * A.ActivationDer(Outputs[i], NonActivatedOutputs[i], Last);
                }
            }
        }

        public void BackPropOutput(float[] expected)
        {
            for (int i = 0; i < NumberOfOutputs; i++)
                Error[i] = Outputs[i] - expected[i];

            for (int i = 0; i < NumberOfOutputs; i++)
                Gamma[i] = Error[i] * A.TanHDer(Outputs[i]);

            for (int i = 0; i < NumberOfOutputs; i++)
                for (int j = 0; j < NumberOfInputs; j++)
                    WeightsDelta[i, j] = Gamma[i] * Inputs[j];
        }

        public void BackPropHidden(float[] gammaForward, float[,] weightsForward)
        {
            for (int i = 0; i < NumberOfOutputs; i++)
            {
                Gamma[i] = 0;

                for (int j = 0; j < gammaForward.Length; j++)
                    Gamma[i] += gammaForward[j] * weightsForward[j, i];

                Gamma[i] *= A.TanH(Outputs[i]);
            }

            for (int i = 0; i < NumberOfOutputs; i++)
                for (int j = 0; j < NumberOfInputs; j++)
                    WeightsDelta[i, j] = Gamma[i] * Inputs[j];
        }

        public void UpdateWeights()
        {
            for (int i = 0; i < NumberOfOutputs; i++)
            {
                for (int j = 0; j < NumberOfInputs; j++)
                {
                    Weights[i, j] -= WeightsDelta[i, j] * LearningRate;
                }
            }
        }
    }

    public static class A
    {
        public static float Activation(float value, bool last)
        {
            if (last)
                return TanH(value);
            switch (Type)
            {
                case 0:
                    return TanH(value);
                case 1:
                    return Swish(value);
                case 2:
                    return Sigmoid(value);
                case 3:
                    return ReLU(value);
                case 4:
                    return ELU(value);
                default:
                    Debug.Log("No Specified Type");
                    return 0;
            }
        }

        public static float ActivationDer(float value, float second, bool last)
        {
            if (last)
                return TanHDer(value);
            switch (Type)
            {
                case 0:
                    return TanHDer(value);
                case 1:
                    return SwishDer(value, second);
                case 2:
                    return SigmoidDer(value);
                case 3:
                    return ReLUDer(value);
                case 4:
                    return ELUDer(value);
                default:
                    Debug.Log("No Specified Derivative Type");
                    return 0;
            }
        }

        public static float TanH(float value)
        {
            return (float)Math.Tanh(value);
        }

        public static float TanHDer(float value)
        {
            return 1 - (value * value);
        }

        private static float Swish(float value)
        {
            return ((value) / (1 + Mathf.Pow((float)Math.E, -value)));
        }

        private static float SwishDer(float value, float og)
        {
            return value * (og - value + 1) / og;
        }

        private static float Sigmoid(float value)
        {
            return (1 / (1 + Mathf.Pow((float)Math.E, -value)));
        }

        private static float SigmoidDer(float value)
        {
            return value * (1 - value);
        }

        private static float ReLU(float value)
        {
            if (value >= 0)
                return value;
            else
                return 0;
        }

        private static float ReLUDer(float value)
        {
            if (value >= 0)
                return 1;
            else
                return 0;
        }

        private static float ELU(float value)
        {
            if (value >= 0)
                return value;
            else
                return Mathf.Pow((float)Math.E, value) + 1;
        }

        private static float ELUDer(float value)
        {
            if (value >= 0)
                return 1;
            else
                return value + 1;
        }
    }
}