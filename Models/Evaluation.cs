namespace PerformanceApi.Models;

public class Evaluation
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ManagerId { get; set; }
    public string Period { get; set; }=string.Empty;
    public decimal Score { get; set; }
    public string Comment { get; set; }= string.Empty;
    public DateTime CreatedAt { get; set; }

}
