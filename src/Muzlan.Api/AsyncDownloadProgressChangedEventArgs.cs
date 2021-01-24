using System;
using System.Threading.Tasks;

namespace Muzlan.Api
{
    public delegate Task AsyncDownloadProgressChanged(object sender, AsyncDownloadProgressChangedEventArgs eventArgs);

    public class AsyncDownloadProgressChangedEventArgs : EventArgs
    {
        public int BytesReceived { get; }
        public int BytesTotal { get; }

        public double ProgressPercentage { get; }

        public AsyncDownloadProgressChangedEventArgs(int bytesReceived, int bytesTotal)
        {
            BytesReceived = bytesReceived;
            BytesTotal = bytesTotal;

            ProgressPercentage = (double)bytesReceived / bytesTotal * 100.0D;
        }
    }
}
