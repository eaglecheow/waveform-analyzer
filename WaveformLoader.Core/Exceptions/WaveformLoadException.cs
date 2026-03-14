namespace WaveformLoader.Core.Exceptions;

public sealed class WaveformLoadException : InvalidOperationException
{
    public WaveformLoadException(string message)
        : base(message)
    {
    }
}
