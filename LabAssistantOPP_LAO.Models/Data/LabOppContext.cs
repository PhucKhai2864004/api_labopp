using System;
using System.Collections.Generic;
using LabAssistantOPP_LAO.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LabAssistantOPP_LAO.Models.Data;

public partial class LabOppContext : DbContext
{
    public LabOppContext()
    {
    }

    public LabOppContext(DbContextOptions<LabOppContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Class> Classes { get; set; }

    public virtual DbSet<ClassHasLabAssignment> ClassHasLabAssignments { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<UploadFile> Files { get; set; }

    public virtual DbSet<LabAssignment> LabAssignments { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<StudentInClass> StudentInClasses { get; set; }

    public virtual DbSet<StudentLabAssignment> StudentLabAssignments { get; set; }

    public virtual DbSet<Submission> Submissions { get; set; }

    public virtual DbSet<TestCase> TestCases { get; set; }

    public virtual DbSet<TestCaseResult> TestCaseResults { get; set; }

    public virtual DbSet<User> Users { get; set; }

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		if (!optionsBuilder.IsConfigured)
		{
			var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
			String ConnectionStr = config.GetConnectionString("DB");

			optionsBuilder.UseSqlServer(ConnectionStr);
		}
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Class__3213E83FA67600ED");

            entity.ToTable("Class");

            entity.Property(e => e.Id)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("id");
            entity.Property(e => e.AcademicYear)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("academic_year");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("created_by");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.LocToPass).HasColumnName("loc_to_pass");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.Semester).HasColumnName("semester");
            entity.Property(e => e.Subject)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("subject");
            entity.Property(e => e.TeacherId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("teacher_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Teacher).WithMany(p => p.Classes)
                .HasForeignKey(d => d.TeacherId)
                .HasConstraintName("FK__Class__teacher_i__412EB0B6");
        });

        modelBuilder.Entity<ClassHasLabAssignment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Class_Ha__3213E83F14D99497");

            entity.ToTable("Class_Has_Lab_Assignment");

            entity.Property(e => e.Id)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("id");
            entity.Property(e => e.AssignmentId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("assignment_id");
            entity.Property(e => e.ClassId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("class_id");

            entity.HasOne(d => d.Assignment).WithMany(p => p.ClassHasLabAssignments)
                .HasForeignKey(d => d.AssignmentId)
                .HasConstraintName("FK__Class_Has__assig__44FF419A");

            entity.HasOne(d => d.Class).WithMany(p => p.ClassHasLabAssignments)
                .HasForeignKey(d => d.ClassId)
                .HasConstraintName("FK__Class_Has__class__440B1D61");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Feedback__3213E83FDCC9F743");

            entity.ToTable("Feedback");

            entity.Property(e => e.Id)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("id");
            entity.Property(e => e.Comment)
                .HasColumnType("text")
                .HasColumnName("comment");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.SubmissionId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("submission_id");
            entity.Property(e => e.TeacherId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("teacher_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Submission).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.SubmissionId)
                .HasConstraintName("FK__Feedback__submis__5812160E");

            entity.HasOne(d => d.Teacher).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.TeacherId)
                .HasConstraintName("FK__Feedback__teache__59063A47");
        });

        modelBuilder.Entity<UploadFile>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__File__3213E83FE7AD3C0F");

            entity.ToTable("File");

            entity.Property(e => e.Id)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("id");
            entity.Property(e => e.MimeType)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("mime_type");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.OriginName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("origin_name");
            entity.Property(e => e.Path)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("path");
            entity.Property(e => e.Size).HasColumnName("size");
            entity.Property(e => e.UploadedAt)
                .HasColumnType("datetime")
                .HasColumnName("uploaded_at");
            entity.Property(e => e.UploadedBy)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("uploaded_by");

            entity.HasOne(d => d.UploadedByNavigation).WithMany(p => p.Files)
                .HasForeignKey(d => d.UploadedBy)
                .HasConstraintName("FK__File__uploaded_b__4F7CD00D");
        });

        modelBuilder.Entity<LabAssignment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Lab_Assi__3213E83F50F1442E");

            entity.ToTable("Lab_Assignment");

