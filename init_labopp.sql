-- Tạo cơ sở dữ liệu
CREATE DATABASE LabOpp;
GO

USE LabOpp;
GO

-- Bảng Role
CREATE TABLE Role (
    id VARCHAR(255) PRIMARY KEY,
    name VARCHAR(255),
    description VARCHAR(500)
);

-- Bảng User
CREATE TABLE [User] (
    id VARCHAR(255) PRIMARY KEY,
    name VARCHAR(255),
    email VARCHAR(255) UNIQUE,
	user_name VARCHAR(255),
	password VARCHAR(255),
    role_id VARCHAR(255),
    is_active BIT,
    created_by VARCHAR(255),
    created_at DATETIME,
    updated_by VARCHAR(255),
    updated_at DATETIME,
    FOREIGN KEY (role_id) REFERENCES Role(id)
);

-- Bảng Student
CREATE TABLE Student (
    id VARCHAR(255) PRIMARY KEY,
    student_code VARCHAR(50) UNIQUE NOT NULL,
    major VARCHAR(255),
    date_of_birth DATE,
    phone VARCHAR(50),
    gender VARCHAR(20),
    address TEXT,
    FOREIGN KEY (id) REFERENCES [User](id)
);

-- Bảng Teacher
CREATE TABLE Teacher (
    id VARCHAR(255) PRIMARY KEY,
    teacher_code VARCHAR(50) UNIQUE NOT NULL,
    academic_title VARCHAR(255),
    academic_degree VARCHAR(255),
    date_of_birth DATE,
    phone VARCHAR(50),
    gender VARCHAR(20),
    address TEXT,
    FOREIGN KEY (id) REFERENCES [User](id)
);

-- Bảng Lab_Assignment
CREATE TABLE Lab_Assignment (
    id VARCHAR(255) PRIMARY KEY,
    title VARCHAR(255),
    description TEXT,
    teacher_id VARCHAR(255),
    loc_total INT,
	status VARCHAR(20) CHECK (status IN ('Pending', 'Active', 'Inactive')),
    created_by VARCHAR(255),
    created_at DATETIME,
    updated_by VARCHAR(255),
    updated_at DATETIME,
    FOREIGN KEY (teacher_id) REFERENCES [User](id)
);

-- Bảng Class
CREATE TABLE Class (
    id VARCHAR(255) PRIMARY KEY,
    name VARCHAR(255),
    subject VARCHAR(255),
    semester INT CHECK (semester IN (1, 2, 3)),
    academic_year VARCHAR(20),
    is_active BIT,
    teacher_id VARCHAR(255),
    loc_to_pass INT,
    created_by VARCHAR(255),
    created_at DATETIME,
    updated_by VARCHAR(255),
    updated_at DATETIME,
    FOREIGN KEY (teacher_id) REFERENCES [User](id)
);

-- Bảng Class_Has_Lab_Assignment
CREATE TABLE Class_Has_Lab_Assignment (
    id VARCHAR(255) PRIMARY KEY,
    class_id VARCHAR(255),
    assignment_id VARCHAR(255),
    FOREIGN KEY (class_id) REFERENCES Class(id),
    FOREIGN KEY (assignment_id) REFERENCES Lab_Assignment(id)
);

-- Bảng Student_In_Class
CREATE TABLE Student_In_Class (
    id VARCHAR(255) PRIMARY KEY,
    class_id VARCHAR(255),
    student_id VARCHAR(255),
    FOREIGN KEY (class_id) REFERENCES Class(id),
    FOREIGN KEY (student_id) REFERENCES [User](id)
);

-- Bảng Student_Lab_Assignment
CREATE TABLE Student_Lab_Assignment (
    id VARCHAR(255) PRIMARY KEY,
    assignment_id VARCHAR(255),
    student_id VARCHAR(255),
    FOREIGN KEY (assignment_id) REFERENCES Lab_Assignment(id),
    FOREIGN KEY (student_id) REFERENCES [User](id)
);

-- Bảng File
CREATE TABLE [File] (
    id VARCHAR(255) PRIMARY KEY,
    origin_name VARCHAR(255),
    name VARCHAR(255),
    path VARCHAR(500),
    mime_type VARCHAR(100),
    size INT,
    uploaded_by VARCHAR(255),
    uploaded_at DATETIME,
    FOREIGN KEY (uploaded_by) REFERENCES [User](id)
);

