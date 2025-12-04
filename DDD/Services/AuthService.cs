using System;
using DDD.Data;
using DDD.Models;

namespace DDD.Services
{
    public class AuthService
    {
        private readonly IDataRepository _repository;

        public AuthService(IDataRepository repository)
        {
            _repository = repository;
        }

        public Student LoginStudent(string username, string password)
        {
            var student = _repository.GetStudentByUsername(username);
            if (student == null || student.Password != password)
                throw new Exception("Invalid student login.");
            return student;
        }

        public PersonalSupervisor LoginSupervisor(string username, string password)
        {
            var supervisor = _repository.GetSupervisorByUsername(username);
            if (supervisor == null || supervisor.Password != password)
                throw new Exception("Invalid supervisor login.");
            return supervisor;
        }

        public SeniorTutor LoginSeniorTutor(string username, string password)
        {
            var seniorTutor = _repository.GetSeniorTutorByUsername(username);
            if (seniorTutor == null || seniorTutor.Password != password)
                throw new Exception("Invalid senior tutor login.");
            return seniorTutor;
        }
    }
}
