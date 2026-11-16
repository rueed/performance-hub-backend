namespace PerformanceApi.Dtos;

public class CreateEvaluationDto
{
    public int UserId { get; set; }            // the employee being evaluated
    public string Period { get; set; } = string.Empty;   // e.g. "2026 Q4"
    public decimal Score { get; set; }         // 0 to 100
    public string Comment { get; set; } = string.Empty;
}