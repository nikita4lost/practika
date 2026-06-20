using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using Microsoft.Win32;
using WarrantyReturns.Desktop.Models;
using WarrantyReturns.Desktop.Services;

namespace WarrantyReturns.Desktop;

public partial class MainWindow : Window
{
    private readonly CaseStorage _storage = new();
    private readonly ExportService _exportService = new();
    private readonly ObservableCollection<WarrantyCase> _cases;
    private WarrantyCase? _selectedCase;

    public MainWindow()
    {
        InitializeComponent();

        _cases = _storage.Load();
        ReasonBox.ItemsSource = WarrantyCase.Reasons;
        StatusBox.ItemsSource = WarrantyCase.Statuses;
        StatusFilterBox.ItemsSource = new[] { "Все статусы" }.Concat(WarrantyCase.Statuses);
        ReasonFilterBox.ItemsSource = new[] { "Все причины" }.Concat(WarrantyCase.Reasons);
        StatusFilterBox.SelectedIndex = 0;
        ReasonFilterBox.SelectedIndex = 0;

        RefreshGrid();
        ClearForm();
    }

    private void NewCase_OnClick(object sender, RoutedEventArgs e)
    {
        CasesGrid.SelectedItem = null;
        ClearForm();
    }

    private void SaveCase_OnClick(object sender, RoutedEventArgs e)
    {
        if (!ValidateForm(out var checkSum))
        {
            return;
        }

        var item = _selectedCase ?? new WarrantyCase
        {
            Id = CaseStorage.GenerateId(),
            ReceivedAt = DateTime.Now
        };

        item.ClientName = ClientBox.Text.Trim();
        item.Phone = PhoneBox.Text.Trim();
        item.ProductName = ProductBox.Text.Trim();
        item.SerialNumber = SerialBox.Text.Trim();
        item.Reason = ReasonBox.SelectedItem?.ToString() ?? WarrantyCase.Reasons[0];
        item.Status = StatusBox.SelectedItem?.ToString() ?? WarrantyCase.Statuses[0];
        item.ReceivedAt = ReceivedDatePicker.SelectedDate ?? DateTime.Now;
        item.CheckSum = checkSum;
        item.RequiresSupplierApproval = SupplierApprovalBox.IsChecked == true;
        item.InternalComment = InternalCommentBox.Text.Trim();
        item.ExternalComment = ExternalCommentBox.Text.Trim();
        item.TechnicalConclusion = ConclusionBox.Text.Trim();
        item.UpdatedAt = DateTime.Now;

        if (_selectedCase is null)
        {
            _cases.Add(item);
            _storage.WriteLog("INFO", $"Создана заявка {item.Id}");
        }
        else
        {
            _storage.WriteLog("INFO", $"Обновлена заявка {item.Id}");
        }

        _storage.Save(_cases);
        RefreshGrid();
        CasesGrid.SelectedItem = item;
        StatusText.Text = $"Сохранено: {DateTime.Now:HH:mm:ss}";
    }

