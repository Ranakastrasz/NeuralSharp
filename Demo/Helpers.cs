using System.Text;

namespace Demo;

public static class Helpers
{
    public static string Format(this float x)
    {
        return x.ToString("0.00");
    }

    public static void Output(float[] expected, float[] actual)
    {
        var expectedOutput = string.Join(", ", expected.Select(f => f.ToString("0.00")));
        var actualOutput = string.Join(", ", actual.Select(f => f.ToString("0.00")));

        var outputBuilder = new StringBuilder();
        outputBuilder.Append("Expected: \t'").Append(expectedOutput).AppendLine("' ");
        outputBuilder.Append("Actual: \t'").Append(actualOutput).AppendLine("' ");
        outputBuilder.AppendLine($"Pass: \t'{expected.SequenceEqual(actual, new FloatOutputComparer())}' ");
        outputBuilder.AppendLine("----------------------------------------");
        Console.WriteLine(outputBuilder.ToString());
    }

    public static bool Output(string name, float[] input, float[] expected, float[] actual)
    {
        var formattedInput = string.Join(", ", input.Select(f => f.ToString("0.00")));
        var expectedOutput = string.Join(", ", expected.Select(f => f.ToString("0.00")));
        var actualOutput = string.Join(", ", actual.Select(f => f.ToString("0.00")));
        bool equal = expected.SequenceEqual(actual, new FloatOutputComparer());

        var outputBuilder = new StringBuilder();
        outputBuilder.Append("Input: \t").Append(name).Append("(").Append(formattedInput).AppendLine(") ");
        outputBuilder.Append("Expected: \t'").Append(expectedOutput).AppendLine("' ");
        outputBuilder.Append("Actual: \t'").Append(actualOutput).AppendLine("' ");
        outputBuilder.AppendLine($"Pass: \t'{equal}' ");
        outputBuilder.AppendLine("----------------------------------------");
        Console.WriteLine(outputBuilder.ToString());

        return equal;
    }

    public class FloatOutputComparer : IEqualityComparer<float>
    {
        public bool Equals(float x, float y)
        {
            return Math.Abs(x - y) < 0.1;
        }

        public int GetHashCode(float obj)
        {
            return obj.GetHashCode();
        }
    }
}