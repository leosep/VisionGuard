using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AnyCam.Models;
using Microsoft.AspNetCore.Authorization;

namespace AnyCam.Controllers
{
    [Authorize]
    public class AiEventsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AiEventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AiEvents
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.AiEvents.Include(a => a.VideoClip);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: AiEvents/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var aiEvent = await _context.AiEvents
                .Include(a => a.VideoClip)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (aiEvent == null)
            {
                return NotFound();
            }

            return View(aiEvent);
        }

        // GET: AiEvents/Create
        public IActionResult Create()
        {
            ViewData["VideoClipId"] = new SelectList(_context.VideoClips, "Id", "Id");
            return View();
        }

        // POST: AiEvents/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,VideoClipId,EventType,Description,Timestamp,DetectedObjects,AlertSent,AlertType")] AiEvent aiEvent)
        {
            if (ModelState.IsValid)
            {
                _context.Add(aiEvent);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["VideoClipId"] = new SelectList(_context.VideoClips, "Id", "Id", aiEvent.VideoClipId);
            return View(aiEvent);
        }

        // GET: AiEvents/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var aiEvent = await _context.AiEvents.FindAsync(id);
            if (aiEvent == null)
            {
                return NotFound();
            }
            ViewData["VideoClipId"] = new SelectList(_context.VideoClips, "Id", "Id", aiEvent.VideoClipId);
            return View(aiEvent);
        }

        // POST: AiEvents/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,VideoClipId,EventType,Description,Timestamp,DetectedObjects,AlertSent,AlertType")] AiEvent aiEvent)
        {
            if (id != aiEvent.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(aiEvent);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AiEventExists(aiEvent.Id))
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
            ViewData["VideoClipId"] = new SelectList(_context.VideoClips, "Id", "Id", aiEvent.VideoClipId);
            return View(aiEvent);
        }

        // GET: AiEvents/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var aiEvent = await _context.AiEvents
                .Include(a => a.VideoClip)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (aiEvent == null)
            {
                return NotFound();
            }

            return View(aiEvent);
        }

        // POST: AiEvents/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var aiEvent = await _context.AiEvents.FindAsync(id);
            if (aiEvent != null)
            {
                _context.AiEvents.Remove(aiEvent);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AiEventExists(int id)
        {
            return _context.AiEvents.Any(e => e.Id == id);
        }
    }
}