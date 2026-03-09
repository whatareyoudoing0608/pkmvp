using System;
using System.Collections.Generic;

namespace Pkmvp.Api.Repositories
{
    public interface IDailyWorklogRepository
    {
        public sealed class DailyWorklogHeaderRow
        {
            public long WorklogId { get; set; }
            public long ReporterId { get; set; }
            public string ReporterTeamId { get; set; }
            public string Status { get; set; }
        }


        long CreateHeader(DateTime workDate, long reporterId, string reporterTeamId, long authorId, string summary);
        IEnumerable<dynamic> List(DateTime fromDate, DateTime toDate, string scope, long meUserId, string meTeamId);
        dynamic GetHeader(long worklogId);

        void Submit(long worklogId, long actorId);
        void Approve(long worklogId, long evaluatorId, int? score, string commentTxt);
        void Reject(long worklogId, long evaluatorId, int? score, string commentTxt);

        long AddItem(long worklogId, int seq, string title, string description, int? spentMinutes, int? progressPct, long actorId);

        DailyWorklogHeaderRow GetHeaderAuth(long worklogId);

    }
}