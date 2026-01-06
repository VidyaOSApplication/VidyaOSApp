

/* =========================================================
   DATABASE
========================================================= */
IF DB_ID('VidyaOS') IS NOT NULL
    DROP DATABASE VidyaOS;
GO
CREATE DATABASE VidyaOS;
GO
USE VidyaOS;
GO

/* =========================================================
   CORE TABLES (FROM FIRST SCRIPT – UNCHANGED)
========================================================= */

CREATE TABLE Schools (
    SchoolId INT IDENTITY PRIMARY KEY,
    SchoolName NVARCHAR(200),
    SchoolCode NVARCHAR(10) UNIQUE,
    RegistrationNumber NVARCHAR(50),
    YearOfFoundation INT,
    BoardType NVARCHAR(50),
    AffiliationNumber NVARCHAR(50),
    Email NVARCHAR(100),
    Phone NVARCHAR(15),
    AddressLine1 NVARCHAR(200),
    City NVARCHAR(100),
    State NVARCHAR(100),
    Pincode NVARCHAR(10),
    AdmissionSeq INT DEFAULT 0,
    IsActive BIT,
    CreatedAt DATETIME DEFAULT GETDATE()
);

CREATE TABLE Users (
    UserId INT IDENTITY PRIMARY KEY,
    SchoolId INT,
    Username NVARCHAR(100),
    PasswordHash NVARCHAR(255),
    Role NVARCHAR(20),
    Phone NVARCHAR(15),
    Email NVARCHAR(100),
    IsFirstLogin BIT,
    IsActive BIT,
    CreatedAt DATETIME DEFAULT GETDATE()
);

CREATE TABLE Classes (
    ClassId INT IDENTITY PRIMARY KEY,
    SchoolId INT,
    ClassName NVARCHAR(20),
    IsActive BIT
);

CREATE TABLE Sections (
    SectionId INT IDENTITY PRIMARY KEY,
    SchoolId INT,
    ClassId INT,
    SectionName NVARCHAR(10),
    RollSeq INT DEFAULT 0,
    IsActive BIT
);

CREATE TABLE Students (
    StudentId INT IDENTITY PRIMARY KEY,
    SchoolId INT,
    UserId INT UNIQUE,
    AdmissionNo NVARCHAR(50) UNIQUE,
    RollNo INT,
    FirstName NVARCHAR(100),
    LastName NVARCHAR(100),
    Gender NVARCHAR(10),
    DOB DATE,
    ClassId INT,
    SectionId INT,
    AcademicYear NVARCHAR(20),
    AdmissionDate DATE,
    FatherName NVARCHAR(150),
    MotherName NVARCHAR(150),
    ParentPhone NVARCHAR(15),
    AddressLine1 NVARCHAR(200),
    City NVARCHAR(100),
    State NVARCHAR(100),
    StudentStatus NVARCHAR(20),
    IsActive BIT,
    CreatedAt DATETIME DEFAULT GETDATE()
);

CREATE TABLE Teachers (
    TeacherId INT IDENTITY PRIMARY KEY,
    SchoolId INT,
    UserId INT UNIQUE,
    FullName NVARCHAR(150),
    Phone NVARCHAR(15),
    Email NVARCHAR(100),
    JoiningDate DATE,
    Qualification NVARCHAR(100),
    IsActive BIT,
    CreatedAt DATETIME DEFAULT GETDATE()
);

CREATE TABLE Subjects (
    SubjectId INT IDENTITY PRIMARY KEY,
    SchoolId INT,
    ClassId INT,
    SubjectName NVARCHAR(100),
    IsActive BIT
);

CREATE TABLE Exams (
    ExamId INT IDENTITY PRIMARY KEY,
    SchoolId INT,
    ClassId INT,
    ExamName NVARCHAR(100),
    AcademicYear NVARCHAR(20),
    ExamDate DATE,
    IsActive BIT,
    CreatedAt DATETIME DEFAULT GETDATE()
);

CREATE TABLE ExamSubjects (
    ExamSubjectId INT IDENTITY PRIMARY KEY,
    ExamId INT,
    SubjectId INT,
    MaxMarks INT
);

