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
    public class VideoClipsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VideoClipsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: VideoClips
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.VideoClips.Include(v => v.Camera);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: VideoClips/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var videoClip = await _context.VideoClips
                .Include(v => v.Camera)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (videoClip == null)
            {
                return NotFound();
            }

            return View(videoClip);
        }

        // GET: VideoClips/Create
        public IActionResult Create()
        {
            ViewData["CameraId"] = new SelectList(_context.Cameras, "Id", "Name");
            return View();
        }

        // POST: VideoClips/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CameraId,StartTime,EndTime,FilePath,StorageType,FileSize")] VideoClip videoClip)
        {
            if (ModelState.IsValid)
            {
                _context.Add(videoClip);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CameraId"] = new SelectList(_context.Cameras, "Id", "Name", videoClip.CameraId);
            return View(videoClip);
        }

        // GET: VideoClips/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var videoClip = await _context.VideoClips.FindAsync(id);
            if (videoClip == null)
            {
                return NotFound();
            }
            ViewData["CameraId"] = new SelectList(_context.Cameras, "Id", "Name", videoClip.CameraId);
            return View(videoClip);
        }

        // POST: VideoClips/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CameraId,StartTime,EndTime,FilePath,StorageType,FileSize")] VideoClip videoClip)
        {
            if (id != videoClip.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(videoClip);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VideoClipExists(videoClip.Id))
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
            ViewData["CameraId"] = new SelectList(_context.Cameras, "Id", "Name", videoClip.CameraId);
            return View(videoClip);
        }

        // GET: VideoClips/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var videoClip = await _context.VideoClips
                .Include(v => v.Camera)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (videoClip == null)
            {
                return NotFound();
            }

            return View(videoClip);
        }

        // POST: VideoClips/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var videoClip = await _context.VideoClips.FindAsync(id);
            if (videoClip != null)
            {
                _context.VideoClips.Remove(videoClip);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool VideoClipExists(int id)
        {
            return _context.VideoClips.Any(e => e.Id == id);
        }
    }
}