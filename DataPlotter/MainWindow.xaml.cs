﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Drawing;

using NeuralNetwork.Activation;
using NeuralNetwork.Network;
using NeuralNetwork.Learning;
using NeuralNetwork.Properties;

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace DataPlotter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Chart neuralNetErrorChart;
        private delegate void AddDataDelegate();
        private AddDataDelegate addDataFunction;
        BlockingCollection<string> data;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += new RoutedEventHandler(AddChartToMainWindow);
        }

        private void AddChartToMainWindow(object sender, EventArgs e)
        {
             data = new BlockingCollection<string>(100);

            neuralNetErrorChart = new Chart();

            //Series is where the points are "stored"
            Series trainingErrorSeries = new Series();
            trainingErrorSeries.ChartType = SeriesChartType.Line;
            trainingErrorSeries.Color = Color.DodgerBlue;

            Series testErrorSeries = new Series();
            testErrorSeries.ChartType = SeriesChartType.Line;
            testErrorSeries.Color = Color.IndianRed;

            //Represent the drawing area
            ChartArea chartArea = new ChartArea();
            chartArea.AxisX.Title = "Epoch";
            chartArea.AxisY.Title = "Net Error (MSE)";

            neuralNetErrorChart.ChartAreas.Add(chartArea);
            neuralNetErrorChart.Series.Add(trainingErrorSeries);
            neuralNetErrorChart.Series.Add(testErrorSeries);

            winformhost.Child = neuralNetErrorChart;

            Thread producer = new Thread(new ThreadStart(BackProp));

            Thread addData = new Thread(() =>
            {
                while (!neuralNetErrorChart.IsDisposed)
                {
                    neuralNetErrorChart.Invoke(addDataFunction);
                    Thread.Sleep(100);
                }
            });

            addDataFunction = new AddDataDelegate(AddData);

            producer.Start();
            addData.Start();
        }

        private void AddData()
        {
            while (!data.IsCompleted)
            {
                string[] s = data.Take().Split(':');
                string epoch = s[0];
                string trainError = s[1];
                string testError = null;
                if (s.Length > 2)
                    testError = s[2];
                neuralNetErrorChart.Series[0].Points.AddXY(Double.Parse(epoch), Double.Parse(trainError));
                if (testError != null)
                    neuralNetErrorChart.Series[1].Points.AddXY(Double.Parse(epoch), Double.Parse(testError));
                neuralNetErrorChart.Invalidate();
            }
        }

        private void BackProp()
        {
            Tuple<double[][], double[][]> dataset;
            Tuple<double[][], double[][]> testset;

            double[][] trainingExamples;
            double[][] expectedOutputs;

            double[][] testInput;
            double[][] testOutput;

            int[] layerSize = { 3, 1 };
            IActivationFunction[] functions = { new SigmoidFunction(), new SigmoidFunction() };
            NeuralNet net = new NeuralNet(17, layerSize, functions);
            
            BackPropagationTrainer backProp = new BackPropagationTrainer(net, 0.3);
            

            using (StringReader trainSet = new StringReader(NeuralNetwork.Properties.Resources.monks_1_train))
            using (StringReader testSet = new StringReader(NeuralNetwork.Properties.Resources.monks_1_test))
            {
                dataset = ReadDataset(trainSet);
                testset = ReadDataset(testSet);

                trainingExamples = dataset.Item1;
                expectedOutputs = dataset.Item2;

                testInput = testset.Item1;
                testOutput = testset.Item2;

                backProp.MaxEpoch = 10000;

                backProp.EnableLogging(data);
                backProp.Learn(trainingExamples, expectedOutputs);

            }
        }

        static double[] Encode(int value, byte encode)
        {
            double[] encoded = new double[encode];
            encoded[value - 1] = 1;
            return encoded;
        }

        static double[] EncodingInput(string[] inputs)
        {
            List<double> input = new List<double>();
            byte[] encoding = { 3, 3, 2, 3, 4, 2 };

            for (int i = 0; i < inputs.Length; i++)
            {
                input.InsertRange(input.Count, Encode(Int32.Parse(inputs[i]), encoding[i]));
            }
            return input.ToArray();
        }

        static Tuple<double[][], double[][]> ReadDataset(StringReader stream, string outputFile = null)
        {
            try
            {
                string trainingExample = stream.ReadLine();
                trainingExample.TrimEnd();
                char[] separator = { ' ' };

                List<double[]> inputs = new List<double[]>();
                List<double[]> outputs = new List<double[]>();
                int c = 0;
                StreamWriter writer = null;


                if (outputFile != null)
                    writer = new StreamWriter(new FileStream(outputFile, FileMode.CreateNew));

                while (trainingExample != null)
                {
                    ++c;
                    string[] strings = trainingExample.Split(separator);
                    double[] output = { Double.Parse(strings[0]) };
                    List<string> stringList = new List<string>(strings);
                    strings = stringList.GetRange(1, strings.Length - 1).ToArray();
                    double[] input = EncodingInput(strings);

                    inputs.Add(input);
                    outputs.Add(output);

                    if (writer != null)
                    {
                        foreach (double d in input)
                            writer.Write(d.ToString() + " ");
                        foreach (double d in output)
                            writer.Write(d.ToString() + " ");
                        writer.WriteLine();
                    }

                    trainingExample = stream.ReadLine();
                }

                if (writer != null)
                    writer.Close();

                double[][] inputList = inputs.ToArray();
                double[][] outputList = outputs.ToArray();

                return new Tuple<double[][], double[][]>(inputList, outputList);


            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

    }
}