-- 1. Users Table
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.Users
	(
		Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
		Email nvarchar(320) NOT NULL,
		PasswordHash nvarchar(255) NOT NULL,
		Role nvarchar(50) NOT NULL CONSTRAINT DF_Users_Role DEFAULT N'Student',
		IsActive bit NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
		TwoFactorEnabled bit NOT NULL CONSTRAINT DF_Users_TwoFactorEnabled DEFAULT 1,
		CreatedUtc datetime2(0) NOT NULL CONSTRAINT DF_Users_CreatedUtc DEFAULT SYSUTCDATETIME(),
		CONSTRAINT UQ_Users_Email UNIQUE (Email),
		CONSTRAINT CK_Users_Role CHECK (Role IN (N'Admin', N'Student'))
	);
END;

-- 2. Students Table
IF OBJECT_ID(N'dbo.Students', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.Students
	(
		Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Students PRIMARY KEY,
		UserId int NOT NULL CONSTRAINT UQ_Students_UserId UNIQUE,
		FullName nvarchar(150) NOT NULL,
		CreatedUtc datetime2(0) NOT NULL CONSTRAINT DF_Students_CreatedUtc DEFAULT SYSUTCDATETIME(),
		CONSTRAINT FK_Students_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE
	);
END;

-- 3. Exams Table
IF OBJECT_ID(N'dbo.Exams', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.Exams
	(
		Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Exams PRIMARY KEY,
		Title nvarchar(200) NOT NULL,
		Description nvarchar(2000) NULL,
		StartUtc datetime2(0) NOT NULL,
		EndUtc datetime2(0) NOT NULL,
		DurationMinutes int NOT NULL,
		TotalMarks decimal(10,2) NOT NULL,
		PassingMarks decimal(10,2) NOT NULL,
		Status nvarchar(30) NOT NULL CONSTRAINT DF_Exams_Status DEFAULT N'Draft',
		IsActive bit NOT NULL CONSTRAINT DF_Exams_IsActive DEFAULT 1,
		CreatedBy int NOT NULL,
		CreatedUtc datetime2(0) NOT NULL CONSTRAINT DF_Exams_CreatedUtc DEFAULT SYSUTCDATETIME(),
		CONSTRAINT FK_Exams_Users FOREIGN KEY (CreatedBy) REFERENCES dbo.Users(Id),
		CONSTRAINT CK_Exams_Dates CHECK (EndUtc > StartUtc),
		CONSTRAINT CK_Exams_Duration CHECK (DurationMinutes > 0),
		CONSTRAINT CK_Exams_Marks CHECK (TotalMarks > 0 AND PassingMarks >= 0 AND PassingMarks <= TotalMarks),
		CONSTRAINT CK_Exams_Status CHECK (Status IN (N'Draft', N'Published', N'Completed', N'Cancelled'))
	);
END;

-- 4. Questions Table
IF OBJECT_ID(N'dbo.Questions', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.Questions
	(
		Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Questions PRIMARY KEY,
		ExamId int NOT NULL,
		QuestionText nvarchar(2000) NOT NULL,
		Marks decimal(10,2) NOT NULL,
		NegativeMarks decimal(10,2) NOT NULL CONSTRAINT DF_Questions_NegativeMarks DEFAULT 0,
		DisplayOrder int NOT NULL,
		IsActive bit NOT NULL CONSTRAINT DF_Questions_IsActive DEFAULT 1,
		CONSTRAINT FK_Questions_Exams FOREIGN KEY (ExamId) REFERENCES dbo.Exams(Id) ON DELETE CASCADE,
		CONSTRAINT CK_Questions_Marks CHECK (Marks > 0 AND NegativeMarks >= 0)
	);
END;

-- 5. Options Table
IF OBJECT_ID(N'dbo.Options', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.Options
	(
		Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Options PRIMARY KEY,
		QuestionId int NOT NULL,
		OptionText nvarchar(1000) NOT NULL,
		OptionKey nvarchar(10) NOT NULL,
		IsCorrect bit NOT NULL CONSTRAINT DF_Options_IsCorrect DEFAULT 0,
		DisplayOrder int NOT NULL,
		CONSTRAINT FK_Options_Questions FOREIGN KEY (QuestionId) REFERENCES dbo.Questions(Id) ON DELETE CASCADE,
		CONSTRAINT UQ_Options_Question_Key UNIQUE (QuestionId, OptionKey)
	);
END;

-- 6. ExamAttempts Table
IF OBJECT_ID(N'dbo.ExamAttempts', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.ExamAttempts
	(
		Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ExamAttempts PRIMARY KEY,
		ExamId int NOT NULL,
		StudentId int NOT NULL,
		StartedUtc datetime2(0) NOT NULL,
		ExpiresUtc datetime2(0) NOT NULL,
		SubmittedUtc datetime2(0) NULL,
		Status nvarchar(30) NOT NULL CONSTRAINT DF_ExamAttempts_Status DEFAULT N'InProgress',
		CONSTRAINT FK_ExamAttempts_Exams FOREIGN KEY (ExamId) REFERENCES dbo.Exams(Id),
		CONSTRAINT FK_ExamAttempts_Students FOREIGN KEY (StudentId) REFERENCES dbo.Students(Id),
		CONSTRAINT UQ_ExamAttempts_Exam_Student UNIQUE (ExamId, StudentId),
		CONSTRAINT CK_ExamAttempts_Status CHECK (Status IN (N'InProgress', N'Submitted', N'TimedOut')),
		CONSTRAINT CK_ExamAttempts_Expiry CHECK (ExpiresUtc > StartedUtc)
	);
END;

-- 7. StudentAnswers Table
IF OBJECT_ID(N'dbo.StudentAnswers', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.StudentAnswers
	(
		Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_StudentAnswers PRIMARY KEY,
		AttemptId int NOT NULL,
		QuestionId int NOT NULL,
		SelectedOptionId int NOT NULL,
		AnsweredUtc datetime2(0) NOT NULL CONSTRAINT DF_StudentAnswers_AnsweredUtc DEFAULT SYSUTCDATETIME(),
		CONSTRAINT FK_StudentAnswers_Attempts FOREIGN KEY (AttemptId) REFERENCES dbo.ExamAttempts(Id) ON DELETE CASCADE,
		CONSTRAINT FK_StudentAnswers_Questions FOREIGN KEY (QuestionId) REFERENCES dbo.Questions(Id),
		CONSTRAINT FK_StudentAnswers_Options FOREIGN KEY (SelectedOptionId) REFERENCES dbo.Options(Id),
		CONSTRAINT UQ_StudentAnswers_Attempt_Question UNIQUE (AttemptId, QuestionId)
	);
END;

-- 8. Results Table
IF OBJECT_ID(N'dbo.Results', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.Results
	(
		Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Results PRIMARY KEY,
		AttemptId int NOT NULL CONSTRAINT UQ_Results_AttemptId UNIQUE,
		ExamId int NOT NULL,
		StudentId int NOT NULL,
		TotalMarks decimal(10,2) NOT NULL,
		PassingMarks decimal(10,2) NOT NULL,
		ObtainedMarks decimal(10,2) NOT NULL,
		Percentage decimal(5,2) NOT NULL,
		CorrectAnswers int NOT NULL,
		WrongAnswers int NOT NULL,
		UnattemptedQuestions int NOT NULL,
		IsPassed bit NOT NULL,
		CalculatedUtc datetime2(0) NOT NULL CONSTRAINT DF_Results_CalculatedUtc DEFAULT SYSUTCDATETIME(),
		CONSTRAINT FK_Results_Attempts FOREIGN KEY (AttemptId) REFERENCES dbo.ExamAttempts(Id) ON DELETE CASCADE,
		CONSTRAINT FK_Results_Exams FOREIGN KEY (ExamId) REFERENCES dbo.Exams(Id),
		CONSTRAINT FK_Results_Students FOREIGN KEY (StudentId) REFERENCES dbo.Students(Id)
	);
END;

-- 9. TwoFactorCodes Table
IF OBJECT_ID(N'dbo.TwoFactorCodes', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.TwoFactorCodes
	(
		Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_TwoFactorCodes PRIMARY KEY,
		UserId int NOT NULL,
		Code nvarchar(10) NOT NULL,
		ExpiresUtc datetime2(0) NOT NULL,
		IsUsed bit NOT NULL CONSTRAINT DF_TwoFactorCodes_IsUsed DEFAULT 0,
		CreatedUtc datetime2(0) NOT NULL CONSTRAINT DF_TwoFactorCodes_CreatedUtc DEFAULT SYSUTCDATETIME(),
		CONSTRAINT FK_TwoFactorCodes_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE
	);
END;

-- 10. Notifications Table
IF OBJECT_ID(N'dbo.Notifications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notifications (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL CONSTRAINT FK_Notifications_Users REFERENCES dbo.Users(Id) ON DELETE CASCADE,
        Title NVARCHAR(150) NOT NULL,
        Message NVARCHAR(500) NOT NULL,
        Type NVARCHAR(20) NOT NULL CONSTRAINT DF_Notifications_Type DEFAULT 'info',
        IsRead BIT NOT NULL CONSTRAINT DF_Notifications_IsRead DEFAULT 0,
        CreatedUtc DATETIME2 NOT NULL CONSTRAINT DF_Notifications_CreatedUtc DEFAULT SYSUTCDATETIME(),
        ActionUrl NVARCHAR(500) NULL
    );

    CREATE NONCLUSTERED INDEX IX_Notifications_UserId_IsRead_CreatedUtc
    ON dbo.Notifications (UserId, IsRead, CreatedUtc DESC);
END;

-- Seed Default Admin User if no Admin exists
-- Password: Password@123 (BCrypt hash)
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Role = N'Admin')
BEGIN
	INSERT INTO dbo.Users (Email, PasswordHash, Role, IsActive, TwoFactorEnabled, CreatedUtc)
	VALUES (N'admin@oes.com', N'$2a$11$eAnfV58k8pM.w49Pms8yReL8mYjG5Q8H8Z8Y8X8W8V8U8T8S8R8Q8', N'Admin', 1, 0, SYSUTCDATETIME());
END;
