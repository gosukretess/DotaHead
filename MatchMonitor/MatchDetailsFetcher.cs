using DotaHead.Infrastructure;
using Microsoft.Extensions.Logging;
using OpenDotaApi;
using OpenDotaApi.Api.Matches.Model;

namespace DotaHead.MatchMonitor
{
    public class MatchDetailsFetcher
    {
        private static ILogger Logger => StaticLoggerFactory.GetStaticLogger<MatchDetailsFetcher>();

        public async Task<Match?> GetMatchDetails(long matchId)
        {
            try
            {
                var openDotaClient = new OpenDota();
                var lastMatch = await openDotaClient.Matches.GetMatchAsync(matchId);

                var isParsed = lastMatch.Version != null;

                if (!isParsed)
                {
                    Logger.LogInformation($"Match not parsed, requested parse - matchId: {matchId}.");
                    await WaitForParseCompletion(openDotaClient, matchId);
                }

                Logger.LogInformation($"Getting match details - matchId: {matchId}");
                var matchDetails = await openDotaClient.Matches.GetMatchAsync(matchId);
                return matchDetails;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "There was exception when getting match details.");
            }

            return null;
        }

        private async Task WaitForParseCompletion(OpenDota openDotaClient, long matchId)
        {
            var waitTime = 8;
            var parseResponse = await openDotaClient.Request.SubmitNewParseRequestAsync(matchId);
            var jobId = parseResponse.Job.JobId;
            while (waitTime <= 16)
            {
                var response = await openDotaClient.Request.GetParseRequestStateAsync(jobId);

                if (response == null)
                {
                    Logger.LogInformation("Parse successful.");
                    return;
                }

                Logger.LogInformation($"Parse not finished. Waiting for {waitTime} seconds.");
                await Task.Delay(waitTime * 1000);
                waitTime += 2;
            }

            Logger.LogInformation("Parse failed.");
        }
    }
}
