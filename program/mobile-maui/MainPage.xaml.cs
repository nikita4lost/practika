using System.Collections.ObjectModel;
using WarrantyReturns.Mobile.Models;
using WarrantyReturns.Mobile.Services;

namespace WarrantyReturns.Mobile;

public partial class MainPage : ContentPage
{
    private const string SessionKey = "engineer_session_started";
    private readonly CaseStore _store = new();
    private readonly ObservableCollection<WarrantyCase> _cases = new();
    private bool _notificationShown;

    public MainPage()
    {
        InitializeComponent();
        StatusPicker.Items.Add("Все статусы");
        foreach (var status in WarrantyCase.Statuses)
        {
            StatusPicker.Items.Add(status);
        }
        ReasonPicker.Items.Add("Все причины");
        foreach (var reason in WarrantyCase.Reasons)
        {
            ReasonPicker.Items.Add(reason);
        }
        StatusPicker.SelectedIndex = 0;
        ReasonPicker.SelectedIndex = 0;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!await HasActiveSessionAsync())
        {
            Application.Current!.MainPage = new LoginPage();
            return;
        }

        await LoadCasesAsync();
    }

    private async Task LoadCasesAsync()
    {
        _cases.Clear();
        foreach (var item in await _store.LoadAsync())
        {
            _cases.Add(item);
        }
        RefreshList();
        await ShowNewCaseNotificationAsync();
    }

    private void RefreshList()
    {
        var status = StatusPicker.SelectedItem?.ToString();
        var reason = ReasonPicker.SelectedItem?.ToString();
        var search = SearchBar.Text?.Trim().ToLowerInvariant() ?? string.Empty;
        var items = _cases.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(status) && status != "Все статусы")
        {
            items = items.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(reason) && reason != "Все причины")
        {
            items = items.Where(x => x.Reason == reason);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            items = items.Where(x =>
                x.ProductName.ToLowerInvariant().Contains(search) ||
                x.SerialNumber.ToLowerInvariant().Contains(search));
        }

        CasesView.ItemsSource = items.OrderByDescending(x => x.ReceivedAt).ToList();
    }

    private async void CasesView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not WarrantyCase item)
        {
            return;
        }

        CasesView.SelectedItem = null;
        await OpenCaseAsync(item);
    }

    private async Task OpenCaseAsync(WarrantyCase item)
    {
        var status = await DisplayActionSheet(
            $"{item.Id} - {item.ProductName}",
            "Отмена",
            null,
            WarrantyCase.Statuses);

        if (string.IsNullOrWhiteSpace(status) || status == "Отмена")
        {
            return;
        }

        var conclusion = await DisplayPromptAsync(
            "Техническое заключение",
            "Введите результат диагностики или ремонта",
            initialValue: item.TechnicalConclusion,
            maxLength: 300,
            keyboard: Keyboard.Text);

        if (conclusion is null)
        {
            return;
        }

        item.Status = status;
        item.TechnicalConclusion = conclusion.Trim();
        item.UpdatedAt = DateTime.Now;
        await _store.SaveAsync(_cases);
        RefreshList();
        await DisplayAlert("Сохранено", "Статус и техническое заключение обновлены.", "OK");
    }

    private void StatusPicker_OnSelectedIndexChanged(object sender, EventArgs e)
    {
        RefreshList();
    }

    private void ReasonPicker_OnSelectedIndexChanged(object sender, EventArgs e)
    {
        RefreshList();
    }

    private void SearchBar_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshList();
    }

    private async void StatsButton_OnClicked(object sender, EventArgs e)
    {
        var today = DateTime.Today;
        var weekStart = today.AddDays(-7);
        var todayItems = _cases.Where(x => x.UpdatedAt.Date == today).ToList();
        var weekItems = _cases.Where(x => x.UpdatedAt.Date >= weekStart).ToList();

        static string BuildStats(IEnumerable<WarrantyCase> items)
        {
            var list = items.ToList();
            var diagnosed = list.Count(x => x.Status == "На диагностике");
            var repaired = list.Count(x => x.Status == "Ремонт" || x.Status == "Возврат средств/Обмен");
            var rejected = list.Count(x => x.Status == "Отказ");
            return $"диагностировано: {diagnosed}, отремонтировано/закрыто: {repaired}, отказов: {rejected}";
        }

        await DisplayAlert(
            "Статистика инженера",
            $"За день: {BuildStats(todayItems)}{Environment.NewLine}За неделю: {BuildStats(weekItems)}",
            "OK");
    }

    private async void ReloadButton_OnClicked(object sender, EventArgs e)
    {
        await LoadCasesAsync();
    }

    private async Task ShowNewCaseNotificationAsync()
    {
        if (_notificationShown)
        {
            return;
        }

        var urgentCount = _cases.Count(x => x.Status == "Принято");
        if (urgentCount == 0)
        {
            return;
        }

        _notificationShown = true;
        await DisplayAlert("Новые заявки", $"Есть новые заявки со статусом «Принято»: {urgentCount}.", "OK");
    }

    private static async Task<bool> HasActiveSessionAsync()
    {
        var value = await SecureStorage.GetAsync(SessionKey);
        if (!DateTime.TryParse(value, out var startedAt))
        {
            return false;
        }

        if (DateTime.UtcNow - startedAt.ToUniversalTime() < TimeSpan.FromHours(24))
        {
            return true;
        }

        SecureStorage.Remove(SessionKey);
        return false;
    }
}
