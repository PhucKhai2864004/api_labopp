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
    public virtual DbSet<Promt> Promts { get; set; }
    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<StudentInClass> StudentInClasses { get; set; }

    public virtual DbSet<StudentLabAssignment> StudentLabAssignments { get; set; }

    public virtual DbSet<Submission> Submissions { get; set; }

    public virtual DbSet<Teacher> Teachers { get; set; }

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
            entity.HasKey(e => e.Id).HasName("PK__Class__3213E83FE7848E5B");

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
                .HasConstraintName("FK__Class__teacher_i__49C3F6B7");
        });

        modelBuilder.Entity<ClassHasLabAssignment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Class_Ha__3213E83FEAF73084");

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
                .HasConstraintName("FK__Class_Has__assig__4D94879B");

            entity.HasOne(d => d.Class).WithMany(p => p.ClassHasLabAssignments)
                .HasForeignKey(d => d.ClassId)
                .HasConstraintName("FK__Class_Has__class__4CA06362");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Feedback__3213E83FF8AC3682");

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
                .HasConstraintName("FK__Feedback__submis__60A75C0F");

            entity.HasOne(d => d.Teacher).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.TeacherId)
                .HasConstraintName("FK__Feedback__teache__619B8048");
        });

        modelBuilder.Entity<UploadFile>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__File__3213E83F0C81CAC6");

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
                .HasConstraintName("FK__File__uploaded_b__5812160E");
        });

        modelBuilder.Entity<LabAssignment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Lab_Assi__3213E83F1D251DD3");

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
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("status");
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
                .HasConstraintName("FK__Lab_Assig__teach__45F365D3");
        });

        modelBuilder.Entity<Promt>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Promt__3213E83FC4EDD200");

            entity.ToTable("Promt");

            entity.Property(e => e.Id)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("id");
            entity.Property(e => e.PromtDetail)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("promt_detail");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Role__3213E83F52181829");

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

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Student__3213E83F0AC08342");

            entity.ToTable("Student");

            entity.HasIndex(e => e.StudentCode, "UQ__Student__6DF33C4543C7E3E1").IsUnique();

            entity.Property(e => e.Id)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("id");
            entity.Property(e => e.Address)
                .HasColumnType("text")
                .HasColumnName("address");
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.Gender)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("gender");
            entity.Property(e => e.Major)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("major");
            entity.Property(e => e.Phone)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("phone");
            entity.Property(e => e.StudentCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("student_code");

            entity.HasOne(d => d.IdNavigation).WithOne(p => p.Student)
                .HasForeignKey<Student>(d => d.Id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Student__id__3E52440B");
        });

        modelBuilder.Entity<StudentInClass>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Student___3213E83F234A8679");

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
                .HasConstraintName("FK__Student_I__class__5070F446");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentInClasses)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK__Student_I__stude__5165187F");
        });

        modelBuilder.Entity<StudentLabAssignment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Student___3213E83FC99EDFF9");

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
                .HasConstraintName("FK__Student_L__assig__5441852A");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentLabAssignments)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK__Student_L__stude__5535A963");
        });

        modelBuilder.Entity<Submission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Submissi__3213E83FBD06EED7");

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
                .HasConstraintName("FK__Submissio__assig__5CD6CB2B");

            entity.HasOne(d => d.Student).WithMany(p => p.Submissions)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK__Submissio__stude__5BE2A6F2");

            entity.HasOne(d => d.ZipCodeNavigation).WithMany(p => p.Submissions)
                .HasForeignKey(d => d.ZipCode)
                .HasConstraintName("FK__Submissio__zip_c__5DCAEF64");
        });

        modelBuilder.Entity<Teacher>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Teacher__3213E83FDFF4E004");

            entity.ToTable("Teacher");

            entity.HasIndex(e => e.TeacherCode, "UQ__Teacher__90D00E1DCB3AEEB3").IsUnique();

            entity.Property(e => e.Id)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("id");
            entity.Property(e => e.AcademicDegree)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("academic_degree");
            entity.Property(e => e.AcademicTitle)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("academic_title");
            entity.Property(e => e.Address)
                .HasColumnType("text")
                .HasColumnName("address");
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.Gender)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("gender");
            entity.Property(e => e.Phone)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("phone");
            entity.Property(e => e.TeacherCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("teacher_code");

            entity.HasOne(d => d.IdNavigation).WithOne(p => p.Teacher)
                .HasForeignKey<Teacher>(d => d.Id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Teacher__id__4222D4EF");
        });

        modelBuilder.Entity<TestCase>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TestCase__3213E83FC2199192");

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
			entity.Property(e => e.Input)
				.HasColumnType("text")
				.HasColumnName("input");
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
                .HasConstraintName("FK__TestCase__assign__6477ECF3");
        });

        modelBuilder.Entity<TestCaseResult>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TestCase__3213E83FCFF27B6B");

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
                .HasConstraintName("FK__TestCaseR__submi__6754599E");

            entity.HasOne(d => d.TestCase).WithMany(p => p.TestCaseResults)
                .HasForeignKey(d => d.TestCaseId)
                .HasConstraintName("FK__TestCaseR__test___68487DD7");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__User__3213E83F18AA5A16");

            entity.ToTable("User");

            entity.HasIndex(e => e.Email, "UQ__User__AB6E61648B07809B").IsUnique();

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
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password");
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
            entity.Property(e => e.UserName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("user_name");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK__User__role_id__3A81B327");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
