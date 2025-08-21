using System;
using System.Collections.Generic;
using LabAssistantOPP_LAO.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LabAssistantOPP_LAO.Models.Data;

public partial class LabOopChangeV6Context : DbContext
{
    public LabOopChangeV6Context()
    {
    }

    public LabOopChangeV6Context(DbContextOptions<LabOopChangeV6Context> options)
        : base(options)
    {
    }

    public virtual DbSet<AssignmentApproval> AssignmentApprovals { get; set; }

    public virtual DbSet<AssignmentDocument> AssignmentDocuments { get; set; }

    public virtual DbSet<AssignmentIngest> AssignmentIngests { get; set; }

    public virtual DbSet<Class> Classes { get; set; }

    public virtual DbSet<ClassHasLabAssignment> ClassHasLabAssignments { get; set; }

    public virtual DbSet<ClassSlot> ClassSlots { get; set; }

    public virtual DbSet<ClassSlotLog> ClassSlotLogs { get; set; }

    public virtual DbSet<FapClass> FapClasses { get; set; }

    public virtual DbSet<FapSemester> FapSemesters { get; set; }

    public virtual DbSet<FapStudent> FapStudents { get; set; }

    public virtual DbSet<LabAssignment> LabAssignments { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Semester> Semesters { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<StudentInClass> StudentInClasses { get; set; }

    public virtual DbSet<StudentLabAssignment> StudentLabAssignments { get; set; }

    public virtual DbSet<Teacher> Teachers { get; set; }

    public virtual DbSet<TestCase> TestCases { get; set; }

    public virtual DbSet<TestCaseResult> TestCaseResults { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VectorIndex> VectorIndices { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=(local);database= LabOopChange_v6;Trusted_Connection=SSPI;Encrypt=false;TrustServerCertificate=true");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AssignmentApproval>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Assignme__3213E83F49A7C909");

            entity.ToTable("Assignment_Approval");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ActedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("acted_at");
            entity.Property(e => e.Action)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("action");
            entity.Property(e => e.ActionNote)
                .HasMaxLength(1000)
                .HasColumnName("action_note");
            entity.Property(e => e.ActorId).HasColumnName("actor_id");
            entity.Property(e => e.AssignmentId).HasColumnName("assignment_id");

            entity.HasOne(d => d.Actor).WithMany(p => p.AssignmentApprovals)
                .HasForeignKey(d => d.ActorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AA_Actor");

            entity.HasOne(d => d.Assignment).WithMany(p => p.AssignmentApprovals)
                .HasForeignKey(d => d.AssignmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AA_Assign");
        });

        modelBuilder.Entity<AssignmentDocument>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Assignme__3213E83FDCA47865");

            entity.ToTable("Assignment_Document");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AssignmentId).HasColumnName("assignment_id");
            entity.Property(e => e.FileName)
                .HasMaxLength(255)
                .HasColumnName("file_name");
            entity.Property(e => e.FilePath)
                .HasMaxLength(500)
                .HasColumnName("file_path");
            entity.Property(e => e.MimeType)
                .HasMaxLength(100)
                .HasColumnName("mime_type");
            entity.Property(e => e.UploadedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("uploaded_at");
            entity.Property(e => e.UploadedBy).HasColumnName("uploaded_by");

            entity.HasOne(d => d.Assignment).WithMany(p => p.AssignmentDocuments)
                .HasForeignKey(d => d.AssignmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AD_Assign");

            entity.HasOne(d => d.UploadedByNavigation).WithMany(p => p.AssignmentDocuments)
                .HasForeignKey(d => d.UploadedBy)
                .HasConstraintName("FK_AD_User");
        });

        modelBuilder.Entity<AssignmentIngest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Assignme__3213E83FBAF09A9A");

            entity.ToTable("Assignment_Ingest");

            entity.HasIndex(e => new { e.AssignmentId, e.DocumentId, e.VectorIndexId }, "UQ_AI").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AssignmentId).HasColumnName("assignment_id");
            entity.Property(e => e.ChunkOverlap).HasColumnName("chunk_overlap");
            entity.Property(e => e.ChunkSize).HasColumnName("chunk_size");
            entity.Property(e => e.ChunksIngested).HasColumnName("chunks_ingested");
            entity.Property(e => e.DocumentId).HasColumnName("document_id");
            entity.Property(e => e.LastChunkedAt).HasColumnName("last_chunked_at");
            entity.Property(e => e.Message)
                .HasMaxLength(1000)
                .HasColumnName("message");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("NotProcessed")
                .HasColumnName("status");
            entity.Property(e => e.VectorIndexId).HasColumnName("vector_index_id");

            entity.HasOne(d => d.Assignment).WithMany(p => p.AssignmentIngests)
                .HasForeignKey(d => d.AssignmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AI_Assign");

            entity.HasOne(d => d.Document).WithMany(p => p.AssignmentIngests)
                .HasForeignKey(d => d.DocumentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AI_Doc");

            entity.HasOne(d => d.VectorIndex).WithMany(p => p.AssignmentIngests)
                .HasForeignKey(d => d.VectorIndexId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AI_VIndex");
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Class__3213E83F98A4F3A1");

            entity.ToTable("Class");

            entity.HasIndex(e => new { e.ClassCode, e.SemesterId }, "UQ_Class").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AcademicYear)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("academic_year");
            entity.Property(e => e.ClassCode)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("class_code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.LocToPass).HasColumnName("loc_to_pass");
            entity.Property(e => e.SemesterId).HasColumnName("semester_id");
            entity.Property(e => e.SubjectCode)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("subject_code");
            entity.Property(e => e.TeacherId).HasColumnName("teacher_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.Semester).WithMany(p => p.Classes)
                .HasForeignKey(d => d.SemesterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Class_Semester");

            entity.HasOne(d => d.Teacher).WithMany(p => p.Classes)
                .HasForeignKey(d => d.TeacherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Class_Teacher");
        });

        modelBuilder.Entity<ClassHasLabAssignment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Class_Ha__3213E83FC1BAFBC9");

            entity.ToTable("Class_Has_Lab_Assignment");

            entity.HasIndex(e => new { e.ClassId, e.AssignmentId }, "UQ_Class_Assignment").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AssignmentId).HasColumnName("assignment_id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.CloseAt).HasColumnName("close_at");
            entity.Property(e => e.OpenAt).HasColumnName("open_at");

            entity.HasOne(d => d.Assignment).WithMany(p => p.ClassHasLabAssignments)
                .HasForeignKey(d => d.AssignmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CLA_Assignment");

            entity.HasOne(d => d.Class).WithMany(p => p.ClassHasLabAssignments)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CLA_Class");
        });

        modelBuilder.Entity<ClassSlot>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Class_Sl__3213E83FAC822314");

            entity.ToTable("Class_Slot");

            entity.HasIndex(e => new { e.ClassId, e.SlotNo }, "UQ_Class_Slot").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.IsEnabled).HasColumnName("is_enabled");
            entity.Property(e => e.Note)
                .HasMaxLength(500)
                .HasColumnName("note");
            entity.Property(e => e.ServerEndpoint)
                .HasMaxLength(500)
                .HasColumnName("server_endpoint");
            entity.Property(e => e.SlotNo).HasColumnName("slot_no");
            entity.Property(e => e.StartTime).HasColumnName("start_time");

            entity.HasOne(d => d.Class).WithMany(p => p.ClassSlots)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Slot_Class");
        });

        modelBuilder.Entity<ClassSlotLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Class_Sl__3213E83FA8B2EA1B");

            entity.ToTable("Class_Slot_Log");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ActedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("acted_at");
            entity.Property(e => e.Action)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("action");
            entity.Property(e => e.ActorId).HasColumnName("actor_id");
            entity.Property(e => e.ClassSlotId).HasColumnName("class_slot_id");

            entity.HasOne(d => d.Actor).WithMany(p => p.ClassSlotLogs)
                .HasForeignKey(d => d.ActorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SlotLog_Actor");

            entity.HasOne(d => d.ClassSlot).WithMany(p => p.ClassSlotLogs)
                .HasForeignKey(d => d.ClassSlotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SlotLog_Slot");
        });

        modelBuilder.Entity<FapClass>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__FAP_Clas__3213E83FE9FD1386");

            entity.ToTable("FAP_Class");

            entity.HasIndex(e => e.Code, "UQ__FAP_Clas__357D4CF9C7AD1A39").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("code");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.SemesterId).HasColumnName("semester_id");

