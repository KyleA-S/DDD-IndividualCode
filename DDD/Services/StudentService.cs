using System;
using System.Collections.Generic;
using System.Linq;
using DDD.Data;
using DDD.Models;

namespace DDD.Services
{
    public class StudentService
    {
        private readonly IDataRepository _repo;

        public StudentService(IDataRepository repo)
        {
            _repo = repo;
        }

        public void SubmitReport(Student student, int score, string notes)
        {
            var r = new WellbeingReport
            {
                Id = GenerateReportId(),
                StudentId = student.Id,
                Score = score,
                Notes = notes ?? string.Empty,
                Date = DateTime.UtcNow
            };

            if (student.Reports == null) student.Reports = new List<WellbeingReport>();
            student.Reports.Add(r);
            _repo.SaveStudent(student);
        }

        public List<WellbeingReport> GetReports(Student s)
        {
            return s.Reports ?? new List<WellbeingReport>();
        }

        public void BookMeeting(Student s, DateTime time)
        {
            if (s.PersonalSupervisorId == 0) throw new Exception("No personal supervisor assigned.");
            var meeting = new Meeting
            {
                Id = GenerateMeetingId(),
                StudentId = s.Id,
                PersonalSupervisorId = s.PersonalSupervisorId,
                ScheduledTime = time
            };

            if (s.Meetings == null) s.Meetings = new List<Meeting>();
            s.Meetings.Add(meeting);

            // Save and keep supervisor in sync
            _repo.SaveStudent(s);
            var sup = _repo.GetSupervisorById(s.PersonalSupervisorId);
            if (sup != null)
            {
                if (sup.Meetings == null) sup.Meetings = new List<Meeting>();
                sup.Meetings.Add(meeting);
                _repo.SavePersonalSupervisor(sup);
            }
        }

        public List<Meeting> GetMeetings(Student s)
        {
            return s.Meetings ?? new List<Meeting>();
        }

        private int GenerateReportId()
        {
            var allReports = _repo.GetAllStudents().SelectMany(st => st.Reports ?? new List<WellbeingReport>()).ToList();
            return allReports.Any() ? allReports.Max(r => r.Id) + 1 : 1;
        }

        private int GenerateMeetingId()
        {
            var allMeetings = _repo.GetAllStudents()
                                  .SelectMany(st => st.Meetings ?? new List<Meeting>())
                                  .Concat(_repo.GetAllPersonalSupervisors().SelectMany(ps => ps.Meetings ?? new List<Meeting>()))
                                  .ToList();

            return allMeetings.Any() ? allMeetings.Max(m => m.Id) + 1 : 1;
        }
    }
}
