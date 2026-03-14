using PureHDF;

namespace WaveformLoader.Tests;

internal static class TestHdf5Factory
{
    public static string CreateWaveformFile()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"waveform-loader-{Guid.NewGuid():N}.h5");

        var file = new H5File
        {
            Attributes = new()
            {
                ["instrument"] = "scope-01",
                ["sample-rate"] = 10_000
            },
            ["waveforms"] = new H5Group
            {
                Attributes = new()
                {
                    ["group-note"] = "test-group"
                },
                ["y"] = new H5Dataset(new double[] { 0, 1.25, -0.5, 0.25 })
                {
                    Attributes = new()
                    {
                        ["unit"] = "V"
                    }
                },
                ["x"] = new H5Dataset(new double[] { 0.0, 0.1, 0.2, 0.3 })
                {
                    Attributes = new()
                    {
                        ["unit"] = "s"
                    }
                },
                ["xShort"] = new double[] { 0.0, 0.1, 0.2 },
                ["notes"] = new string[] { "a", "b" },
                ["matrix"] = new double[,] { { 1, 2 }, { 3, 4 } }
            }
        };

        file.Write(filePath);
        return filePath;
    }
}
