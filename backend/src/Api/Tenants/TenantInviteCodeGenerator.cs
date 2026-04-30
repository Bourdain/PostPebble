namespace Api.Tenants;

public static class TenantInviteCodeGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string Create(int length = 8)
    {
        Span<char> buffer = stackalloc char[length];
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = Alphabet[Random.Shared.Next(Alphabet.Length)];
        }

        return new string(buffer);
    }

    public static string Normalize(string code)
    {
        return code.Trim().Replace("-", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
    }
}
