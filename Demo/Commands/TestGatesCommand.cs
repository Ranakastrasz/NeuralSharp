using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Text;
using System.Xml.Linq;

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
    TestGatesCommand : Command<TestGatesCommand.Settings> //Got rid of the code in this because it's no longer correct with how the products are structured, need to rewrite this at some point.
{
    private readonly IActivationFunction _activationFunction;
    private readonly IBiasGenerator _biasGenerator;
    private readonly IWeightGenerator _weightGenerator;


    public TestGatesCommand(IWeightGenerator weightGenerator, IBiasGenerator biasGenerator,
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

        GateTest[] gateTests = new[]
        {
            new GateTest("BUFFER", 1, inputs => CalculateBUFFER(inputs[0]),
                new[] { 1, settings.Size, settings.Size, 1 }),
            new GateTest("NOT", 1, inputs => CalculateNOT(inputs[0]),
                new[] { 1, settings.Size, settings.Size, 1 }),
            new GateTest("AND", 2, inputs => CalculateAND(inputs[0], inputs[1]),
                new[] { 2, settings.Size, settings.Size, 1 }),
            new GateTest("OR", 2, inputs => CalculateOR(inputs[0], inputs[1]),
                new[] { 2, settings.Size, settings.Size, 1 }),
            new GateTest("NAND", 2, inputs => CalculateNAND(inputs[0], inputs[1]),
                new[] { 2, settings.Size, settings.Size, 1 }),
            new GateTest("NOR", 2, inputs => CalculateNOR(inputs[0], inputs[1]),
                new[] { 2, settings.Size, settings.Size, 1 }),
            new GateTest("XOR", 2, inputs => CalculateXOR(inputs[0], inputs[1]),
                new[] { 2, settings.Size, settings.Size, 1 }),
            new GateTest("XNOR", 2, inputs => CalculateXNOR(inputs[0], inputs[1]),
                new[] { 2, settings.Size, settings.Size, 1 }),
        };

        int totalTests = 0;
        int[] softPassAt = new int[gateTests.Length];
        int[] hardPassAt = new int[gateTests.Length];
        float[] error = new float[gateTests.Length];

        for (int gateIndex = 0; gateIndex < gateTests.Length; gateIndex++)
        {
            GateTest gateTest = gateTests[gateIndex];
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
            GateTest gateTest = gateTests[i];
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

    private static List<(float[] Inputs, float[] Expected)> CreateTruthTable(GateTest gateTest)
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
#region Calculations
    private static float CalculateBUFFER(float a)
    {
        return (float)Math.Round(a);
    }
    private static float CalculateNOT(float a)
    {
        return 1f - (float)Math.Round(a);
    }
    private static float CalculateAND(float a, float b)
    {
        return (float)Math.Round(a) * (float)Math.Round(b);
    }
    private static float CalculateOR(float a, float b)
    {
        return Math.Max((float) Math.Round(a), (float) Math.Round(b));
    }
    private static float CalculateNAND(float a, float b)
    {
        return 1f - ((float)Math.Round(a) * (float)Math.Round(b));
    }
    private static float CalculateNOR(float a, float b)
    {
        return 1f - Math.Max((float)Math.Round(a), (float)Math.Round(b));
    }
    private static float CalculateXOR(float a, float b)
    {
        return Math.Abs((float)Math.Round(a) - (float)Math.Round(b));
    }
    private static float CalculateXNOR(float a, float b)
    {
        return 1f - Math.Abs((float)Math.Round(a) - (float)Math.Round(b));
    }
#endregion

    public sealed class Settings : CommandSettings
    {

        [Description("Size.")]
        [CommandArgument(0, "<size>")]
        public int Size { get; init; }

        [Description("Iteration.")]
        [CommandArgument(2, "<iterations>")]
        public int Iterations { get; init; }
    }

    private sealed record GateTest(
        string Name,
        int InputCount,
        Func<float[], float> Calculate,
        int[] LayerSizes);


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