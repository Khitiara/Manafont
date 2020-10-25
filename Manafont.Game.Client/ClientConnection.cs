using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Manafont.Game.Common;
using Manafont.Packets.IO;
using Manafont.Session;

namespace Manafont.Game.Client
{
    public sealed class ClientConnection : IAsyncDisposable, IDisposable, IManafontConnection
    {
        private          TcpClient?                 _client;
        private          SslStream?                 _stream;
        private readonly X509Certificate2Collection _certs = new X509Certificate2Collection();

        private readonly PacketIo _packetIo;

        public ClientConnection(PacketIo packetIo) {
            _packetIo = packetIo;
        }

        public async ValueTask Connect(IPAddress target, CancellationToken cancellationToken = default) {
            _client ??= new TcpClient();
            if (_client.Connected)
                return;
            await _client.ConnectAsync(target, 54995, cancellationToken);
            _stream ??= new SslStream(_client.GetStream(), false,
                CertUtils.CheckManafontCert, null, EncryptionPolicy.RequireEncryption);
            if (_certs.Count == 0) {
                _certs.Import(Resources.ClientCert);
            }

            try {
                await _stream.AuthenticateAsClientAsync("ManafontServer", _certs, false);
            }
            catch (AuthenticationException) {
                await DisposeAsync();
                throw;
            }
        }

        public async ValueTask AuthenticateAsync(string ticket, Version protocolVersion,
            CancellationToken cancellationToken = default) {
            await SendPacketAsync(new AuthPacket(ticket, protocolVersion), cancellationToken);
        }

        public async ValueTask SendPacketAsync<T>(T packet, CancellationToken cancellationToken = default)
            where T : struct {
            await _packetIo.WritePacketAsync(_stream!, packet, cancellationToken);
        }

        public async ValueTask<IPacket> RecvPacketAsync(CancellationToken cancellationToken = default) {
            return await _packetIo.ReadPacketAsync(_stream!, cancellationToken);
        }

        public async ValueTask DisposeAsync() {
            _client?.Dispose();
            if (_stream != null) {
                await _stream.DisposeAsync();
            }

            foreach (X509Certificate2 cert in _certs) {
                cert.Dispose();
            }

            _certs.Clear();
        }

        public void Dispose() {
            _stream?.Dispose();
            _client?.Dispose();
            foreach (X509Certificate2 cert in _certs) {
                cert.Dispose();
            }

            _certs.Clear();
        }
    }
}