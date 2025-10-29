using System;

namespace IvanovItog.Shared.Dtos
{
    public class RequestsByStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class RequestsTimelinePointDto
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }

    public class TechnicianLoadDto
    {
        public string TechnicianName { get; set; } = string.Empty;
        public int ActiveRequests { get; set; }
        public int ClosedRequests { get; set; }
    }
}
