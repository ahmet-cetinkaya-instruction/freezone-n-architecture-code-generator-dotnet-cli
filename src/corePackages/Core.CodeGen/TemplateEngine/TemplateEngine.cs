using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Core.CodeGen.File;

namespace Core.CodeGen.TemplateEngine
{
    public class TemplateEngine : ITemplateEngine
    {
        private readonly ITemplateRenderer _templateRenderer;

        public TemplateEngine(ITemplateRenderer templateRenderer)
        {
            _templateRenderer = templateRenderer;
        }

        public async Task<string> RenderAsync(string template, ITemplateData templateData) =>
            await _templateRenderer.RenderAsync(template, templateData);

        public async Task RenderFileAsync(string templateFilePath, string templateDir, ITemplateData templateData)
        {
            string templateFileText = await System.IO.File.ReadAllTextAsync(templateFilePath);

            string newRenderedFileText = await _templateRenderer.RenderAsync(templateFileText, templateData);
            string newRenderedFilePath = await _templateRenderer.RenderAsync(
                                             template: getOutputFilePath(templateFilePath, templateDir),
                                             templateData);

            await FileHelper.CreateFileAsync(newRenderedFilePath, newRenderedFileText);
        }

        public async Task RenderFileAsync(IList<string> templateFilePaths, string templateDir,
                                          ITemplateData templateData)

        {
            foreach (string templateFilePath in templateFilePaths)
                await RenderFileAsync(templateFilePath, templateDir, templateData);
        }

        private string getOutputFilePath(string templateFilePath, string templateDir) =>
            templateFilePath
                .Replace(
                    oldValue: @$"{DirectoryHelper.AssemblyDirectory}\{templateDir}",
                    Environment.CurrentDirectory)
                .Replace(_templateRenderer.TemplateExtension, string.Empty);
    }
}