    private void DeleteCase_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedCase is null)
        {
            MessageBox.Show("Выберите заявку для удаления.", "Удаление", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show($"Удалить заявку {_selectedCase.Id}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        _storage.WriteLog("WARNING", $"Удалена заявка {_selectedCase.Id}");
        _cases.Remove(_selectedCase);
        _selectedCase = null;
        _storage.Save(_cases);
        ClearForm();
        RefreshGrid();
    }

    private void Sync_OnClick(object sender, RoutedEventArgs e)
    {
        _storage.Save(_cases);
        _storage.SyncToCloud(_cases);
        StatusText.Text = $"Синхронизация выполнена: {DateTime.Now:HH:mm:ss}";
        MessageBox.Show("Данные выгружены в общий файл синхронизации.", "Синхронизация", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ExportDocx_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Word document (*.docx)|*.docx",
            FileName = "warranty_cases.docx"
        };
        if (dialog.ShowDialog() == true)
        {
            _exportService.ExportDocx(GetFilteredCases(), dialog.FileName);
            StatusText.Text = "DOCX экспортирован";
        }
    }

    private void ExportXlsx_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Excel workbook (*.xlsx)|*.xlsx",
            FileName = "warranty_cases.xlsx"
        };
        if (dialog.ShowDialog() == true)
        {
            _exportService.ExportXlsx(GetFilteredCases(), dialog.FileName);
            StatusText.Text = "XLSX экспортирован";
        }
    }

    private void Analytics_OnClick(object sender, RoutedEventArgs e)
    {
        var items = GetFilteredCases().ToList();
        if (items.Count == 0)
        {
            MessageBox.Show("Нет данных для аналитики.", "Аналитика", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var byReason = string.Join(Environment.NewLine, items
            .GroupBy(x => x.Reason)
            .OrderByDescending(x => x.Count())
            .Select(x => $"{x.Key}: {x.Count()} шт., сумма {x.Sum(y => y.CheckSum):0.00} руб."));
        var byStatus = string.Join(Environment.NewLine, items
            .GroupBy(x => x.Status)
            .OrderByDescending(x => x.Count())
            .Select(x => $"{x.Key}: {x.Count()} шт."));
        var fromText = DateFromPicker.SelectedDate?.ToString("dd.MM.yyyy") ?? "не задано";
        var toText = DateToPicker.SelectedDate?.ToString("dd.MM.yyyy") ?? "не задано";

        MessageBox.Show(
            $"Период: {fromText} - {toText}{Environment.NewLine}{Environment.NewLine}" +
            $"По причинам:{Environment.NewLine}{byReason}{Environment.NewLine}{Environment.NewLine}" +
            $"По статусам:{Environment.NewLine}{byStatus}{Environment.NewLine}{Environment.NewLine}" +
            $"Индикатор облака: файл синхронизации доступен",
            "Аналитика возвратов",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void CasesGrid_OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        _selectedCase = CasesGrid.SelectedItem as WarrantyCase;
        if (_selectedCase is null)
        {
            return;
        }

        ClientBox.Text = _selectedCase.ClientName;
        PhoneBox.Text = _selectedCase.Phone;
        ProductBox.Text = _selectedCase.ProductName;
        SerialBox.Text = _selectedCase.SerialNumber;
        ReasonBox.SelectedItem = _selectedCase.Reason;
        StatusBox.SelectedItem = _selectedCase.Status;
        SumBox.Text = _selectedCase.CheckSum.ToString("0.00");
        ReceivedDatePicker.SelectedDate = _selectedCase.ReceivedAt;
        SupplierApprovalBox.IsChecked = _selectedCase.RequiresSupplierApproval;
        InternalCommentBox.Text = _selectedCase.InternalComment;
        ExternalCommentBox.Text = _selectedCase.ExternalComment;
        ConclusionBox.Text = _selectedCase.TechnicalConclusion;
    }

    private void SearchBox_OnTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        RefreshGrid();
    }

    private void StatusFilterBox_OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        RefreshGrid();
    }

    private void ReasonFilterBox_OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        RefreshGrid();
    }

    private void DateFilter_OnSelectedDateChanged(object? sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        RefreshGrid();
    }

    private void ResetFilter_OnClick(object sender, RoutedEventArgs e)
    {
        SearchBox.Text = string.Empty;
        StatusFilterBox.SelectedIndex = 0;
        ReasonFilterBox.SelectedIndex = 0;
        DateFromPicker.SelectedDate = null;
        DateToPicker.SelectedDate = null;
        RefreshGrid();
    }

    private void RefreshGrid()
    {
        CasesGrid.ItemsSource = GetFilteredCases().ToList();
    }

    private IEnumerable<WarrantyCase> GetFilteredCases()
    {
        var query = _cases.AsEnumerable();
        var search = SearchBox?.Text.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                x.ClientName.ToLowerInvariant().Contains(search) ||
                x.ProductName.ToLowerInvariant().Contains(search) ||
                x.SerialNumber.ToLowerInvariant().Contains(search));
        }

        var status = StatusFilterBox?.SelectedItem?.ToString();
        if (!string.IsNullOrWhiteSpace(status) && status != "Все статусы")
        {
            query = query.Where(x => x.Status == status);
        }

        var reason = ReasonFilterBox?.SelectedItem?.ToString();
        if (!string.IsNullOrWhiteSpace(reason) && reason != "Все причины")
        {
            query = query.Where(x => x.Reason == reason);
        }

        if (DateFromPicker?.SelectedDate is DateTime from)
        {
            query = query.Where(x => x.ReceivedAt.Date >= from.Date);
        }

        if (DateToPicker?.SelectedDate is DateTime to)
        {
            query = query.Where(x => x.ReceivedAt.Date <= to.Date);
        }

        return query.OrderByDescending(x => x.ReceivedAt);
    }

    private void ClearForm()
    {
        _selectedCase = null;
        ClientBox.Text = string.Empty;
        PhoneBox.Text = "+7";
        ProductBox.Text = string.Empty;
        SerialBox.Text = string.Empty;
        ReasonBox.SelectedIndex = 0;
        StatusBox.SelectedIndex = 0;
        SumBox.Text = "0";
        ReceivedDatePicker.SelectedDate = DateTime.Now;
        SupplierApprovalBox.IsChecked = false;
        InternalCommentBox.Text = string.Empty;
        ExternalCommentBox.Text = string.Empty;
        ConclusionBox.Text = string.Empty;
    }

    private bool ValidateForm(out decimal checkSum)
    {
        checkSum = 0;
        if (string.IsNullOrWhiteSpace(ClientBox.Text))
        {
            MessageBox.Show("Укажите ФИО клиента или название организации.", "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (!Regex.IsMatch(ClientBox.Text.Trim(), @"^[\p{L}\d\s]+$"))
        {
            MessageBox.Show("ФИО/название должны содержать только буквы, цифры и пробелы.", "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (ClientBox.Text.Trim().Length > 100)
        {
            MessageBox.Show("ФИО/название не должны превышать 100 символов.", "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (!Regex.IsMatch(PhoneBox.Text.Trim(), @"^\+7\d{10}$"))
        {
            MessageBox.Show("Телефон должен иметь формат +7XXXXXXXXXX.", "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (string.IsNullOrWhiteSpace(ProductBox.Text))
        {
            MessageBox.Show("Укажите наименование товара.", "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if ($"{ProductBox.Text.Trim()} {SerialBox.Text.Trim()}".Length > 150)
        {
            MessageBox.Show("Наименование товара и серийный номер вместе не должны превышать 150 символов.", "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (InternalCommentBox.Text.Length > 400)
        {
            MessageBox.Show("Внутренний комментарий не должен превышать 400 символов.", "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (ExternalCommentBox.Text.Length > 300)
        {
            MessageBox.Show("Внешний комментарий не должен превышать 300 символов.", "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (ConclusionBox.Text.Length > 300)
        {
            MessageBox.Show("Техническое заключение не должно превышать 300 символов.", "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (!decimal.TryParse(SumBox.Text.Replace(',', '.'), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out checkSum) || checkSum < 0)
        {
            MessageBox.Show("Сумма чека должна быть числом больше или равным нулю.", "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        return true;
    }
}
