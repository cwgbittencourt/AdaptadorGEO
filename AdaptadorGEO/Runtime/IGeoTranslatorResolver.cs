using AdaptadorGEO.Spatial;

namespace AdaptadorGEO.Runtime;

public interface IGeoTranslatorResolver
{
    IGeoTranslator Resolve(string providerName);
}
