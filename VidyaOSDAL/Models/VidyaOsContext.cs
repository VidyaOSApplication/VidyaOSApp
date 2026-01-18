using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace VidyaOSDAL.Models;

public partial class VidyaOsContext : DbContext
{
    public VidyaOsContext()
    {
    }

    public VidyaOsContext(DbContextOptions<VidyaOsContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AdmissionYearSequence> AdmissionYearSequences { get; set; }

    public virtual DbSet<Attendance> Attendances { get; set; }

    public virtual DbSet<BookIssue> BookIssues { get; set; }

    public virtual DbSet<Class> Classes { get; set; }

    public virtual DbSet<Exam> Exams { get; set; }

    public virtual DbSet<ExamClass> ExamClasses { get; set; }

    public virtual DbSet<ExamResultSummary> ExamResultSummaries { get; set; }

    public virtual DbSet<ExamSubject> ExamSubjects { get; set; }

    public virtual DbSet<FeeStructure> FeeStructures { get; set; }

    public virtual DbSet<Homework> Homeworks { get; set; }

    public virtual DbSet<LeaveRequest> Leaves { get; set; }

    public virtual DbSet<LibraryBook> LibraryBooks { get; set; }

    public virtual DbSet<NotificationLog> NotificationLogs { get; set; }

    public virtual DbSet<School> Schools { get; set; }

    public virtual DbSet<SchoolCalendarEvent> SchoolCalendarEvents { get; set; }

    public virtual DbSet<Section> Sections { get; set; }

    public virtual DbSet<Stream> Streams { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<StudentFee> StudentFees { get; set; }

    public virtual DbSet<StudentMark> StudentMarks { get; set; }

    public virtual DbSet<StudyMaterial> StudyMaterials { get; set; }

    public virtual DbSet<Subject> Subjects { get; set; }

    public virtual DbSet<Subscription> Subscriptions { get; set; }

    public virtual DbSet<SubscriptionPayment> SubscriptionPayments { get; set; }

    public virtual DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }

    public virtual DbSet<Teacher> Teachers { get; set; }

    public virtual DbSet<TimeTable> TimeTables { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=VidyaOS;Trusted_Connection=True;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdmissionYearSequence>(entity =>
        {
            entity.HasKey(e => new { e.SchoolId, e.AdmissionYear }).HasName("PK__Admissio__88E0A12B90AFAD47");
        });

        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.AttendanceId).HasName("PK__Attendan__8B69261C514C1CFB");

            entity.ToTable("Attendance");

            entity.Property(e => e.Source).HasMaxLength(20);
            entity.Property(e => e.Status).HasMaxLength(20);
        });

        modelBuilder.Entity<BookIssue>(entity =>
        {
            entity.HasKey(e => e.IssueId).HasName("PK__BookIssu__6C86160417F43888");

            entity.Property(e => e.FineAmount).HasColumnType("decimal(10, 2)");
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasKey(e => e.ClassId).HasName("PK__Classes__CB1927C0CB38D3AF");

            entity.Property(e => e.ClassName).HasMaxLength(20);
        });

        modelBuilder.Entity<Exam>(entity =>
        {
            entity.HasKey(e => e.ExamId).HasName("PK__Exams__297521C7823E868C");

            entity.Property(e => e.AcademicYear).HasMaxLength(20);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ExamName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Draft");
        });

        modelBuilder.Entity<ExamClass>(entity =>
        {
            entity.HasKey(e => e.ExamClassId).HasName("PK__ExamClas__42309F5F242EED52");

            entity.HasIndex(e => new { e.ExamId, e.ClassId }, "UX_Exam_Class").IsUnique();

            entity.HasOne(d => d.Exam).WithMany(p => p.ExamClasses)
                .HasForeignKey(d => d.ExamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExamClasses_Exam");
        });

        modelBuilder.Entity<ExamResultSummary>(entity =>
        {
            entity.HasKey(e => e.ResultId).HasName("PK__ExamResu__97690208C8345F2F");

            entity.ToTable("ExamResultSummary");

            entity.HasIndex(e => new { e.StudentId, e.ExamId }, "UX_Student_Exam_Result").IsUnique();

            entity.Property(e => e.GeneratedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Grade).HasMaxLength(10);
            entity.Property(e => e.Percentage).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.ResultStatus).HasMaxLength(20);
        });

        modelBuilder.Entity<ExamSubject>(entity =>
        {
            entity.HasKey(e => e.ExamSubjectId).HasName("PK__ExamSubj__C5C4E54DAA53286C");

            entity.HasIndex(e => new { e.ExamId, e.ClassId, e.SubjectId }, "UX_Exam_Class_Subject").IsUnique();

            entity.HasOne(d => d.Exam).WithMany(p => p.ExamSubjects)
                .HasForeignKey(d => d.ExamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExamSubjects_Exam");

            entity.HasOne(d => d.Subject).WithMany(p => p.ExamSubjects)
                .HasForeignKey(d => d.SubjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExamSubjects_Subject");
        });

        modelBuilder.Entity<FeeStructure>(entity =>
        {
            entity.HasKey(e => e.FeeStructureId).HasName("PK__FeeStruc__DDDC2504859D4FD5");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FeeName).HasMaxLength(100);
            entity.Property(e => e.MonthlyAmount).HasColumnType("decimal(10, 2)");
        });

        modelBuilder.Entity<Homework>(entity =>
        {
            entity.HasKey(e => e.HomeworkId).HasName("PK__Homework__FDE46A72A5FECEEE");

            entity.ToTable("Homework");

            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.HomeworkType).HasMaxLength(20);
            entity.Property(e => e.Title).HasMaxLength(100);
        });

        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.HasKey(e => e.LeaveId).HasName("PK__Leaves__796DB959AF67D1E5");

            entity.Property(e => e.Reason).HasMaxLength(200);
            entity.Property(e => e.Status).HasMaxLength(20);
        });

        modelBuilder.Entity<LibraryBook>(entity =>
        {
            entity.HasKey(e => e.BookId).HasName("PK__LibraryB__3DE0C207BD36D295");

            entity.Property(e => e.Author).HasMaxLength(100);
            entity.Property(e => e.BookTitle).HasMaxLength(200);
            entity.Property(e => e.Isbn)
                .HasMaxLength(50)
                .HasColumnName("ISBN");
        });

        modelBuilder.Entity<NotificationLog>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E126BEF1719");

            entity.Property(e => e.Channel).HasMaxLength(20);
            entity.Property(e => e.Message).HasMaxLength(300);
            entity.Property(e => e.SentAt).HasColumnType("datetime");
            entity.Property(e => e.Status).HasMaxLength(20);
        });

        modelBuilder.Entity<School>(entity =>
        {
            entity.HasKey(e => e.SchoolId).HasName("PK__Schools__3DA4675B40A9D7CB");

            entity.HasIndex(e => e.SchoolCode, "UQ__Schools__38CCE1FA18D78AB3").IsUnique();

            entity.Property(e => e.AddressLine1).HasMaxLength(200);
            entity.Property(e => e.AdmissionSeq).HasDefaultValue(0);
            entity.Property(e => e.AffiliationNumber).HasMaxLength(50);
            entity.Property(e => e.BoardType).HasMaxLength(50);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(15);
            entity.Property(e => e.Pincode).HasMaxLength(10);
            entity.Property(e => e.RegistrationNumber).HasMaxLength(50);
            entity.Property(e => e.SchoolCode).HasMaxLength(10);
            entity.Property(e => e.SchoolName).HasMaxLength(200);
            entity.Property(e => e.State).HasMaxLength(100);
        });

        modelBuilder.Entity<SchoolCalendarEvent>(entity =>
        {
            entity.HasKey(e => e.EventId).HasName("PK__SchoolCa__7944C810447EC27D");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.EventName).HasMaxLength(150);
            entity.Property(e => e.EventType).HasMaxLength(30);
        });

        modelBuilder.Entity<Section>(entity =>
        {
            entity.HasKey(e => e.SectionId).HasName("PK__Sections__80EF0872D900617C");

            entity.Property(e => e.RollSeq).HasDefaultValue(0);
            entity.Property(e => e.SectionName).HasMaxLength(10);
        });

        modelBuilder.Entity<Stream>(entity =>
        {
            entity.HasKey(e => e.StreamId).HasName("PK__Streams__07C87A924187F0A7");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.StreamName).HasMaxLength(50);
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK__Students__32C52B9950DA9478");

            entity.HasIndex(e => e.UserId, "UQ__Students__1788CC4D19970CC5").IsUnique();

            entity.HasIndex(e => e.AdmissionNo, "UQ__Students__C97E2711EFBB1EA9").IsUnique();

            entity.Property(e => e.AcademicYear).HasMaxLength(20);
            entity.Property(e => e.AddressLine1).HasMaxLength(200);
            entity.Property(e => e.AdmissionNo).HasMaxLength(50);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Dob).HasColumnName("DOB");
            entity.Property(e => e.FatherName).HasMaxLength(150);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.Gender).HasMaxLength(10);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.MotherName).HasMaxLength(150);
            entity.Property(e => e.ParentPhone).HasMaxLength(15);
            entity.Property(e => e.State).HasMaxLength(100);
            entity.Property(e => e.StudentStatus).HasMaxLength(20);
        });

        modelBuilder.Entity<StudentFee>(entity =>
        {
            entity.HasKey(e => e.StudentFeeId).HasName("PK__StudentF__9D02D8734915D4B7");

            entity.Property(e => e.Amount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.FeeMonth).HasMaxLength(20);
            entity.Property(e => e.PaymentMode).HasMaxLength(20);
            entity.Property(e => e.Status).HasMaxLength(20);
        });

        modelBuilder.Entity<StudentMark>(entity =>
        {
            entity.HasKey(e => e.StudentMarkId).HasName("PK__StudentM__1B7251FCE75F2F5F");

            entity.HasIndex(e => new { e.StudentId, e.ExamId, e.SubjectId }, "UX_Student_Exam_Subject").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsAbsent).HasDefaultValue(false);
        });

        modelBuilder.Entity<StudyMaterial>(entity =>
        {
            entity.HasKey(e => e.StudyMaterialId).HasName("PK__StudyMat__CB6A618CD8F8675E");

            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.FileUrl).HasMaxLength(300);
            entity.Property(e => e.MaterialType).HasMaxLength(20);
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.UploadedOn)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.SubjectId).HasName("PK__Subjects__AC1BA3A84119C6CF");

            entity.HasIndex(e => new { e.SchoolId, e.ClassId, e.SubjectName }, "UX_Subject").IsUnique();

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.SubjectName).HasMaxLength(100);
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.SubscriptionId).HasName("PK__Subscrip__9A2B249D39B9869F");
        });

        modelBuilder.Entity<SubscriptionPayment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Subscrip__9B556A38BE96567B");

            entity.Property(e => e.Amount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.BillingCycle).HasMaxLength(20);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Currency).HasMaxLength(10);
            entity.Property(e => e.PaymentDate).HasColumnType("datetime");
            entity.Property(e => e.PaymentGateway).HasMaxLength(50);
            entity.Property(e => e.PaymentOrderId).HasMaxLength(100);
            entity.Property(e => e.PaymentStatus).HasMaxLength(20);
            entity.Property(e => e.PaymentTransactionId).HasMaxLength(100);
        });

        modelBuilder.Entity<SubscriptionPlan>(entity =>
        {
            entity.HasKey(e => e.PlanId).HasName("PK__Subscrip__755C22B73CD807CE");

            entity.Property(e => e.PlanName).HasMaxLength(50);
            entity.Property(e => e.PriceMonthly).HasColumnType("decimal(10, 2)");
        });

        modelBuilder.Entity<Teacher>(entity =>
        {
            entity.HasKey(e => e.TeacherId).HasName("PK__Teachers__EDF2596495A66758");

            entity.HasIndex(e => e.UserId, "UQ__Teachers__1788CC4D997E757E").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.Phone).HasMaxLength(15);
            entity.Property(e => e.Qualification).HasMaxLength(100);
        });

        modelBuilder.Entity<TimeTable>(entity =>
        {
            entity.HasKey(e => e.TimeTableId).HasName("PK__TimeTabl__C087BD0A3263A7F3");

            entity.Property(e => e.DayOfWeek).HasMaxLength(10);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4CA4A2679A");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(15);
            entity.Property(e => e.Role).HasMaxLength(20);
            entity.Property(e => e.Username).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
