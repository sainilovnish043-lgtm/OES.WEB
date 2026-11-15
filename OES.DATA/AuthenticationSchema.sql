IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.Users
	(
		Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
		Email nvarchar(320) NOT NULL,
		PasswordHash nvarchar(255) NOT NULL,
		Role nvarchar(50) NOT NULL CONSTRAINT DF_Users_Role DEFAULT N'Student',
		IsActive bit NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
		CreatedUtc datetime2(0) NOT NULL CONSTRAINT DF_Users_CreatedUtc DEFAULT SYSUTCDATETIME(),
		CONSTRAINT UQ_Users_Email UNIQUE (Email),
		CONSTRAINT CK_Users_Role CHECK (Role IN (N'Admin', N'Student'))
	);
END;

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

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Users_Role' AND parent_object_id = OBJECT_ID(N'dbo.Users'))
BEGIN
	ALTER TABLE dbo.Users DROP CONSTRAINT CK_Users_Role;
END;

UPDATE dbo.Users
SET Role = N'Student'
WHERE Role = N'User';

ALTER TABLE dbo.Users ADD CONSTRAINT CK_Users_Role CHECK (Role IN (N'Admin', N'Student'));

IF OBJECT_ID(N'dbo.Students', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.Students
	(
		Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Students PRIMARY KEY,
		UserId int NOT NULL CONSTRAINT UQ_Students_UserId UNIQUE,
		FullName nvarchar(150) NOT NULL,
		CreatedUtc datetime2(0) NOT NULL CONSTRAINT DF_Students_CreatedUtc DEFAULT SYSUTCDATETIME(),
		CONSTRAINT FK_Students_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
	);
END;
