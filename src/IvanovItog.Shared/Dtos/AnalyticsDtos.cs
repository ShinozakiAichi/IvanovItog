using System;

namespace IvanovItog.Shared.Dtos
{
    public class RequestsByStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; } = 0;
    }

    public class RequestsTimelinePointDto
    {
        public DateTime Date { get; set; } = DateTime.MinValue;
        public int Count { get; set; } = 0;
    }

    public class TechnicianLoadDto
    {
        public string TechnicianName { get; set; } = string.Empty;
        public int ActiveRequests { get; set; } = 0;
        public int ClosedRequests { get; set; } = 0;
    }
}
