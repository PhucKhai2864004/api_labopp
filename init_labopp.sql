	IF DB_ID('LabOopChange_v6') IS NOT NULL
	BEGIN
		ALTER DATABASE LabOopChange_v6 SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
		DROP DATABASE LabOopChange_v6;
	END;
	CREATE DATABASE LabOopChange_v6;
	GO
	USE LabOopChange_v6;
	GO

	/* ===================== 1) CORE TABLES ===================== */

	-- Role
	CREATE TABLE dbo.Role (
		id INT IDENTITY(1,1) PRIMARY KEY,
		name NVARCHAR(255) NOT NULL,
		description NVARCHAR(500) NULL,
		CONSTRAINT UQ_Role_Name UNIQUE (name)
	);

	-- User
	CREATE TABLE dbo.[User] (
		id INT IDENTITY(1,1) PRIMARY KEY,
		name NVARCHAR(100) NOT NULL,
		email VARCHAR(255) NOT NULL,
		user_name VARCHAR(255) NULL,
		[password] VARCHAR(255) NULL,
		role_id INT NOT NULL,
		is_active BIT NOT NULL DEFAULT 1,
		created_by INT NULL,
		created_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
		updated_by INT NULL,
		updated_at DATETIME2 NULL,
		CONSTRAINT UQ_User_Email UNIQUE (email),
		CONSTRAINT FK_User_Role FOREIGN KEY (role_id) REFERENCES dbo.Role(id)
	);

	-- Student
	CREATE TABLE dbo.Student (
		id INT PRIMARY KEY,                       -- FK -> User(id)
		student_code VARCHAR(50) NOT NULL,        -- HE1xxxxx
		major NVARCHAR(255) NULL,
		date_of_birth DATE NULL,
		phone VARCHAR(20) NULL,
		gender BIT NULL,                          -- 0/1
		[address] NVARCHAR(500) NULL,
		CONSTRAINT UQ_Student_Code UNIQUE (student_code),
		CONSTRAINT FK_Student_User FOREIGN KEY (id) REFERENCES dbo.[User](id)
	);

	-- Teacher
	CREATE TABLE dbo.Teacher (
		id INT PRIMARY KEY,                       -- FK -> User(id)
		teacher_code VARCHAR(50) NOT NULL,
		academic_degree NVARCHAR(50) NULL,
		date_of_birth DATE NULL,
		phone VARCHAR(20) NULL,
		gender BIT NULL,
		[address] NVARCHAR(500) NULL,
		CONSTRAINT UQ_Teacher_Code UNIQUE (teacher_code),
		CONSTRAINT FK_Teacher_User FOREIGN KEY (id) REFERENCES dbo.[User](id)
	);

	-- Semester
	CREATE TABLE dbo.Semester (
		id INT IDENTITY(1,1) PRIMARY KEY,
		name NVARCHAR(100) NOT NULL,              -- spring2025, summer2025, fall2025
		start_date DATE NULL,
		end_date DATE NULL,
		CONSTRAINT UQ_Semester_Name UNIQUE (name)
	);

	-- Class
	CREATE TABLE dbo.Class (
		id INT IDENTITY(1,1) PRIMARY KEY,
		class_code VARCHAR(20) NOT NULL,
		subject_code VARCHAR(20) NULL,
		semester_id INT NOT NULL,
		academic_year VARCHAR(20) NULL,
		is_active BIT NOT NULL DEFAULT 1,
		teacher_id INT NOT NULL,
		loc_to_pass INT NULL,
		created_by INT NULL,
		created_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
		updated_by INT NULL,
		updated_at DATETIME2 NULL,
		CONSTRAINT UQ_Class UNIQUE (class_code, semester_id),
		CONSTRAINT FK_Class_Semester FOREIGN KEY (semester_id) REFERENCES dbo.Semester(id),
		CONSTRAINT FK_Class_Teacher  FOREIGN KEY (teacher_id)  REFERENCES dbo.[User](id)
	);

	-- Lab_Assignment
	CREATE TABLE dbo.Lab_Assignment (
		id INT IDENTITY(1,1) PRIMARY KEY,
		title NVARCHAR(255) NOT NULL,
		[description] NVARCHAR(MAX) NULL,
		teacher_id INT NOT NULL,
		loc_total INT NULL,
		[status] VARCHAR(20) NOT NULL DEFAULT 'Pending'
			CHECK ([status] IN ('Pending','Active','Inactive')),
		created_by INT NULL,
		approved_by INT NULL,
		approved_at DATETIME2 NULL,
		review_note NVARCHAR(1000) NULL,
		created_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
		updated_by INT NULL,
		updated_at DATETIME2 NULL,
		CONSTRAINT FK_LA_Teacher     FOREIGN KEY (teacher_id)  REFERENCES dbo.[User](id),
		CONSTRAINT FK_LA_CreatedBy   FOREIGN KEY (created_by)  REFERENCES dbo.[User](id),
		CONSTRAINT FK_LA_ApprovedBy  FOREIGN KEY (approved_by) REFERENCES dbo.[User](id)
	);

	-- Class_Has_Lab_Assignment
	CREATE TABLE dbo.Class_Has_Lab_Assignment (
		id INT IDENTITY(1,1) PRIMARY KEY,
		class_id INT NOT NULL,
		assignment_id INT NOT NULL,
		open_at DATETIME2 NULL,
		close_at DATETIME2 NULL,
		CONSTRAINT FK_CLA_Class      FOREIGN KEY (class_id)      REFERENCES dbo.Class(id),
		CONSTRAINT FK_CLA_Assignment FOREIGN KEY (assignment_id) REFERENCES dbo.Lab_Assignment(id),
		CONSTRAINT UQ_Class_Assignment UNIQUE (class_id, assignment_id)
	);

	-- Student_In_Class
	CREATE TABLE dbo.Student_In_Class (
		id INT IDENTITY(1,1) PRIMARY KEY,
		class_id INT NOT NULL,
		student_id INT NOT NULL,
		CONSTRAINT FK_SIC_Class   FOREIGN KEY (class_id)   REFERENCES dbo.Class(id),
		CONSTRAINT FK_SIC_Student FOREIGN KEY (student_id) REFERENCES dbo.[User](id),
		CONSTRAINT UQ_SIC UNIQUE (class_id, student_id)
	);

	-- Student_Lab_Assignment
	CREATE TABLE dbo.Student_Lab_Assignment (
		id INT IDENTITY(1,1) PRIMARY KEY,
		assignment_id INT NOT NULL,
		student_id INT NOT NULL,
		semester_id INT NOT NULL,
		submission_zip NVARCHAR(500) NULL,
		[status] VARCHAR(50) NOT NULL DEFAULT 'Draft'
			CHECK ([status] IN ('Passed','Draft','Reject')),
		submitted_at DATETIME2 NULL,
		loc_result INT NULL,
		manually_edited BIT NULL,
		manual_reason NVARCHAR(MAX) NULL,
		CONSTRAINT FK_SLA_Assign  FOREIGN KEY (assignment_id) REFERENCES dbo.Lab_Assignment(id),
		CONSTRAINT FK_SLA_Student FOREIGN KEY (student_id)    REFERENCES dbo.[User](id),
		CONSTRAINT FK_SLA_Sem     FOREIGN KEY (semester_id)   REFERENCES dbo.Semester(id),
		CONSTRAINT UQ_SLA UNIQUE (assignment_id, student_id, semester_id)
	);

	-- TestCase
	CREATE TABLE dbo.TestCase (
		id INT IDENTITY(1,1) PRIMARY KEY,
		assignment_id INT NOT NULL,
		expected_output NVARCHAR(MAX) NULL,
		[input] NVARCHAR(MAX) NULL,
		loc INT NULL CHECK (loc IS NULL OR loc >= 0),
		created_by INT NULL,
		created_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
		updated_by INT NULL,
		updated_at DATETIME2 NULL,
		CONSTRAINT FK_TC_Assign  FOREIGN KEY (assignment_id) REFERENCES dbo.Lab_Assignment(id),
		CONSTRAINT FK_TC_Creator FOREIGN KEY (created_by)    REFERENCES dbo.[User](id)
	);

	-- TestCaseResult
	CREATE TABLE dbo.TestCaseResult (
		id INT IDENTITY(1,1) PRIMARY KEY,
		student_lab_assignment_id INT NOT NULL,
		test_case_id INT NOT NULL,
		actual_output NVARCHAR(MAX) NULL,
		is_passed BIT NULL,
		CONSTRAINT FK_TCR_SLA      FOREIGN KEY (student_lab_assignment_id) REFERENCES dbo.Student_Lab_Assignment(id),
		CONSTRAINT FK_TCR_TestCase FOREIGN KEY (test_case_id)               REFERENCES dbo.TestCase(id),
		CONSTRAINT UQ_TCR UNIQUE (student_lab_assignment_id, test_case_id)
	);

	-- Assignment_Approval
	CREATE TABLE dbo.Assignment_Approval (
	  id INT IDENTITY(1,1) PRIMARY KEY,
	  assignment_id INT NOT NULL,
	  [action] VARCHAR(20) NOT NULL CHECK ([action] IN ('Submit','Approve','Reject','Update')),
	  actor_id INT NOT NULL,
	  action_note NVARCHAR(1000) NULL,
	  acted_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
	  CONSTRAINT FK_AA_Assign FOREIGN KEY (assignment_id) REFERENCES dbo.Lab_Assignment(id),
	  CONSTRAINT FK_AA_Actor  FOREIGN KEY (actor_id)      REFERENCES dbo.[User](id)
	);

	/* ================== 2) ENABLE CLASS BY SLOT ================== */
	CREATE TABLE dbo.Class_Slot (
	  id INT IDENTITY(1,1) PRIMARY KEY,
	  class_id INT NOT NULL,
	  slot_no INT NOT NULL,
	  start_time DATETIME2 NOT NULL,
	  end_time DATETIME2 NOT NULL,
	  is_enabled BIT NOT NULL DEFAULT 0,
	  server_endpoint NVARCHAR(500) NULL,
	  note NVARCHAR(500) NULL,
	  CONSTRAINT FK_Slot_Class FOREIGN KEY (class_id) REFERENCES dbo.Class(id),
	  CONSTRAINT UQ_Class_Slot UNIQUE (class_id, slot_no)
	);

	CREATE TABLE dbo.Class_Slot_Log (
	  id INT IDENTITY(1,1) PRIMARY KEY,
	  class_slot_id INT NOT NULL,
	  actor_id INT NOT NULL,
	  [action] VARCHAR(20) NOT NULL CHECK ([action] IN ('Enable','Disable')),
	  acted_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
	  CONSTRAINT FK_SlotLog_Slot  FOREIGN KEY (class_slot_id) REFERENCES dbo.Class_Slot(id),
	  CONSTRAINT FK_SlotLog_Actor FOREIGN KEY (actor_id)      REFERENCES dbo.[User](id)
	);

	/* ==================== 3) RAG INGEST STORAGE ==================== */
	CREATE TABLE dbo.Assignment_Document (
	  id INT IDENTITY(1,1) PRIMARY KEY,
	  assignment_id INT NOT NULL,
	  file_name NVARCHAR(255) NOT NULL,
	  file_path NVARCHAR(500) NOT NULL,
	  mime_type NVARCHAR(100) NULL,
	  uploaded_by INT NULL,
	  uploaded_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
	  CONSTRAINT FK_AD_Assign FOREIGN KEY (assignment_id) REFERENCES dbo.Lab_Assignment(id),
	  CONSTRAINT FK_AD_User   FOREIGN KEY (uploaded_by)   REFERENCES dbo.[User](id)
	);

	CREATE TABLE dbo.Vector_Index (
	  id INT IDENTITY(1,1) PRIMARY KEY,
	  provider VARCHAR(50) NOT NULL,
	  index_name NVARCHAR(255) NOT NULL,
	  external_id NVARCHAR(255) NULL,
	  created_at DATETIME2 NOT NULL DEFAULT SYSDATETIME()
	);

	CREATE TABLE dbo.Assignment_Ingest (
	  id INT IDENTITY(1,1) PRIMARY KEY,
	  assignment_id INT NOT NULL,
	  document_id INT NOT NULL,
	  vector_index_id INT NOT NULL,
	  chunk_size INT NULL,
	  chunk_overlap INT NULL,
	  chunks_ingested INT NULL,
	  last_chunked_at DATETIME2 NULL,
	  [status] VARCHAR(20) NOT NULL DEFAULT 'NotProcessed'
		CHECK ([status] IN ('NotProcessed','Running','Success','Failed','AlreadyExists')),
	  [message] NVARCHAR(1000) NULL,
	  CONSTRAINT FK_AI_Assign  FOREIGN KEY (assignment_id)   REFERENCES dbo.Lab_Assignment(id),
	  CONSTRAINT FK_AI_Doc     FOREIGN KEY (document_id)     REFERENCES dbo.Assignment_Document(id),
	  CONSTRAINT FK_AI_VIndex  FOREIGN KEY (vector_index_id) REFERENCES dbo.Vector_Index(id),
	  CONSTRAINT UQ_AI UNIQUE (assignment_id, document_id, vector_index_id)
	);
	-- Bảng giả lập Semester (mỗi học kỳ của trường)
	CREATE TABLE [dbo].[FAP_Semester] (
		id INT IDENTITY(1,1) PRIMARY KEY,
		code VARCHAR(20) NOT NULL UNIQUE,   -- Ví dụ: Spring2025, Summer2025, Fall2025
		name NVARCHAR(100) NOT NULL
	);

	-- Bảng giả lập Lớp học từ FAP
	CREATE TABLE [dbo].[FAP_Class] (
		id INT IDENTITY(1,1) PRIMARY KEY,
		code VARCHAR(50) NOT NULL UNIQUE,   -- Ví dụ: SE1742
		name NVARCHAR(100) NOT NULL,
		semester_id INT NOT NULL,
		FOREIGN KEY (semester_id) REFERENCES [dbo].[FAP_Semester](id)
	);

	-- Bảng giả lập Sinh viên từ FAP
	CREATE TABLE [dbo].[FAP_Student] (
		id INT IDENTITY(1,1) PRIMARY KEY,
		student_code VARCHAR(50) NOT NULL UNIQUE,   -- Ví dụ: HE1742001
		name NVARCHAR(100) NOT NULL,
		semester_id INT NOT NULL,
		class_id INT NOT NULL,
		FOREIGN KEY (semester_id) REFERENCES [dbo].[FAP_Semester](id),
		FOREIGN KEY (class_id) REFERENCES [dbo].[FAP_Class](id)
	);


