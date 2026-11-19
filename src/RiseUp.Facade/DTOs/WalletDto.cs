using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Wallets;
using RiseUp.Facade.Common;
using RiseUp.Framework.Results;

namespace RiseUp.Facade.Wallets;

public sealed record WalletDto(int WelcomeBalance, int PurchasedBalance, IReadOnlyList<LedgerLine> Ledger);