            entity.Property(e => e.Id)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("created_by");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.LocTotal).HasColumnName("loc_total");
            entity.Property(e => e.TeacherId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("teacher_id");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Teacher).WithMany(p => p.LabAssignments)
                .HasForeignKey(d => d.TeacherId)
                .HasConstraintName("FK__Lab_Assig__teach__3D5E1FD2");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Role__3213E83F552E757A");

            entity.ToTable("Role");

            entity.Property(e => e.Id)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("id");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("name");
        });

        modelBuilder.Entity<StudentInClass>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Student___3213E83F3A902367");

            entity.ToTable("Student_In_Class");

            entity.Property(e => e.Id)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("id");
            entity.Property(e => e.ClassId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("class_id");
            entity.Property(e => e.StudentId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("student_id");

            entity.HasOne(d => d.Class).WithMany(p => p.StudentInClasses)
                .HasForeignKey(d => d.ClassId)
                .HasConstraintName("FK__Student_I__class__47DBAE45");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentInClasses)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK__Student_I__stude__48CFD27E");
        });

        modelBuilder.Entity<StudentLabAssignment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Student___3213E83FE42B8C59");

            entity.ToTable("Student_Lab_Assignment");

            entity.Property(e => e.Id)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("id");
            entity.Property(e => e.AssignmentId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("assignment_id");
            entity.Property(e => e.StudentId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("student_id");

            entity.HasOne(d => d.Assignment).WithMany(p => p.StudentLabAssignments)
                .HasForeignKey(d => d.AssignmentId)
                .HasConstraintName("FK__Student_L__assig__4BAC3F29");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentLabAssignments)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK__Student_L__stude__4CA06362");
        });

        modelBuilder.Entity<Submission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Submissi__3213E83FFE2E38CC");

            entity.ToTable("Submission");

            entity.Property(e => e.Id)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("id");
            entity.Property(e => e.AssignmentId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("assignment_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("created_by");
            entity.Property(e => e.LocResult).HasColumnName("loc_result");
            entity.Property(e => e.ManualReason)
                .HasColumnType("text")
                .HasColumnName("manual_reason");
            entity.Property(e => e.ManuallyEdited).HasColumnName("manually_edited");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.StudentId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("student_id");
            entity.Property(e => e.SubmittedAt)
                .HasColumnType("datetime")
                .HasColumnName("submitted_at");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("updated_by");
            entity.Property(e => e.ZipCode)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("zip_code");

            entity.HasOne(d => d.Assignment).WithMany(p => p.Submissions)
                .HasForeignKey(d => d.AssignmentId)
                .HasConstraintName("FK__Submissio__assig__5441852A");

            entity.HasOne(d => d.Student).WithMany(p => p.Submissions)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK__Submissio__stude__534D60F1");

            entity.HasOne(d => d.ZipCodeNavigation).WithMany(p => p.Submissions)
                .HasForeignKey(d => d.ZipCode)
                .HasConstraintName("FK__Submissio__zip_c__5535A963");
        });

        modelBuilder.Entity<TestCase>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TestCase__3213E83FB3B55764");

            entity.ToTable("TestCase");

            entity.Property(e => e.Id)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("id");
            entity.Property(e => e.AssignmentId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("assignment_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("created_by");
            entity.Property(e => e.ExpectedOutput)
                .HasColumnType("text")
                .HasColumnName("expected_output");
            entity.Property(e => e.Loc).HasColumnName("loc");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Assignment).WithMany(p => p.TestCases)
                .HasForeignKey(d => d.AssignmentId)
                .HasConstraintName("FK__TestCase__assign__5BE2A6F2");
        });

        modelBuilder.Entity<TestCaseResult>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TestCase__3213E83FA4D61C17");

            entity.ToTable("TestCaseResult");

            entity.Property(e => e.Id)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("id");
            entity.Property(e => e.ActualOutput)
                .HasColumnType("text")
                .HasColumnName("actual_output");
            entity.Property(e => e.IsPassed).HasColumnName("is_passed");
            entity.Property(e => e.SubmissionId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("submission_id");
            entity.Property(e => e.TestCaseId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("test_case_id");

            entity.HasOne(d => d.Submission).WithMany(p => p.TestCaseResults)
                .HasForeignKey(d => d.SubmissionId)
                .HasConstraintName("FK__TestCaseR__submi__5EBF139D");

            entity.HasOne(d => d.TestCase).WithMany(p => p.TestCaseResults)
                .HasForeignKey(d => d.TestCaseId)
                .HasConstraintName("FK__TestCaseR__test___5FB337D6");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__User__3213E83FC5D9ADF8");

            entity.ToTable("User");

            entity.HasIndex(e => e.Email, "UQ__User__AB6E61646CCD7CBD").IsUnique();

            entity.Property(e => e.Id)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("created_by");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.RoleId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("role_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK__User__role_id__3A81B327");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
