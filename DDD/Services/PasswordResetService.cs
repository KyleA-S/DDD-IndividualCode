using System;
using DDD.Data;

namespace DDD.Services
{
    public class PasswordResetService
    {
        private readonly IDataRepository _repository;

        public PasswordResetService(IDataRepository repository)
        {
            _repository = repository;
        }

        public bool SetSecurityQuestion(string username, string question, string answer)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(question) || string.IsNullOrEmpty(answer))
                return false;

            return _repository.SetSecurityQuestion(username, question, answer);
        }

        public bool ResetPassword(string username, string securityAnswer, string newPassword)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(securityAnswer) || string.IsNullOrEmpty(newPassword))
                return false;

            // Verify security answer
            if (!_repository.VerifySecurityAnswer(username, securityAnswer))
                return false;

            // Update password
            return _repository.UpdatePassword(username, newPassword);
        }

        public string GetSecurityQuestion(string username)
        {
            return _repository.GetSecurityQuestion(username);
        }

        public bool ChangePassword(string username, string currentPassword, string newPassword)
        {
            // Get user by username and verify current password
            var student = _repository.GetStudentByUsername(username);
            if (student != null && student.Password == currentPassword)
            {
                return _repository.UpdatePassword(username, newPassword);
            }

            var supervisor = _repository.GetSupervisorByUsername(username);
            if (supervisor != null && supervisor.Password == currentPassword)
            {
                return _repository.UpdatePassword(username, newPassword);
            }

            var seniorTutor = _repository.GetSeniorTutorByUsername(username);
            if (seniorTutor != null && seniorTutor.Password == currentPassword)
            {
                return _repository.UpdatePassword(username, newPassword);
            }

            return false;
        }
    }
}