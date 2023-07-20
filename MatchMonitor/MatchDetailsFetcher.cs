using DotaHead.Infrastructure;
using Microsoft.Extensions.Logging;
using OpenDotaApi;
using OpenDotaApi.Api.Matches.Model;

namespace DotaHead.MatchMonitor
{
    public class MatchDetailsFetcher
    {
        private ILogger Logger => StaticLoggerFactory.GetStaticLogger<MatchDetailsBuilder>();

        public async Task<Match> GetMatchDetails(long matchId)
        {
            var openDotaClient = new OpenDota();
            var lastMatch = await openDotaClient.Matches.GetMatchAsync(matchId);
            var isParsed = lastMatch.Version != null;

            if (!isParsed)
            {
                Logger.LogInformation("Match not parsed, requested parse.");
                isParsed = await WaitForParseCompletion(openDotaClient, matchId);
            }

            Logger.LogInformation($"Getting match details - matchId: {matchId}");
            var matchDetails = await openDotaClient.Matches.GetMatchAsync(matchId);
            return matchDetails;
        }

        private async Task<bool> WaitForParseCompletion(OpenDota openDotaClient, long matchId)
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
                    return true;
                }

                Logger.LogInformation($"Parse not finished. Waiting for {waitTime} seconds.");
                await Task.Delay(waitTime * 1000);
                waitTime += 2;
            }

            Logger.LogInformation("Parse failed.");
            return false;
        }
    }
}
