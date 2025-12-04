using System;
using System.Collections.Generic;
using System.Linq;
using DDD.Data;
using DDD.Models;

namespace DDD.Services
{
    public class PersonalSupervisorService
    {
        private readonly IDataRepository _repo;

        public PersonalSupervisorService(IDataRepository repo)
        {
            _repo = repo;
        }

        public List<Student> GetSupervisees(PersonalSupervisor ps)
        {
            return _repo.GetAllStudents().Where(s => s.PersonalSupervisorId == ps.Id).ToList();
        }

        public void BookMeeting(PersonalSupervisor ps, int studentId, DateTime date)
        {
            var s = _repo.GetStudentById(studentId);
            if (s == null) throw new Exception("Student not found.");

            var meeting = new Meeting
            {
                Id = GenerateMeetingId(),
                StudentId = s.Id,
                PersonalSupervisorId = ps.Id,
                ScheduledTime = date
            };

            if (s.Meetings == null) s.Meetings = new List<Meeting>();
            s.Meetings.Add(meeting);

            if (ps.Meetings == null) ps.Meetings = new List<Meeting>();
            ps.Meetings.Add(meeting);

            _repo.SaveStudent(s);
            _repo.SavePersonalSupervisor(ps);
        }

        private int GenerateMeetingId()
        {
            var allMeetings = _repo.GetAllStudents().SelectMany(st => st.Meetings ?? new List<Meeting>())
                                  .Concat(_repo.GetAllPersonalSupervisors().SelectMany(ps => ps.Meetings ?? new List<Meeting>()))
                                  .ToList();
            return allMeetings.Any() ? allMeetings.Max(m => m.Id) + 1 : 1;
        }
    }
}
