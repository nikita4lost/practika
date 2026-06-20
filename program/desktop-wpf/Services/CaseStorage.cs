using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using WarrantyReturns.Desktop.Models;

namespace WarrantyReturns.Desktop.Services;

public sealed class CaseStorage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _dataFile;
    private readonly string _cloudFile;
    private readonly string _logFile;

    public CaseStorage()
    {
        var baseDir = AppContext.BaseDirectory;
        var appData = Path.Combine(baseDir, "data");
        Directory.CreateDirectory(appData);

        var cloudDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "cloud"));
        Directory.CreateDirectory(cloudDir);

        _dataFile = Path.Combine(appData, "local_cases.json");
        _cloudFile = Path.Combine(cloudDir, "warranty_cases.json");
        _logFile = Path.Combine(appData, "app.log");
    }

    public ObservableCollection<WarrantyCase> Load()
    {
        var source = File.Exists(_dataFile) ? _dataFile : _cloudFile;
        if (!File.Exists(source))
        {
            return new ObservableCollection<WarrantyCase>(CreateDemoCases());
        }

        var json = File.ReadAllText(source);
        var items = JsonSerializer.Deserialize<List<WarrantyCase>>(json, JsonOptions) ?? new List<WarrantyCase>();
        return new ObservableCollection<WarrantyCase>(items);
    }

    public void Save(IEnumerable<WarrantyCase> cases)
    {
        var list = cases.OrderByDescending(x => x.ReceivedAt).ToList();
        File.WriteAllText(_dataFile, JsonSerializer.Serialize(list, JsonOptions));
        WriteLog("INFO", $"Локально сохранено заявок: {list.Count}");
    }

    public void SyncToCloud(IEnumerable<WarrantyCase> cases)
    {
        var list = cases.OrderByDescending(x => x.UpdatedAt).ToList();
        File.WriteAllText(_cloudFile, JsonSerializer.Serialize(list, JsonOptions));
        WriteLog("INFO", $"Синхронизация выполнена. Передано заявок: {list.Count}");
    }

    public void WriteLog(string level, string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
        File.AppendAllLines(_logFile, [line]);
    }

    public static string GenerateId()
    {
        return $"ВР-{DateTime.Now:yyMMdd}-{Random.Shared.Next(1, 999):000}";
    }

    private static List<WarrantyCase> CreateDemoCases()
    {
        return new List<WarrantyCase>
        {
            new WarrantyCase
            {
                Id = "ВР-260619-001",
                ClientName = "Иванов Иван",
                Phone = "+79000000001",
                ProductName = "Паяльник ZD-99",
                SerialNumber = "ZD9917-26",
                Reason = "Неисправность при эксплуатации",
                Status = "Ремонт",
                CheckSum = 2400,
                InternalComment = "Проверить историю покупок клиента",
                ExternalComment = "Комплект полный, есть чек"
            },
            new WarrantyCase
            {
                Id = "ВР-260619-002",
                ClientName = "ООО Радио",
                Phone = "+79000000002",
                ProductName = "STM32F103",
                SerialNumber = "STM-103-26",
                Reason = "Брак",
                Status = "На диагностике",
                CheckSum = 1350,
                RequiresSupplierApproval = true,
                InternalComment = "Возможна партия с браком",
                ExternalComment = "Клиент просит ускорить диагностику"
            }
        };
    }
}
