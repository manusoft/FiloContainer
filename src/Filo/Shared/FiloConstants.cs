namespace ManuHub.Filo.Shared;

public static class FiloConstants
{
    public const string FooterMagic = "FLOF";

    public const int IvSize = 16;
    public const int LengthSize = 4;
}

public static class FiloFooter
{
    public const int Size =
        sizeof(long) + sizeof(long) + sizeof(long) + 4;
}