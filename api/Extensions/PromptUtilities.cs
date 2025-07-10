using Microsoft.SemanticKernel;
using System.Reflection;

namespace AgentHub.Api.Extensions
{
    public static class PromptUtilities
    {
        public static string GetPromptByName(string prompt)
        {
            var resourceName = $"AgentHub.Api.Agents.{prompt}.txt";
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new ArgumentException($"The resource {resourceName} was not found.");

                using (StreamReader reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
        }

        public static async Task<string> RenderPromptAsync(Kernel kernel, string prompt, KernelArguments arguments)
        {
            var ptf = new KernelPromptTemplateFactory();
            var pt = ptf.Create(new PromptTemplateConfig(prompt));
            string intentUserMessage = await pt.RenderAsync(kernel, arguments);
            return intentUserMessage;
        }
    }
}
