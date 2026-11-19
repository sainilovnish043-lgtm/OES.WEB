using Dapper;
using Microsoft.Extensions.Logging;
using OES.DATA.Database;

namespace OES.SERVICES.Database;

public sealed class DbSeeder(
    ISqlConnectionFactory connectionFactory,
    ILogger<DbSeeder> logger) : IDbSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = connectionFactory.CreateConnection();

            // 1. Seed or Update Admin User
            var adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
            const string adminSql = """
                IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = 'admin@oes.com')
                BEGIN
                    INSERT INTO dbo.Users (Email, PasswordHash, Role, IsActive, TwoFactorEnabled, CreatedUtc)
                    VALUES ('admin@oes.com', @Hash, 'Admin', 1, 0, SYSUTCDATETIME());
                END
                ELSE
                BEGIN
                    UPDATE dbo.Users
                    SET PasswordHash = @Hash, Role = 'Admin', IsActive = 1, TwoFactorEnabled = 0
                    WHERE Email = 'admin@oes.com';
                END
                """;

            await connection.ExecuteAsync(new CommandDefinition(adminSql, new { Hash = adminPasswordHash }, cancellationToken: cancellationToken));
            logger.LogInformation("Admin user seeded: admin@oes.com / Admin@123");

            // 2. Seed or Update Demo Student User
            var studentPasswordHash = BCrypt.Net.BCrypt.HashPassword("Student@123");
            const string studentSql = """
                DECLARE @StudentUserId INT;
                IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = 'student@oes.com')
                BEGIN
                    INSERT INTO dbo.Users (Email, PasswordHash, Role, IsActive, TwoFactorEnabled, CreatedUtc)
                    VALUES ('student@oes.com', @Hash, 'Student', 1, 0, SYSUTCDATETIME());
                    SET @StudentUserId = SCOPE_IDENTITY();
                END
                ELSE
                BEGIN
                    UPDATE dbo.Users
                    SET PasswordHash = @Hash, Role = 'Student', IsActive = 1, TwoFactorEnabled = 0
                    WHERE Email = 'student@oes.com';
                    SELECT @StudentUserId = Id FROM dbo.Users WHERE Email = 'student@oes.com';
                END

                IF NOT EXISTS (SELECT 1 FROM dbo.Students WHERE UserId = @StudentUserId)
                BEGIN
                    INSERT INTO dbo.Students (UserId, FullName, CreatedUtc)
                    VALUES (@StudentUserId, 'Demo Student', SYSUTCDATETIME());
                END
                """;

            await connection.ExecuteAsync(new CommandDefinition(studentSql, new { Hash = studentPasswordHash }, cancellationToken: cancellationToken));
            logger.LogInformation("Student user seeded: student@oes.com / Student@123");

            // 3. Seed Sample Published Exam if no exams exist
            const string examCheckSql = "SELECT COUNT(1) FROM dbo.Exams;";
            var examCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(examCheckSql, cancellationToken: cancellationToken));

            if (examCount == 0)
            {
                var adminUserId = await connection.ExecuteScalarAsync<int>(
                    new CommandDefinition("SELECT TOP 1 Id FROM dbo.Users WHERE Role = 'Admin';", cancellationToken: cancellationToken));

                const string createExamSql = """
                    INSERT INTO dbo.Exams (Title, Description, StartUtc, EndUtc, DurationMinutes, TotalMarks, PassingMarks, Status, IsActive, CreatedBy, CreatedUtc)
                    VALUES (
                        'C# & Web Development Fundamentals',
                        'Comprehensive multiple-choice examination covering C# OOP, ASP.NET Core, and Database concepts.',
                        DATEADD(day, -1, SYSUTCDATETIME()),
                        DATEADD(day, 30, SYSUTCDATETIME()),
                        20,
                        20.00,
                        10.00,
                        'Published',
                        1,
                        @AdminUserId,
                        SYSUTCDATETIME()
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                    """;

                var examId = await connection.ExecuteScalarAsync<int>(
                    new CommandDefinition(createExamSql, new { AdminUserId = adminUserId }, cancellationToken: cancellationToken));

                // Add 4 Sample Questions
                var questions = new[]
                {
                    ("Which of the following is a reference type in C#?", 5.0m, new[] { ("Class", true), ("struct", false), ("int", false), ("enum", false) }),
                    ("Which HTTP status code signifies a resource was successfully created?", 5.0m, new[] { ("201 Created", true), ("200 OK", false), ("204 No Content", false), ("301 Moved", false) }),
                    ("What does Dapper primarily provide in .NET applications?", 5.0m, new[] { ("Micro-ORM for fast object-relational mapping", true), ("A JavaScript bundler", false), ("A cloud hosting engine", false), ("A CSS styling framework", false) }),
                    ("Which SQL clause is used to filter aggregated group records?", 5.0m, new[] { ("HAVING", true), ("WHERE", false), ("GROUP BY", false), ("ORDER BY", false) })
                };

                var order = 1;
                foreach (var (qText, marks, opts) in questions)
                {
                    const string qSql = """
                        INSERT INTO dbo.Questions (ExamId, QuestionText, Marks, NegativeMarks, DisplayOrder, IsActive)
                        VALUES (@ExamId, @Text, @Marks, 0, @Order, 1);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);
                        """;

                    var qId = await connection.ExecuteScalarAsync<int>(
                        new CommandDefinition(qSql, new { ExamId = examId, Text = qText, Marks = marks, Order = order++ }, cancellationToken: cancellationToken));

                    char key = 'A';
                    var optOrder = 1;
                    foreach (var (optText, isCorrect) in opts)
                    {
                        const string optSql = """
                            INSERT INTO dbo.Options (QuestionId, OptionText, OptionKey, IsCorrect, DisplayOrder)
                            VALUES (@QuestionId, @Text, @Key, @IsCorrect, @Order);
                            """;

                        await connection.ExecuteAsync(
                            new CommandDefinition(optSql, new { QuestionId = qId, Text = optText, Key = key++.ToString(), IsCorrect = isCorrect, Order = optOrder++ }, cancellationToken: cancellationToken));
                    }
                }

                logger.LogInformation("Sample Exam and Questions successfully seeded!");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "DbSeeder encountered an error during startup data seeding.");
        }
    }
}
