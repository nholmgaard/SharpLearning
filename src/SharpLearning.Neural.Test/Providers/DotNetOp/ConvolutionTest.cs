﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpLearning.Neural.Layers;
using System.Diagnostics;
using SharpLearning.Neural.Initializations;
using MathNet.Numerics.LinearAlgebra;
using SharpLearning.Neural.Providers.DotNetOp;
using SharpLearning.Containers.Tensors;
using System.Linq;
using MathNet.Numerics;

namespace SharpLearning.Neural.Test.Providers.DotNetOp
{
    [TestClass]
    public class ConvolutionTest
    {
        [TestMethod]
        public void ConvolutionTest_Forward_Prototype_Timing()
        {
            const int width = 25;
            const int height = 25;
            const int depth = 1;
            const int batchSize = 128;

            int filterCount = 8;
            int filterHeight = 5;
            int filterWidth = 5;

            int strideH = 1;
            int strideW = 1;

            int padH = 0;
            int padW = 0;

            var iterations = 100;

            var timer = new Stopwatch();
            var ellapsed = RunCurrent(iterations, timer,
                width, height, depth, batchSize,
                filterCount, filterHeight, filterWidth, 
                strideH, strideW, 
                padH, padW);

            Trace.WriteLine($"Current: {ellapsed}");

            timer.Reset();
            ellapsed = RunDotNet(iterations, timer,
                width, height, depth, batchSize,
                filterCount, filterHeight, filterWidth,
                strideH, strideW,
                padH, padW);

            Trace.WriteLine($"DotNet: {ellapsed}");

            //Assert.IsFalse(true);
        }

        double RunDotNet(int iterations, Stopwatch timer,
            int width, int height, int depth, int batchSize,
            int filterCount, int filterHeight, int filterWidth,
            int strideH, int strideW,
            int padH, int padW)
        {
            var filterGridWidth = ConvUtils.GetFilterGridLength(width, filterWidth, strideH, padH, BorderMode.Undefined);
            var filterGridHeight = ConvUtils.GetFilterGridLength(height, filterHeight, strideH, padH, BorderMode.Undefined);

            // Calculations of dimensions based on:
            // Nvidia, cuDNN: Efficient Primitives for Deep Learning: https://arxiv.org/pdf/1410.0759.pdf
            var filterCubeSize = depth * filterWidth * filterHeight;
            var filterGridSize = filterGridWidth * filterGridHeight;

            var fans = WeightInitialization.GetFans(new Conv2DLayer(1, 1, 1), width, height, depth);

            var weights = Tensor<float>.CreateRowMajor(filterCount, depth, filterHeight, filterWidth);
            var bias = Tensor<float>.CreateRowMajor(filterCount);
            
            var input = Tensor<float>.CreateRowMajor(batchSize, depth, height, width);
            var output = Tensor<float>.CreateRowMajor(batchSize, filterCount, filterGridHeight, filterGridWidth);


            for (int i = 0; i < iterations; i++)
            {
                timer.Start();

                Convolution.Forward(input, weights, bias,
                    filterCount, filterHeight, filterWidth, 
                    strideH, strideW, 
                    padH, padW, 
                    output);

                timer.Stop();
            }

            return timer.ElapsedMilliseconds / (double)iterations;
        }

        double RunCurrent(int iterations, Stopwatch timer,
            int width, int height, int depth, int batchSize,
            int filterCount, int filterHeight, int filterWidth, 
            int strideH, int strideW, 
            int padH, int padW)
        {
            var sut = new Conv2DLayer(filterWidth, filterHeight, filterCount, strideH, padW, padH);

            sut.Initialize(width, height, depth, batchSize, 
                Initialization.GlorotUniform, new Random(232));

            var fanIn = width * height * depth;
            var input = Matrix<float>.Build.Dense(batchSize, fanIn);

            for (int i = 0; i < iterations; i++)
            {
                timer.Start();
                var actual = sut.Forward(input);
                timer.Stop();
            }

            return timer.ElapsedMilliseconds / (double)iterations;
        }   
    }
}
