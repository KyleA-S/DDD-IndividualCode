using System;
using System.Collections.Generic;
using System.Data;
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
                    PersonalSupervisorId INTEGER NULL,
                    StudentCode TEXT,
                    SupervisorCode TEXT,
                    SeniorTutorCode TEXT,
                    YearGroup INTEGER,
                    SecurityQuestion TEXT DEFAULT '',
                    SecurityAnswer TEXT DEFAULT '',
                    LastWellbeingReportDate TEXT,
                    HasMissedWellbeingReport INTEGER DEFAULT 0,
                    MissedReportCount INTEGER DEFAULT 0
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
                    IsHighPriority INTEGER DEFAULT 0,
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

                CREATE TABLE IF NOT EXISTS WellbeingAlerts (
                    Id INTEGER PRIMARY KEY,
                    StudentId INTEGER NOT NULL,
                    StudentName TEXT NOT NULL,
                    AlertDate TEXT NOT NULL,
                    Reason TEXT NOT NULL,
                    IsResolved INTEGER DEFAULT 0,
                    ResolvedDate TEXT,
                    FOREIGN KEY(StudentId) REFERENCES Users(Id) ON DELETE CASCADE
                );
            ";
            cmd.ExecuteNonQuery();
        }

        // ---------- Helper ----------
        private static bool HasColumn(IDataRecord r, string columnName)
        {
            for (int i = 0; i < r.FieldCount; i++)
                if (string.Equals(r.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        #region Mappers & Loaders
        private static Student MapStudent(IDataRecord r)
        {
            var student = new Student
            {
                Id = Convert.ToInt32(r["Id"]),
                Username = r["Username"].ToString(),
                Name = r["Name"].ToString(),
                Password = r["Password"].ToString(),
                PersonalSupervisorId = r["PersonalSupervisorId"] == DBNull.Value ? 0 : Convert.ToInt32(r["PersonalSupervisorId"]),
                YearGroup = HasColumn(r, "YearGroup") && r["YearGroup"] != DBNull.Value ? Convert.ToInt32(r["YearGroup"]) : 1
            };

            // Handle nullable string columns
            student.StudentCode = HasColumn(r, "StudentCode") && r["StudentCode"] != DBNull.Value ? r["StudentCode"].ToString() : string.Empty;
            student.SecurityQuestion = HasColumn(r, "SecurityQuestion") && r["SecurityQuestion"] != DBNull.Value ? r["SecurityQuestion"].ToString() : string.Empty;
            student.SecurityAnswer = HasColumn(r, "SecurityAnswer") && r["SecurityAnswer"] != DBNull.Value ? r["SecurityAnswer"].ToString() : string.Empty;

            // Handle wellbeing tracking fields
            if (HasColumn(r, "LastWellbeingReportDate") && r["LastWellbeingReportDate"] != DBNull.Value)
            {
                student.LastWellbeingReportDate = DateTime.Parse(r["LastWellbeingReportDate"].ToString());
            }
            else
            {
                student.LastWellbeingReportDate = DateTime.MinValue;
            }

            student.HasMissedWellbeingReport = HasColumn(r, "HasMissedWellbeingReport") && r["HasMissedWellbeingReport"] != DBNull.Value ?
                Convert.ToInt32(r["HasMissedWellbeingReport"]) != 0 : false;
            student.MissedReportCount = HasColumn(r, "MissedReportCount") && r["MissedReportCount"] != DBNull.Value ?
                Convert.ToInt32(r["MissedReportCount"]) : 0;

            return student;
        }

        private static PersonalSupervisor MapSupervisor(IDataRecord r)
        {
            var supervisor = new PersonalSupervisor
            {
                Id = Convert.ToInt32(r["Id"]),
                Username = r["Username"].ToString(),
                Name = r["Name"].ToString(),
                Password = r["Password"].ToString()
            };

            // Handle nullable string columns
            supervisor.SupervisorCode = HasColumn(r, "SupervisorCode") && r["SupervisorCode"] != DBNull.Value ? r["SupervisorCode"].ToString() : string.Empty;
            supervisor.SecurityQuestion = HasColumn(r, "SecurityQuestion") && r["SecurityQuestion"] != DBNull.Value ? r["SecurityQuestion"].ToString() : string.Empty;
            supervisor.SecurityAnswer = HasColumn(r, "SecurityAnswer") && r["SecurityAnswer"] != DBNull.Value ? r["SecurityAnswer"].ToString() : string.Empty;

            return supervisor;
        }

        private static SeniorTutor MapSenior(IDataRecord r)
        {
            var tutor = new SeniorTutor
            {
                Id = Convert.ToInt32(r["Id"]),
                Username = r["Username"].ToString(),
                Name = r["Name"].ToString(),
                Password = r["Password"].ToString()
            };

            // Handle nullable string columns
            tutor.SeniorTutorCode = HasColumn(r, "SeniorTutorCode") && r["SeniorTutorCode"] != DBNull.Value ? r["SeniorTutorCode"].ToString() : string.Empty;
            tutor.SecurityQuestion = HasColumn(r, "SecurityQuestion") && r["SecurityQuestion"] != DBNull.Value ? r["SecurityQuestion"].ToString() : string.Empty;
            tutor.SecurityAnswer = HasColumn(r, "SecurityAnswer") && r["SecurityAnswer"] != DBNull.Value ? r["SecurityAnswer"].ToString() : string.Empty;

            return tutor;
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
            cmd.CommandText = @"SELECT Id, StudentId, Score, Notes, Date, IsHighPriority FROM Reports WHERE StudentId = @sid ORDER BY Date;";
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
                    Date = DateTime.Parse(rdr["Date"].ToString()),
                    IsHighPriority = Convert.ToInt32(rdr["IsHighPriority"]) != 0
                });
            }
            return list;
        }

        private List<WellbeingAlert> LoadAlertsForStudent(int studentId)
        {
            var list = new List<WellbeingAlert>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"SELECT Id, StudentId, StudentName, AlertDate, Reason, IsResolved, ResolvedDate FROM WellbeingAlerts WHERE StudentId = @sid ORDER BY AlertDate DESC;";
            cmd.Parameters.AddWithValue("@sid", studentId);
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new WellbeingAlert
                {
                    Id = Convert.ToInt32(rdr["Id"]),
                    StudentId = Convert.ToInt32(rdr["StudentId"]),
                    StudentName = rdr["StudentName"].ToString(),
                    AlertDate = DateTime.Parse(rdr["AlertDate"].ToString()),
                    Reason = rdr["Reason"].ToString(),
                    IsResolved = Convert.ToInt32(rdr["IsResolved"]) != 0,
                    ResolvedDate = rdr["ResolvedDate"] == DBNull.Value ? null : (DateTime?)DateTime.Parse(rdr["ResolvedDate"].ToString())
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

        public Student GetStudentByCode(string studentCode)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Users WHERE StudentCode = @c AND Role='Student';";
            cmd.Parameters.AddWithValue("@c", studentCode);
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
                cmd.CommandText = @"INSERT INTO Users (Username, Name, Password, Role, PersonalSupervisorId, StudentCode, YearGroup, SecurityQuestion, SecurityAnswer, LastWellbeingReportDate, HasMissedWellbeingReport, MissedReportCount)
                                    VALUES (@u,@n,@p,'Student', @ps, @scode, @yg, @sq, @sa, @lwrd, @hmwr, @mrc);
                                    SELECT last_insert_rowid();";
                cmd.Parameters.AddWithValue("@u", student.Username ?? "");
                cmd.Parameters.AddWithValue("@n", student.Name ?? "");
                cmd.Parameters.AddWithValue("@p", student.Password ?? "");
                if (student.PersonalSupervisorId == 0) cmd.Parameters.AddWithValue("@ps", DBNull.Value);
                else cmd.Parameters.AddWithValue("@ps", student.PersonalSupervisorId);
                cmd.Parameters.AddWithValue("@scode", string.IsNullOrEmpty(student.StudentCode) ? DBNull.Value : (object)student.StudentCode);
                cmd.Parameters.AddWithValue("@yg", student.YearGroup);
                cmd.Parameters.AddWithValue("@sq", student.SecurityQuestion ?? "");
                cmd.Parameters.AddWithValue("@sa", student.SecurityAnswer ?? "");
                cmd.Parameters.AddWithValue("@lwrd", student.LastWellbeingReportDate == DateTime.MinValue ? DBNull.Value : (object)student.LastWellbeingReportDate.ToString("o"));
                cmd.Parameters.AddWithValue("@hmwr", student.HasMissedWellbeingReport ? 1 : 0);
                cmd.Parameters.AddWithValue("@mrc", student.MissedReportCount);
                var id = (long)cmd.ExecuteScalar();
                student.Id = (int)id;
            }
            else
            {
                using var cmd = _conn.CreateCommand();
                cmd.CommandText = @"UPDATE Users SET Username=@u, Name=@n, Password=@p, PersonalSupervisorId=@ps, StudentCode=@scode, YearGroup=@yg, SecurityQuestion=@sq, SecurityAnswer=@sa, LastWellbeingReportDate=@lwrd, HasMissedWellbeingReport=@hmwr, MissedReportCount=@mrc WHERE Id=@id;";
                cmd.Parameters.AddWithValue("@u", student.Username ?? "");
                cmd.Parameters.AddWithValue("@n", student.Name ?? "");
                cmd.Parameters.AddWithValue("@p", student.Password ?? "");
                if (student.PersonalSupervisorId == 0) cmd.Parameters.AddWithValue("@ps", DBNull.Value);
                else cmd.Parameters.AddWithValue("@ps", student.PersonalSupervisorId);
                cmd.Parameters.AddWithValue("@scode", string.IsNullOrEmpty(student.StudentCode) ? DBNull.Value : (object)student.StudentCode);
                cmd.Parameters.AddWithValue("@yg", student.YearGroup);
                cmd.Parameters.AddWithValue("@sq", student.SecurityQuestion ?? "");
                cmd.Parameters.AddWithValue("@sa", student.SecurityAnswer ?? "");
                cmd.Parameters.AddWithValue("@lwrd", student.LastWellbeingReportDate == DateTime.MinValue ? DBNull.Value : (object)student.LastWellbeingReportDate.ToString("o"));
                cmd.Parameters.AddWithValue("@hmwr", student.HasMissedWellbeingReport ? 1 : 0);
                cmd.Parameters.AddWithValue("@mrc", student.MissedReportCount);
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
                    ir.CommandText = "INSERT INTO Reports (StudentId, Score, Notes, Date, IsHighPriority) VALUES (@sid,@score,@notes,@date,@high);";
                    ir.Parameters.AddWithValue("@sid", student.Id);
                    ir.Parameters.AddWithValue("@score", r.Score);
                    ir.Parameters.AddWithValue("@notes", r.Notes ?? "");
                    ir.Parameters.AddWithValue("@date", r.Date.ToString("o"));
                    ir.Parameters.AddWithValue("@high", r.IsHighPriority ? 1 : 0);
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

        // New method: Get students with low wellbeing scores (<5)
        public List<Student> GetStudentsWithLowWellbeing()
        {
            var list = new List<Student>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT DISTINCT u.* FROM Users u
                JOIN Reports r ON u.Id = r.StudentId
                WHERE u.Role = 'Student' AND r.Score < 5
                ORDER BY u.Id;";
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

        // New method: Get students who have missed wellbeing reports
        public List<Student> GetStudentsWithMissedReports()
        {
            var list = new List<Student>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT * FROM Users 
                WHERE Role = 'Student' AND HasMissedWellbeingReport = 1
                ORDER BY Id;";
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

        public PersonalSupervisor GetSupervisorByCode(string supervisorCode)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Users WHERE SupervisorCode = @c AND Role='PersonalSupervisor';";
            cmd.Parameters.AddWithValue("@c", supervisorCode);
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
                cmd.CommandText = @"INSERT INTO Users (Username, Name, Password, Role, SupervisorCode, SecurityQuestion, SecurityAnswer)
                                    VALUES (@u,@n,@p,'PersonalSupervisor', @scode, @sq, @sa);
                                    SELECT last_insert_rowid();";
                cmd.Parameters.AddWithValue("@u", supervisor.Username ?? "");
                cmd.Parameters.AddWithValue("@n", supervisor.Name ?? "");
                cmd.Parameters.AddWithValue("@p", supervisor.Password ?? "");
                cmd.Parameters.AddWithValue("@scode", string.IsNullOrEmpty(supervisor.SupervisorCode) ? DBNull.Value : (object)supervisor.SupervisorCode);
                cmd.Parameters.AddWithValue("@sq", supervisor.SecurityQuestion ?? "");
                cmd.Parameters.AddWithValue("@sa", supervisor.SecurityAnswer ?? "");
                var id = (long)cmd.ExecuteScalar();
                supervisor.Id = (int)id;
            }
            else
            {
                using var cmd = _conn.CreateCommand();
                cmd.CommandText = "UPDATE Users SET Username=@u, Name=@n, Password=@p, SupervisorCode=@scode, SecurityQuestion=@sq, SecurityAnswer=@sa WHERE Id=@id;";
                cmd.Parameters.AddWithValue("@u", supervisor.Username ?? "");
                cmd.Parameters.AddWithValue("@n", supervisor.Name ?? "");
                cmd.Parameters.AddWithValue("@p", supervisor.Password ?? "");
                cmd.Parameters.AddWithValue("@scode", string.IsNullOrEmpty(supervisor.SupervisorCode) ? DBNull.Value : (object)supervisor.SupervisorCode);
                cmd.Parameters.AddWithValue("@sq", supervisor.SecurityQuestion ?? "");
                cmd.Parameters.AddWithValue("@sa", supervisor.SecurityAnswer ?? "");
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

        public SeniorTutor GetSeniorTutorByCode(string code)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Users WHERE SeniorTutorCode = @c AND Role='SeniorTutor';";
            cmd.Parameters.AddWithValue("@c", code);
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
                cmd.CommandText = "INSERT INTO Users (Username, Name, Password, Role, SeniorTutorCode, SecurityQuestion, SecurityAnswer) VALUES (@u,@n,@p,'SeniorTutor', @scode, @sq, @sa); SELECT last_insert_rowid();";
                cmd.Parameters.AddWithValue("@u", seniorTutor.Username ?? "");
                cmd.Parameters.AddWithValue("@n", seniorTutor.Name ?? "");
                cmd.Parameters.AddWithValue("@p", seniorTutor.Password ?? "");
                cmd.Parameters.AddWithValue("@scode", string.IsNullOrEmpty(seniorTutor.SeniorTutorCode) ? DBNull.Value : (object)seniorTutor.SeniorTutorCode);
                cmd.Parameters.AddWithValue("@sq", seniorTutor.SecurityQuestion ?? "");
                cmd.Parameters.AddWithValue("@sa", seniorTutor.SecurityAnswer ?? "");
                var id = (long)cmd.ExecuteScalar();
                seniorTutor.Id = (int)id;
            }
            else
            {
                using var cmd = _conn.CreateCommand();
                cmd.CommandText = "UPDATE Users SET Username=@u, Name=@n, Password=@p, SeniorTutorCode=@scode, SecurityQuestion=@sq, SecurityAnswer=@sa WHERE Id=@id;";
                cmd.Parameters.AddWithValue("@u", seniorTutor.Username ?? "");
                cmd.Parameters.AddWithValue("@n", seniorTutor.Name ?? "");
                cmd.Parameters.AddWithValue("@p", seniorTutor.Password ?? "");
                cmd.Parameters.AddWithValue("@scode", string.IsNullOrEmpty(seniorTutor.SeniorTutorCode) ? DBNull.Value : (object)seniorTutor.SeniorTutorCode);
                cmd.Parameters.AddWithValue("@sq", seniorTutor.SecurityQuestion ?? "");
                cmd.Parameters.AddWithValue("@sa", seniorTutor.SecurityAnswer ?? "");
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

        #region Wellbeing Alerts CRUD
        public void AddWellbeingAlert(WellbeingAlert alert)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO WellbeingAlerts (StudentId, StudentName, AlertDate, Reason, IsResolved, ResolvedDate)
                VALUES (@sid, @sname, @adate, @reason, @resolved, @rdate);
                SELECT last_insert_rowid();
            ";
            cmd.Parameters.AddWithValue("@sid", alert.StudentId);
            cmd.Parameters.AddWithValue("@sname", alert.StudentName ?? "");
            cmd.Parameters.AddWithValue("@adate", alert.AlertDate.ToString("o"));
            cmd.Parameters.AddWithValue("@reason", alert.Reason ?? "");
            cmd.Parameters.AddWithValue("@resolved", alert.IsResolved ? 1 : 0);
            cmd.Parameters.AddWithValue("@rdate",
    alert.ResolvedDate.HasValue
        ? alert.ResolvedDate.Value.ToString("o")
        : (object)DBNull.Value);

            var id = (long)cmd.ExecuteScalar();
            alert.Id = (int)id;
        }

        public List<WellbeingAlert> GetActiveWellbeingAlerts()
        {
            var list = new List<WellbeingAlert>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"SELECT Id, StudentId, StudentName, AlertDate, Reason, IsResolved, ResolvedDate 
                                FROM WellbeingAlerts 
                                WHERE IsResolved = 0
                                ORDER BY AlertDate DESC;";
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new WellbeingAlert
                {
                    Id = Convert.ToInt32(rdr["Id"]),
                    StudentId = Convert.ToInt32(rdr["StudentId"]),
                    StudentName = rdr["StudentName"].ToString(),
                    AlertDate = DateTime.Parse(rdr["AlertDate"].ToString()),
                    Reason = rdr["Reason"].ToString(),
                    IsResolved = Convert.ToInt32(rdr["IsResolved"]) != 0,
                    ResolvedDate = rdr["ResolvedDate"] == DBNull.Value ? null : (DateTime?)DateTime.Parse(rdr["ResolvedDate"].ToString())
                });
            }
            return list;
        }

        public void ResolveAlert(int alertId)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"UPDATE WellbeingAlerts SET IsResolved = 1, ResolvedDate = @now WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", alertId);
            cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("o"));
            cmd.ExecuteNonQuery();
        }
        #endregion

        #region Password Reset Methods
        public bool UpdatePassword(string username, string newPassword)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "UPDATE Users SET Password = @pass WHERE Username = @user;";
            cmd.Parameters.AddWithValue("@pass", newPassword ?? "");
            cmd.Parameters.AddWithValue("@user", username);
            int rows = cmd.ExecuteNonQuery();
            return rows > 0;
        }

        public bool SetSecurityQuestion(string username, string question, string answer)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "UPDATE Users SET SecurityQuestion = @q, SecurityAnswer = @a WHERE Username = @user;";
            cmd.Parameters.AddWithValue("@q", question ?? "");
            cmd.Parameters.AddWithValue("@a", answer ?? "");
            cmd.Parameters.AddWithValue("@user", username);
            int rows = cmd.ExecuteNonQuery();
            return rows > 0;
        }

        public bool VerifySecurityAnswer(string username, string answer)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT SecurityAnswer FROM Users WHERE Username = @user;";
            cmd.Parameters.AddWithValue("@user", username);
            var result = cmd.ExecuteScalar();

            // Handle DBNull properly
            if (result == null || result == DBNull.Value)
                return false;

            var storedAnswer = result.ToString();
            if (string.IsNullOrEmpty(storedAnswer))
                return false;

            return string.Equals(storedAnswer, answer, StringComparison.OrdinalIgnoreCase);
        }

        public string GetSecurityQuestion(string username)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT SecurityQuestion FROM Users WHERE Username = @user;";
            cmd.Parameters.AddWithValue("@user", username);
            var result = cmd.ExecuteScalar();

            // Handle DBNull properly
            if (result == null || result == DBNull.Value)
                return string.Empty;

            var question = result.ToString();
            return question ?? string.Empty;
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