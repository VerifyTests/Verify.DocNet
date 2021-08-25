using System;
using System.IO;

static class Extensions
{
    public static bool HasValue(this object? input)
    {
        if (input is ValueType)
        {
            var obj = Activator.CreateInstance(input.GetType());
            return !obj!.Equals(input);
        }

        if (input is string s)
        {
            return !string.IsNullOrEmpty(s);
        }

        return input is not null;
    }

    public static byte[] ToBytes(this Stream stream)
    {
        if (stream is MemoryStream memoryStream1)
        {
            return memoryStream1.ToArray();                
        }

        using var memoryStream2 = new MemoryStream();
        stream.CopyTo(memoryStream2);
        return memoryStream2.ToArray();
    }
}