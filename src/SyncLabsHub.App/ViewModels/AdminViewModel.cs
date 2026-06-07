using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using SyncLabsHub.App.Mvvm;
using SyncLabsHub.Core;
using SyncLabsHub.Core.Models;
using SyncLabsHub.Core.Services;

namespace SyncLabsHub.App.ViewModels;

// ─────────────────────────────────────────────────────────────────────────────
//  Admin section — shown only to users with the 'admin' role
// ─────────────────────────────────────────────────────────────────────────────
public sealed class AdminViewModel : SectionViewModel
{
    private readonly LicenseService _license;
    private readonly SupabaseAdminClient _admin = new();

    private string _searchText = "";
    private bool _isLoading;
    private string _errorMessage = "";
    private List<AdminUserRowViewModel> _allUsers = new();

    public override string Name    => "Users";
    public override string NavGlyph => "People24";

    public AdminViewModel(LicenseService license)
    {
        _license = license;
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
        _ = LoadAsync();
    }

    // ── Data ──────────────────────────────────────────────────────────────────

    public ObservableCollection<AdminUserRowViewModel> Users { get; } = new();

    public string SearchText
    {
        get => _searchText;
        set { if (SetProperty(ref _searchText, value)) ApplyFilter(); }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(Subtitle));
            }
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    /// <summary>True when not loading and the filtered list is empty.</summary>
    public bool IsEmpty => !_isLoading && Users.Count == 0;

    public string Subtitle => _isLoading
        ? "Loading…"
        : string.Format("{0} user{1}", _allUsers.Count, _allUsers.Count == 1 ? "" : "s");

    // ── Commands ──────────────────────────────────────────────────────────────

    public ICommand RefreshCommand { get; }

    // ── Private ───────────────────────────────────────────────────────────────

    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var token = _license.Current?.Session.AccessToken;
            if (string.IsNullOrEmpty(token))
            {
                ErrorMessage = "Session token missing — please sign out and back in.";
                return;
            }

            var summaries = await _admin.GetAllUsersAsync(token, CancellationToken.None)
                                        .ConfigureAwait(false);

            _allUsers = summaries
                .Select(s => new AdminUserRowViewModel(s, _admin, _license))
                .ToList();

            ApplyFilter();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(Subtitle));
        }
    }

    private void ApplyFilter()
    {
        Users.Clear();
        var q = _searchText.Trim();
        foreach (var u in _allUsers)
        {
            if (string.IsNullOrEmpty(q)
                || u.Email.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                || u.DisplayName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Users.Add(u);
            }
        }
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(Subtitle));
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  One user row — header + collapsible tool list
// ─────────────────────────────────────────────────────────────────────────────
public sealed class AdminUserRowViewModel : ObservableObject
{
    private bool _isExpanded;

    public AdminUserRowViewModel(
        AdminUserSummary summary,
        SupabaseAdminClient admin,
        LicenseService license)
    {
        UserId      = summary.UserId;
        Email       = summary.Email;
        DisplayName = string.IsNullOrWhiteSpace(summary.FullName)
                          ? DeriveName(summary.Email)
                          : summary.FullName;
        IsAdmin = summary.IsAdmin;

        // Build a row for every catalog tool.
        // Tools the user already has an order for come first (catalog order preserved),
        // then tools with no order yet so the admin can grant them.
        var orderByToolId = new Dictionary<string, AdminOrderRow>();
        foreach (var o in summary.Orders)
            orderByToolId[o.ToolId] = o;   // last-write wins (keeps latest status)

        foreach (var cat in ToolCatalog.All)
        {
            orderByToolId.TryGetValue(cat.Id, out var order);
            Tools.Add(new AdminToolRowViewModel(cat, order, admin, license, this));
        }

        ToggleExpandedCommand = new RelayCommand(() => IsExpanded = !IsExpanded);
    }

    // ── Properties ────────────────────────────────────────────────────────────

    public string UserId      { get; }
    public string Email       { get; }
    public string DisplayName { get; }
    public bool   IsAdmin     { get; }

    public string RoleText => IsAdmin ? "Admin" : "User";

    public ObservableCollection<AdminToolRowViewModel> Tools { get; } = new();

    public int    ActiveToolCount => Tools.Count(t => t.IsActive);
    public string ToolSummary     => ActiveToolCount == 0
                                         ? "No licenses"
                                         : string.Format("{0} active", ActiveToolCount);

    /// <summary>Initials shown in the avatar circle (1 or 2 characters).</summary>
    public string UserInitials
    {
        get
        {
            if (DisplayName.Length == 0) return "?";
            var parts = DisplayName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && parts[0].Length > 0 && parts[1].Length > 0)
                return "" + char.ToUpper(parts[0][0]) + char.ToUpper(parts[1][0]);
            return char.ToUpper(DisplayName[0]).ToString();
        }
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public ICommand ToggleExpandedCommand { get; }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Called by child tool rows after a status change to refresh the count badge.</summary>
    public void RefreshCounts()
    {
        OnPropertyChanged(nameof(ActiveToolCount));
        OnPropertyChanged(nameof(ToolSummary));
    }

