using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M3U8_Downloader
{
    public class RaceInfo
    {
        public string MeetingId { get; set; }
        public string MeetingCourse { get; set; }
        public long MeetingCourseID { get; set; }
        public string MeetingCourseCode { get; set; }
        public string MeetingDiscipline { get; set; }
        public DateTime MeetingDate { get; set; }
        public string RaceId { get; set; }
        public string RaceName { get; set; }
        public string RaceNumber { get; set; }
        public string PlayerUrl { get; set; }
    }
}
