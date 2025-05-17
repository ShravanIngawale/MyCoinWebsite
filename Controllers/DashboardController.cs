using Expense_Tracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Globalization;

namespace Expense_Tracker.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Set range for the current month
            DateTime StartDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            DateTime EndDate = StartDate.AddMonths(1).AddDays(-1); // Last day of current month


            List<Transaction> SelectedTransactions = await _context.Transactions
                .Include(x => x.Category)
                .Where(y => y.UserId == userId && y.Date >= StartDate && y.Date <= EndDate)
                .ToListAsync();

            int TotalIncome = SelectedTransactions
                .Where(i => i.Category.Type == "Income")
                .Sum(j => j.Amount);

            int TotalExpense = SelectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .Sum(j => j.Amount);

            int Balance = TotalIncome - TotalExpense;

            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-IN");
            culture.NumberFormat.CurrencyNegativePattern = 1;

            ViewBag.TotalIncome = TotalIncome.ToString("C0", culture).Replace(" ", "");
            ViewBag.TotalExpense = TotalExpense.ToString("C0", culture).Replace(" ", "");
            ViewBag.Balance = Balance.ToString("C0", culture).Replace(" ", "");

            // Doughnut Chart - Expense By Category
            ViewBag.DoughnutChartData = SelectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .GroupBy(j => j.Category.CategoryId)
                .Select(k => new
                {
                    categoryTitle = k.First().Category.Title,
                    categoryIcon = k.First().Category.Icon,
                    formatedAmount = k.Sum(j => j.Amount).ToString("C0", culture).Replace(" ", ""),
                    amount = k.Sum(j => j.Amount),
                })
                .OrderByDescending(l => l.amount)
                .ToList();

            // Days in current month up to today
            int daysInMonth = DateTime.DaysInMonth(StartDate.Year, StartDate.Month);
            string[] DaysOfMonth = Enumerable.Range(1, daysInMonth)
                .Select(day => day.ToString("D2")) // Gives 01, 02, ..., 31
                .ToArray();

            ViewBag.CurrentMonthName = StartDate.ToString("MMMM yyyy"); // e.g., "May 2025"


            // Income Summary
            List<SplineChartData> IncomeSummary = SelectedTransactions
                .Where(i => i.Category.Type == "Income")
                .GroupBy(j => j.Date.Day)
                .Select(k => new SplineChartData()
                {
                    day = k.Key.ToString("D2"),
                    income = k.Sum(l => l.Amount)
                })
                .ToList();

            // Expense Summary
            List<SplineChartData> ExpenseSummary = SelectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .GroupBy(j => j.Date.Day)
                .Select(k => new SplineChartData()
                {
                    day = k.Key.ToString("D2"),
                    expense = k.Sum(l => l.Amount)
                })
                .ToList();


            // Combine Income and Expense Data for Spline Chart
            ViewBag.SplineChartData = from day in DaysOfMonth
                                      join income in IncomeSummary on day equals income.day into incomeJoined
                                      from income in incomeJoined.DefaultIfEmpty()
                                      join expense in ExpenseSummary on day equals expense.day into expenseJoined
                                      from expense in expenseJoined.DefaultIfEmpty()
                                      select new
                                      {
                                          day = day,
                                          income = income?.income ?? 0,
                                          expense = expense?.expense ?? 0
                                      };

            // Recent Transactions
            ViewBag.RecentTransactions = await _context.Transactions
                .Include(i => i.Category)
                .Where(t => t.UserId == userId)
                .OrderByDescending(j => j.Date)
                .Take(5)
                .ToListAsync();

            return View();
        }

    }
}

    public class SplineChartData
    {
        public string day;
        public int income;
        public int expense;
    }
