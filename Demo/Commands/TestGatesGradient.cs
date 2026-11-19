using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Text;
using System.Xml.Linq;

using Demo.Gates;

using Microsoft.Extensions.Logging;

using NeuralSharp;
using NeuralSharp.Activation;
using NeuralSharp.Generators;

using Newtonsoft.Json.Linq;

using Spectre.Console;
using Spectre.Console.Cli;

using static Demo.Helpers;

namespace Demo.Commands;

internal sealed class
    TestGatesGradient : Command<TestGatesGradient.Settings> //Got rid of the code in this because it's no longer correct with how the products are structured, need to rewrite this at some point.
{
    private readonly IActivationFunction _activationFunction;
    private readonly IBiasGenerator _biasGenerator;
    private readonly IWeightGenerator _weightGenerator;


    public TestGatesGradient(IWeightGenerator weightGenerator, IBiasGenerator biasGenerator,
        IActivationFunction activationFunction)
    {
        _weightGenerator = weightGenerator;
        _biasGenerator = biasGenerator;
        _activationFunction = activationFunction;
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        Console.WriteLine("#####################################");
        Console.WriteLine($"Running test Gates with for {settings.Iterations} iterations.");
        Console.WriteLine("#####################################");

        Gates.Gates.GateTest[] gateTests = Gates.Gates.GetTests(settings.Size);

        int totalTests = 0;
        int[] softPassAt = new int[gateTests.Length];
        int[] hardPassAt = new int[gateTests.Length];
        float[] error = new float[gateTests.Length];

        for (int gateIndex = 0; gateIndex < gateTests.Length; gateIndex++)
        {
            Gates.Gates.GateTest gateTest = gateTests[gateIndex];
            List<(float[] Inputs, float[] Expected)> values =
                CreateTruthTable(gateTest);

            NeuralNetwork network = new NeuralNetwork(
                _weightGenerator,
                _biasGenerator,
                gateTest.LayerSizes);

            totalTests += values.Count;

            for (int iteration = 1; iteration <= settings.Iterations; iteration++)
            {
                foreach ((float[] inputs, float[] expected) in values)
                {
                    network.BackPropagate(_activationFunction, inputs, expected);
                }

                if (softPassAt[gateIndex] == 0 || hardPassAt[gateIndex] == 0)
                {
                    error[gateIndex] = 0;

                    foreach ((float[] inputs, float[] expected) in values)
                    {
                        float[] actual =
                            network.FeedForward(_activationFunction, inputs);

                        for (int outputIndex = 0;
                             outputIndex < actual.Length;
                             outputIndex++)
                        {
                            error[gateIndex] +=
                                Math.Abs(expected[outputIndex] - actual[outputIndex]);
                        }
                    }

                    error[gateIndex] /= values.Count;

                    if (error[gateIndex] <= 0.1f &&
                        softPassAt[gateIndex] == 0)
                    {
                        softPassAt[gateIndex] = iteration;
                    }

                    if (error[gateIndex] <= 0.001f &&
                        hardPassAt[gateIndex] == 0)
                    {
                        hardPassAt[gateIndex] = iteration;
                    }
                }
            }

            foreach ((float[] inputs, float[] expected) in values)
            {
                float[] actual = network.FeedForward(_activationFunction, inputs);

                Output(gateTest.Name, inputs, expected, actual);
            }
        }
        for(int i = 0; i < gateTests.Length; i++)
        {
            Gates.Gates.GateTest gateTest = gateTests[i];
            Console.WriteLine($"{gateTest.Name}: errorRate on completion of {error[i]}");
            if(softPassAt[i] > 0)
            {
                if(hardPassAt[i] > 0)
                {
                    Console.WriteLine($"{gateTest.Name}: completed at {hardPassAt[i]}");
                }
                if(softPassAt[i] > 0)
                {
                    Console.WriteLine($"{gateTest.Name}: Soft Completed at {softPassAt[i]}");
                }
            }
            else
            {
                Console.WriteLine($"{gateTest.Name}: did not soft complete");
            }
        }
        return 0;
    }

    private static List<(float[] Inputs, float[] Expected)> CreateTruthTable(Gates.Gates.GateTest gateTest)
    {
        List<(float[] Inputs, float[] Expected)> values = new List<(float[] Inputs, float[] Expected)>();
        int combinations = 1 << gateTest.InputCount;

        for (int combination = 0; combination < combinations; combination++)
        {
            float[] inputs = new float[gateTest.InputCount];

            for (int inputIndex = 0; inputIndex < gateTest.InputCount; inputIndex++)
            {
                int bitPosition = gateTest.InputCount - inputIndex - 1;
                inputs[inputIndex] = (combination & (1 << bitPosition)) == 0 ? 0f : 1f;
            }

            values.Add((inputs, new[] { gateTest.Calculate(inputs) }));
        }

        return values;
    }

    public sealed class Settings : CommandSettings
    {

        [Description("Size.")]
        [CommandArgument(0, "<size>")]
        public int Size { get; init; }

        [Description("Iteration.")]
        [CommandArgument(2, "<iterations>")]
        public int Iterations { get; init; }
    }

    public static void Output(string name, float[] input, float[] expected, float[] actual)
    {
        var formattedInput = string.Join(", ", input.Select(f => f.ToString("0.0000")));
        var expectedOutput = string.Join(", ", expected.Select(f => f.ToString("0.0000")));
        var actualOutput = string.Join(", ", actual.Select(f => f.ToString("0.0000")));
        float error = expected.Zip(actual, (e, a) => Math.Abs(e - a)).Sum();
        var outputBuilder = new StringBuilder();
        outputBuilder.Append("Input: \t").Append(name).Append("(").Append(formattedInput).AppendLine(") ");
        outputBuilder.Append("Expected: \t'").Append(expectedOutput).AppendLine("' ");
        outputBuilder.Append("Actual: \t'").Append(actualOutput).AppendLine("' ");
        outputBuilder.AppendLine($"Error: \t'{error}' ");
        outputBuilder.AppendLine("----------------------------------------");
        Console.WriteLine(outputBuilder.ToString());
    }


    public class FloatOutputComparer: IEqualityComparer<float>
    {
        float _tolerance;
        public FloatOutputComparer(float tolerance = 0.1f)
        {
            _tolerance = tolerance;
        }
        public bool Equals(float x, float y)
        {
            return Math.Abs(x - y) < _tolerance;
        }

        public int GetHashCode(float obj)
        {
            return obj.GetHashCode();
        }
    }

}