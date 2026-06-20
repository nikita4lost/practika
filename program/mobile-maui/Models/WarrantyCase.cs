namespace WarrantyReturns.Mobile.Models;

public sealed class WarrantyCase
{
    public string Id { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Reason { get; set; } = "Брак";
    public string Status { get; set; } = "Принято";
    public decimal CheckSum { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.Now;
    public bool RequiresSupplierApproval { get; set; }
    public string InternalComment { get; set; } = string.Empty;
    public string ExternalComment { get; set; } = string.Empty;
    public string ManagerComment
    {
        get => ExternalComment;
        set => ExternalComment = value;
    }
    public string TechnicalConclusion { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public static readonly string[] Statuses =
    {
        "Принято",
        "На диагностике",
        "Ремонт",
        "Возврат средств/Обмен",
        "Отказ"
    };

    public static readonly string[] Reasons =
    {
        "Брак",
        "Не подошла модель",
        "Ошибка в описании",
        "Неисправность при эксплуатации"
    };
}
