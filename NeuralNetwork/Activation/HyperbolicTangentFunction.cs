﻿namespace NeuralNetwork.Activation
{
    using System;

    public class HyperbolicTangentFunction : IActivationFunction
    {
        private double alpha;
        private double beta;

        public HyperbolicTangentFunction(double alpha = 1, double beta = 1)
        {
            this.alpha = alpha;
            this.beta = beta;
        }

        public double Function(double x)
        {
            return alpha * Math.Tanh(beta * x);
        }

        public double Derivative(double x)
        {
            double y = Function(x);
            return (beta / alpha) * (alpha - y) * (alpha + y);
        }
    }
}
