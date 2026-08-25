using HealthCareAppointmentSystem.Data;
using HealthCareAppointmentSystem.Models;
using HealthCareAppointmentSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthCareAppointmentSystem.Controllers
{
    [Authorize]
    public class WalletController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStripePaymentService _stripePaymentService;
        private readonly IConfiguration _configuration;

        public WalletController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IStripePaymentService stripePaymentService, IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _stripePaymentService = stripePaymentService;
            _configuration = configuration;
        }

        // GET: Wallet Dashboard
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            Wallet? wallet = null;
            
            if (User.IsInRole("Admin"))
            {
                // Admins see the Platform Escrow Wallet
                wallet = await _context.Wallets
                    .Include(w => w.Transactions.OrderByDescending(t => t.CreatedAt).Take(50))
                    .FirstOrDefaultAsync(w => w.ApplicationUserId == null);
            }
            else
            {
                wallet = await _context.Wallets
                    .Include(w => w.Transactions.OrderByDescending(t => t.CreatedAt).Take(50))
                    .FirstOrDefaultAsync(w => w.ApplicationUserId == user.Id);
            }

            if (wallet == null)
            {
                TempData["Error"] = "Wallet not found. Please contact support.";
                return RedirectToAction("Index", "Home");
            }

            return View(wallet);
        }

        // POST: Initiate Deposit via Stripe (Patients)
        [HttpPost]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> InitiateDeposit(decimal amount)
        {
            if (amount < 100)
            {
                TempData["Error"] = "Minimum deposit amount is Rs. 100";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.GetUserAsync(User);
            var referenceId = $"DEP-{user.Id}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

            // Store pending amount in session or tempdata for verification
            TempData["PendingDepositAmount"] = amount.ToString();
            TempData["PendingDepositRef"] = referenceId;

            var returnUrl = Url.Action("VerifyDeposit", "Wallet", null, Request.Scheme);
            var cancelUrl = Url.Action("Index", "Wallet", null, Request.Scheme);

            var checkoutUrl = await _stripePaymentService.CreateCheckoutSessionAsync(amount, referenceId, returnUrl, cancelUrl);
            
            return Redirect(checkoutUrl);
        }

        // GET: Verify Stripe Deposit (Callback)
        [HttpGet]
        public async Task<IActionResult> VerifyDeposit(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                TempData["Error"] = "Invalid payment session.";
                return RedirectToAction(nameof(Index));
            }

            var isValid = await _stripePaymentService.IsPaymentSuccessfulAsync(sessionId);
            if (!isValid)
            {
                TempData["Error"] = "Payment was not successful or has not been completed.";
                return RedirectToAction(nameof(Index));
            }

            var expectedRef = TempData["PendingDepositRef"]?.ToString();
            var amountStr = TempData["PendingDepositAmount"]?.ToString();
            
            if (!decimal.TryParse(amountStr, out var amount))
            {
                TempData["Error"] = "Invalid payment session or already processed.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.GetUserAsync(User);
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.ApplicationUserId == user.Id);
            
            if (wallet != null)
            {
                wallet.Balance += amount;
                
                var tx = new WalletTransaction
                {
                    WalletId = wallet.Id,
                    Amount = amount,
                    Type = TransactionType.Deposit,
                    Description = "Deposit via Stripe",
                    ReferenceId = sessionId
                };
                
                _context.WalletTransactions.Add(tx);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = $"Successfully deposited Rs. {amount.ToString("N2")} into your wallet!";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Withdraw Funds (Doctors/Pharmacists)
        [HttpPost]
        [Authorize(Roles = "Doctor,Pharmacist")]
        public async Task<IActionResult> WithdrawFunds(decimal amount, string bankDetails)
        {
            if (amount < 500)
            {
                TempData["Error"] = "Minimum withdrawal amount is Rs. 500";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(bankDetails))
            {
                TempData["Error"] = "Bank details are required.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.GetUserAsync(User);
            var userWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.ApplicationUserId == user.Id);
            var platformWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.ApplicationUserId == null);

            var withdrawalFee = 50m; // Flat Rs. 50 fee

            if (userWallet == null || platformWallet == null || userWallet.Balance < (amount + withdrawalFee))
            {
                TempData["Error"] = "Insufficient funds. Remember there is a Rs. 50 withdrawal fee.";
                return RedirectToAction(nameof(Index));
            }

            // Debit User Wallet
            userWallet.Balance -= (amount + withdrawalFee);
            
            var withdrawTx = new WalletTransaction
            {
                WalletId = userWallet.Id,
                Amount = -amount,
                Type = TransactionType.Withdrawal,
                Description = $"Withdrawal to {bankDetails}"
            };
            
            var feeTx = new WalletTransaction
            {
                WalletId = userWallet.Id,
                Amount = -withdrawalFee,
                Type = TransactionType.WithdrawalFee,
                Description = "Withdrawal Fee"
            };

            // Credit Platform Escrow for the fee
            platformWallet.Balance += withdrawalFee;
            var platformFeeTx = new WalletTransaction
            {
                WalletId = platformWallet.Id,
                Amount = withdrawalFee,
                Type = TransactionType.PlatformCommission,
                Description = $"Withdrawal Fee collected from {user.FullName}"
            };

            _context.WalletTransactions.AddRange(withdrawTx, feeTx, platformFeeTx);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Withdrawal of Rs. {amount.ToString("N2")} initiated successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
