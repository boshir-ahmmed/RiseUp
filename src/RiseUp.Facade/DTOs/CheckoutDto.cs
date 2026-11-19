using System.Security.Cryptography;
using System.Text;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Payments;
using RiseUp.Entities.Wallets;
using RiseUp.Facade.Common;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.Payments;

public sealed record CheckoutDto(string OrderNumber, string RedirectUrl, bool IsSandbox);
