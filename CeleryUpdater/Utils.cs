using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CeleryUpdater;

public static class Utils
{
    public async static Task DownloadAsync(this HttpClient client, string requestUri, Stream destination, IProgress<double> progressFloat, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        
        long? contentLength = response.Content.Headers.ContentLength;
        using Stream downloadStream = await response.Content.ReadAsStreamAsync();
        
        if (!contentLength.HasValue)
        {
            await downloadStream.CopyToAsync(destination);
            return;
        }

        IProgress<long> relativeProgress = new Progress<long>(totalBytes =>
        {
            float progress = (float)totalBytes / contentLength.Value;
            progressFloat.Report(progress);
        });

        await downloadStream.CopyToAsync(destination, 81920, relativeProgress, cancellationToken);
    }
    
    public static async Task CopyToAsync(this Stream source, Stream destination, int bufferSize, IProgress<long> progress = null, CancellationToken cancellationToken = default)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (!source.CanRead)
            throw new ArgumentException("Has to be readable", nameof(source));
        if (destination == null)
            throw new ArgumentNullException(nameof(destination));
        if (!destination.CanWrite)
            throw new ArgumentException("Has to be writable", nameof(destination));
        if (bufferSize < 0)
            throw new ArgumentOutOfRangeException(nameof(bufferSize));

        byte[] buffer = new byte[bufferSize];
        long totalBytesRead = 0;
        int bytesRead;
        while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
        {
            await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
            totalBytesRead += bytesRead;
            progress?.Report(totalBytesRead);
        }
    }
}