CREATE TABLE StudentMarks (
    StudentMarkId INT IDENTITY PRIMARY KEY,
    SchoolId INT,
    StudentId INT,
    ExamId INT,
    SubjectId INT,
    MarksObtained INT,
    MaxMarks INT,
    CreatedAt DATETIME DEFAULT GETDATE()
);

CREATE TABLE FeeStructures (
    FeeStructureId INT IDENTITY PRIMARY KEY,
    SchoolId INT,
    ClassId INT,
    FeeName NVARCHAR(100),
    MonthlyAmount DECIMAL(10,2),
    IsActive BIT,
    CreatedAt DATETIME DEFAULT GETDATE()
);

CREATE TABLE SubscriptionPlans (
    PlanId INT IDENTITY PRIMARY KEY,
    PlanName NVARCHAR(50),
    PriceMonthly DECIMAL(10,2),
    MaxStudents INT,
    IsActive BIT
);

/* =========================================================
   EXTENSION TABLES (FROM SECOND SCRIPT – ADDED)
========================================================= */

CREATE TABLE TimeTables (
    TimeTableId INT IDENTITY PRIMARY KEY,
    SchoolId INT,
    ClassId INT,
    SectionId INT,
    DayOfWeek NVARCHAR(10),
    PeriodNo INT,
    SubjectId INT,
    TeacherId INT,
    StartTime TIME,
    EndTime TIME
);

CREATE TABLE Attendance (
    AttendanceId INT IDENTITY PRIMARY KEY,
    SchoolId INT,
    UserId INT,
    AttendanceDate DATE,
    Status NVARCHAR(20),
    Source NVARCHAR(20)
);

CREATE TABLE StudentFees (
    StudentFeeId INT IDENTITY PRIMARY KEY,
    StudentId INT,
    FeeMonth NVARCHAR(20),
    Amount DECIMAL(10,2),
    PaymentMode NVARCHAR(20),
    Status NVARCHAR(20),
    PaidOn DATE
);

CREATE TABLE Leaves (
    LeaveId INT IDENTITY PRIMARY KEY,
    SchoolId INT,
    UserId INT,
    FromDate DATE,
    ToDate DATE,
    Reason NVARCHAR(200),
    Status NVARCHAR(20),
    AppliedOn DATE,
    ApprovedBy INT,
    ApprovedOn DATE
);

CREATE TABLE Homework (
    HomeworkId INT IDENTITY PRIMARY KEY,
    SchoolId INT,
    ClassId INT,
    SectionId INT,
    SubjectId INT,
    TeacherId INT,
    HomeworkType NVARCHAR(20),
    Title NVARCHAR(100),
    Description NVARCHAR(300),
    AssignedDate DATE,
    DueDate DATE
);

CREATE TABLE StudyMaterials (
    StudyMaterialId INT IDENTITY PRIMARY KEY,
    SchoolId INT,
    ClassId INT,
    SubjectId INT,
    TeacherId INT,
    Title NVARCHAR(100),
    Description NVARCHAR(300),
    MaterialType NVARCHAR(20),
    FileUrl NVARCHAR(300),
    UploadedOn DATETIME DEFAULT GETDATE(),
    IsActive BIT
);

CREATE TABLE SchoolCalendarEvents (
    EventId INT IDENTITY PRIMARY KEY,
    SchoolId INT,
    EventType NVARCHAR(30),
    EventName NVARCHAR(150),
    StartDate DATE,
    EndDate DATE,
    ClassId INT NULL,
    SectionId INT NULL,
    Description NVARCHAR(300),
    IsActive BIT,
    CreatedAt DATETIME DEFAULT GETDATE()
);

CREATE TABLE LibraryBooks (
    BookId INT IDENTITY PRIMARY KEY,
    SchoolId INT,
    BookTitle NVARCHAR(200),
    Author NVARCHAR(100),
    ISBN NVARCHAR(50),
    TotalCopies INT,
    AvailableCopies INT
);

CREATE TABLE BookIssues (
    IssueId INT IDENTITY PRIMARY KEY,
    BookId INT,
    StudentId INT,
    IssueDate DATE,
    DueDate DATE,
    ReturnDate DATE,
    FineAmount DECIMAL(10,2)
);