/* ============ INSERT ROLES ============ */
INSERT INTO dbo.Role (name, description)
VALUES 
  ('Student', 'Role for students'),
  ('Teacher', 'Role for teachers'),
  ('Head_Subject', 'Role for subject heads');

/* ============ INSERT USERS ============ */
-- Student user
INSERT INTO dbo.[User] (name, email, user_name, [password], role_id, is_active)
VALUES ('Nguyen Van A', 'student1@example.com', 'student1', '123456', 
        (SELECT id FROM dbo.Role WHERE name = 'Student'), 1);

-- Teacher user
INSERT INTO dbo.[User] (name, email, user_name, [password], role_id, is_active)
VALUES ('Tran Van B', 'teacher1@example.com', 'teacher1', '123456', 
        (SELECT id FROM dbo.Role WHERE name = 'Teacher'), 1);

-- Head Subject user
INSERT INTO dbo.[User] (name, email, user_name, [password], role_id, is_active)
VALUES ('Le Thi C', 'head1@example.com', 'headsubject', '123456', 
        (SELECT id FROM dbo.Role WHERE name = 'Head_Subject'), 1);


/* ============ INSERT STUDENT / TEACHER PROFILE ============ */
-- Lấy id của Student user
DECLARE @studentId INT = (SELECT id FROM dbo.[User] WHERE user_name = 'student1');
DECLARE @teacherId INT = (SELECT id FROM dbo.[User] WHERE user_name = 'teacher1');

