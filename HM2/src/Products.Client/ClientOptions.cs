using System.Diagnostics.CodeAnalysis;

namespace Products.Client;

internal sealed record ClientOptions(string Mode, Uri Address)
{
    public static bool TryParse(string[] args, [NotNullWhen(true)] out ClientOptions? options)
    {
        options = null;
        if (args.Length > 2)
        {
            return false;
        }

        var mode = args.Length > 0 ? args[0].ToLowerInvariant() : "grpc";
        if (mode is not ("http" or "grpc"))
        {
            return false;
        }

        var defaultAddress = mode == "grpc" ? "http://localhost:5081" : "http://localhost:5080";
        var addressText = args.Length == 2 ? args[1] : defaultAddress;
        if (!Uri.TryCreate(addressText, UriKind.Absolute, out var address) ||
            address.Scheme is not ("http" or "https"))
        {
            return false;
        }

        options = new ClientOptions(mode, address);
        return true;
    }
}
