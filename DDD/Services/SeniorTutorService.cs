using System;
using System.Collections.Generic;
using DDD.Data;
using DDD.Models;
using System.Linq;

namespace DDD.Services
{
    public class SeniorTutorService
    {
        private readonly IDataRepository _repo;
        private readonly Random _rng = new Random();

        public SeniorTutorService(IDataRepository repo)
        {
            _repo = repo;
        }

        // Create new student with yearGroup (1..4)
        public Student CreateStudent(string username, string name, string password, int yearGroup)
        {
            if (yearGroup < 1 || yearGroup > 4) throw new ArgumentException("Year group must be 1..4");

            var s = new Student
            {
                Username = username,
                Name = name,
                Password = password,
                YearGroup = yearGroup
            };

            // generate unique StudentCode
            s.StudentCode = GenerateUniqueStudentCode(yearGroup);

            _repo.SaveStudent(s);
            return s;
        }

        // Create personal supervisor with unique code
        public PersonalSupervisor CreatePersonalSupervisor(string username, string name, string password)
        {
            var ps = new PersonalSupervisor
            {
                Username = username,
                Name = name,
                Password = password
            };

            ps.SupervisorCode = GenerateUniqueSupervisorCode();
            _repo.SavePersonalSupervisor(ps);
            return ps;
        }

        // Create senior tutor with unique code
        public SeniorTutor CreateSeniorTutor(string username, string name, string password)
        {
            var st = new SeniorTutor
            {
                Username = username,
                Name = name,
                Password = password
            };

            st.SeniorTutorCode = GenerateUniqueSeniorTutorCode();
            _repo.SaveSeniorTutor(st);
            return st;
        }

        // Assign student -> personal supervisor
        public void AssignStudentToSupervisor(int studentId, int supervisorId)
        {
            var s = _repo.GetStudentById(studentId);
            var sup = _repo.GetSupervisorById(supervisorId);
            if (s == null || sup == null) throw new System.Exception("Student or Supervisor not found.");
            s.PersonalSupervisorId = sup.Id;
            _repo.SaveStudent(s);
        }

        public List<Student> GetAllStudents() => _repo.GetAllStudents();
        public List<PersonalSupervisor> GetAllPersonalSupervisors() => _repo.GetAllPersonalSupervisors();
        public List<SeniorTutor> GetAllSeniorTutors() => _repo.GetAllSeniorTutors();

        // ----- helper code generation -----
        private string GenerateUniqueStudentCode(int yearGroup)
        {
            var currentYear = DateTime.UtcNow.Year;
            int startYear = currentYear - (yearGroup - 1); // year the student started
            // ensure 4 digits
            string prefix = startYear.ToString("0000");

            for (int attempts = 0; attempts < 1000; attempts++)
            {
                int rand5 = _rng.Next(0, 100000); // 0..99999
                string suffix = rand5.ToString("D5");
                string candidate = prefix + suffix;
                var existing = _repo.GetStudentByCode(candidate);
                if (existing == null) return candidate;
            }
            throw new Exception("Unable to generate unique student code after many attempts.");
        }

        private string GenerateUniqueSupervisorCode()
        {
            for (int attempts = 0; attempts < 1000; attempts++)
            {
                int rand5 = _rng.Next(0, 100000);
                string candidate = "PS" + rand5.ToString("D5");
                var existing = _repo.GetSupervisorByCode(candidate);
                if (existing == null) return candidate;
            }
            throw new Exception("Unable to generate unique supervisor code after many attempts.");
        }

        private string GenerateUniqueSeniorTutorCode()
        {
            for (int attempts = 0; attempts < 1000; attempts++)
            {
                int rand5 = _rng.Next(0, 100000);
                string candidate = "ST" + rand5.ToString("D5");
                var existing = _repo.GetSeniorTutorByCode(candidate);
                if (existing == null) return candidate;
            }
            throw new Exception("Unable to generate unique senior tutor code after many attempts.");
        }
    }
}