-- Insert Student profile
INSERT INTO dbo.Student (id, student_code, major, date_of_birth, phone, gender, [address])
VALUES (@studentId, 'HE1742001', N'Software Engineering', '2004-05-10', '0123456789', 1, N'Hanoi');

-- Insert Teacher profile
INSERT INTO dbo.Teacher (id, teacher_code, academic_degree, date_of_birth, phone, gender, [address])
VALUES (@teacherId, 'GV001', N'Master', '1985-03-15', '0987654321', 1, N'Da Nang');


/* ============ SEMESTER DEMO ============ */
INSERT INTO dbo.Semester (name, start_date, end_date)
VALUES ('Spring2025', '2025-01-15', '2025-05-30');

/* ============ CLASS DEMO ============ */
DECLARE @teacherId INT = (SELECT id FROM dbo.[User] WHERE user_name = 'teacher1');
DECLARE @semesterId INT = (SELECT id FROM dbo.Semester WHERE name = 'Spring2025');

INSERT INTO dbo.Class (class_code, subject_code, semester_id, academic_year, teacher_id, loc_to_pass)
VALUES ('SE1742', 'OOP101', @semesterId, '2024-2025', @teacherId, 200);

/* ============ ASSIGNMENT DEMO ============ */
INSERT INTO dbo.Lab_Assignment (title, [description], teacher_id, loc_total, [status], created_by)
VALUES ('OOP Assignment 1', N'Basic OOP Concepts', @teacherId, 150, 'Active', @teacherId);

