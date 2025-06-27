using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartFleet.Data;
using SmartFleet.Models;
using SmartFleet.Services;

namespace SmartFleet.Controllers
{
    public class SimCardsController : Controller
    {
        private readonly SmartFleetContext _context;
        private readonly INotificationService _notificationService;
        private readonly IUserRoleService _userRoleService;

        public SimCardsController(SmartFleetContext context, INotificationService notificationService, IUserRoleService userRoleService)
        {
            _context = context;
            _notificationService = notificationService;
            _userRoleService = userRoleService;
        }

        // GET: SimCards
        //search & filter
        public async Task<IActionResult> Index(string searchSimNumber, string searchCarrier, string statusFilter)
        {
            var simCards = _context.SimCards.AsQueryable();

            if (!string.IsNullOrEmpty(searchSimNumber))
            {
                simCards = simCards.Where(s => s.SimNumber.Contains(searchSimNumber));
            }

            if (!string.IsNullOrEmpty(searchCarrier))
            {
                simCards = simCards.Where(s => s.Carrier.Contains(searchCarrier));
            }

            if (Enum.TryParse<SimCardStatus>(statusFilter, out var parsedStatus))
            {
                simCards = simCards.Where(s => s.Status == parsedStatus);
            }

            return View(await simCards.ToListAsync());
        }

        // GET: SimCards/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var simCard = await _context.SimCards
                .FirstOrDefaultAsync(m => m.Id == id);
            if (simCard == null)
            {
                return NotFound();
            }

            return View(simCard);
        }

        // GET: SimCards/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: SimCards/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,SimNumber,Carrier,ActivatedAt,Status,CreatedAt")] SimCard simCard)
        {
            if (ModelState.IsValid)
            {
                _context.Add(simCard);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(simCard);
        }

        // GET: SimCards/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var simCard = await _context.SimCards.FindAsync(id);
            if (simCard == null)
            {
                return NotFound();
            }
            return View(simCard);
        }

        // POST: SimCards/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,SimNumber,Carrier,ActivatedAt,Status,CreatedAt")] SimCard simCard)
        {
            if (id != simCard.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get the original SimCard from DB
                    var originalSimCard = await _context.SimCards.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
                    bool statusChanged = originalSimCard != null && originalSimCard.Status != simCard.Status;

                    _context.Update(simCard);
                    await _context.SaveChangesAsync();

                    // --- Notification Logic ---
                    if (statusChanged)
                    {
                        var fleetManagers = await _userRoleService.GetUsersByRole("FleetManager");
                        string title = "SimCard Status Changed";
                        string message = $"SimCard {simCard.SimNumber} status changed to {simCard.Status}.";
                        foreach (var user in fleetManagers)
                        {
                            await _notificationService.CreateNotificationAsync(user.Id, title, message, RelatedTable.SimCard, simCard.Id);
                        }
                    }
                    // --- End Notification Logic ---
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SimCardExists(simCard.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(simCard);
        }

        // GET: SimCards/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var simCard = await _context.SimCards
                .FirstOrDefaultAsync(m => m.Id == id);
            if (simCard == null)
            {
                return NotFound();
            }

            return View(simCard);
        }

        // POST: SimCards/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var simCard = await _context.SimCards.FindAsync(id);
            if (simCard != null)
            {
                _context.SimCards.Remove(simCard);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SimCardExists(int id)
        {
            return _context.SimCards.Any(e => e.Id == id);
        }
    }
}
