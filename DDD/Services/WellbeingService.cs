using System;
using System.Collections.Generic;
using System.Linq;
using DDD.Data;
using DDD.Models;

namespace DDD.Services
{
    public class WellbeingService
    {
        private readonly IDataRepository _repository;

        public WellbeingService(IDataRepository repository)
        {
            _repository = repository;
        }

        public void SubmitReport(Student student, int score, string notes)
        {
            var report = new WellbeingReport
            {
                Id = GenerateReportId(),
                StudentId = student.Id,
                Score = score,
                Notes = notes ?? string.Empty,
                Date = DateTime.UtcNow,
                IsHighPriority = score < 5
            };

            if (student.Reports == null) student.Reports = new List<WellbeingReport>();
            student.Reports.Add(report);

            // Update student's last report date
            student.LastWellbeingReportDate = DateTime.UtcNow;
            student.HasMissedWellbeingReport = false;
            student.MissedReportCount = 0;

            _repository.SaveStudent(student);

            // Create alert for low score
            if (score < 5)
            {
                var alert = new WellbeingAlert
                {
                    StudentId = student.Id,
                    StudentName = student.Name,
                    AlertDate = DateTime.UtcNow,
                    Reason = "low_score",
                    IsResolved = false
                };
                _repository.AddWellbeingAlert(alert);
            }
        }

        public List<WellbeingReport> GetReports(Student student)
        {
            return student.Reports ?? new List<WellbeingReport>();
        }

        public void CheckAndUpdateMissedReports()
        {
            var allStudents = _repository.GetAllStudents();
            var now = DateTime.UtcNow;

            foreach (var student in allStudents)
            {
                // Calculate next Monday at 12:00 PM
                var nextMonday = GetNextMondayNoon(DateTime.UtcNow);

                // Check if student has submitted this week
                var lastReportDate = student.LastWellbeingReportDate;
                var daysSinceLastReport = (now - lastReportDate).TotalDays;

                // If it's past Monday noon and no report submitted this week
                if (now > nextMonday && (now - lastReportDate).TotalDays >= 7)
                {
                    if (!student.HasMissedWellbeingReport)
                    {
                        student.HasMissedWellbeingReport = true;
                        student.MissedReportCount++;

                        // Create alert for missed report
                        var alert = new WellbeingAlert
                        {
                            StudentId = student.Id,
                            StudentName = student.Name,
                            AlertDate = now,
                            Reason = "missed_report",
                            IsResolved = false
                        };
                        _repository.AddWellbeingAlert(alert);
                    }
                }
                else
                {
                    student.HasMissedWellbeingReport = false;
                }

                _repository.SaveStudent(student);
            }
        }

        public (TimeSpan timeUntil, bool isOverdue, int daysOverdue) GetWellbeingReportStatus(Student student)
        {
            var now = DateTime.UtcNow;
            var nextMondayNoon = GetNextMondayNoon(now);
            var timeUntil = nextMondayNoon - now;

            if (timeUntil.TotalSeconds < 0)
            {
                // Overdue
                var daysOverdue = (int)Math.Ceiling(Math.Abs(timeUntil.TotalDays));
                return (TimeSpan.Zero, true, daysOverdue);
            }

            return (timeUntil, false, 0);
        }

        public DateTime GetNextMondayNoon(DateTime fromDate)
        {
            // Get the next Monday
            var nextMonday = fromDate;
            while (nextMonday.DayOfWeek != DayOfWeek.Monday)
            {
                nextMonday = nextMonday.AddDays(1);
            }

            // Set to 12:00 PM
            return new DateTime(nextMonday.Year, nextMonday.Month, nextMonday.Day, 12, 0, 0);
        }

        public List<WellbeingAlert> GetActiveAlerts()
        {
            return _repository.GetActiveWellbeingAlerts();
        }

        public void ResolveAlert(int alertId)
        {
            _repository.ResolveAlert(alertId);
        }

        public List<Student> GetHighPriorityStudents()
        {
            return _repository.GetStudentsWithLowWellbeing();
        }

        public List<Student> GetStudentsWithMissedReports()
        {
            return _repository.GetStudentsWithMissedReports();
        }

        private int GenerateReportId()
        {
            var allReports = new List<WellbeingReport>();
            foreach (var s in _repository.GetAllStudents())
            {
                if (s.Reports != null) allReports.AddRange(s.Reports);
            }
            return allReports.Count > 0 ? allReports.Max(r => r.Id) + 1 : 1;
        }
    }
}