INSERT INTO dbo.Lab_Assignment (title, [description], teacher_id, loc_total, [status], created_by)
VALUES ('OOP Assignment 2', N'Inheritance & Polymorphism', @teacherId, 200, 'Active', @teacherId);

/* ============ CLASS_HAS_LAB_ASSIGNMENT ============ */
DECLARE @classId INT = (SELECT id FROM dbo.Class WHERE class_code = 'SE1742');
DECLARE @ass1 INT = (SELECT id FROM dbo.Lab_Assignment WHERE title = 'OOP Assignment 1');
DECLARE @ass2 INT = (SELECT id FROM dbo.Lab_Assignment WHERE title = 'OOP Assignment 2');

INSERT INTO dbo.Class_Has_Lab_Assignment (class_id, assignment_id, open_at, close_at)
VALUES (@classId, @ass1, '2025-02-01', '2025-02-28');

INSERT INTO dbo.Class_Has_Lab_Assignment (class_id, assignment_id, open_at, close_at)
VALUES (@classId, @ass2, '2025-03-01', '2025-03-30');

/* ============ STUDENT_IN_CLASS ============ */
DECLARE @studentId INT = (SELECT id FROM dbo.[User] WHERE user_name = 'student1');

INSERT INTO dbo.Student_In_Class (class_id, student_id)
VALUES (@classId, @studentId);