CREATE TABLE NotificationLogs (
    NotificationId INT IDENTITY PRIMARY KEY,
    SchoolId INT,
    UserId INT,
    Message NVARCHAR(300),
    Channel NVARCHAR(20),
    Status NVARCHAR(20),
    SentAt DATETIME
);

CREATE TABLE Subscriptions (
    SubscriptionId INT IDENTITY PRIMARY KEY,
    SchoolId INT,
    PlanId INT,
    StartDate DATE,
    EndDate DATE,
    IsTrial BIT,
    IsActive BIT
);

CREATE TABLE SubscriptionPayments (
    PaymentId INT IDENTITY PRIMARY KEY,
    SchoolId INT,
    SubscriptionId INT,
    PaymentGateway NVARCHAR(50),
    PaymentOrderId NVARCHAR(100),
    PaymentTransactionId NVARCHAR(100),
    Amount DECIMAL(10,2),
    Currency NVARCHAR(10),
    PaymentStatus NVARCHAR(20),
    PaymentDate DATETIME,
    BillingCycle NVARCHAR(20),
    CreatedAt DATETIME DEFAULT GETDATE()
);

PRINT 'VidyaOS database created successfully (merged & conflict-free).';

INSERT INTO Users (SchoolId, Username, PasswordHash, Role, Phone, Email, IsFirstLogin, IsActive)
VALUES
(1,'teacher3@vps','HASH','Teacher','9999990006','t3@vps.edu',1,1),
(1,'teacher4@vps','HASH','Teacher','9999990007','t4@vps.edu',1,1),
(1,'teacher5@vps','HASH','Teacher','9999990008','t5@vps.edu',1,1),

(1,'student3@vps','HASH','Student','9999990009','s3@vps.edu',1,1),
(1,'student4@vps','HASH','Student','9999990010','s4@vps.edu',1,1),
(1,'student5@vps','HASH','Student','9999990011','s5@vps.edu',1,1);



INSERT INTO Schools VALUES
('Vidya Public School','VPS','REG-DEL-001',1998,'CBSE','CBSE-11301',
 'info@vps.edu','9876543210','Sector 15','Delhi','Delhi','110015',0,1,GETDATE()),

('Green Valley School','GVS','REG-MH-002',2004,'State Board',NULL,
 'contact@gvs.edu','9898989898','MG Road','Pune','Maharashtra','411001',0,1,GETDATE()),

('St Mary Convent','SMC','REG-UP-003',1992,'ICSE','ICSE-88321',
 'office@smc.edu','9123456789','Civil Lines','Lucknow','UP','226001',0,1,GETDATE()),

('Bright Future School','BFS','REG-RJ-004',2010,'CBSE','CBSE-77889',
 'hello@bfs.edu','9012345678','Vaishali Nagar','Jaipur','Rajasthan','302021',0,1,GETDATE()),

('Oxford Model School','OMS','REG-GJ-005',2001,'CBSE','CBSE-55667',
 'info@oms.edu','9090909090','CG Road','Ahmedabad','Gujarat','380009',0,1,GETDATE());


 INSERT INTO Users VALUES
(1,'admin@vps','HASH','Admin','9999990001','admin@vps.edu',0,1,GETDATE()),
(1,'teacher1@vps','HASH','Teacher','9999990002','t1@vps.edu',1,1,GETDATE()),
(1,'teacher2@vps','HASH','Teacher','9999990003','t2@vps.edu',1,1,GETDATE()),
(1,'student1@vps','HASH','Student','9999990004','s1@vps.edu',1,1,GETDATE()),
(1,'student2@vps','HASH','Student','9999990005','s2@vps.edu',1,1,GETDATE());

INSERT INTO Classes VALUES
(1,'1',1),
(1,'2',1),
(1,'3',1),
(1,'4',1),
(1,'5',1);

INSERT INTO Sections VALUES
(1,1,'A',0,1),
(1,1,'B',0,1),
(1,2,'A',0,1),
(1,2,'B',0,1),
(1,3,'A',0,1);

