using System;
using System.Collections.Generic;
using System.Linq;
using DDD.Data;
using DDD.Models;

namespace DDD.Services
{
    public class MeetingService
    {
        private readonly IDataRepository _repository;

        public MeetingService(IDataRepository repository)
        {
            _repository = repository;
        }

        // Student requests a meeting (adds to student's meetings)
        public void BookMeeting(Student student, DateTime scheduledTime)
        {
            if (student.PersonalSupervisorId == 0)
                throw new Exception("Student has no assigned personal supervisor.");

            var meeting = new Meeting
            {
                Id = GenerateMeetingId(),
                StudentId = student.Id,
                PersonalSupervisorId = student.PersonalSupervisorId,
                ScheduledTime = scheduledTime
            };

            student.Meetings.Add(meeting);
            _repository.SaveStudent(student);

            // Also update supervisor's meetings list so their view stays consistent
            var sup = _repository.GetSupervisorById(student.PersonalSupervisorId);
            if (sup != null)
            {
                sup.Meetings.Add(meeting);
                _repository.SavePersonalSupervisor(sup);
            }
        }

        // Supervisor books meeting with a student
        public void BookMeeting(PersonalSupervisor supervisor, int studentId, DateTime scheduledTime)
        {
            var student = _repository.GetStudentById(studentId);
            if (student == null) throw new Exception("Student not found.");

            var meeting = new Meeting
            {
                Id = GenerateMeetingId(),
                StudentId = student.Id,
                PersonalSupervisorId = supervisor.Id,
                ScheduledTime = scheduledTime
            };

            student.Meetings.Add(meeting);
            supervisor.Meetings.Add(meeting);

            _repository.SaveStudent(student);
            _repository.SavePersonalSupervisor(supervisor);
        }

        public List<Meeting> GetMeetings(Student student)
        {
            return student.Meetings.OrderBy(m => m.ScheduledTime).ToList();
        }

        public List<Meeting> GetMeetings(PersonalSupervisor supervisor)
        {
            return supervisor.Meetings.OrderBy(m => m.ScheduledTime).ToList();
        }

        private int GenerateMeetingId()
        {
            var allStudents = _repository.GetAllStudents();
            var allSupervisors = _repository.GetAllPersonalSupervisors();

            var allMeetings = allStudents.SelectMany(s => s.Meetings ?? new List<Meeting>())
                                         .Concat(allSupervisors.SelectMany(ps => ps.Meetings ?? new List<Meeting>()))
                                         .ToList();

            return allMeetings.Any() ? allMeetings.Max(m => m.Id) + 1 : 1;
        }
    }
}
