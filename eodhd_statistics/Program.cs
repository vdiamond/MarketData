namespace eodhd_statistics;

internal sealed class Program
{
    internal static int Main(string[] args)
    {
        var path = ResolveInputPath(args);
        Console.WriteLine($"Loading {path}");
        var table = FeatureTable.Load(path);

        Statistic[] statistics =
        [
            Statistic.PercentWhereGreaterThan("qqq_yield_la1", 0.0),
            Statistic.PercentWhereGreaterThan("qqq_yield_la1", 0.0, "qqq_close", "qqq_ema20")
        ];

        var report = new StatisticReport(table, statistics);
        report.Write(Console.Out, path);
        return 0;
    }

    #region private

    private const string TransformCsvEnvironmentVariable = "MARKETDATA_TRANSFORM_CSV";
    private const string DefaultTransformCsv = @"d:\predict\index_extract_ta.csv";

    private static string ResolveInputPath(string[] args)
    {
        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
        {
            return Path.GetFullPath(args[0]);
        }

        var fromEnv = Environment.GetEnvironmentVariable(TransformCsvEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv;
        }

        return DefaultTransformCsv;
    }

    #endregion
}
