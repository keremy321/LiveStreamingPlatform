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
        var channels = await _context.Channels
            .OrderByDescending(c => c.IsLive)
            .ThenByDescending(c => c.CreatedAt)
            .ToListAsync();

        await SetFollowerCounts(channels);

        ViewData["BrowseEyebrow"] = "Browse";
        ViewData["BrowseTitle"] = "Discover live channels";
        ViewData["BrowseDescription"] = "Find creators, communities, and streams across the platform.";

        return View(channels);
    }

    [AllowAnonymous]
    public async Task<IActionResult> LiveChannels()
    {
        var channels = await _context.Channels
            .Where(c => c.IsLive)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        await SetFollowerCounts(channels);

        ViewData["Title"] = "Live Channels";
        ViewData["BrowseEyebrow"] = "Live now";
        ViewData["BrowseTitle"] = "Live channels";
        ViewData["BrowseDescription"] = "Streams that are currently marked live.";

        return View("Index", channels);
    }

    [Authorize]
    public async Task<IActionResult> Following()
    {
        var userId = GetUserId();

        var channels = await _context.Follows
            .Where(f => f.FollowerId == userId)
            .Join(
                _context.Channels,
                follow => follow.ChannelId,
                channel => channel.Id,
                (follow, channel) => channel)
            .OrderByDescending(c => c.IsLive)
            .ThenBy(c => c.Name)
            .ToListAsync();

        await SetFollowerCounts(channels);

        ViewData["Title"] = "Following";
        ViewData["BrowseEyebrow"] = "Following";
        ViewData["BrowseTitle"] = "Channels you follow";
        ViewData["BrowseDescription"] = "Keep up with the creators you already follow.";

        return View("Index", channels);
    }

    [Authorize]
    public async Task<IActionResult> MyChannel()
    {
        var userId = GetUserId();

        var channel = await _context.Channels
            .FirstOrDefaultAsync(c => c.OwnerId == userId);

        if (channel == null)
        {
            return RedirectToAction(nameof(Create));
        }

        return RedirectToAction(nameof(Details), new { id = channel.Id });
    }

    [Authorize]
    public async Task<IActionResult> MyDashboard()
    {
        var userId = GetUserId();

        var channel = await _context.Channels
            .FirstOrDefaultAsync(c => c.OwnerId == userId);

        if (channel == null)
        {
            return RedirectToAction(nameof(Create));
        }

        return RedirectToAction(nameof(Dashboard), new { id = channel.Id });
    }

    // GET: CHANNELS/Details/5
    [AllowAnonymous]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var channel = await _context.Channels
            .FirstOrDefaultAsync(c => c.Id == id);

        if (channel == null)
            return NotFound();

        var followerCount = await _context.Follows
            .CountAsync(f => f.ChannelId == channel.Id);

        ViewBag.FollowerCount = followerCount;

        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var userId = GetUserId();

            var isFollowing = await _context.Follows
                .AnyAsync(f => f.ChannelId == channel.Id && f.FollowerId == userId);

            ViewBag.IsFollowing = isFollowing;
        }
        else
        {
            ViewBag.IsFollowing = false;
        }

        var hlsBaseUrl = HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()["MediaServer:HlsBaseUrl"];

        ViewBag.HlsUrl = $"{hlsBaseUrl}/live/channel-{channel.Id}/index.m3u8";

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

        var rtmpBaseUrl = HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()["MediaServer:RtmpBaseUrl"];

        ViewBag.RtmpBaseUrl = rtmpBaseUrl;

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

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Follow(int id)
    {
        var channel = await _context.Channels.FindAsync(id);

        if (channel == null)
            return NotFound();

        var userId = GetUserId();

        if (channel.OwnerId == userId)
        {
            return RedirectToAction(nameof(Details), new { id });
        }

        var alreadyFollowing = await _context.Follows
            .AnyAsync(f => f.ChannelId == id && f.FollowerId == userId);

        if (!alreadyFollowing)
        {
            var follow = new Follow 
            {
                ChannelId = id,
                FollowerId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Follows.Add(follow);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unfollow(int id)
    {
        var userId = GetUserId();

        var follow = await _context.Follows
            .FirstOrDefaultAsync(f => f.ChannelId == id && f.FollowerId == userId);

        if (follow != null)
        {
            _context.Follows.Remove(follow);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Details), new { id });
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

    private async Task SetFollowerCounts(IReadOnlyCollection<Channel> channels)
    {
        var channelIds = channels.Select(c => c.Id).ToList();

        ViewBag.FollowerCounts = await _context.Follows
            .Where(f => channelIds.Contains(f.ChannelId))
            .GroupBy(f => f.ChannelId)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
    }
}
