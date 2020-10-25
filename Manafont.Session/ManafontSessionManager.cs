using System;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Manafont.Db;
using Manafont.Db.Model;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.EntityFrameworkCore.Models;

namespace Manafont.Session
{
    public sealed class ManafontSessionManager
    {
        private readonly IServiceProvider _serviceProvider;

        public ManafontSessionManager(IServiceProvider serviceProvider) {
            _serviceProvider = serviceProvider;
        }

        public async Task<ManafontGameSession> VerifyCreateSessionAsync(string ticket,
            CancellationToken cancellationToken = default) {
            using IServiceScope scope = _serviceProvider.CreateScope();
            IServiceProvider sp = scope.ServiceProvider;
            ManafontDbContext dbContext = sp.GetRequiredService<ManafontDbContext>();
            OpenIddictTokenManager<OpenIddictEntityFrameworkCoreToken> tokenManager =
                sp.GetRequiredService<OpenIddictTokenManager<OpenIddictEntityFrameworkCoreToken>>();

            OpenIddictEntityFrameworkCoreToken?
                token = await tokenManager.FindByIdAsync(ticket, cancellationToken);
            if (token is null) {
                throw new SecurityException(
                    $"Token {ticket} is null!]");
            }

            if (!await tokenManager.HasStatusAsync(token,
                OpenIddictConstants.Statuses.Valid, cancellationToken)) {
                throw new SecurityException($"Token {ticket} is invalid: " +
                    await tokenManager.GetStatusAsync(token, cancellationToken));
            }

            string? subject = await tokenManager.GetSubjectAsync(token, cancellationToken);
            if (subject is null) {
                throw new SecurityException($"Token {ticket} has null subject!");
            }

            ManafontUser? manafontUser = await dbContext.Users.FindAsync(new object[] {subject},
                cancellationToken);
            if (manafontUser is null) {
                throw new SecurityException($"Token {ticket} returned null user!");
            }

            if (manafontUser.GameSessions.Any()) {
                foreach (ManafontGameSession sess in manafontUser.GameSessions) {
                    sess.Status = GameSessionState.Revoked;
                    dbContext.Update(sess);
                }
            }

            ManafontGameSession session = new ManafontGameSession(manafontUser) {Status = GameSessionState.Valid};
            await dbContext.AddAsync(session, cancellationToken);
            await tokenManager.TryRevokeAsync(token, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return session;
        }
    }
}