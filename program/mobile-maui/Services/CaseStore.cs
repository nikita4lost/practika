using System.Text.Json;
using WarrantyReturns.Mobile.Models;

namespace WarrantyReturns.Mobile.Services;

public sealed class CaseStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _localFile = Path.Combine(FileSystem.AppDataDirectory, "warranty_cases.json");

    public async Task<List<WarrantyCase>> LoadAsync()
    {
        if (!File.Exists(_localFile))
        {
            var demo = CreateDemoCases();
            await SaveAsync(demo);
            return demo;
        }

        var json = await File.ReadAllTextAsync(_localFile);
        return JsonSerializer.Deserialize<List<WarrantyCase>>(json, JsonOptions) ?? [];
    }

    public async Task SaveAsync(IEnumerable<WarrantyCase> cases)
    {
        var json = JsonSerializer.Serialize(cases.OrderByDescending(x => x.UpdatedAt), JsonOptions);
        await File.WriteAllTextAsync(_localFile, json);
    }

    public async Task<string> ExportSyncCopyAsync(IEnumerable<WarrantyCase> cases)
    {
        var exportPath = Path.Combine(FileSystem.CacheDirectory, "warranty_cases_sync.json");
        var json = JsonSerializer.Serialize(cases.OrderByDescending(x => x.UpdatedAt), JsonOptions);
        await File.WriteAllTextAsync(exportPath, json);
        return exportPath;
    }

    private static List<WarrantyCase> CreateDemoCases()
    {
        return
        [
            new WarrantyCase
            {
                Id = "ВР-260619-001",
                ClientName = "Иванов И.И.",
                Phone = "+79000000001",
                ProductName = "Паяльник ZD-99",
                SerialNumber = "ZD9917-26",
                Reason = "Неисправность при эксплуатации",
                Status = "Ремонт",
                CheckSum = 2400,
                ManagerComment = "Комплект полный, есть чек",
                TechnicalConclusion = "Требуется замена нагревательного элемента"
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
                ManagerComment = "Клиент просит ускорить диагностику"
            }
        ];
    }
}
