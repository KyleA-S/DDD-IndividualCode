using System;
using System.Collections.Generic;

namespace DDD.Models
{
    // base user
    public abstract class User
    {
        public int Id { get; set; } = 0;
        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class Student : User
    {
        public int PersonalSupervisorId { get; set; } = 0; // 0 => none assigned
        public List<Meeting> Meetings { get; set; } = new List<Meeting>();
        public List<WellbeingReport> Reports { get; set; } = new List<WellbeingReport>();
    }

    public class PersonalSupervisor : User
    {
        public List<Meeting> Meetings { get; set; } = new List<Meeting>();
    }

    public class SeniorTutor : User
    {
        // placeholder for future senior-specific fields
    }

    public class Meeting
    {
        public int Id { get; set; } = 0;
        public int StudentId { get; set; }
        public int PersonalSupervisorId { get; set; }
        public DateTime ScheduledTime { get; set; }
    }

    public class WellbeingReport
    {
        public int Id { get; set; } = 0;
        public int StudentId { get; set; }
        public int Score { get; set; }
        public string Notes { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }

    public class Message
    {
        public int Id { get; set; } = 0;
        public int StudentId { get; set; }
        public int PersonalSupervisorId { get; set; }
        public string SenderRole { get; set; } = string.Empty; // "student" or "supervisor"
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; } = false;
    }
}