    private static string DeriveName(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return "User";
        var at    = email.IndexOf('@');
        var local = at > 0 ? email.Substring(0, at) : email;
        return local.Length == 0
            ? "User"
            : char.ToUpper(local[0]) + (local.Length > 1 ? local.Substring(1) : "");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  One tool row inside a user row (covers all 6 catalog tools)
// ─────────────────────────────────────────────────────────────────────────────
public sealed class AdminToolRowViewModel : ObservableObject
{
    private readonly SupabaseAdminClient    _admin;
    private readonly LicenseService         _license;
    private readonly AdminUserRowViewModel  _parent;

    private bool      _hasOrder;
    private string    _status;
    private bool      _isBusy;
    private DateTime? _expiresAtUtc;
    private DateTime? _selectedExpiry;

    public AdminToolRowViewModel(
        ToolCatalogEntry    catalog,
        AdminOrderRow?      order,
        SupabaseAdminClient admin,
        LicenseService      license,
        AdminUserRowViewModel parent)
    {
        ToolId   = catalog.Id;
        ToolName = catalog.Name;
        Glyph    = catalog.Glyph;

        _status       = order?.Status ?? "none";
        _hasOrder     = order != null;
        _expiresAtUtc = order?.ExpiresAtUtc;
        LicenseType   = order?.LicenseType ?? "";

        // Pre-populate expiry picker with the existing expiry (if any).
        // Admin can then change it before clicking Unlock/Grant.
        _selectedExpiry = _expiresAtUtc;

        _admin   = admin;
        _license = license;
        _parent  = parent;

        ToggleCommand = new AsyncRelayCommand(ToggleAsync);
    }

    // ── Properties ────────────────────────────────────────────────────────────

    public string ToolId      { get; }
    public string ToolName    { get; }
    public string Glyph       { get; }
    public string LicenseType { get; }

    /// <summary>Effective expiry stored in the DB (refreshed after successful operations).</summary>
    public DateTime? ExpiresAtUtc => _expiresAtUtc;

    /// <summary>
    /// The expiry the admin has chosen in the DatePicker.
    /// Passed to Unlock / Grant calls. Null = perpetual.
    /// </summary>
    public DateTime? SelectedExpiry
    {
        get => _selectedExpiry;
        set => SetProperty(ref _selectedExpiry, value);
    }

    /// <summary>Show the expiry picker only when the action is Unlock or Grant (not Lock).</summary>
    public bool ShowExpiryPicker => !(_status == "active" || _status == "confirmed");

    public bool IsActive =>
        (_status == "active" || _status == "confirmed") &&
        (_expiresAtUtc == null || _expiresAtUtc > DateTime.UtcNow);

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public string StatusText
    {
        get
        {
            switch (_status)
            {
                case "active":
                case "confirmed":
                    if (ExpiresAtUtc is DateTime exp)
                    {
                        var days = Math.Max(0, (int)Math.Ceiling((exp - DateTime.UtcNow).TotalDays));
                        return string.Format("Active · {0}d left", days);
                    }
                    return "Active";

                case "suspended":
                case "cancelled": return "Locked";
                case "none":      return "No license";
                default:
                    if (string.IsNullOrEmpty(_status)) return "—";
                    return char.ToUpper(_status[0]) + (_status.Length > 1 ? _status.Substring(1) : "");
            }
        }
    }

    public string ActionText => _status == "active" || _status == "confirmed"
        ? "Lock"
        : (_status == "cancelled" || _status == "suspended" ? "Unlock" : "Grant");

    /// <summary>WPF-UI ControlAppearance string — "Secondary" = outline, "Primary" = filled.</summary>
    public string ActionAppearance => _status == "active" || _status == "confirmed"
        ? "Secondary"
        : "Primary";

    public ICommand ToggleCommand { get; }

    // ── Private ───────────────────────────────────────────────────────────────

    private async Task ToggleAsync()
    {
        var token = _license.Current?.Session.AccessToken;
        if (string.IsNullOrEmpty(token)) return;

        IsBusy = true;
        try
        {
            bool ok;
            if (!_hasOrder)
            {
                // ── Grant: new order, optional expiry from DatePicker ─────────
                ok = await _admin.GrantToolAsync(
                        token, _parent.UserId, ToolId, ToolName,
                        _parent.DisplayName, _parent.Email,
                        _selectedExpiry,
                        CancellationToken.None)
                    .ConfigureAwait(false);
                if (ok) { _hasOrder = true; SetStatus("active", _selectedExpiry, updateExpiry: true); }
            }
            else if (_status == "active" || _status == "confirmed")
            {
                // ── Lock: set status to "cancelled", do NOT touch expires_at ──
                ok = await _admin.LockToolAsync(token, _parent.UserId, ToolId, CancellationToken.None)
                                 .ConfigureAwait(false);
                if (ok) SetStatus("cancelled");
            }
            else
            {
                // ── Unlock: restore to "active" + apply the chosen expiry ─────
                ok = await _admin.UnlockToolAsync(
                        token, _parent.UserId, ToolId,
                        _selectedExpiry,
                        CancellationToken.None)
                    .ConfigureAwait(false);
                if (ok) SetStatus("active", _selectedExpiry, updateExpiry: true);
            }
        }
        finally
        {
            IsBusy = false;
            _parent.RefreshCounts();
        }
    }

    private void SetStatus(string newStatus, DateTime? newExpiry = null, bool updateExpiry = false)
    {
        _status = newStatus;
        if (updateExpiry)
        {
            _expiresAtUtc   = newExpiry;
            _selectedExpiry = newExpiry;
            OnPropertyChanged(nameof(ExpiresAtUtc));
            OnPropertyChanged(nameof(SelectedExpiry));
        }
        OnPropertyChanged(nameof(IsActive));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(ActionText));
        OnPropertyChanged(nameof(ActionAppearance));
        OnPropertyChanged(nameof(ShowExpiryPicker));
    }
}
