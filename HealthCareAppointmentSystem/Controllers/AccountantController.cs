using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthCareAppointmentSystem.Data;
using HealthCareAppointmentSystem.Models;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace HealthCareAppointmentSystem.Controllers
{
    [Authorize(Roles = "Accountant")]
    public class AccountantController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountantController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Accountant/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var pendingRequests = await _context.WithdrawalRequests
                .Include(w => w.Wallet)
                .ThenInclude(w => w.ApplicationUser)
                .Where(w => w.Status == WithdrawalStatus.Pending)
                .OrderBy(w => w.RequestedAt)
                .ToListAsync();

            var completedRequests = await _context.WithdrawalRequests
                .Include(w => w.Wallet)
                .ThenInclude(w => w.ApplicationUser)
                .Where(w => w.Status != WithdrawalStatus.Pending)
                .OrderByDescending(w => w.ProcessedAt)
                .Take(20)
                .ToListAsync();

            var platformWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.ApplicationUserId == null);

            ViewBag.PlatformBalance = platformWallet?.Balance ?? 0;
            ViewBag.CompletedRequests = completedRequests;

            return View(pendingRequests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessWithdrawal(int id, string action, string notes)
        {
            var request = await _context.WithdrawalRequests
                .Include(w => w.Wallet)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (request == null || request.Status != WithdrawalStatus.Pending)
            {
                TempData["Error"] = "Invalid or already processed withdrawal request.";
                return RedirectToAction(nameof(Dashboard));
            }

            if (action == "Approve")
            {
                request.Status = WithdrawalStatus.Completed;
                request.ProcessedAt = DateTime.UtcNow;
                request.ProcessedByAccountId = User.Identity?.Name;
                request.Notes = notes;

                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = User.Identity?.Name ?? "Unknown",
                    Action = "Withdrawal Approved",
                    Details = $"Approved withdrawal #{id} for Rs. {request.Amount.ToString("N2")}."
                });

                TempData["Success"] = $"Withdrawal #{id} marked as Completed.";
            }
            else if (action == "Reject")
            {
                request.Status = WithdrawalStatus.Rejected;
                request.ProcessedAt = DateTime.UtcNow;
                request.ProcessedByAccountId = User.Identity?.Name;
                request.Notes = notes;

                // Refund to user wallet
                var wallet = request.Wallet;
                if (wallet != null)
                {
                    // Full amount + 50 withdrawal fee was deducted, so refund it all.
                    var withdrawalFee = 50m;
                    wallet.Balance += (request.Amount + withdrawalFee);
                    
                    _context.WalletTransactions.Add(new WalletTransaction
                    {
                        WalletId = wallet.Id,
                        Amount = request.Amount + withdrawalFee,
                        Type = TransactionType.Deposit,
                        Description = $"Refund for rejected withdrawal #{id}",
                        ReferenceId = $"REJECT-{id}"
                    });

                    // We also need to deduct the fee from the Platform Escrow since it was refunded
                    var platformWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.ApplicationUserId == null);
                    if (platformWallet != null)
                    {
                        platformWallet.Balance -= withdrawalFee;
                        _context.WalletTransactions.Add(new WalletTransaction
                        {
                            WalletId = platformWallet.Id,
                            Amount = -withdrawalFee,
                            Type = TransactionType.ServicePayment,
                            Description = $"Reversal of Withdrawal Fee for rejected request #{id}",
                            ReferenceId = $"REJECT-{id}"
                        });
                    }
                }

                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = User.Identity?.Name ?? "Unknown",
                    Action = "Withdrawal Rejected",
                    Details = $"Rejected withdrawal #{id} for Rs. {request.Amount.ToString("N2")}."
                });

                TempData["Success"] = $"Withdrawal #{id} Rejected and funds refunded to user's wallet.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Dashboard));
        }
    }
}
