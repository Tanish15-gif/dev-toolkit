namespace DevToolkit.Auth.Helpers;

internal static class RequestBodyReader
{
    public static async Task<byte[]> ReadAsync(HttpRequest request, int maxBytes, CancellationToken cancellationToken)
    {
        if (request.ContentLength > maxBytes)
        {
            throw new BadHttpRequestException("Request body is too large.", StatusCodes.Status413PayloadTooLarge);
        }

        request.EnableBuffering(bufferThreshold: Math.Min(maxBytes + 1, 30720), bufferLimit: maxBytes + 1);
        var bytes = new byte[maxBytes + 1];

        try
        {
            var count = await request.Body.ReadAtLeastAsync(bytes, bytes.Length, false, cancellationToken);

            if (count > maxBytes)
            {
                throw new BadHttpRequestException("Request body is too large.", StatusCodes.Status413PayloadTooLarge);
            }

            return bytes.AsSpan(0, count).ToArray();
        }
        finally
        {
            request.Body.Position = 0;
        }
    }
}
