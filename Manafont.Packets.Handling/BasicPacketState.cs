using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Manafont.Packets.Handling
{
    public class BasicPacketState : IDisposable, IAsyncDisposable
    {
        private readonly CancellationTokenSource _endPacketProcessing = new CancellationTokenSource();

        public CancellationToken EndPacketProcessingToken => _endPacketProcessing.Token;

        public readonly Stream Stream;


        public BasicPacketState(Stream stream) {
            Stream = stream;
        }

        public void CancelProcessing() {
            _endPacketProcessing.Cancel();
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposing) return;
            _endPacketProcessing?.Dispose();
            Stream.Dispose();
        }

        private async ValueTask DisposeAsync(bool disposing) {
            if (!disposing) return;
            _endPacketProcessing?.Dispose();
            await Stream.DisposeAsync();
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync() {
            await DisposeAsync(true);
            GC.SuppressFinalize(this);
        }
    }
}