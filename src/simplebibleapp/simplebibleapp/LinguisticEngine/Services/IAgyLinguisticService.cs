using System.Threading;
using System.Threading.Tasks;
using simplebibleapp.LinguisticEngine.Models;

namespace simplebibleapp.LinguisticEngine.Services
{
    public interface IAgyLinguisticService
    {
        /// <summary>
        /// Analyze an original-language token and return a synonym/semantic network payload.
        /// </summary>
        Task<AgyLinguisticPayloadDto?> AnalyzeTokenAsync(
            string anchorStrongs,
            string anchorLemma,
            string language,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Check the cache for an existing synonym/semantic network payload without invoking the AI.
        /// </summary>
        Task<AgyLinguisticPayloadDto?> GetCachedTokenAsync(
            string anchorStrongs,
            CancellationToken cancellationToken = default);
    }
}
