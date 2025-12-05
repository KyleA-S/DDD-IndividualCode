using System.Collections.Generic;
using DDD.Models;

namespace DDD.Data
{
    public interface IDataRepository
    {
        // Students
        Student GetStudentById(int id);
        Student GetStudentByUsername(string username);
        Student GetStudentByCode(string studentCode);
        void SaveStudent(Student student);
        List<Student> GetAllStudents();

        // Personal Supervisors
        PersonalSupervisor GetSupervisorById(int id);
        PersonalSupervisor GetSupervisorByUsername(string username);
        PersonalSupervisor GetSupervisorByCode(string supervisorCode);
        void SavePersonalSupervisor(PersonalSupervisor supervisor);
        List<PersonalSupervisor> GetAllPersonalSupervisors();

        // Senior tutors
        SeniorTutor GetSeniorTutorById(int id);
        SeniorTutor GetSeniorTutorByUsername(string username);
        SeniorTutor GetSeniorTutorByCode(string seniorTutorCode);
        void SaveSeniorTutor(SeniorTutor seniorTutor);
        List<SeniorTutor> GetAllSeniorTutors();

        // Messages
        void SendMessage(Message msg);
        List<Message> GetMessages(int studentId, int supervisorId);
        Message GetMessageById(int messageId);
        void UpdateMessageContent(int messageId, string newContent);
        void DeleteMessage(int messageId);
        void MarkMessagesAsRead(int studentId, int supervisorId, string readerRole);
        int GetUnreadCount(int studentId, int supervisorId, string readerRole);
        List<Message> GetMessagesByStudent(int studentId);
        List<Message> GetMessagesBySupervisor(int supervisorId);
    }
}
