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

