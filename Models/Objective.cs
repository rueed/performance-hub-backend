namespace PerformanceApi.Models;

public class Objective //every class in EF is a table in the database
{
    public int Id { get; set; } //every property in the class is a column in the table and the name of the property should be capitalized
    public int UserId { get; set; }
    public string Title { get; set; }=string.Empty;
    public string Description { get; set; }= string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Progress { get; set; }
    public ObjectiveStatus Status { get; set; } = ObjectiveStatus.Draft; //default value for the status property
    public string? RejectionReason { get; set; } //? means that the property can be null
    public int? ReviewedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

}
//EF is the translation layer between the database and the application.

