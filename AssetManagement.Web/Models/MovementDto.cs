namespace AssetManagement.Web.Models;

public class MovementDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = ""; // "purchase-in", "allocate", "return", "send-repair", "repair-return", "obsolete"
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public string ItemId { get; set; } = "";
    public string Note { get; set; } = "";
    public DateTime Date { get; set; }
    public string? InvoiceFilePath { get; set; }
    public string TypeLabel => Type switch
    {
        "purchase-in" => "Received in inventory",
        "allocate" => "Allocated to employee",
        "return" => "Returned to inventory",
        "send-repair" => "Sent to repair shop",
        "repair-return" => "Returned from repair",
        "obsolete" => "Declared obsolete",
        _ => Type
    };
}
