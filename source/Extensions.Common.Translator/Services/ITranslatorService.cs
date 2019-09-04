using Extensions.Common.Translator.Domain;
using System.Threading.Tasks;

namespace Extensions.Common.Translator.Services
{
    public interface ITranslatorService
    {
        TranslatorConfig GetConfiguration();

        Task<string> GetDesiredLanguageAsync(string content);

        Task<string> TranslateSentenceAsync(string content, string originLanguage, string targetLanguage);
    }
}