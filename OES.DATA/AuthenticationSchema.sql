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

BEGIN
	(
	);
END;

BEGIN
	(
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

BEGIN
	(
	);
END;

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Users_Role' AND parent_object_id = OBJECT_ID(N'dbo.Users'))
BEGIN
	ALTER TABLE dbo.Users DROP CONSTRAINT CK_Users_Role;
END;

BEGIN
	(
	);
END;

IF OBJECT_ID(N'dbo.Results', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.Results
	(
		Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Results PRIMARY KEY,
		ExamId int NOT NULL,
		StudentId int NOT NULL,
		TotalQuestions int NOT NULL,
		CorrectAnswers int NOT NULL,
		WrongAnswers int NOT NULL,
		UnansweredQuestions int NOT NULL,
		TotalMarks decimal(10,2) NOT NULL,
		ObtainedMarks decimal(10,2) NOT NULL,
		Percentage decimal(6,2) NOT NULL,
		PassingMarks decimal(10,2) NOT NULL,
		CONSTRAINT FK_Results_Exams FOREIGN KEY (ExamId) REFERENCES dbo.Exams(Id),
	);
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = N'TwoFactorEnabled')
BEGIN
	ALTER TABLE dbo.Users ADD TwoFactorEnabled bit NOT NULL CONSTRAINT DF_Users_TwoFactorEnabled DEFAULT 1;
END;

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