-- Bảng Submission
CREATE TABLE Submission (
    id VARCHAR(255) PRIMARY KEY,
    student_id VARCHAR(255),
    assignment_id VARCHAR(255),
    zip_code VARCHAR(255),
    status VARCHAR(50) CHECK (status IN ('Passed', 'Draft', 'Reject')),
    submitted_at DATETIME,
    loc_result INT,
    manually_edited BIT,
    manual_reason TEXT,
    created_by VARCHAR(255),
    created_at DATETIME,
    updated_by VARCHAR(255),
    updated_at DATETIME,
    FOREIGN KEY (student_id) REFERENCES [User](id),
    FOREIGN KEY (assignment_id) REFERENCES Lab_Assignment(id),
    FOREIGN KEY (zip_code) REFERENCES [File](id)
);

-- Bảng Feedback
CREATE TABLE Feedback (
    id VARCHAR(255) PRIMARY KEY,
    submission_id VARCHAR(255),
    teacher_id VARCHAR(255),
    comment TEXT,
    created_at DATETIME,
    updated_by VARCHAR(255),
    updated_at DATETIME,
    FOREIGN KEY (submission_id) REFERENCES Submission(id),
    FOREIGN KEY (teacher_id) REFERENCES [User](id)
);

-- Bảng TestCase
CREATE TABLE TestCase (
    id VARCHAR(255) PRIMARY KEY,
    assignment_id VARCHAR(255),
    expected_output TEXT,
    loc INT,
    created_by VARCHAR(255),
    created_at DATETIME,
    updated_by VARCHAR(255),
    updated_at DATETIME,
    FOREIGN KEY (assignment_id) REFERENCES Lab_Assignment(id)
);

-- Bảng TestCaseResult
CREATE TABLE TestCaseResult (
    id VARCHAR(255) PRIMARY KEY,
    submission_id VARCHAR(255),
    test_case_id VARCHAR(255),
    actual_output TEXT,
    is_passed BIT,
    FOREIGN KEY (submission_id) REFERENCES Submission(id),
    FOREIGN KEY (test_case_id) REFERENCES TestCase(id)
);

INSERT INTO Role (id, name, description) VALUES
('role_admin', 'Admin', 'System Administrator'),
('role_head', 'Head Subject', 'Department Head'),
('role_student', 'Student', 'Student Role'),
('role_teacher', 'Teacher', 'Teacher Role');

INSERT INTO [User] (
    id, name, email, user_name, password, role_id, is_active, created_by, created_at, updated_by, updated_at
) VALUES
('admin', 'Dung', 'dungnthe172310@fpt.edu.vn', 'admin1', 'admin1', 'role_admin', 1, 'admin', GETDATE(), 'admin', GETDATE()),
('HE172310', N'Nguyễn Tuấn Dũng', 'dung9v3@gmail.com', 'dungnthe172310', 'he172310', 'role_student', 1, 'admin', GETDATE(), 'admin', GETDATE()),
('HE183210', N'Lê Tuấn Quân', 'dung7v3@gmail.com', 'quanlthe183210', 'he183210', 'role_teacher', 1, 'admin', GETDATE(), 'admin', GETDATE());


INSERT INTO Student (
    id, student_code, major, date_of_birth, phone, gender, address
) VALUES (
    'HE172310', 'SE172310', 'Software Engineering', '2003-01-10', '0912345678', 'Male', N'123 Nguyễn Văn Cừ, Cần Thơ'
);

INSERT INTO Teacher (
    id, teacher_code, academic_title, academic_degree, date_of_birth, phone, gender, address
) VALUES (
    'HE183210', 'GV001', 'ThS.', 'Master of Computer Science', '1990-06-05', '0987654321', 'Male', N'456 Trần Hưng Đạo, Cần Thơ'
);


INSERT INTO Lab_Assignment (
    id, title, description, teacher_id, loc_total, status, created_by, created_at, updated_by, updated_at
) VALUES
('lab1', 'OOP Basics', 'Learn about classes and objects', 'HE183210', 120, 'Active', 'HE183210', GETDATE(), 'HE183210', GETDATE()),
('lab2', 'Inheritance', 'Deep dive into inheritance', 'HE183210', 150, 'Active', 'HE183210', GETDATE(), 'HE183210', GETDATE()),
('lab3', 'Polymorphism', 'Understanding polymorphism', 'HE183210', 100, 'Inactive', 'HE183210', GETDATE(), 'HE183210', GETDATE());


