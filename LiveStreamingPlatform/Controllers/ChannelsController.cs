using LiveStreamingPlatform.Models.ViewModels;
using LiveStreamingPlatform.Data;
using LiveStreamingPlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public class ChannelsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ChannelsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: CHANNELS
    [AllowAnonymous]
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Channels.ToListAsync());
    }

    // GET: CHANNELS/Details/5
    [AllowAnonymous]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var channel = await _context.Channels
            .FirstOrDefaultAsync(m => m.Id == id);
        if (channel == null)
        {
            return NotFound();
        }

        return View(channel);
    }

    // GET: CHANNELS/Create
    [Authorize]
    public async Task<IActionResult> Create()
    {
        var userId = GetUserId();

        var existingChannel = await _context.Channels
            .FirstOrDefaultAsync(c => c.OwnerId == userId);

        if (existingChannel != null)
        {
            return RedirectToAction(nameof(Details), new { id = existingChannel.Id });
        }

        return View(new ChannelFormViewModel());
    }

    // POST: CHANNELS/Create
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ChannelFormViewModel model)
    {
        var userId = GetUserId();

        var existingChannel = await _context.Channels
            .FirstOrDefaultAsync(c => c.OwnerId == userId);

        if (existingChannel != null)
        {
            return RedirectToAction(nameof(Details), new { id = existingChannel.Id });
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var channel = new Channel {
            OwnerId = userId,
            Name = model.Name,
            Description = model.Description,
            CurrentTitle = model.CurrentTitle,
            Category = model.Category,
            StreamKey = Guid.NewGuid().ToString("N"),
            IsLive = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Channels.Add(channel);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = channel.Id });
    }

    // GET: CHANNELS/Edit/5
    [Authorize]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var channel = await _context.Channels.FindAsync(id);
        
        if (channel == null)
        {
            return NotFound();
        }

        if (!CanManage(channel))
        {
            return Forbid();
        }

        var model = new ChannelFormViewModel
        {
            Name = channel.Name,
            Description = channel.Description,
            CurrentTitle = channel.CurrentTitle,
            Category = channel.Category
        };

        ViewBag.ChannelId = channel.Id;

        return View(model);
    }

    // POST: CHANNELS/Edit/5
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, ChannelFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ChannelId = id;
            return View(model);
        }

        var channel = await _context.Channels.FindAsync(id);

        if (channel == null)
            return NotFound();

        if (!CanManage(channel))
            return Forbid();

        channel.Name = model.Name;
        channel.Description = model.Description;
        channel.CurrentTitle = model.CurrentTitle;
        channel.Category = model.Category;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = channel.Id });
    }

    // GET: CHANNELS/Delete/5
    [Authorize]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var channel = await _context.Channels
            .FirstOrDefaultAsync(m => m.Id == id);

        if (channel == null)
        {
            return NotFound();
        }

        if (!CanManage(channel))
        {
            return Forbid();
        }

        return View(channel);
    }

    // POST: CHANNELS/Delete/5
    [Authorize]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var channel = await _context.Channels.FindAsync(id);

        if (channel == null)
        {
            return NotFound();
        }

        if (!CanManage(channel))
        {
            return Forbid();
        }

        _context.Channels.Remove(channel);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    public async Task<IActionResult> Dashboard(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var channel = await _context.Channels.FindAsync(id);

        if (channel == null)
        {
            return NotFound();
        }

        if (!CanManage(channel))
        {
            return Forbid();
        }

        return View(channel);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GoLive(int id)
    {
        var channel = await _context.Channels.FindAsync(id);

        if (channel == null)
        {
            return NotFound();
        }

        if (!CanManage(channel))
        {
            return Forbid();
        }

        channel.IsLive = true;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Dashboard), new { id = channel.Id });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EndLive(int id)
    {
        var channel = await _context.Channels.FindAsync(id);

        if (channel == null)
        {
            return NotFound();
        }

        if (!CanManage(channel))
        {
            return Forbid();
        }

        channel.IsLive = false;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Dashboard), new { id = channel.Id });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegenerateStreamKey(int id)
    {
        var channel = await _context.Channels.FindAsync(id);

        if (channel == null)
            return NotFound();

        if (!CanManage(channel))
            return Forbid();

        channel.StreamKey = Guid.NewGuid().ToString("N");
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Dashboard), new { id = channel.Id });
    }

    private bool ChannelExists(int? id)
    {
        return _context.Channels.Any(e => e.Id == id);
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }

    private bool CanManage(Channel channel)
    {
        return channel.OwnerId == GetUserId() || User.IsInRole("Admin");
    }
}
