using System;
using System.Collections.Generic;
using System.Linq;
using DDD.Data;
using DDD.Models;

namespace DDD.Services
{
    public class MessageService
    {
        private readonly IDataRepository _repo;

        public MessageService(IDataRepository repo)
        {
            _repo = repo;
        }

        public void SendMessageFromStudent(Student student, string content)
        {
            if (student.PersonalSupervisorId == 0)
                throw new Exception("You do not have a supervisor assigned.");

            var msg = new Message
            {
                StudentId = student.Id,
                PersonalSupervisorId = student.PersonalSupervisorId,
                SenderRole = "student",
                Content = content ?? string.Empty,
                Timestamp = DateTime.UtcNow,
                IsRead = false
            };

            _repo.SendMessage(msg);
        }

        public void SendMessageFromSupervisor(PersonalSupervisor ps, int studentId, string content)
        {
            var msg = new Message
            {
                StudentId = studentId,
                PersonalSupervisorId = ps.Id,
                SenderRole = "supervisor",
                Content = content ?? string.Empty,
                Timestamp = DateTime.UtcNow,
                IsRead = false
            };

            _repo.SendMessage(msg);
        }

        public List<Message> GetConversation(int studentId, int supervisorId)
        {
            return _repo.GetMessages(studentId, supervisorId) ?? new List<Message>();
        }

        public void MarkConversationAsRead(int studentId, int supervisorId, string readerRole)
        {
            _repo.MarkMessagesAsRead(studentId, supervisorId, readerRole);
        }

        public int GetUnreadCountForViewer(int studentId, int supervisorId, string readerRole)
        {
            return _repo.GetUnreadCount(studentId, supervisorId, readerRole);
        }

        public Message GetMessage(int messageId)
        {
            return _repo.GetMessageById(messageId);
        }

        public void EditMessageAsStudent(Student student, int messageId, string newContent)
        {
            var msg = _repo.GetMessageById(messageId);
            if (msg == null) throw new Exception("Message not found.");
            if (msg.SenderRole != "student" || msg.StudentId != student.Id)
                throw new Exception("You can only edit your own messages.");
            _repo.UpdateMessageContent(messageId, newContent);
        }

        public void EditMessageAsSupervisor(PersonalSupervisor ps, int messageId, string newContent)
        {
            var msg = _repo.GetMessageById(messageId);
            if (msg == null) throw new Exception("Message not found.");
            if (msg.SenderRole != "supervisor" || msg.PersonalSupervisorId != ps.Id)
                throw new Exception("You can only edit your own messages.");
            _repo.UpdateMessageContent(messageId, newContent);
        }

        public void DeleteMessageAsStudent(Student student, int messageId)
        {
            var msg = _repo.GetMessageById(messageId);
            if (msg == null) throw new Exception("Message not found.");
            if (msg.SenderRole != "student" || msg.StudentId != student.Id)
                throw new Exception("You can only delete your own messages.");
            _repo.DeleteMessage(messageId);
        }

        public void DeleteMessageAsSupervisor(PersonalSupervisor ps, int messageId)
        {
            var msg = _repo.GetMessageById(messageId);
            if (msg == null) throw new Exception("Message not found.");
            if (msg.SenderRole != "supervisor" || msg.PersonalSupervisorId != ps.Id)
                throw new Exception("You can only delete your own messages.");
            _repo.DeleteMessage(messageId);
        }

        // Senior tutor convenience queries (read-only)
        public List<Message> GetMessagesByStudent(int studentId) => _repo.GetMessagesByStudent(studentId);
        public List<Message> GetMessagesBySupervisor(int supervisorId) => _repo.GetMessagesBySupervisor(supervisorId);
    }
}
