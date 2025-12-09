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
        public static void StudentMenu(Student student, StudentService studentService, MeetingService meetingService,
            WellbeingService wellbeingService, MessageService messageService, IDataRepository repo,
            PasswordResetService passwordResetService)
        {
            // Check wellbeing report status on login
            var wellbeingStatus = wellbeingService.GetWellbeingReportStatus(student);
            if (wellbeingStatus.isOverdue)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n⚠ WELLBEING REPORT OVERDUE: {wellbeingStatus.daysOverdue} days overdue!");
                Console.ResetColor();
                InputHelper.PressEnterToContinue();
            }
            else if (wellbeingStatus.timeUntil.TotalDays < 2)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n⚠ Wellbeing report due soon: {wellbeingStatus.timeUntil.Days}d {wellbeingStatus.timeUntil.Hours}h remaining");
                Console.ResetColor();
                InputHelper.PressEnterToContinue();
            }

            int choice;
            do
            {
                Console.Clear();

                // Show wellbeing report status
                wellbeingStatus = wellbeingService.GetWellbeingReportStatus(student);
                Console.WriteLine($"=== Student Menu ({student.Name}) ===");
                if (wellbeingStatus.isOverdue)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"⚠ Wellbeing Report: {wellbeingStatus.daysOverdue} days OVERDUE!");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine($"Next wellbeing report due in: {wellbeingStatus.timeUntil.Days}d {wellbeingStatus.timeUntil.Hours}h");
                }
                Console.WriteLine("================================");

                Console.WriteLine("1. Submit Wellbeing Report");
                Console.WriteLine("2. View My Reports");
                Console.WriteLine("3. Book Meeting with Personal Supervisor");
                Console.WriteLine("4. View My Meetings");
                Console.WriteLine("5. Messaging");
                Console.WriteLine("6. Password & Security Settings");
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
                        else
                        {
                            foreach (var r in reports)
                            {
                                var priority = r.IsHighPriority ? " ⚠ HIGH PRIORITY" : "";
                                Console.WriteLine($"{r.Date:u}: Score={r.Score}, Notes={r.Notes}{priority}");
                            }
                        }
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

                    case 6:
                        StudentPasswordMenu(student, passwordResetService);
                        break;
                }

            } while (choice != 0);
        }

        private static void StudentPasswordMenu(Student student, PasswordResetService passwordResetService)
        {
            int choice;
            do
            {
                Console.Clear();
                Console.WriteLine($"=== Password & Security Settings ===");
                Console.WriteLine($"User: {student.Username}");
                Console.WriteLine("1. Change Password");
                Console.WriteLine("2. Set Security Question");
                Console.WriteLine("0. Back");

                choice = InputHelper.GetInt("Choice: ");

                switch (choice)
                {
                    case 1:
                        Console.WriteLine("\n=== Change Password ===");
                        var current = InputHelper.GetString("Current password: ");
                        var newPass = InputHelper.GetString("New password: ");
                        var confirm = InputHelper.GetString("Confirm new password: ");

                        if (newPass != confirm)
                        {
                            Console.WriteLine("Passwords don't match.");
                        }
                        else if (passwordResetService.ChangePassword(student.Username, current, newPass))
                        {
                            Console.WriteLine("Password changed successfully.");
                            student.Password = newPass;
                        }
                        else
                        {
                            Console.WriteLine("Failed to change password. Current password may be incorrect.");
                        }
                        InputHelper.PressEnterToContinue();
                        break;

                    case 2:
                        Console.WriteLine("\n=== Set Security Question ===");
                        var question = InputHelper.GetString("Security question (e.g., 'What is your mother's maiden name?'): ");
                        var answer = InputHelper.GetString("Answer: ");
                        var confirmAnswer = InputHelper.GetString("Confirm answer: ");

                        if (answer != confirmAnswer)
                        {
                            Console.WriteLine("Answers don't match.");
                        }
                        else if (passwordResetService.SetSecurityQuestion(student.Username, question, answer))
                        {
                            Console.WriteLine("Security question set successfully.");
                        }
                        else
                        {
                            Console.WriteLine("Failed to set security question.");
                        }
                        InputHelper.PressEnterToContinue();
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

            if (score < 5)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠ Note: Your score indicates you may need support. Your senior tutor has been alerted.");
                Console.ResetColor();
            }

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

                        messageService.MarkConversationAsRead(student.Id, student.PersonalSupervisorId, "student");

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
        public static void PersonalSupervisorMenu(PersonalSupervisor ps, PersonalSupervisorService psService,
            MeetingService meetingService, WellbeingService wellbeingService, MessageService messageService,
            IDataRepository repo, PasswordResetService passwordResetService)
        {
            int choice;
            do
            {
                Console.Clear();
                Console.WriteLine($"=== Personal Supervisor Menu ({ps.Name}) ===");
                Console.WriteLine("1. View Supervisees");
                Console.WriteLine("2. View Supervisees' Wellbeing Reports");
                Console.WriteLine("3. Book Meeting with a Student");
                Console.WriteLine("4. View My Meetings");
                Console.WriteLine("5. Messaging");
                Console.WriteLine("6. Password & Security Settings");
                Console.WriteLine("0. Logout");

                choice = InputHelper.GetInt("Choice: ");

                switch (choice)
                {
                    case 1:
                        ViewSupervisees(ps, psService, wellbeingService, messageService);
                        break;

                    case 2:
                        ViewSuperviseesWellbeingReports(ps, psService, repo);
                        break;

                    case 3:
                        BookMeetingSupervisor(ps, psService, repo);
                        break;

                    case 4:
                        var meetings = meetingService.GetMeetings(ps);
                        if (!meetings.Any()) Console.WriteLine("No meetings scheduled.");
                        else foreach (var m in meetings) Console.WriteLine($"{m.ScheduledTime:u}: Meeting with Student ID {m.StudentId}");
                        InputHelper.PressEnterToContinue();
                        break;

                    case 5:
                        SupervisorMessagingMenu(ps, psService, messageService);
                        break;

                    case 6:
                        SupervisorPasswordMenu(ps, passwordResetService);
                        break;
                }

            } while (choice != 0);
        }

        private static void ViewSupervisees(PersonalSupervisor ps, PersonalSupervisorService psService, WellbeingService wellbeingService, MessageService messageService)
        {
            var supervisees = psService.GetSupervisees(ps);
            if (!supervisees.Any())
            {
                Console.WriteLine("No supervisees assigned.");
                InputHelper.PressEnterToContinue();
                return;
            }

            Console.WriteLine("=== My Supervisees ===");
            for (int i = 0; i < supervisees.Count; i++)
            {
                var s = supervisees[i];
                int unread = messageService.GetUnreadCountForViewer(s.Id, ps.Id, "supervisor");
                var wellbeingStatus = wellbeingService.GetWellbeingReportStatus(s);
                var status = wellbeingStatus.isOverdue ? $" ⚠ {wellbeingStatus.daysOverdue}d overdue" : "";
                var missedStatus = s.HasMissedWellbeingReport ? " ⚠ MISSED REPORT" : "";
                var lowScoreStatus = s.Reports?.Any(r => r.IsHighPriority) == true ? " ⚠ LOW SCORE" : "";

                Console.WriteLine($"{s.Id}: {s.Name} ({s.Username}) - {unread} unread{status}{missedStatus}{lowScoreStatus}");
            }
            InputHelper.PressEnterToContinue();
        }

        private static void ViewSuperviseesWellbeingReports(PersonalSupervisor ps, PersonalSupervisorService psService, IDataRepository repo)
        {
            var supervisees = psService.GetSupervisees(ps);
            if (!supervisees.Any())
            {
                Console.WriteLine("No supervisees assigned.");
                InputHelper.PressEnterToContinue();
                return;
            }

            Console.WriteLine("=== Supervisees' Wellbeing Reports ===");
            Console.WriteLine("Select a student to view their reports:");

            for (int i = 0; i < supervisees.Count; i++)
            {
                var s = supervisees[i];
                var latestReport = s.Reports?.OrderByDescending(r => r.Date).FirstOrDefault();
                var latestScore = latestReport != null ? $"Latest: {latestReport.Score}/10" : "No reports";
                var overdue = s.HasMissedWellbeingReport ? " ⚠ MISSED" : "";
                Console.WriteLine($"{i + 1}. {s.Name} - {latestScore}{overdue}");
            }

            int idx = InputHelper.GetInt("Choice (0 to cancel): ") - 1;
            if (idx < 0 || idx >= supervisees.Count)
            {
                Console.WriteLine("Cancelled or invalid choice.");
                InputHelper.PressEnterToContinue();
                return;
            }

            var selectedStudent = supervisees[idx];

            Console.WriteLine($"\n=== Wellbeing Reports for {selectedStudent.Name} ===");

            var reports = selectedStudent.Reports?.OrderByDescending(r => r.Date).ToList();
            if (reports == null || !reports.Any())
            {
                Console.WriteLine("No wellbeing reports submitted.");
            }
            else
            {
                foreach (var report in reports)
                {
                    var priority = report.IsHighPriority ? " ⚠ HIGH PRIORITY" : "";
                    Console.WriteLine($"Date: {report.Date:yyyy-MM-dd}");
                    Console.WriteLine($"Score: {report.Score}/10{priority}");
                    Console.WriteLine($"Notes: {report.Notes}");
                    Console.WriteLine("---");
                }

                // Show statistics
                var averageScore = reports.Average(r => r.Score);
                var lowCount = reports.Count(r => r.Score < 5);
                Console.WriteLine($"\nStatistics:");
                Console.WriteLine($"Average score: {averageScore:F1}/10");
                Console.WriteLine($"Low scores (<5): {lowCount}");
                Console.WriteLine($"Total reports: {reports.Count}");
            }

            InputHelper.PressEnterToContinue();
        }

        private static void SupervisorPasswordMenu(PersonalSupervisor ps, PasswordResetService passwordResetService)
        {
            int choice;
            do
            {
                Console.Clear();
                Console.WriteLine($"=== Password & Security Settings ===");
                Console.WriteLine($"User: {ps.Username}");
                Console.WriteLine("1. Change Password");
                Console.WriteLine("2. Set Security Question");
                Console.WriteLine("0. Back");

                choice = InputHelper.GetInt("Choice: ");

                switch (choice)
                {
                    case 1:
                        Console.WriteLine("\n=== Change Password ===");
                        var current = InputHelper.GetString("Current password: ");
                        var newPass = InputHelper.GetString("New password: ");
                        var confirm = InputHelper.GetString("Confirm new password: ");

                        if (newPass != confirm)
                        {
                            Console.WriteLine("Passwords don't match.");
                        }
                        else if (passwordResetService.ChangePassword(ps.Username, current, newPass))
                        {
                            Console.WriteLine("Password changed successfully.");
                            ps.Password = newPass;
                        }
                        else
                        {
                            Console.WriteLine("Failed to change password. Current password may be incorrect.");
                        }
                        InputHelper.PressEnterToContinue();
                        break;

                    case 2:
                        Console.WriteLine("\n=== Set Security Question ===");
                        var question = InputHelper.GetString("Security question (e.g., 'What is your mother's maiden name?'): ");
                        var answer = InputHelper.GetString("Answer: ");
                        var confirmAnswer = InputHelper.GetString("Confirm answer: ");

                        if (answer != confirmAnswer)
                        {
                            Console.WriteLine("Answers don't match.");
                        }
                        else if (passwordResetService.SetSecurityQuestion(ps.Username, question, answer))
                        {
                            Console.WriteLine("Security question set successfully.");
                        }
                        else
                        {
                            Console.WriteLine("Failed to set security question.");
                        }
                        InputHelper.PressEnterToContinue();
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

                        messageService.MarkConversationAsRead(student.Id, ps.Id, "supervisor");

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
        public static void SeniorTutorMenu(SeniorTutor st, SeniorTutorService stService, IDataRepository repo,
            MessageService messageService, WellbeingService wellbeingService, PasswordResetService passwordResetService)
        {
            // Check for alerts on login
            var alerts = wellbeingService.GetActiveAlerts();
            if (alerts.Any())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n⚠ {alerts.Count} ACTIVE WELLBEING ALERT(S)!");
                Console.ResetColor();
                InputHelper.PressEnterToContinue();
            }

            int choice;
            do
            {
                Console.Clear();
                Console.WriteLine($"=== Senior Tutor Menu ({st.Name}) ===");

                // Show alert count
                alerts = wellbeingService.GetActiveAlerts();
                if (alerts.Any())
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"⚠ {alerts.Count} active wellbeing alert(s)");
                    Console.ResetColor();
                }

                Console.WriteLine("1. Create Student");
                Console.WriteLine("2. Create Personal Supervisor");
                Console.WriteLine("3. Create Senior Tutor");
                Console.WriteLine("4. Assign Student -> Personal Supervisor");
                Console.WriteLine("5. List All Students");
                Console.WriteLine("6. List All Personal Supervisors");
                Console.WriteLine("7. View ALL Meetings & Reports");
                Console.WriteLine("8. Show database path");
                Console.WriteLine("9. View All Conversations");
                Console.WriteLine("10. View High Priority Students");
                Console.WriteLine("11. View Active Wellbeing Alerts");
                Console.WriteLine("12. Password & Security Settings");
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
                        ListAllStudents(stService, repo, wellbeingService);
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
                    case 10:
                        ViewHighPriorityStudents(wellbeingService, repo);
                        break;
                    case 11:
                        ViewActiveAlerts(wellbeingService, repo);
                        break;
                    case 12:
                        SeniorTutorPasswordMenu(st, passwordResetService);
                        break;
                }

            } while (choice != 0);
        }

        private static void SeniorTutorPasswordMenu(SeniorTutor st, PasswordResetService passwordResetService)
        {
            int choice;
            do
            {
                Console.Clear();
                Console.WriteLine($"=== Password & Security Settings ===");
                Console.WriteLine($"User: {st.Username}");
                Console.WriteLine("1. Change Password");
                Console.WriteLine("2. Set Security Question");
                Console.WriteLine("0. Back");

                choice = InputHelper.GetInt("Choice: ");

                switch (choice)
                {
                    case 1:
                        Console.WriteLine("\n=== Change Password ===");
                        var current = InputHelper.GetString("Current password: ");
                        var newPass = InputHelper.GetString("New password: ");
                        var confirm = InputHelper.GetString("Confirm new password: ");

                        if (newPass != confirm)
                        {
                            Console.WriteLine("Passwords don't match.");
                        }
                        else if (passwordResetService.ChangePassword(st.Username, current, newPass))
                        {
                            Console.WriteLine("Password changed successfully.");
                            st.Password = newPass;
                        }
                        else
                        {
                            Console.WriteLine("Failed to change password. Current password may be incorrect.");
                        }
                        InputHelper.PressEnterToContinue();
                        break;

                    case 2:
                        Console.WriteLine("\n=== Set Security Question ===");
                        var question = InputHelper.GetString("Security question (e.g., 'What is your mother's maiden name?'): ");
                        var answer = InputHelper.GetString("Answer: ");
                        var confirmAnswer = InputHelper.GetString("Confirm answer: ");

                        if (answer != confirmAnswer)
                        {
                            Console.WriteLine("Answers don't match.");
                        }
                        else if (passwordResetService.SetSecurityQuestion(st.Username, question, answer))
                        {
                            Console.WriteLine("Security question set successfully.");
                        }
                        else
                        {
                            Console.WriteLine("Failed to set security question.");
                        }
                        InputHelper.PressEnterToContinue();
                        break;
                }
            } while (choice != 0);
        }

        private static void ViewHighPriorityStudents(WellbeingService wellbeingService, IDataRepository repo)
        {
            Console.Clear();
            Console.WriteLine("=== High Priority Students ===");
            Console.WriteLine("Students with wellbeing scores under 5:");

            var highPriorityStudents = wellbeingService.GetHighPriorityStudents();
            var missedReportStudents = wellbeingService.GetStudentsWithMissedReports();

            if (!highPriorityStudents.Any() && !missedReportStudents.Any())
            {
                Console.WriteLine("No high priority students at this time.");
            }
            else
            {
                if (highPriorityStudents.Any())
                {
                    Console.WriteLine("\n🔴 LOW WELLBEING SCORES:");
                    foreach (var student in highPriorityStudents)
                    {
                        var latestReport = student.Reports?.OrderByDescending(r => r.Date).FirstOrDefault();
                        var supervisorName = student.PersonalSupervisorId == 0
                            ? "(none)"
                            : (repo.GetSupervisorById(student.PersonalSupervisorId)?.Name ?? "(unknown)");

                        Console.WriteLine($"  Student: {student.Name} (ID: {student.Id})");
                        Console.WriteLine($"  Latest score: {latestReport?.Score ?? 0}");
                        Console.WriteLine($"  Supervisor: {supervisorName}");
                        Console.WriteLine($"  Last report: {student.LastWellbeingReportDate:yyyy-MM-dd}");
                        Console.WriteLine();
                    }
                }

                if (missedReportStudents.Any())
                {
                    Console.WriteLine("\n⚠ MISSED WELLBEING REPORTS:");
                    foreach (var student in missedReportStudents)
                    {
                        var supervisorName = student.PersonalSupervisorId == 0
                            ? "(none)"
                            : (repo.GetSupervisorById(student.PersonalSupervisorId)?.Name ?? "(unknown)");

                        Console.WriteLine($"  Student: {student.Name} (ID: {student.Id})");
                        Console.WriteLine($"  Missed reports: {student.MissedReportCount}");
                        Console.WriteLine($"  Supervisor: {supervisorName}");
                        Console.WriteLine($"  Last report: {student.LastWellbeingReportDate:yyyy-MM-dd}");
                        Console.WriteLine();
                    }
                }
            }

            InputHelper.PressEnterToContinue();
        }

        private static void ViewActiveAlerts(WellbeingService wellbeingService, IDataRepository repo)
        {
            Console.Clear();
            Console.WriteLine("=== Active Wellbeing Alerts ===");

            var alerts = wellbeingService.GetActiveAlerts();

            if (!alerts.Any())
            {
                Console.WriteLine("No active alerts.");
            }
            else
            {
                foreach (var alert in alerts)
                {
                    Console.WriteLine($"\nAlert ID: {alert.Id}");
                    Console.WriteLine($"Student: {alert.StudentName} (ID: {alert.StudentId})");
                    Console.WriteLine($"Reason: {alert.Reason}");
                    Console.WriteLine($"Date: {alert.AlertDate:yyyy-MM-dd HH:mm}");

                    if (alert.Reason == "low_score")
                    {
                        Console.WriteLine("Type: Low wellbeing score (<5)");
                    }
                    else if (alert.Reason == "missed_report")
                    {
                        Console.WriteLine("Type: Missed wellbeing report");
                    }

                    Console.WriteLine("\nOptions: (R)esolve alert, (Enter) to continue");
                    var opt = Console.ReadLine()?.Trim().ToUpper();

                    if (opt == "R")
                    {
                        wellbeingService.ResolveAlert(alert.Id);
                        Console.WriteLine("Alert resolved.");
                    }
                }
            }

            InputHelper.PressEnterToContinue();
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

        #region Senior Tutor helpers

        private static void CreateStudent(SeniorTutorService stService)
        {
            Console.Write("New student's username: ");
            var u = Console.ReadLine() ?? "";

            Console.Write("Name: ");
            var n = Console.ReadLine() ?? "";

            Console.Write("Password: ");
            var p = Console.ReadLine() ?? "";

            Console.WriteLine("Select year group:");
            Console.WriteLine("1. First year");
            Console.WriteLine("2. Second year");
            Console.WriteLine("3. Third year");
            Console.WriteLine("4. Fourth year");
            int yearGroup = InputHelper.GetInt("Year group (1–4): ");
            if (yearGroup < 1 || yearGroup > 4)
            {
                Console.WriteLine("Invalid year group. Cancelling.");
                InputHelper.PressEnterToContinue();
                return;
            }

            Console.Write("Security question (optional, for password recovery): ");
            var sq = Console.ReadLine() ?? "";

            Console.Write("Security answer (optional): ");
            var sa = Console.ReadLine() ?? "";

            var s = stService.CreateStudent(u, n, p, yearGroup, sq, sa);
            Console.WriteLine($"Student created with Id {s.Id} (External StudentID: {s.StudentCode})");
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

            Console.Write("Security question (optional, for password recovery): ");
            var sq = Console.ReadLine() ?? "";

            Console.Write("Security answer (optional): ");
            var sa = Console.ReadLine() ?? "";

            var sup = stService.CreatePersonalSupervisor(u, n, p, sq, sa);
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

            Console.Write("Security question (optional, for password recovery): ");
            var sq = Console.ReadLine() ?? "";

            Console.Write("Security answer (optional): ");
            var sa = Console.ReadLine() ?? "";

            var st = stService.CreateSeniorTutor(u, n, p, sq, sa);
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

        private static void ListAllStudents(SeniorTutorService stService, IDataRepository repo, WellbeingService wellbeingService)
        {
            var students = stService.GetAllStudents();
            Console.WriteLine("All students:");

            foreach (var s in students)
            {
                var supervisorName = s.PersonalSupervisorId == 0
                    ? "(none)"
                    : (repo.GetSupervisorById(s.PersonalSupervisorId)?.Name ?? "(unknown)");

                var wellbeingStatus = wellbeingService.GetWellbeingReportStatus(s);
                var status = wellbeingStatus.isOverdue ? $" ⚠ {wellbeingStatus.daysOverdue}d overdue" : "";

                var missedStatus = s.HasMissedWellbeingReport ? " ⚠ MISSED REPORT" : "";
                var lowScoreStatus = s.Reports?.Any(r => r.IsHighPriority) == true ? " ⚠ LOW SCORE" : "";

                Console.WriteLine(
                    $"{s.Id}) {s.Username} - {s.Name} - Supervisor: {supervisorName}{status}{missedStatus}{lowScoreStatus}"
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
                        var priority = r.IsHighPriority ? " ⚠ HIGH PRIORITY" : "";
                        Console.WriteLine($"  Report {r.Id} Score:{r.Score} Date:{r.Date:u} Notes:{r.Notes}{priority}");
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