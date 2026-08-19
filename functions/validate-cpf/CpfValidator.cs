namespace GearFlow.Lambda.ValidateCpf;

/// <summary>
/// Lógica migrada de GearFlow.Domain (Cpf + Utils.IsValidCpf).
/// </summary>
public static class CpfValidator
{
    public static string OnlyNumbers(string? src)
    {
        if (string.IsNullOrEmpty(src))
            return string.Empty;

        return new string(src.Where(char.IsDigit).ToArray());
    }

    public static bool IsValidCpf(string? src)
    {
        if (string.IsNullOrEmpty(src))
            return false;

        src = OnlyNumbers(src);

        if (src.Length != 11)
            return false;

        if (new string(src[0], 11) == src)
            return false;

        int[] mult1 = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] mult2 = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

        var temp = src[..9];
        var sum = 0;

        for (var i = 0; i < 9; i++)
            sum += (temp[i] - '0') * mult1[i];

        var digit = GetDigit(sum);
        temp += digit;

        sum = 0;

        for (var i = 0; i < 10; i++)
            sum += (temp[i] - '0') * mult2[i];

        digit += GetDigit(sum);

        return src.EndsWith(digit, StringComparison.Ordinal);
    }

    private static string GetDigit(int sum)
    {
        var rest = sum % 11;
        rest = rest < 2 ? 0 : 11 - rest;
        return rest.ToString();
    }
}
