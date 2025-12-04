using System;
using System.Collections.Generic;
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
                Date = DateTime.UtcNow
            };

            if (student.Reports == null) student.Reports = new List<WellbeingReport>();
            student.Reports.Add(report);
            _repository.SaveStudent(student);
        }

        public List<WellbeingReport> GetReports(Student student)
        {
            return student.Reports ?? new List<WellbeingReport>();
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
