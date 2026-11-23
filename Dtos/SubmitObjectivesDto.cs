namespace PerformanceApi.Dtos;

// The ids of the objectives the employee wants to submit together
public class SubmitObjectivesDto
{
    public List<int> Ids { get; set; } = new();
}