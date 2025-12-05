// DDD.Models (models file)
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
        // internal FK remains
        public int PersonalSupervisorId { get; set; } = 0; // 0 => none assigned

        // NEW external code
        public string StudentCode { get; set; } = string.Empty; // 9-digit code like 202512345

        // Year group (1..4)
        public int YearGroup { get; set; } = 1;

        public List<Meeting> Meetings { get; set; } = new List<Meeting>();
        public List<WellbeingReport> Reports { get; set; } = new List<WellbeingReport>();
    }

    public class PersonalSupervisor : User
    {
        // NEW external code
        public string SupervisorCode { get; set; } = string.Empty; // PSxxxxx

        public List<Meeting> Meetings { get; set; } = new List<Meeting>();
    }

    public class SeniorTutor : User
    {
        // NEW external code
        public string SeniorTutorCode { get; set; } = string.Empty; // STxxxxx
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
