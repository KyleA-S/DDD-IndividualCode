using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.Sqlite;
using DDD.Models;

namespace DDD.Data
{
    public class SqliteRepository : IDataRepository, IDisposable
    {
        private readonly SqliteConnection _conn;
        public string DatabasePath { get; }

        public SqliteRepository(string dbPath = "ddd.db")
        {
            DatabasePath = dbPath;
            var csb = new SqliteConnectionStringBuilder { DataSource = dbPath };
            _conn = new SqliteConnection(csb.ToString());
            _conn.Open();
            EnsureTablesCreated();
        }

        private void EnsureTablesCreated()
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                PRAGMA foreign_keys = ON;

                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY,
                    Username TEXT NOT NULL UNIQUE,
                    Name TEXT NOT NULL,
                    Password TEXT NOT NULL,
                    Role TEXT NOT NULL,
                    PersonalSupervisorId INTEGER NULL
                );

                CREATE TABLE IF NOT EXISTS Meetings (
                    Id INTEGER PRIMARY KEY,
                    StudentId INTEGER NOT NULL,
                    PersonalSupervisorId INTEGER NOT NULL,
                    ScheduledTime TEXT NOT NULL,
                    FOREIGN KEY(StudentId) REFERENCES Users(Id) ON DELETE CASCADE,
                    FOREIGN KEY(PersonalSupervisorId) REFERENCES Users(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS Reports (
                    Id INTEGER PRIMARY KEY,
                    StudentId INTEGER NOT NULL,
                    Score INTEGER NOT NULL,
                    Notes TEXT,
                    Date TEXT NOT NULL,
                    FOREIGN KEY(StudentId) REFERENCES Users(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS Messages (
                    Id INTEGER PRIMARY KEY,
                    StudentId INTEGER NOT NULL,
                    PersonalSupervisorId INTEGER NOT NULL,
                    SenderRole TEXT NOT NULL,
                    Content TEXT NOT NULL,
                    Timestamp TEXT NOT NULL,
                    IsRead INTEGER NOT NULL DEFAULT 0,
                    FOREIGN KEY(StudentId) REFERENCES Users(Id) ON DELETE CASCADE,
                    FOREIGN KEY(PersonalSupervisorId) REFERENCES Users(Id) ON DELETE CASCADE
                );
            ";
            cmd.ExecuteNonQuery();
        }

        #region Mappers & Loaders
        private static Student MapStudent(IDataRecord r)
        {
            return new Student
            {
                Id = Convert.ToInt32(r["Id"]),
                Username = r["Username"].ToString(),
                Name = r["Name"].ToString(),
                Password = r["Password"].ToString(),
                PersonalSupervisorId = r["PersonalSupervisorId"] == DBNull.Value ? 0 : Convert.ToInt32(r["PersonalSupervisorId"])
            };
        }

        private static PersonalSupervisor MapSupervisor(IDataRecord r)
        {
            return new PersonalSupervisor
            {
                Id = Convert.ToInt32(r["Id"]),
                Username = r["Username"].ToString(),
                Name = r["Name"].ToString(),
                Password = r["Password"].ToString()
            };
        }

        private static SeniorTutor MapSenior(IDataRecord r)
        {
            return new SeniorTutor
            {
                Id = Convert.ToInt32(r["Id"]),
                Username = r["Username"].ToString(),
                Name = r["Name"].ToString(),
                Password = r["Password"].ToString()
            };
        }

        private List<Meeting> LoadMeetingsForStudent(int studentId)
        {
            var list = new List<Meeting>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"SELECT Id, StudentId, PersonalSupervisorId, ScheduledTime FROM Meetings WHERE StudentId = @sid ORDER BY ScheduledTime;";
            cmd.Parameters.AddWithValue("@sid", studentId);
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new Meeting
                {
                    Id = Convert.ToInt32(rdr["Id"]),
                    StudentId = Convert.ToInt32(rdr["StudentId"]),
                    PersonalSupervisorId = Convert.ToInt32(rdr["PersonalSupervisorId"]),
                    ScheduledTime = DateTime.Parse(rdr["ScheduledTime"].ToString())
                });
            }
            return list;
        }

        private List<Meeting> LoadMeetingsForSupervisor(int supervisorId)
        {
            var list = new List<Meeting>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"SELECT Id, StudentId, PersonalSupervisorId, ScheduledTime FROM Meetings WHERE PersonalSupervisorId = @pid ORDER BY ScheduledTime;";
            cmd.Parameters.AddWithValue("@pid", supervisorId);
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new Meeting
                {
                    Id = Convert.ToInt32(rdr["Id"]),
                    StudentId = Convert.ToInt32(rdr["StudentId"]),
                    PersonalSupervisorId = Convert.ToInt32(rdr["PersonalSupervisorId"]),
                    ScheduledTime = DateTime.Parse(rdr["ScheduledTime"].ToString())
                });
            }
            return list;
        }

        private List<WellbeingReport> LoadReportsForStudent(int studentId)
        {
            var list = new List<WellbeingReport>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"SELECT Id, StudentId, Score, Notes, Date FROM Reports WHERE StudentId = @sid ORDER BY Date;";
            cmd.Parameters.AddWithValue("@sid", studentId);
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new WellbeingReport
                {
                    Id = Convert.ToInt32(rdr["Id"]),
                    StudentId = Convert.ToInt32(rdr["StudentId"]),
                    Score = Convert.ToInt32(rdr["Score"]),
                    Notes = rdr["Notes"] == DBNull.Value ? string.Empty : rdr["Notes"].ToString(),
                    Date = DateTime.Parse(rdr["Date"].ToString())
                });
            }
            return list;
        }
        #endregion

        #region Student CRUD
        public Student GetStudentById(int id)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Users WHERE Id = @id AND Role = 'Student';";
            cmd.Parameters.AddWithValue("@id", id);
            using var rdr = cmd.ExecuteReader();
            if (!rdr.Read()) return null;
            var s = MapStudent(rdr);
            s.Meetings = LoadMeetingsForStudent(s.Id);
            s.Reports = LoadReportsForStudent(s.Id);
            return s;
        }

        public Student GetStudentByUsername(string username)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Users WHERE lower(Username) = lower(@u) AND Role='Student';";
            cmd.Parameters.AddWithValue("@u", username);
            using var rdr = cmd.ExecuteReader();
            if (!rdr.Read()) return null;
            var s = MapStudent(rdr);
            s.Meetings = LoadMeetingsForStudent(s.Id);
            s.Reports = LoadReportsForStudent(s.Id);
            return s;
        }

        public void SaveStudent(Student student)
        {
            if (student == null) throw new ArgumentNullException(nameof(student));

            if (student.Id <= 0)
            {
                using var cmd = _conn.CreateCommand();
                cmd.CommandText = @"INSERT INTO Users (Username, Name, Password, Role, PersonalSupervisorId)
                                    VALUES (@u,@n,@p,'Student', @ps);
                                    SELECT last_insert_rowid();";
                cmd.Parameters.AddWithValue("@u", student.Username ?? "");
                cmd.Parameters.AddWithValue("@n", student.Name ?? "");
                cmd.Parameters.AddWithValue("@p", student.Password ?? "");
                if (student.PersonalSupervisorId == 0) cmd.Parameters.AddWithValue("@ps", DBNull.Value);
                else cmd.Parameters.AddWithValue("@ps", student.PersonalSupervisorId);
                var id = (long)cmd.ExecuteScalar();
                student.Id = (int)id;
            }
            else
            {
                using var cmd = _conn.CreateCommand();
                cmd.CommandText = @"UPDATE Users SET Username=@u, Name=@n, Password=@p, PersonalSupervisorId=@ps WHERE Id=@id;";
                cmd.Parameters.AddWithValue("@u", student.Username ?? "");
                cmd.Parameters.AddWithValue("@n", student.Name ?? "");
                cmd.Parameters.AddWithValue("@p", student.Password ?? "");
                if (student.PersonalSupervisorId == 0) cmd.Parameters.AddWithValue("@ps", DBNull.Value);
                else cmd.Parameters.AddWithValue("@ps", student.PersonalSupervisorId);
                cmd.Parameters.AddWithValue("@id", student.Id);
                cmd.ExecuteNonQuery();
            }

            using var tx = _conn.BeginTransaction();

            using var delM = _conn.CreateCommand();
            delM.Transaction = tx;
            delM.CommandText = "DELETE FROM Meetings WHERE StudentId = @sid;";
            delM.Parameters.AddWithValue("@sid", student.Id);
            delM.ExecuteNonQuery();

            using var delR = _conn.CreateCommand();
            delR.Transaction = tx;
            delR.CommandText = "DELETE FROM Reports WHERE StudentId = @sid;";
            delR.Parameters.AddWithValue("@sid", student.Id);
            delR.ExecuteNonQuery();

            if (student.Meetings != null)
            {
                foreach (var m in student.Meetings)
                {
                    using var im = _conn.CreateCommand();
                    im.Transaction = tx;
                    im.CommandText = "INSERT INTO Meetings (StudentId, PersonalSupervisorId, ScheduledTime) VALUES (@sid,@pid,@dt);";
                    im.Parameters.AddWithValue("@sid", student.Id);
                    im.Parameters.AddWithValue("@pid", m.PersonalSupervisorId);
                    im.Parameters.AddWithValue("@dt", m.ScheduledTime.ToString("o"));
                    im.ExecuteNonQuery();
                }
            }

            if (student.Reports != null)
            {
                foreach (var r in student.Reports)
                {
                    using var ir = _conn.CreateCommand();
                    ir.Transaction = tx;
                    ir.CommandText = "INSERT INTO Reports (StudentId, Score, Notes, Date) VALUES (@sid,@score,@notes,@date);";
                    ir.Parameters.AddWithValue("@sid", student.Id);
                    ir.Parameters.AddWithValue("@score", r.Score);
                    ir.Parameters.AddWithValue("@notes", r.Notes ?? "");
                    ir.Parameters.AddWithValue("@date", r.Date.ToString("o"));
                    ir.ExecuteNonQuery();
                }
            }

            tx.Commit();
        }

        public List<Student> GetAllStudents()
        {
            var list = new List<Student>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Users WHERE Role='Student' ORDER BY Id;";
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                var s = MapStudent(rdr);
                s.Meetings = LoadMeetingsForStudent(s.Id);
                s.Reports = LoadReportsForStudent(s.Id);
                list.Add(s);
            }
            return list;
        }
        #endregion

        #region Supervisor CRUD
        public PersonalSupervisor GetSupervisorById(int id)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Users WHERE Id=@id AND Role='PersonalSupervisor';";
            cmd.Parameters.AddWithValue("@id", id);
            using var rdr = cmd.ExecuteReader();
            if (!rdr.Read()) return null;
            var ps = MapSupervisor(rdr);
            ps.Meetings = LoadMeetingsForSupervisor(ps.Id);
            return ps;
        }

        public PersonalSupervisor GetSupervisorByUsername(string username)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Users WHERE lower(Username)=lower(@u) AND Role='PersonalSupervisor';";
            cmd.Parameters.AddWithValue("@u", username);
            using var rdr = cmd.ExecuteReader();
            if (!rdr.Read()) return null;
            var ps = MapSupervisor(rdr);
            ps.Meetings = LoadMeetingsForSupervisor(ps.Id);
            return ps;
        }

        public void SavePersonalSupervisor(PersonalSupervisor supervisor)
        {
            if (supervisor == null) throw new ArgumentNullException(nameof(supervisor));
            if (supervisor.Id <= 0)
            {
                using var cmd = _conn.CreateCommand();
                cmd.CommandText = @"INSERT INTO Users (Username, Name, Password, Role) VALUES (@u,@n,@p,'PersonalSupervisor'); SELECT last_insert_rowid();";
                cmd.Parameters.AddWithValue("@u", supervisor.Username ?? "");
                cmd.Parameters.AddWithValue("@n", supervisor.Name ?? "");
                cmd.Parameters.AddWithValue("@p", supervisor.Password ?? "");
                var id = (long)cmd.ExecuteScalar();
                supervisor.Id = (int)id;
            }
            else
            {
                using var cmd = _conn.CreateCommand();
                cmd.CommandText = "UPDATE Users SET Username=@u, Name=@n, Password=@p WHERE Id=@id;";
                cmd.Parameters.AddWithValue("@u", supervisor.Username ?? "");
                cmd.Parameters.AddWithValue("@n", supervisor.Name ?? "");
                cmd.Parameters.AddWithValue("@p", supervisor.Password ?? "");
                cmd.Parameters.AddWithValue("@id", supervisor.Id);
                cmd.ExecuteNonQuery();
            }

            using var tx = _conn.BeginTransaction();
            using var del = _conn.CreateCommand();
            del.Transaction = tx;
            del.CommandText = "DELETE FROM Meetings WHERE PersonalSupervisorId = @pid;";
            del.Parameters.AddWithValue("@pid", supervisor.Id);
            del.ExecuteNonQuery();

            if (supervisor.Meetings != null)
            {
                foreach (var m in supervisor.Meetings)
                {
                    using var im = _conn.CreateCommand();
                    im.Transaction = tx;
                    im.CommandText = "INSERT INTO Meetings (StudentId, PersonalSupervisorId, ScheduledTime) VALUES (@sid,@pid,@dt);";
                    im.Parameters.AddWithValue("@sid", m.StudentId);
                    im.Parameters.AddWithValue("@pid", supervisor.Id);
                    im.Parameters.AddWithValue("@dt", m.ScheduledTime.ToString("o"));
                    im.ExecuteNonQuery();
                }
            }
            tx.Commit();
        }

        public List<PersonalSupervisor> GetAllPersonalSupervisors()
        {
            var list = new List<PersonalSupervisor>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Users WHERE Role='PersonalSupervisor' ORDER BY Id;";
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                var ps = MapSupervisor(rdr);
                ps.Meetings = LoadMeetingsForSupervisor(ps.Id);
                list.Add(ps);
            }
            return list;
        }
        #endregion

        #region SeniorTutor CRUD
        public SeniorTutor GetSeniorTutorById(int id)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Users WHERE Id=@id AND Role='SeniorTutor';";
            cmd.Parameters.AddWithValue("@id", id);
            using var rdr = cmd.ExecuteReader();
            if (!rdr.Read()) return null;
            return MapSenior(rdr);
        }

        public SeniorTutor GetSeniorTutorByUsername(string username)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Users WHERE lower(Username)=lower(@u) AND Role='SeniorTutor';";
            cmd.Parameters.AddWithValue("@u", username);
            using var rdr = cmd.ExecuteReader();
            if (!rdr.Read()) return null;
            return MapSenior(rdr);
        }

        public void SaveSeniorTutor(SeniorTutor seniorTutor)
        {
            if (seniorTutor == null) throw new ArgumentNullException(nameof(seniorTutor));
            if (seniorTutor.Id <= 0)
            {
                using var cmd = _conn.CreateCommand();
                cmd.CommandText = "INSERT INTO Users (Username, Name, Password, Role) VALUES (@u,@n,@p,'SeniorTutor'); SELECT last_insert_rowid();";
                cmd.Parameters.AddWithValue("@u", seniorTutor.Username ?? "");
                cmd.Parameters.AddWithValue("@n", seniorTutor.Name ?? "");
                cmd.Parameters.AddWithValue("@p", seniorTutor.Password ?? "");
                var id = (long)cmd.ExecuteScalar();
                seniorTutor.Id = (int)id;
            }
            else
            {
                using var cmd = _conn.CreateCommand();
                cmd.CommandText = "UPDATE Users SET Username=@u, Name=@n, Password=@p WHERE Id=@id;";
                cmd.Parameters.AddWithValue("@u", seniorTutor.Username ?? "");
                cmd.Parameters.AddWithValue("@n", seniorTutor.Name ?? "");
                cmd.Parameters.AddWithValue("@p", seniorTutor.Password ?? "");
                cmd.Parameters.AddWithValue("@id", seniorTutor.Id);
                cmd.ExecuteNonQuery();
            }
        }

        public List<SeniorTutor> GetAllSeniorTutors()
        {
            var list = new List<SeniorTutor>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Users WHERE Role='SeniorTutor' ORDER BY Id;";
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(MapSenior(rdr));
            }
            return list;
        }
        #endregion

        #region Messages CRUD
        public void SendMessage(Message msg)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Messages (StudentId, PersonalSupervisorId, SenderRole, Content, Timestamp, IsRead)
                VALUES (@s, @p, @r, @c, @t, @isread);
                SELECT last_insert_rowid();
            ";
            cmd.Parameters.AddWithValue("@s", msg.StudentId);
            cmd.Parameters.AddWithValue("@p", msg.PersonalSupervisorId);
            cmd.Parameters.AddWithValue("@r", msg.SenderRole ?? "");
            cmd.Parameters.AddWithValue("@c", msg.Content ?? "");
            cmd.Parameters.AddWithValue("@t", msg.Timestamp.ToString("o"));
            cmd.Parameters.AddWithValue("@isread", msg.IsRead ? 1 : 0);
            var id = (long)cmd.ExecuteScalar();
            msg.Id = (int)id;
        }

        public List<Message> GetMessages(int studentId, int supervisorId)
        {
            var list = new List<Message>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"SELECT Id, StudentId, PersonalSupervisorId, SenderRole, Content, Timestamp, IsRead
                                FROM Messages
                                WHERE StudentId = @s AND PersonalSupervisorId = @p
                                ORDER BY Timestamp;";
            cmd.Parameters.AddWithValue("@s", studentId);
            cmd.Parameters.AddWithValue("@p", supervisorId);
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new Message
                {
                    Id = Convert.ToInt32(rdr["Id"]),
                    StudentId = Convert.ToInt32(rdr["StudentId"]),
                    PersonalSupervisorId = Convert.ToInt32(rdr["PersonalSupervisorId"]),
                    SenderRole = rdr["SenderRole"].ToString(),
                    Content = rdr["Content"].ToString(),
                    Timestamp = DateTime.Parse(rdr["Timestamp"].ToString()),
                    IsRead = Convert.ToInt32(rdr["IsRead"]) != 0
                });
            }
            return list;
        }

        public Message GetMessageById(int messageId)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"SELECT Id, StudentId, PersonalSupervisorId, SenderRole, Content, Timestamp, IsRead
                                FROM Messages
                                WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", messageId);
            using var rdr = cmd.ExecuteReader();
            if (!rdr.Read()) return null;
            return new Message
            {
                Id = Convert.ToInt32(rdr["Id"]),
                StudentId = Convert.ToInt32(rdr["StudentId"]),
                PersonalSupervisorId = Convert.ToInt32(rdr["PersonalSupervisorId"]),
                SenderRole = rdr["SenderRole"].ToString(),
                Content = rdr["Content"].ToString(),
                Timestamp = DateTime.Parse(rdr["Timestamp"].ToString()),
                IsRead = Convert.ToInt32(rdr["IsRead"]) != 0
            };
        }

        public void UpdateMessageContent(int messageId, string newContent)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"UPDATE Messages SET Content = @c WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@c", newContent ?? "");
            cmd.Parameters.AddWithValue("@id", messageId);
            cmd.ExecuteNonQuery();
        }

        public void DeleteMessage(int messageId)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"DELETE FROM Messages WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", messageId);
            cmd.ExecuteNonQuery();
        }

        public void MarkMessagesAsRead(int studentId, int supervisorId, string readerRole)
        {
            // mark messages where SenderRole != readerRole and IsRead = 0
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE Messages
                SET IsRead = 1
                WHERE StudentId = @s AND PersonalSupervisorId = @p AND SenderRole != @reader AND IsRead = 0;
            ";
            cmd.Parameters.AddWithValue("@s", studentId);
            cmd.Parameters.AddWithValue("@p", supervisorId);
            cmd.Parameters.AddWithValue("@reader", readerRole ?? "");
            cmd.ExecuteNonQuery();
        }

        public int GetUnreadCount(int studentId, int supervisorId, string readerRole)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT COUNT(*) FROM Messages
                WHERE StudentId = @s AND PersonalSupervisorId = @p AND SenderRole != @reader AND IsRead = 0;
            ";
            cmd.Parameters.AddWithValue("@s", studentId);
            cmd.Parameters.AddWithValue("@p", supervisorId);
            cmd.Parameters.AddWithValue("@reader", readerRole ?? "");
            var res = (long)cmd.ExecuteScalar();
            return (int)res;
        }

        public List<Message> GetMessagesByStudent(int studentId)
        {
            var list = new List<Message>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"SELECT Id, StudentId, PersonalSupervisorId, SenderRole, Content, Timestamp, IsRead
                                FROM Messages
                                WHERE StudentId = @s
                                ORDER BY Timestamp;";
            cmd.Parameters.AddWithValue("@s", studentId);
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new Message
                {
                    Id = Convert.ToInt32(rdr["Id"]),
                    StudentId = Convert.ToInt32(rdr["StudentId"]),
                    PersonalSupervisorId = Convert.ToInt32(rdr["PersonalSupervisorId"]),
                    SenderRole = rdr["SenderRole"].ToString(),
                    Content = rdr["Content"].ToString(),
                    Timestamp = DateTime.Parse(rdr["Timestamp"].ToString()),
                    IsRead = Convert.ToInt32(rdr["IsRead"]) != 0
                });
            }
            return list;
        }

        public List<Message> GetMessagesBySupervisor(int supervisorId)
        {
            var list = new List<Message>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"SELECT Id, StudentId, PersonalSupervisorId, SenderRole, Content, Timestamp, IsRead
                                FROM Messages
                                WHERE PersonalSupervisorId = @p
                                ORDER BY Timestamp;";
            cmd.Parameters.AddWithValue("@p", supervisorId);
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new Message
                {
                    Id = Convert.ToInt32(rdr["Id"]),
                    StudentId = Convert.ToInt32(rdr["StudentId"]),
                    PersonalSupervisorId = Convert.ToInt32(rdr["PersonalSupervisorId"]),
                    SenderRole = rdr["SenderRole"].ToString(),
                    Content = rdr["Content"].ToString(),
                    Timestamp = DateTime.Parse(rdr["Timestamp"].ToString()),
                    IsRead = Convert.ToInt32(rdr["IsRead"]) != 0
                });
            }
            return list;
        }
        #endregion

        public void Dispose()
        {
            try
            {
                _conn?.Close();
                _conn?.Dispose();
            }
            catch { }
        }
    }
}
