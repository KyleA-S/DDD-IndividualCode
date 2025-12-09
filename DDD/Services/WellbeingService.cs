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

        // ------------------------------
        // MAIN SUBMIT REPORT (MERGED)
        // ------------------------------
        public void SubmitReport(Student student, int score, string notes)
        {
            // Move the existing current report into history
            if (student.CurrentWellbeing != null)
            {
                student.CurrentWellbeing.IsCurrent = false;

                if (student.WellbeingHistory == null)
                    student.WellbeingHistory = new List<WellbeingReport>();

                student.WellbeingHistory.Add(student.CurrentWellbeing);
            }

            // Create new current report
            var report = new WellbeingReport
            {
                Id = GenerateReportId(),
                StudentId = student.Id,
                Score = score,
                Notes = notes ?? string.Empty,
                Date = DateTime.UtcNow,
                IsHighPriority = score < 5,
                IsCurrent = true
            };

            // Set current report reference
            student.CurrentWellbeing = report;

            // Add to Reports list (backwards compatibility)
            if (student.Reports == null)
                student.Reports = new List<WellbeingReport>();

            student.Reports.Add(report);

            // Reset missed-report status
            student.LastWellbeingReportDate = DateTime.UtcNow;
            student.HasMissedWellbeingReport = false;
            student.MissedReportCount = 0;

            // Save changes
            _repository.SaveStudent(student);

            // Generate wellbeing alert
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

        // ------------------------------
        //   CURRENT REPORT
        // ------------------------------
        public WellbeingReport GetCurrentWellbeing(Student student)
        {
            return student.CurrentWellbeing;
        }

        // ------------------------------
        //   HISTORY REPORTS (OLD ONES)
        // ------------------------------
        public List<WellbeingReport> GetWellbeingHistory(Student student)
        {
            if (student.WellbeingHistory == null)
                return new List<WellbeingReport>();

            return student.WellbeingHistory
                .OrderByDescending(r => r.Date)
                .ToList();
        }

        // ------------------------------
        //   ALL REPORTS (CURRENT + HISTORY)
        // ------------------------------
        public List<WellbeingReport> GetAllWellbeingReports(Student student)
        {
            var all = new List<WellbeingReport>();

            if (student.CurrentWellbeing != null)
                all.Add(student.CurrentWellbeing);

            if (student.WellbeingHistory != null)
                all.AddRange(student.WellbeingHistory);

            return all.OrderByDescending(r => r.Date).ToList();
        }

        // ------------------------------
        // EXISTING "GetReports" (BACKWARD COMPATIBILITY)
        // ------------------------------
        public List<WellbeingReport> GetReports(Student student)
        {
            return student.Reports ?? new List<WellbeingReport>();
        }

        // ------------------------------
        // MISSED REPORT CHECKING
        // ------------------------------
        public void CheckAndUpdateMissedReports()
        {
            var allStudents = _repository.GetAllStudents();
            var now = DateTime.UtcNow;

            foreach (var student in allStudents)
            {
                var nextMonday = GetNextMondayNoon(DateTime.UtcNow);
                var lastReport = student.LastWellbeingReportDate;

                if (now > nextMonday && (now - lastReport).TotalDays >= 7)
                {
                    if (!student.HasMissedWellbeingReport)
                    {
                        student.HasMissedWellbeingReport = true;
                        student.MissedReportCount++;

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

        // ------------------------------
        // REPORT DEADLINE STATUS
        // ------------------------------
        public (TimeSpan timeUntil, bool isOverdue, int daysOverdue) GetWellbeingReportStatus(Student student)
        {
            var now = DateTime.UtcNow;
            var nextMonday = GetNextMondayNoon(now);
            var diff = nextMonday - now;

            if (diff.TotalSeconds < 0)
            {
                var overdue = (int)Math.Ceiling(Math.Abs(diff.TotalDays));
                return (TimeSpan.Zero, true, overdue);
            }

            return (diff, false, 0);
        }

        // Monday 12 PM deadline
        public DateTime GetNextMondayNoon(DateTime fromDate)
        {
            var next = fromDate;

            while (next.DayOfWeek != DayOfWeek.Monday)
                next = next.AddDays(1);

            return new DateTime(next.Year, next.Month, next.Day, 12, 0, 0);
        }

        // ------------------------------
        // ALERTS
        // ------------------------------
        public List<WellbeingAlert> GetActiveAlerts()
        {
            return _repository.GetActiveWellbeingAlerts();
        }

        public void ResolveAlert(int alertId)
        {
            _repository.ResolveAlert(alertId);
        }

        // ------------------------------
        // HIGH PRIORITY STUDENTS
        // ------------------------------
        public List<Student> GetHighPriorityStudents()
        {
            return _repository.GetStudentsWithLowWellbeing();
        }

        // Supervisor-specific high priority logic (from NEW VERSION)
        public List<Student> GetHighPrioritySupervisees(PersonalSupervisor ps, PersonalSupervisorService psService)
        {
            var supervisees = psService.GetSupervisees(ps);
            var highPriority = new List<Student>();

            foreach (var student in supervisees)
            {
                if (student.CurrentWellbeing != null && student.CurrentWellbeing.Score < 5)
                {
                    highPriority.Add(student);
                }
                else if (student.HasMissedWellbeingReport)
                {
                    highPriority.Add(student);
                }
                else if (student.WellbeingHistory != null &&
                        student.WellbeingHistory.Any(r => r.Score < 5 && (DateTime.UtcNow - r.Date).TotalDays <= 30))
                {
                    highPriority.Add(student);
                }
            }

            return highPriority.OrderBy(s => s.Name).ToList();
        }

        public List<Student> GetStudentsWithMissedReports()
        {
            return _repository.GetStudentsWithMissedReports();
        }

        // ------------------------------
        // ID GENERATION
        // ------------------------------
        private int GenerateReportId()
        {
            var allReports = new List<WellbeingReport>();

            foreach (var s in _repository.GetAllStudents())
            {
                if (s.Reports != null)
                    allReports.AddRange(s.Reports);
            }

            return allReports.Count > 0
                ? allReports.Max(r => r.Id) + 1
                : 1;
        }
    }
}
