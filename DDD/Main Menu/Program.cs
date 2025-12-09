using System;
using DDD.Data;
using DDD.Services;
using DDD.Models;
using DDD.Utils;

namespace DDD
{
    class Program
    {
        static void Main(string[] args)
        {
            // Ensure SQLite provider package is installed:
            // dotnet add package Microsoft.Data.Sqlite

            // initialize repository
            using var repo = new SqliteRepository("ddd.db");

            // services
            var authService = new AuthService(repo);
            var meetingService = new MeetingService(repo);
            var wellbeingService = new WellbeingService(repo);
            var studentService = new StudentService(repo);
            var psService = new PersonalSupervisorService(repo);
            var stService = new SeniorTutorService(repo);
            var messageService = new MessageService(repo);
            var passwordResetService = new PasswordResetService(repo);

            // Check for missed wellbeing reports on startup
            wellbeingService.CheckAndUpdateMissedReports();

            // Seed a senior tutor account if none exist
            if (repo.GetAllSeniorTutors().Count == 0)
            {
                var seed = new SeniorTutor
                {
                    Username = "admin",
                    Name = "Senior Admin",
                    Password = "admin123",
                    SecurityQuestion = "What is your favorite color?",
                    SecurityAnswer = "blue"
                };
                repo.SaveSeniorTutor(seed);
                Console.WriteLine($"Seeded Senior Tutor: username 'admin' / password 'admin123' (id {seed.Id})");
            }

            // Add password reset option to login screen
            bool exit = false;
            while (!exit)
            {
                Console.Clear();
                Console.WriteLine("=== Personal Supervisor System ===");
                Console.WriteLine("1. Student Login");
                Console.WriteLine("2. Personal Supervisor Login");
                Console.WriteLine("3. Senior Tutor Login");
                Console.WriteLine("4. Forgot Password");
                Console.WriteLine("0. Exit");

                int choice = InputHelper.GetInt("\nSelect an option: ");

                try
                {
                    switch (choice)
                    {
                        case 1:
                            // Student
                            Console.Clear();
                            Console.WriteLine("=== Student Login ===");
                            var sUser = InputHelper.GetString("Enter username: ");
                            var sPass = InputHelper.GetString("Enter password: ");
                            var student = authService.LoginStudent(sUser, sPass);
                            Console.WriteLine($"\n✔ Welcome {student.Name}!");
                            InputHelper.PressEnterToContinue();
                            MenuHelper.StudentMenu(student, studentService, meetingService, wellbeingService, messageService, repo, passwordResetService);
                            break;

                        case 2:
                            // Personal Supervisor
                            Console.Clear();
                            Console.WriteLine("=== Personal Supervisor Login ===");
                            var psUser = InputHelper.GetString("Enter username: ");
                            var psPass = InputHelper.GetString("Enter password: ");
                            var ps = authService.LoginSupervisor(psUser, psPass);
                            Console.WriteLine($"\n✔ Welcome {ps.Name}!");
                            InputHelper.PressEnterToContinue();
                            MenuHelper.PersonalSupervisorMenu(ps, psService, meetingService, wellbeingService, messageService, repo, passwordResetService);
                            break;

                        case 3:
                            // Senior tutor
                            Console.Clear();
                            Console.WriteLine("=== Senior Tutor Login ===");
                            var stUser = InputHelper.GetString("Enter username: ");
                            var stPass = InputHelper.GetString("Enter password: ");
                            var st = authService.LoginSeniorTutor(stUser, stPass);
                            Console.WriteLine($"\n✔ Welcome {st.Name}!");
                            InputHelper.PressEnterToContinue();
                            MenuHelper.SeniorTutorMenu(st, stService, repo, messageService, wellbeingService, passwordResetService);
                            break;

                        case 4:
                            ForgotPasswordMenu(passwordResetService);
                            break;

                        case 0:
                            exit = true;
                            break;

                        default:
                            Console.WriteLine("Invalid choice. Press ENTER to continue.");
                            Console.ReadLine();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nError: {ex.Message}");
                    InputHelper.PressEnterToContinue();
                }
            }
        }

        private static void ForgotPasswordMenu(PasswordResetService passwordResetService)
        {
            Console.Clear();
            Console.WriteLine("=== Password Recovery ===");

            var username = InputHelper.GetString("Enter your username: ");
            var securityQuestion = passwordResetService.GetSecurityQuestion(username);

            if (string.IsNullOrEmpty(securityQuestion))
            {
                Console.WriteLine("No security question set for this user. Please contact your senior tutor.");
                InputHelper.PressEnterToContinue();
                return;
            }

            Console.WriteLine($"Security Question: {securityQuestion}");
            var answer = InputHelper.GetString("Answer: ");

            var newPassword = InputHelper.GetString("New password: ");
            var confirmPassword = InputHelper.GetString("Confirm new password: ");

            if (newPassword != confirmPassword)
            {
                Console.WriteLine("Passwords do not match.");
            }
            else if (passwordResetService.ResetPassword(username, answer, newPassword))
            {
                Console.WriteLine("Password reset successful! You can now login with your new password.");
            }
            else
            {
                Console.WriteLine("Password reset failed. Incorrect security answer or user not found.");
            }

            InputHelper.PressEnterToContinue();
        }
    }
}