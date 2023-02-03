namespace Core.CodeGen.TemplateEngine;

public interface ITemplateEngine
{
    public Task<string> RenderAsync(string template, ITemplateData templateData);
    public Task RenderFileAsync(string templateFilePath, string templateDir, ITemplateData templateData);
    public Task RenderFileAsync(IList<string> templateFilePaths, string templateDir, ITemplateData templateData);
}