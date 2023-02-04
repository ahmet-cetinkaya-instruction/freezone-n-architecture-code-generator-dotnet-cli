using Core.CodeGen.TemplateEngine;

namespace Domain.ValueObjects;

public class CrudTemplateData : ITemplateData
{
    public Entity Entity { get; set; }
    public bool IsCachingUsed { get; set; }
    public bool IsLoggingUsed { get; set; }
    public bool IsTransactionUsed { get; set; }
    public string DbContextName { get; set; }
}