using System.Collections.Generic;
using DDD.Data;
using DDD.Models;

namespace DDD.Services
{
    public class SeniorTutorService
    {
        private readonly IDataRepository _repo;

        public SeniorTutorService(IDataRepository repo)
        {
            _repo = repo;
        }

        // Create new student
        public Student CreateStudent(string username, string name, string password)
        {
            var s = new Student { Username = username, Name = name, Password = password };
            _repo.SaveStudent(s);
            return s;
        }

        // Create personal supervisor
        public PersonalSupervisor CreatePersonalSupervisor(string username, string name, string password)
        {
            var ps = new PersonalSupervisor { Username = username, Name = name, Password = password };
            _repo.SavePersonalSupervisor(ps);
            return ps;
        }

        // Create senior tutor
        public SeniorTutor CreateSeniorTutor(string username, string name, string password)
        {
            var st = new SeniorTutor { Username = username, Name = name, Password = password };
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
    }
}