INSERT INTO Class (id, name, subject, semester, academic_year, is_active, teacher_id, loc_to_pass, created_by, created_at, updated_by, updated_at) VALUES
('SE1732', 'OOP K17', 'OOP', 1, '2021-2022', 1, 'HE183210', 750, 'HE183210', GETDATE(), 'HE183210', GETDATE()),
('SE1860', 'OOP K18', 'OOP', 2, '2022-2023', 1, 'HE183210', 750, 'HE183210', GETDATE(), 'HE183210', GETDATE()),
('SE1940', 'OOP K19', 'OOP', 3, '2023-2024', 1, 'HE183210', 750, 'HE183210', GETDATE(), 'HE183210', GETDATE());



INSERT INTO Class_Has_Lab_Assignment (id, class_id, assignment_id) VALUES
('clha1', 'SE1732', 'lab1'),
('clha2', 'SE1860', 'lab2'),
('clha3', 'SE1940', 'lab3');


INSERT INTO Student_In_Class (id, class_id, student_id) VALUES
('sic1', 'SE1732', 'HE172310'),
('sic2', 'SE1860', 'HE172310'),
('sic3', 'SE1940', 'HE172310');


INSERT INTO Student_Lab_Assignment (id, assignment_id, student_id) VALUES
('sla1', 'lab1', 'HE172310'),
('sla2', 'lab2', 'HE172310'),
('sla3', 'lab3', 'HE172310');


INSERT INTO [File] (id, origin_name, name, path, mime_type, size, uploaded_by, uploaded_at) VALUES
('file1', 'Assignment1.zip', 'file1.zip', '/uploads/file1.zip', 'application/zip', 204800, 'HE172310', GETDATE()),
('file2', 'Assignment2.zip', 'file2.zip', '/uploads/file2.zip', 'application/zip', 307200, 'HE172310', GETDATE()),
('file3', 'Assignment3.zip', 'file3.zip', '/uploads/file3.zip', 'application/zip', 102400, 'HE172310', GETDATE());



INSERT INTO Submission (id, student_id, assignment_id, zip_code, status, submitted_at, loc_result, manually_edited, manual_reason, created_by, created_at, updated_by, updated_at) VALUES
('sub1', 'HE172310', 'lab1', 'file1', 'Passed', GETDATE(), 120, 0, NULL, 'HE172310', GETDATE(), 'HE172310', GETDATE()),
('sub2', 'HE172310', 'lab2', 'file2', 'Passed', GETDATE(), 150, 0, NULL, 'HE172310', GETDATE(), 'HE172310', GETDATE()),
('sub3', 'HE172310', 'lab3', 'file3', 'Reject', GETDATE(), 80, 1, 'Too few LOC', 'HE172310', GETDATE(), 'HE172310', GETDATE());


INSERT INTO Feedback (id, submission_id, teacher_id, comment, created_at, updated_by, updated_at) VALUES
('fb1', 'sub1', 'HE183210', 'Well done!', GETDATE(), 'HE183210', GETDATE()),
('fb2', 'sub2', 'HE183210', 'Good effort.', GETDATE(), 'HE183210', GETDATE()),
('fb3', 'sub3', 'HE183210', 'Please resubmit with more LOC.', GETDATE(), 'HE183210', GETDATE());



INSERT INTO TestCase (id, assignment_id, expected_output, loc, created_by, created_at, updated_by, updated_at) VALUES
('tc1', 'lab1', 'Hello World', 50, 'HE183210', GETDATE(), 'HE183210', GETDATE()),
('tc2', 'lab2', 'Area: 25', 60, 'HE183210', GETDATE(), 'HE183210', GETDATE()),
('tc3', 'lab3', 'Polymorphism Example', 40, 'HE183210', GETDATE(), 'HE183210', GETDATE());


INSERT INTO TestCaseResult (id, submission_id, test_case_id, actual_output, is_passed) VALUES
('tcr1', 'sub1', 'tc1', 'Hello World', 1),
('tcr2', 'sub2', 'tc2', 'Area: 25', 1),
('tcr3', 'sub3', 'tc3', 'Wrong Output', 0);


