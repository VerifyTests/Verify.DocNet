static class Extensions
{
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