            entity.HasOne(d => d.Semester).WithMany(p => p.FapClasses)
                .HasForeignKey(d => d.SemesterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__FAP_Class__semes__17F790F9");
        });

        modelBuilder.Entity<FapSemester>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__FAP_Seme__3213E83FEA94FDD2");

            entity.ToTable("FAP_Semester");

            entity.HasIndex(e => e.Code, "UQ__FAP_Seme__357D4CF9EFF339C4").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("code");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<FapStudent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__FAP_Stud__3213E83F8B621A87");

            entity.ToTable("FAP_Student");

            entity.HasIndex(e => e.StudentCode, "UQ__FAP_Stud__6DF33C45B31DFADC").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.SemesterId).HasColumnName("semester_id");
            entity.Property(e => e.StudentCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("student_code");

            entity.HasOne(d => d.Class).WithMany(p => p.FapStudents)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__FAP_Stude__class__1CBC4616");

            entity.HasOne(d => d.Semester).WithMany(p => p.FapStudents)
                .HasForeignKey(d => d.SemesterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__FAP_Stude__semes__1BC821DD");
        });

        modelBuilder.Entity<LabAssignment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Lab_Assi__3213E83F6DE4C71C");

            entity.ToTable("Lab_Assignment");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ApprovedAt).HasColumnName("approved_at");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.LocTotal).HasColumnName("loc_total");
            entity.Property(e => e.ReviewNote)
                .HasMaxLength(1000)
                .HasColumnName("review_note");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending")
                .HasColumnName("status");
            entity.Property(e => e.TeacherId).HasColumnName("teacher_id");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.LabAssignmentApprovedByNavigations)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("FK_LA_ApprovedBy");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.LabAssignmentCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_LA_CreatedBy");

            entity.HasOne(d => d.Teacher).WithMany(p => p.LabAssignmentTeachers)
                .HasForeignKey(d => d.TeacherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LA_Teacher");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Role__3213E83F389A165C");

            entity.ToTable("Role");

            entity.HasIndex(e => e.Name, "UQ_Role_Name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Semester>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Semester__3213E83FA74D13E9");

            entity.ToTable("Semester");

            entity.HasIndex(e => e.Name, "UQ_Semester_Name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Student__3213E83F39922CD1");

            entity.ToTable("Student");

            entity.HasIndex(e => e.StudentCode, "UQ_Student_Code").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(500)
                .HasColumnName("address");
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.Gender).HasColumnName("gender");
            entity.Property(e => e.Major)
                .HasMaxLength(255)
                .HasColumnName("major");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("phone");
            entity.Property(e => e.StudentCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("student_code");

            entity.HasOne(d => d.IdNavigation).WithOne(p => p.Student)
                .HasForeignKey<Student>(d => d.Id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Student_User");
        });

        modelBuilder.Entity<StudentInClass>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Student___3213E83F254529EB");

            entity.ToTable("Student_In_Class");

            entity.HasIndex(e => new { e.ClassId, e.StudentId }, "UQ_SIC").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.StudentId).HasColumnName("student_id");

            entity.HasOne(d => d.Class).WithMany(p => p.StudentInClasses)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SIC_Class");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentInClasses)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SIC_Student");
        });

        modelBuilder.Entity<StudentLabAssignment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Student___3213E83F83912FBF");

            entity.ToTable("Student_Lab_Assignment");

            entity.HasIndex(e => new { e.AssignmentId, e.StudentId, e.SemesterId }, "UQ_SLA").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AssignmentId).HasColumnName("assignment_id");
            entity.Property(e => e.LocResult).HasColumnName("loc_result");
            entity.Property(e => e.ManualReason).HasColumnName("manual_reason");
            entity.Property(e => e.ManuallyEdited).HasColumnName("manually_edited");
            entity.Property(e => e.SemesterId).HasColumnName("semester_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Draft")
                .HasColumnName("status");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.SubmissionZip)
                .HasMaxLength(500)
                .HasColumnName("submission_zip");
            entity.Property(e => e.SubmittedAt).HasColumnName("submitted_at");

            entity.HasOne(d => d.Assignment).WithMany(p => p.StudentLabAssignments)
                .HasForeignKey(d => d.AssignmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SLA_Assign");

            entity.HasOne(d => d.Semester).WithMany(p => p.StudentLabAssignments)
                .HasForeignKey(d => d.SemesterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SLA_Sem");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentLabAssignments)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SLA_Student");
        });

        modelBuilder.Entity<Teacher>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Teacher__3213E83F8F6D20F5");

            entity.ToTable("Teacher");

            entity.HasIndex(e => e.TeacherCode, "UQ_Teacher_Code").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.AcademicDegree)
                .HasMaxLength(50)
                .HasColumnName("academic_degree");
            entity.Property(e => e.Address)
                .HasMaxLength(500)
                .HasColumnName("address");
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.Gender).HasColumnName("gender");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("phone");
            entity.Property(e => e.TeacherCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("teacher_code");

            entity.HasOne(d => d.IdNavigation).WithOne(p => p.Teacher)
                .HasForeignKey<Teacher>(d => d.Id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Teacher_User");
        });

        modelBuilder.Entity<TestCase>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TestCase__3213E83F8D39F545");

            entity.ToTable("TestCase");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AssignmentId).HasColumnName("assignment_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.ExpectedOutput).HasColumnName("expected_output");
            entity.Property(e => e.Input).HasColumnName("input");
            entity.Property(e => e.Loc).HasColumnName("loc");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.Assignment).WithMany(p => p.TestCases)
                .HasForeignKey(d => d.AssignmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TC_Assign");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.TestCases)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_TC_Creator");
        });

        modelBuilder.Entity<TestCaseResult>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TestCase__3213E83F5BF7FE25");

            entity.ToTable("TestCaseResult");

            entity.HasIndex(e => new { e.StudentLabAssignmentId, e.TestCaseId }, "UQ_TCR").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ActualOutput).HasColumnName("actual_output");
            entity.Property(e => e.IsPassed).HasColumnName("is_passed");
            entity.Property(e => e.StudentLabAssignmentId).HasColumnName("student_lab_assignment_id");
            entity.Property(e => e.TestCaseId).HasColumnName("test_case_id");

            entity.HasOne(d => d.StudentLabAssignment).WithMany(p => p.TestCaseResults)
                .HasForeignKey(d => d.StudentLabAssignmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TCR_SLA");

            entity.HasOne(d => d.TestCase).WithMany(p => p.TestCaseResults)
                .HasForeignKey(d => d.TestCaseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TCR_TestCase");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__User__3213E83F0FB548D6");

            entity.ToTable("User");

            entity.HasIndex(e => e.Email, "UQ_User_Email").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.UserName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("user_name");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_User_Role");
        });

        modelBuilder.Entity<VectorIndex>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Vector_I__3213E83FE2B9D15B");

            entity.ToTable("Vector_Index");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.ExternalId)
                .HasMaxLength(255)
                .HasColumnName("external_id");
            entity.Property(e => e.IndexName)
                .HasMaxLength(255)
                .HasColumnName("index_name");
            entity.Property(e => e.Provider)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("provider");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
