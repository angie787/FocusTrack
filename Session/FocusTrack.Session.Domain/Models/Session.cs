using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusTrack.Session.Domain.Models
{
    public class Session
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset? EndTime { get; set; }
        public SessionMode Mode { get; set; }

        //set when EndTime is set
        public decimal DurationMin { get; set; }

        //true when this session triggered the daily 120-min goal
        public bool IsDailyGoalAchieved { get; set; }

        //computed duration when EndTime is set
        public static decimal ComputeDurationMin(DateTimeOffset start, DateTimeOffset? end) =>
            end.HasValue ? (decimal)(end.Value - start).TotalMinutes : 0;
    }
}