INSERT INTO Teachers
(SchoolId, UserId, FullName, Phone, Email, JoiningDate, Qualification, IsActive)
VALUES
(1,2,'Amit Sharma','9876500011','amit@vps.edu','2018-06-01','MSc Maths',1),
(1,3,'Neha Verma','9876500022','neha@vps.edu','2019-07-10','MA English',1),
(1,6,'Rohit Mehta','9876500033','rohit@vps.edu','2020-01-15','BEd',1),
(1,7,'Pooja Singh','9876500044','pooja@vps.edu','2017-04-12','MSc Science',1),
(1,8,'Sunil Jain','9876500055','sunil@vps.edu','2016-03-01','MEd',1);

INSERT INTO Students
(SchoolId, UserId, AdmissionNo, RollNo, FirstName, LastName, Gender, DOB,
 ClassId, SectionId, AcademicYear, AdmissionDate,
 FatherName, MotherName, ParentPhone,
 AddressLine1, City, State,
 StudentStatus, IsActive)
VALUES
(1,4,'VPS-001',1,'Rahul','Kumar','Male','2014-06-12',1,1,'2025-26','2025-04-01',
 'Suresh Kumar','Sunita','9876540001','Sector 15','Delhi','Delhi','Active',1),

(1,5,'VPS-002',2,'Anita','Sharma','Female','2014-09-20',1,2,'2025-26','2025-04-01',
 'Ramesh Sharma','Meena','9876540002','Sector 15','Delhi','Delhi','Active',1),

(1,9,'VPS-003',3,'Rohit','Verma','Male','2013-11-10',2,3,'2025-26','2025-04-01',
 'Amit Verma','Neha','9876540003','Sector 15','Delhi','Delhi','Active',1),

(1,10,'VPS-004',4,'Pooja','Singh','Female','2015-02-05',2,4,'2025-26','2025-04-01',
 'Manoj Singh','Kiran','9876540004','Sector 15','Delhi','Delhi','Active',1),

(1,11,'VPS-005',5,'Aman','Gupta','Male','2014-12-18',3,5,'2025-26','2025-04-01',
 'Rajesh Gupta','Sunita','9876540005','Sector 15','Delhi','Delhi','Active',1);


 INSERT INTO Subjects VALUES
(1,1,'Mathematics',1),
(1,1,'English',1),
(1,2,'Science',1),
(1,2,'Hindi',1),
(1,3,'Social Studies',1);

INSERT INTO Exams VALUES
(1,1,'Unit Test 1','2025-26','2025-07-15',1,GETDATE()),
(1,1,'Mid Term','2025-26','2025-09-10',1,GETDATE()),
(1,2,'Unit Test','2025-26','2025-07-20',1,GETDATE()),
(1,3,'Final Exam','2025-26','2026-03-10',1,GETDATE()),
(1,4,'Assessment','2025-26','2025-08-25',1,GETDATE());

INSERT INTO StudentMarks VALUES
(1,1,1,1,18,20,GETDATE()),
(1,1,1,2,16,20,GETDATE()),
(1,2,2,1,17,20,GETDATE()),
(1,3,3,3,65,80,GETDATE()),
(1,4,4,4,70,80,GETDATE());

INSERT INTO FeeStructures VALUES
(1,1,'Tuition Fee',1500,1,GETDATE()),
(1,2,'Tuition Fee',1600,1,GETDATE()),
(1,3,'Tuition Fee',1700,1,GETDATE()),
(1,4,'Tuition Fee',1800,1,GETDATE()),
(1,5,'Tuition Fee',2000,1,GETDATE());

INSERT INTO Attendance VALUES
(1,4,'2025-07-01','Present','Manual'),
(1,5,'2025-07-01','Present','Manual'),
(1,4,'2025-07-02','Absent','Manual'),
(1,5,'2025-07-02','Present','Manual'),
(1,2,'2025-07-01','Present','Biometric');

INSERT INTO SubscriptionPlans VALUES
('Trial',0,100,1),
('Basic',999,300,1),
('Standard',1999,600,1),
('Premium',2999,2000,1),
('Enterprise',4999,5000,1);



