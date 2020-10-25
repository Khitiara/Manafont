using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Manafont.Db.Model;
using Manafont.Game.Common;
using Manafont.Packets.IO;
using Manafont.Session;

namespace Manafont.Game.Server
{
    public sealed class ServerConnection : IAsyncDisposable, IDisposable
    {
        private          TcpListener?           _listener;
        private          X509Certificate2?      _cert;
        private readonly List<ClientConnection> _clients = new List<ClientConnection>();
        private          bool                   _disposing;
        private          bool                   _stop;
        private readonly PacketIo               _packetIo;
        private readonly ManafontSessionManager _sessionManager;
        private          Version                _protocolVersion = null!;

        public ServerConnection(PacketIo packetIo, ManafontSessionManager sessionManager) {
            _packetIo = packetIo;
            _sessionManager = sessionManager;
        }

        public void Stop() => _stop = true;

        public async Task RunAsync(Version protocolVersion, bool disposeOnStop = false,
            CancellationToken cancellationToken = default) {
            _protocolVersion = protocolVersion;
            _cert = new X509Certificate2(Resources.ServerCert);
            await using (cancellationToken.Register(Stop)) {
                _listener ??= TcpListener.Create(54995);
                _listener.Start();
                while (!_stop) {
                    TcpClient tcpClient = await _listener.AcceptTcpClientAsync();
                    ClientConnection connection = new ClientConnection(this, tcpClient);
                    _clients.Add(connection);
                    connection.HandleAsync(cancellationToken);
                }
            }

            if (disposeOnStop) {
                await DisposeAsync();
            }
        }

        public async ValueTask DisposeAsync() {
            _disposing = true;
            _listener?.Stop();
            _cert?.Dispose();
            foreach (ClientConnection client in _clients) {
                await client.DisposeAsync();
            }

            _clients.Clear();
        }

        public void Dispose() {
            _disposing = true;
            _listener?.Stop();
            _cert?.Dispose();
            foreach (ClientConnection client in _clients) {
                client.Dispose();
            }

            _clients.Clear();
        }

        private class ClientConnection : IAsyncDisposable, IDisposable, IManafontConnection
        {
            private ServerConnection     _server;
            private TcpClient            _conn;
            private SslStream            _stream;
            private ManafontGameSession? _session;

            public ClientConnection(ServerConnection server, TcpClient conn) {
                _server = server;
                _conn = conn;
                _stream = new SslStream(_conn.GetStream(), false,
                    CertUtils.CheckManafontCert, null, EncryptionPolicy.RequireEncryption);
            }

            public async void HandleAsync(CancellationToken cancellationToken = default) {
                try {
                    await _stream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions {
                        ServerCertificate = _server._cert,
                        ClientCertificateRequired = true,
                        CertificateRevocationCheckMode = X509RevocationMode.NoCheck
                    }, cancellationToken);
                }
                catch (AuthenticationException) {
                    await DisposeAsync();
                }

                AuthPacket authPacket = await this.ExpectPacketAsync<AuthPacket>(cancellationToken);
                if (_server._protocolVersion != authPacket.ProtocolVersion) { }

                _session = await _server._sessionManager.VerifyCreateSessionAsync(authPacket.OAuthToken,
                    cancellationToken);
            }


            public async ValueTask SendPacketAsync<T>(T packet, CancellationToken cancellationToken)
                where T : struct {
                await _server._packetIo.WritePacketAsync(_stream, packet, cancellationToken);
            }

            public async ValueTask<IPacket> RecvPacketAsync(CancellationToken cancellationToken) {
                return await _server._packetIo.ReadPacketAsync(_stream, cancellationToken);
            }

            public async ValueTask DisposeAsync() {
                await _stream.DisposeAsync();
                _conn.Dispose();
                if (!_server._disposing) {
                    _server._clients.Remove(this);
                }
            }

            public void Dispose() {
                _stream.Dispose();
                _conn.Dispose();
                if (!_server._disposing) {
                    _server._clients.Remove(this);
                }
            }
        }
    }
}