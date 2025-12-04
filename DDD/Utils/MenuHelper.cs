using System;
using System.Linq;
using DDD.Data;
using DDD.Models;
using DDD.Services;

namespace DDD.Utils
{
    public static class MenuHelper
    {
        #region Student Menu
        public static void StudentMenu(Student student, StudentService studentService, MeetingService meetingService, WellbeingService wellbeingService, MessageService messageService, IDataRepository repo)
        {
            int choice;
            do
            {
                Console.Clear();
                Console.WriteLine($"--- Student Menu ({student.Name}) ---");
                Console.WriteLine("1. Submit Wellbeing Report");
                Console.WriteLine("2. View My Reports");
                Console.WriteLine("3. Book Meeting with Personal Supervisor");
                Console.WriteLine("4. View My Meetings");
                Console.WriteLine("5. Messaging");
                Console.WriteLine("0. Logout");

                choice = InputHelper.GetInt("Choice: ");

                switch (choice)
                {
                    case 1:
                        SubmitWellbeing(student, wellbeingService);
                        break;

                    case 2:
                        var reports = wellbeingService.GetReports(student);
                        if (!reports.Any()) Console.WriteLine("No reports found.");
                        else foreach (var r in reports) Console.WriteLine($"{r.Date:u}: Score={r.Score}, Notes={r.Notes}");
                        InputHelper.PressEnterToContinue();
                        break;

                    case 3:
                        BookMeetingStudent(student, studentService);
                        break;

                    case 4:
                        var meetings = meetingService.GetMeetings(student);
                        if (!meetings.Any()) Console.WriteLine("No meetings scheduled.");
                        else foreach (var m in meetings) Console.WriteLine($"{m.ScheduledTime:u}: Meeting with PS ID {m.PersonalSupervisorId}");
                        InputHelper.PressEnterToContinue();
                        break;

                    case 5:
                        StudentMessagingMenu(student, messageService, repo);
                        break;
                }

            } while (choice != 0);
        }

        private static void SubmitWellbeing(Student student, WellbeingService wellbeingService)
        {
            Console.WriteLine("=== Submit Wellbeing Report ===");
            int score = InputHelper.GetInt("Score (0-10): ");
            string notes = InputHelper.GetString("Notes: ");
            wellbeingService.SubmitReport(student, score, notes);
            Console.WriteLine("Report submitted.");
            InputHelper.PressEnterToContinue();
        }

