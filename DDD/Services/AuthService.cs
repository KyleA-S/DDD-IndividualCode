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

        public Student LoginStudent(string usernameOrCode, string password)
        {
            Student student = _repository.GetStudentByUsername(usernameOrCode) ?? _repository.GetStudentByCode(usernameOrCode);
            if (student == null || student.Password != password)
                throw new Exception("Invalid student login.");
            return student;
        }

        public PersonalSupervisor LoginSupervisor(string usernameOrCode, string password)
        {
            PersonalSupervisor supervisor = _repository.GetSupervisorByUsername(usernameOrCode) ?? _repository.GetSupervisorByCode(usernameOrCode);
            if (supervisor == null || supervisor.Password != password)
                throw new Exception("Invalid supervisor login.");
            return supervisor;
        }

        public SeniorTutor LoginSeniorTutor(string usernameOrCode, string password)
        {
            SeniorTutor seniorTutor = _repository.GetSeniorTutorByUsername(usernameOrCode) ?? _repository.GetSeniorTutorByCode(usernameOrCode);
            if (seniorTutor == null || seniorTutor.Password != password)
                throw new Exception("Invalid senior tutor login.");
            return seniorTutor;
        }
    }
}