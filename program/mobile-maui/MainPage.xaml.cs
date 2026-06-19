using System.Collections.ObjectModel;
using WarrantyReturns.Mobile.Models;
using WarrantyReturns.Mobile.Services;

namespace WarrantyReturns.Mobile;

public partial class MainPage : ContentPage
{
    private readonly CaseStore _store = new();
    private readonly ObservableCollection<WarrantyCase> _cases = new();

    public MainPage()
    {
        InitializeComponent();
        StatusPicker.Items.Add("Все статусы");
        foreach (var status in WarrantyCase.Statuses)
        {
            StatusPicker.Items.Add(status);
        }
        StatusPicker.SelectedIndex = 0;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
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
    }

    private void RefreshList()
    {
        var status = StatusPicker.SelectedItem?.ToString();
        var items = _cases.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(status) && status != "Все статусы")
        {
            items = items.Where(x => x.Status == status);
        }

        CasesView.ItemsSource = items.OrderByDescending(x => x.UpdatedAt).ToList();
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

    private async void ReloadButton_OnClicked(object sender, EventArgs e)
    {
        await LoadCasesAsync();
    }
}