        private static void BookMeetingStudent(Student student, StudentService studentService)
        {
            Console.WriteLine("--- Book Meeting ---");
            var dt = InputHelper.GetDateTime("Enter meeting date & time (yyyy-MM-dd HH:mm): ");
            try
            {
                studentService.BookMeeting(student, dt);
                Console.WriteLine("Meeting booked with your Personal Supervisor.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            InputHelper.PressEnterToContinue();
        }

        private static void StudentMessagingMenu(Student student, MessageService messageService, IDataRepository repo)
        {
            if (student.PersonalSupervisorId == 0)
            {
                Console.WriteLine("You do not have a personal supervisor assigned. Cannot message.");
                InputHelper.PressEnterToContinue();
                return;
            }

            var sup = repo.GetSupervisorById(student.PersonalSupervisorId);
            int choice;
            do
            {
                Console.Clear();
                int unread = messageService.GetUnreadCountForViewer(student.Id, student.PersonalSupervisorId, "student");
                Console.WriteLine($"--- Messaging with {(sup?.Name ?? "(unknown)")} ---");
                Console.WriteLine($"You have {unread} unread message(s).");
                Console.WriteLine("1. View conversation");
                Console.WriteLine("2. Send message");
                Console.WriteLine("0. Back");

                choice = InputHelper.GetInt("Choice: ");
                switch (choice)
                {
                    case 1:
                        var messages = messageService.GetConversation(student.Id, student.PersonalSupervisorId);
                        Console.WriteLine("=== Conversation ===");
                        if (!messages.Any()) Console.WriteLine("(No messages)");
                        else
                        {
                            foreach (var m in messages)
                            {
                                var sender = m.SenderRole == "student" ? "You" : (sup?.Name ?? "Supervisor");
                                Console.WriteLine($"[{m.Id}] {TimeHelpers.ToRelative(m.Timestamp)} - {sender}:");
                                ConsoleHelpers.WrapAndPrint(m.Content);
                                Console.WriteLine();
                            }
                        }

                        // mark other party's messages as read (Q2 A)
                        messageService.MarkConversationAsRead(student.Id, student.PersonalSupervisorId, "student");

                        // allow edit/delete for your own messages
                        Console.WriteLine();
                        Console.WriteLine("Options: (E)dit message by id, (D)elete message by id, (Enter) to return");
                        var opt = Console.ReadLine() ?? "";
                        if (!string.IsNullOrWhiteSpace(opt))
                        {
                            opt = opt.Trim().ToUpper();
                            if (opt == "E")
                            {
                                int mid = InputHelper.GetInt("Message Id to edit: ");
                                string newContent = InputHelper.GetString("New content: ");
                                try
                                {
                                    messageService.EditMessageAsStudent(student, mid, newContent);
                                    Console.WriteLine("Message edited.");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error: {ex.Message}");
                                }
                            }
                            else if (opt == "D")
                            {
                                int mid = InputHelper.GetInt("Message Id to delete: ");

                                // Confirm delete:
                                Console.Write("Are you sure you want to delete this message? (y/n): ");
                                var conf = (Console.ReadLine() ?? "").Trim().ToLower();
                                if (conf != "y")
                                {
                                    Console.WriteLine("Delete cancelled.");
                                    break;
                                }

                                try
                                {
                                    messageService.DeleteMessageAsStudent(student, mid);
                                    Console.WriteLine("Message deleted.");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error: {ex.Message}");
                                }
                            }

                        }

                        InputHelper.PressEnterToContinue();
                        break;

                    case 2:
                        Console.WriteLine("=== Send Message ===");
                        var content = InputHelper.GetString("Message: ");
                        try
                        {
                            messageService.SendMessageFromStudent(student, content);
                            Console.WriteLine("Message sent.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error: {ex.Message}");
                        }
                        InputHelper.PressEnterToContinue();
                        break;
                }

            } while (choice != 0);
        }
        #endregion

        #region Personal Supervisor Menu
        public static void PersonalSupervisorMenu(PersonalSupervisor ps, PersonalSupervisorService psService, MeetingService meetingService, WellbeingService wellbeingService, MessageService messageService, IDataRepository repo)
        {
            int choice;
            do
            {
                Console.Clear();
                Console.WriteLine($"--- Personal Supervisor Menu ({ps.Name}) ---");
                Console.WriteLine("1. View Supervisees");
                Console.WriteLine("2. Book Meeting with a Student");
                Console.WriteLine("3. View My Meetings");
                Console.WriteLine("4. Messaging");
                Console.WriteLine("0. Logout");

                choice = InputHelper.GetInt("Choice: ");

                switch (choice)
                {
                    case 1:
                        var supervisees = psService.GetSupervisees(ps);
                        if (!supervisees.Any()) Console.WriteLine("No supervisees assigned.");
                        else
                        {
                            Console.WriteLine("Supervisees:");
                            for (int i = 0; i < supervisees.Count; i++)
                            {
                                var s = supervisees[i];
                                int unread = messageService.GetUnreadCountForViewer(s.Id, ps.Id, "supervisor");
                                Console.WriteLine($"{s.Id}: {s.Name} ({s.Username}) - {unread} unread");
                            }
                        }
                        InputHelper.PressEnterToContinue();
                        break;

                    case 2:
                        BookMeetingSupervisor(ps, psService, repo);
                        break;

                    case 3:
                        var meetings = meetingService.GetMeetings(ps);
                        if (!meetings.Any()) Console.WriteLine("No meetings scheduled.");
                        else foreach (var m in meetings) Console.WriteLine($"{m.ScheduledTime:u}: Meeting with Student ID {m.StudentId}");
                        InputHelper.PressEnterToContinue();
                        break;

                    case 4:
                        SupervisorMessagingMenu(ps, psService, messageService);
                        break;
                }

            } while (choice != 0);
        }

        private static void BookMeetingSupervisor(PersonalSupervisor ps, PersonalSupervisorService psService, IDataRepository repo)
        {
            var students = psService.GetSupervisees(ps);
            if (!students.Any())
            {
                Console.WriteLine("No supervisees to book a meeting with.");
                InputHelper.PressEnterToContinue();
                return;
            }

            Console.WriteLine("Select a student:");
            for (int i = 0; i < students.Count; i++) Console.WriteLine($"{i + 1}. {students[i].Name}");

            int idx = InputHelper.GetInt("Choice: ") - 1;
            if (idx < 0 || idx >= students.Count)
            {
                Console.WriteLine("Invalid choice.");
                InputHelper.PressEnterToContinue();
                return;
            }

            DateTime dt = InputHelper.GetDateTime("Enter meeting date & time (yyyy-MM-dd HH:mm): ");
            psService.BookMeeting(ps, students[idx].Id, dt);
            Console.WriteLine("Meeting booked.");
            InputHelper.PressEnterToContinue();
        }

        private static void SupervisorMessagingMenu(PersonalSupervisor ps, PersonalSupervisorService psService, MessageService messageService)
        {
            var students = psService.GetSupervisees(ps);
            if (!students.Any())
            {
                Console.WriteLine("No supervisees.");
                InputHelper.PressEnterToContinue();
                return;
            }

            Console.WriteLine("Choose a student to message:");
            for (int i = 0; i < students.Count; i++) Console.WriteLine($"{i + 1}. {students[i].Name}");
            int idx = InputHelper.GetInt("Choice: ") - 1;
            if (idx < 0 || idx >= students.Count)
            {
                Console.WriteLine("Invalid choice.");
                InputHelper.PressEnterToContinue();
                return;
            }

            var student = students[idx];

            int choice;
            do
            {
                Console.Clear();
                int unread = messageService.GetUnreadCountForViewer(student.Id, ps.Id, "supervisor");
                Console.WriteLine($"--- Messaging with {student.Name} ---");
                Console.WriteLine($"{unread} unread message(s) from this student.");
                Console.WriteLine("1. View conversation");
                Console.WriteLine("2. Send message");
                Console.WriteLine("0. Back");

                choice = InputHelper.GetInt("Choice: ");
                switch (choice)
                {
                    case 1:
                        var messages = messageService.GetConversation(student.Id, ps.Id);
                        Console.WriteLine("=== Conversation ===");
                        if (!messages.Any()) Console.WriteLine("(No messages)");
                        else
                        {
                            foreach (var m in messages)
                            {
                                var sender = m.SenderRole == "supervisor" ? "You" : student.Name;
                                Console.WriteLine($"[{m.Id}] {TimeHelpers.ToRelative(m.Timestamp)} - {sender}:");
                                ConsoleHelpers.WrapAndPrint(m.Content);
                                Console.WriteLine();
                            }
                        }

                        // mark other party's messages as read (Q2 A)
                        messageService.MarkConversationAsRead(student.Id, ps.Id, "supervisor");

                        // allow edit/delete for your own messages
                        Console.WriteLine();
                        Console.WriteLine("Options: (E)dit message by id, (D)elete message by id, (Enter) to return");
                        var opt = Console.ReadLine() ?? "";
                        if (!string.IsNullOrWhiteSpace(opt))
                        {
                            opt = opt.Trim().ToUpper();
                            if (opt == "E")
                            {
                                int mid = InputHelper.GetInt("Message Id to edit: ");
                                string newContent = InputHelper.GetString("New content: ");
                                try
                                {
                                    messageService.EditMessageAsSupervisor(ps, mid, newContent);
                                    Console.WriteLine("Message edited.");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error: {ex.Message}");
                                }
                            }
                            else if (opt == "D")
                            {
                                int mid = InputHelper.GetInt("Message Id to delete: ");

                                // Confirm delete:
                                Console.Write("Are you sure you want to delete this message? (y/n): ");
                                var conf = (Console.ReadLine() ?? "").Trim().ToLower();
                                if (conf != "y")
                                {
                                    Console.WriteLine("Delete cancelled.");
                                    break;
                                }

                                try
                                {
                                    messageService.DeleteMessageAsSupervisor(ps, mid);
                                    Console.WriteLine("Message deleted.");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error: {ex.Message}");
                                }
                            }

                        }

                        InputHelper.PressEnterToContinue();
                        break;

                    case 2:
                        Console.WriteLine("=== Send Message ===");
                        var content = InputHelper.GetString("Message: ");
                        try
                        {
                            messageService.SendMessageFromSupervisor(ps, student.Id, content);
                            Console.WriteLine("Message sent.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error: {ex.Message}");
                        }
                        InputHelper.PressEnterToContinue();
                        break;
                }

            } while (choice != 0);
        }
        #endregion

        #region Senior Tutor Menu
        public static void SeniorTutorMenu(SeniorTutor st, SeniorTutorService stService, IDataRepository repo, MessageService messageService)
        {
            int choice;
            do
            {
                Console.Clear();
                Console.WriteLine($"--- Senior Tutor Menu ({st.Name}) ---");
                Console.WriteLine("1. Create Student");
                Console.WriteLine("2. Create Personal Supervisor");
                Console.WriteLine("3. Create Senior Tutor");
                Console.WriteLine("4. Assign Student -> Personal Supervisor");
                Console.WriteLine("5. List All Students");
                Console.WriteLine("6. List All Personal Supervisors");
                Console.WriteLine("7. View ALL Meetings & Reports");
                Console.WriteLine("8. Show database path");
                Console.WriteLine("9. View All Conversations");
                Console.WriteLine("0. Logout");

                choice = InputHelper.GetInt("Choice: ");

                switch (choice)
                {
                    case 1:
                        CreateStudent(stService);
                        break;
                    case 2:
                        CreatePersonalSupervisor(stService);
                        break;
                    case 3:
                        CreateSeniorTutor(stService);
                        break;
                    case 4:
                        AssignStudentToSupervisor(stService);
                        break;
                    case 5:
                        ListAllStudents(stService, repo);
                        break;
                    case 6:
                        ListAllPersonalSupervisors(stService);
                        break;
                    case 7:
                        ViewAllMeetingsAndReports(stService, repo);
                        break;
                    case 8:
                        Console.WriteLine($"Database path: {(repo is DDD.Data.SqliteRepository s ? s.DatabasePath : "unknown")}");
                        InputHelper.PressEnterToContinue();
                        break;
                    case 9:
                        SeniorTutorViewAllConversations(stService, messageService);
                        break;
                }

            } while (choice != 0);
        }

        private static void SeniorTutorViewAllConversations(SeniorTutorService stService, MessageService messageService)
        {
            int choice;
            do
            {
                Console.Clear();
                Console.WriteLine("--- View All Conversations ---");
                Console.WriteLine("1. View by Student");
                Console.WriteLine("2. View by Personal Supervisor");
                Console.WriteLine("0. Back");
                choice = InputHelper.GetInt("Choice: ");
                switch (choice)
                {
                    case 1:
                        var students = stService.GetAllStudents();
                        if (!students.Any()) { Console.WriteLine("No students."); InputHelper.PressEnterToContinue(); break; }
                        Console.WriteLine("Select student:");
                        foreach (var s in students) Console.WriteLine($"{s.Id}) {s.Name} ({s.Username})");
                        int sid = InputHelper.GetInt("Student Id: ");
                        var student = students.FirstOrDefault(x => x.Id == sid);
                        if (student == null) { Console.WriteLine("Invalid student."); InputHelper.PressEnterToContinue(); break; }
                        if (student.PersonalSupervisorId == 0) { Console.WriteLine("Student has no supervisor assigned."); InputHelper.PressEnterToContinue(); break; }
                        var conv = messageService.GetConversation(student.Id, student.PersonalSupervisorId);
                        Console.WriteLine($"=== Conversation: {student.Name} <-> SupervisorId {student.PersonalSupervisorId} ===");
                        if (!conv.Any()) Console.WriteLine("(No messages)");
                        else
                        {
                            foreach (var m in conv)
                            {
                                var who = m.SenderRole == "student" ? student.Name : $"Supervisor #{m.PersonalSupervisorId}";
                                Console.WriteLine($"[{m.Id}] {TimeHelpers.ToRelative(m.Timestamp)} - {who}:");
                                ConsoleHelpers.WrapAndPrint(m.Content);
                                Console.WriteLine();
                            }
                        }
                        InputHelper.PressEnterToContinue();
                        break;

                    case 2:
                        var sups = stService.GetAllPersonalSupervisors();
                        if (!sups.Any()) { Console.WriteLine("No supervisors."); InputHelper.PressEnterToContinue(); break; }
                        Console.WriteLine("Select supervisor:");
                        foreach (var sp in sups) Console.WriteLine($"{sp.Id}) {sp.Name} ({sp.Username})");
                        int pid = InputHelper.GetInt("Supervisor Id: ");
                        var sup = sups.FirstOrDefault(x => x.Id == pid);
                        if (sup == null) { Console.WriteLine("Invalid supervisor."); InputHelper.PressEnterToContinue(); break; }
                        var supervisees = stService.GetAllStudents().Where(s => s.PersonalSupervisorId == sup.Id).ToList();
                        if (!supervisees.Any()) { Console.WriteLine("This supervisor has no supervisees."); InputHelper.PressEnterToContinue(); break; }
                        Console.WriteLine("Select supervisee:");
                        foreach (var s2 in supervisees) Console.WriteLine($"{s2.Id}) {s2.Name} ({s2.Username})");
                        int sid2 = InputHelper.GetInt("Student Id: ");
                        var selected = supervisees.FirstOrDefault(x => x.Id == sid2);
                        if (selected == null) { Console.WriteLine("Invalid student."); InputHelper.PressEnterToContinue(); break; }
                        var conv2 = messageService.GetConversation(selected.Id, sup.Id);
                        Console.WriteLine($"=== Conversation: {selected.Name} <-> {sup.Name} ===");
                        if (!conv2.Any()) Console.WriteLine("(No messages)");
                        else
                        {
                            foreach (var m in conv2)
                            {
                                var who = m.SenderRole == "student" ? selected.Name : sup.Name;
                                Console.WriteLine($"[{m.Id}] {TimeHelpers.ToRelative(m.Timestamp)} - {who}:");
                                ConsoleHelpers.WrapAndPrint(m.Content);
                                Console.WriteLine();
                            }
                        }
                        InputHelper.PressEnterToContinue();
                        break;
                }
            } while (choice != 0);
        }
        #endregion

        #region Senior Tutor helpers (existing methods reused)
        private static void CreateStudent(SeniorTutorService stService)
        {
            Console.Write("New student's username: ");
            var u = Console.ReadLine() ?? "";
            Console.Write("Name: ");
            var n = Console.ReadLine() ?? "";
            Console.Write("Password: ");
            var p = Console.ReadLine() ?? "";

            var s = stService.CreateStudent(u, n, p);
            Console.WriteLine($"Student created with Id {s.Id}");
            InputHelper.PressEnterToContinue();
        }

        private static void CreatePersonalSupervisor(SeniorTutorService stService)
        {
            Console.Write("Supervisor username: ");
            var u = Console.ReadLine() ?? "";
            Console.Write("Name: ");
            var n = Console.ReadLine() ?? "";
            Console.Write("Password: ");
            var p = Console.ReadLine() ?? "";

            var sup = stService.CreatePersonalSupervisor(u, n, p);
            Console.WriteLine($"Supervisor created with Id {sup.Id}");
            InputHelper.PressEnterToContinue();
        }

        private static void CreateSeniorTutor(SeniorTutorService stService)
        {
            Console.Write("Senior tutor username: ");
            var u = Console.ReadLine() ?? "";
            Console.Write("Name: ");
            var n = Console.ReadLine() ?? "";
            Console.Write("Password: ");
            var p = Console.ReadLine() ?? "";

            var st = stService.CreateSeniorTutor(u, n, p);
            Console.WriteLine($"Senior tutor created with Id {st.Id}");
            InputHelper.PressEnterToContinue();
        }

        private static void AssignStudentToSupervisor(SeniorTutorService stService)
        {
            var students = stService.GetAllStudents();
            var sups = stService.GetAllPersonalSupervisors();
            if (!students.Any()) { Console.WriteLine("No students."); InputHelper.PressEnterToContinue(); return; }
            if (!sups.Any()) { Console.WriteLine("No supervisors."); InputHelper.PressEnterToContinue(); return; }

            Console.WriteLine("Students:");
            foreach (var s in students) Console.WriteLine($"{s.Id}) {s.Username} - {s.Name} (SupervisorId: {s.PersonalSupervisorId})");
            Console.Write("Choose student Id: ");
            if (!int.TryParse(Console.ReadLine(), out var sid)) { Console.WriteLine("Invalid"); InputHelper.PressEnterToContinue(); return; }

            Console.WriteLine("Supervisors:");
            foreach (var sp in sups) Console.WriteLine($"{sp.Id}) {sp.Username} - {sp.Name}");
            Console.Write("Choose supervisor Id: ");
            if (!int.TryParse(Console.ReadLine(), out var pid)) { Console.WriteLine("Invalid"); InputHelper.PressEnterToContinue(); return; }

            try
            {
                stService.AssignStudentToSupervisor(sid, pid);
                Console.WriteLine("Assignment saved.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            InputHelper.PressEnterToContinue();
        }

        private static void ListAllStudents(SeniorTutorService stService, IDataRepository repo)
        {
            var students = stService.GetAllStudents();
            Console.WriteLine("All students:");

            foreach (var s in students)
            {
                var supervisorName = s.PersonalSupervisorId == 0
                    ? "(none)"
                    : (repo.GetSupervisorById(s.PersonalSupervisorId)?.Name ?? "(unknown)");

                Console.WriteLine(
                    $"{s.Id}) {s.Username} - {s.Name} - Supervisor: {supervisorName} - Meetings: {s.Meetings.Count} - Reports: {s.Reports.Count}"
                );
            }

            InputHelper.PressEnterToContinue();
        }

        private static void ListAllPersonalSupervisors(SeniorTutorService stService)
        {
            var list = stService.GetAllPersonalSupervisors();
            Console.WriteLine("All personal supervisors:");
            foreach (var p in list) Console.WriteLine($"{p.Id}) {p.Username} - {p.Name} - Meetings: {p.Meetings.Count}");
            InputHelper.PressEnterToContinue();
        }

        private static void ViewAllMeetingsAndReports(SeniorTutorService stService, IDataRepository repo)
        {
            Console.WriteLine("=== All Students Meetings & Reports ===");
            foreach (var s in stService.GetAllStudents())
            {
                var supName = s.PersonalSupervisorId == 0
                    ? "(none)"
                    : (repo.GetSupervisorById(s.PersonalSupervisorId)?.Name ?? "(unknown)");

                Console.WriteLine($"Student {s.Id} {s.Name} (Supervisor: {supName})");

                if (!s.Meetings.Any())
                {
                    Console.WriteLine("  Meetings: (none)");
                }
                else
                {
                    foreach (var m in s.Meetings)
                    {
                        Console.WriteLine($"  Meeting {m.Id} at {m.ScheduledTime:u} with supervisorId {m.PersonalSupervisorId}");
                    }
                }

                if (!s.Reports.Any())
                {
                    Console.WriteLine("  Reports: (none)");
                }
                else
                {
                    foreach (var r in s.Reports)
                    {
                        Console.WriteLine($"  Report {r.Id} Score:{r.Score} Date:{r.Date:u} Notes:{r.Notes}");
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine("=== All Supervisors Meetings ===");
            foreach (var sup in stService.GetAllPersonalSupervisors())
            {
                Console.WriteLine($"Supervisor {sup.Id} {sup.Name}");
                if (!sup.Meetings.Any())
                {
                    Console.WriteLine("  Meetings: (none)");
                }
                else
                {
                    foreach (var m in sup.Meetings)
                    {
                        Console.WriteLine($"  Meeting {m.Id} at {m.ScheduledTime:u} studentId {m.StudentId}");
                    }
                }
            }

            InputHelper.PressEnterToContinue();
        }
        #endregion
    }
}
