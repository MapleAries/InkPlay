namespace InkPlay.Core.Interfaces;

public interface IAiProviderFactory
{
    IAiProvider GetProvider(string providerId);
    IReadOnlyList<string> GetAvailableProviders();
}
