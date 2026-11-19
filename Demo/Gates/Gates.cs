using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo.Gates
{
    internal class Gates
    {

        internal static GateTest[] GetTests(int size)
        {
            GateTest[] gateTests = new[]
            {
                new GateTest("BUFFER", 1, inputs => CalculateBUFFER(inputs[0]),
                    BuildLayers(1, 1,size,size  )),

                new GateTest("NOT", 1, inputs => CalculateNOT(inputs[0]),
                    BuildLayers(1, 1,size,size)),

                new GateTest("AND", 2, inputs => CalculateAND(inputs[0], inputs[1]),
                    BuildLayers(2, 1,size,size)),

                new GateTest("OR", 2, inputs => CalculateOR(inputs[0], inputs[1]),
                    BuildLayers(2, 1,size,size)),

                new GateTest("NAND", 2, inputs => CalculateNAND(inputs[0], inputs[1]),
                    BuildLayers(2, 1,size,size)),

                new GateTest("NOR", 2, inputs => CalculateNOR(inputs[0], inputs[1]),
                    BuildLayers(2, 1,size,size)),

                new GateTest("XOR", 2, inputs => CalculateXOR(inputs[0], inputs[1]),
                    BuildLayers(2, 1,size,size)),

                new GateTest("XNOR", 2, inputs => CalculateXNOR(inputs[0], inputs[1]),
                    BuildLayers(2, 1,size,size)),
            };
            return gateTests;
        }

        public sealed record GateTest(
    string Name,
    int InputCount,
    Func<float[], float> Calculate,
    int[] LayerSizes);

        public static int[] BuildLayers(int inputCount, int outputCount, params int[] hiddenSizes)
        {
            return new[] { inputCount }.Concat(hiddenSizes).Append(outputCount).ToArray();
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
            return Math.Max((float)Math.Round(a), (float)Math.Round(b));
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
}