/* ============ STUDENT_LAB_ASSIGNMENT ============ */
INSERT INTO dbo.Student_Lab_Assignment (assignment_id, student_id, semester_id, [status], submitted_at, loc_result)
VALUES (@ass1, @studentId, @semesterId, 'Passed', GETDATE(), 120);

INSERT INTO dbo.Student_Lab_Assignment (assignment_id, student_id, semester_id, [status], submitted_at, loc_result)
VALUES (@ass2, @studentId, @semesterId, 'Draft', NULL, NULL);

/* ============ TESTCASE DEMO ============ */
INSERT INTO dbo.TestCase (assignment_id, expected_output, [input], loc, created_by)
VALUES (@ass1, N'Hello World', N'input.txt', 5, @teacherId);

/* ============ TESTCASE RESULT DEMO ============ */
DECLARE @sla1 INT = (SELECT id FROM dbo.Student_Lab_Assignment WHERE assignment_id = @ass1 AND student_id = @studentId);
DECLARE @tc1 INT = (SELECT id FROM dbo.TestCase WHERE assignment_id = @ass1);

INSERT INTO dbo.TestCaseResult (student_lab_assignment_id, test_case_id, actual_output, is_passed)
VALUES (@sla1, @tc1, N'Hello World', 1);


/* ================= INSERT SAMPLE DATA ================= */

-- Giả sử Class có id = 1, 2
-- Giả sử User (Teacher/Admin) có id = 1

-- Class Slot
INSERT INTO dbo.Class_Slot (class_id, slot_no, start_time, end_time, is_enabled, server_endpoint, note)
VALUES 
(1, 1, '2025-08-21T08:00:00', '2025-08-21T10:00:00', 1, N'http://server1.local:8080', N'Slot 1 cho lớp 1'),
(1, 2, '2025-08-22T08:00:00', '2025-08-22T10:00:00', 0, N'http://server1.local:8081', N'Slot 2 cho lớp 1'),
(1, 3, '2025-08-21T13:00:00', '2025-08-21T15:00:00', 1, N'http://server2.local:8080', N'Slot 3 cho lớp 1');


-- Ví dụ sửa dữ liệu slot sang UTC
UPDATE Class_Slot
SET start_time = DATEADD(HOUR, -7, start_time),
    end_time = DATEADD(HOUR, -7, end_time);

-- Class Slot Log
-- Giả sử actor_id = 1 (admin/teacher)
INSERT INTO dbo.Class_Slot_Log (class_slot_id, actor_id, [action])
VALUES
(1, 2, 'Enable'),
(2, 2, 'Disable'),
(3, 2, 'Enable');

