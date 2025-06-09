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
            entity.HasKey(e => e.Id).HasName("PK__Class__3213E83F4579C025");

            entity.ToTable("Class");

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("created_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.LocToPass).HasColumnName("loc_to_pass");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.Subject)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("subject");
            entity.Property(e => e.TeacherId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("teacher_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Teacher).WithMany(p => p.Classes)
                .HasForeignKey(d => d.TeacherId)
                .HasConstraintName("FK_Class_Teacher");
        });

        modelBuilder.Entity<ClassHasLabAssignment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Class_Ha__3213E83FF774265E");

            entity.ToTable("Class_Has_Lab_Assignment");

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("id");
            entity.Property(e => e.AssignmentId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("assignment_id");
            entity.Property(e => e.ClassId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("class_id");

            entity.HasOne(d => d.Assignment).WithMany(p => p.ClassHasLabAssignments)
                .HasForeignKey(d => d.AssignmentId)
                .HasConstraintName("FK_CHLA_Assignment");

            entity.HasOne(d => d.Class).WithMany(p => p.ClassHasLabAssignments)
                .HasForeignKey(d => d.ClassId)
                .HasConstraintName("FK_CHLA_Class");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Feedback__3213E83FD075EC0B");

            entity.ToTable("Feedback");

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("id");
            entity.Property(e => e.Comment)
                .HasColumnType("text")
                .HasColumnName("comment");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.SubmissionId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("submission_id");
            entity.Property(e => e.TeacherId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("teacher_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Submission).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.SubmissionId)
                .HasConstraintName("FK_Feedback_Submission");

            entity.HasOne(d => d.Teacher).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.TeacherId)
                .HasConstraintName("FK_Feedback_Teacher");
        });

        modelBuilder.Entity<LabAssignment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Lab_Assi__3213E83F020BE640");

            entity.ToTable("Lab_Assignment");

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("created_by");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.LocTotal).HasColumnName("loc_total");
            entity.Property(e => e.TeacherId)
                .HasMaxLength(50)
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
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Teacher).WithMany(p => p.LabAssignments)
                .HasForeignKey(d => d.TeacherId)
                .HasConstraintName("FK_LabAssignment_Teacher");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Role__3213E83F8D83EB35");

            entity.ToTable("Role");

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("id");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("name");
        });

        modelBuilder.Entity<StudentInClass>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Student___3213E83F8B09D01D");

            entity.ToTable("Student_In_Class");

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("id");
            entity.Property(e => e.ClassId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("class_id");
            entity.Property(e => e.StudentId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("student_id");

            entity.HasOne(d => d.Class).WithMany(p => p.StudentInClasses)
                .HasForeignKey(d => d.ClassId)
                .HasConstraintName("FK_SIC_Class");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentInClasses)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK_SIC_Student");
        });

        modelBuilder.Entity<StudentLabAssignment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Student___3213E83FCA7296B2");

            entity.ToTable("Student_Lab_Assignment");

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("id");
            entity.Property(e => e.AssignmentId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("assignment_id");
            entity.Property(e => e.StudentId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("student_id");

            entity.HasOne(d => d.Assignment).WithMany(p => p.StudentLabAssignments)
                .HasForeignKey(d => d.AssignmentId)
                .HasConstraintName("FK_SLA_Assignment");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentLabAssignments)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK_SLA_Student");
        });

        modelBuilder.Entity<Submission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Submissi__3213E83FBA6885C7");

            entity.ToTable("Submission");

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("id");
            entity.Property(e => e.AssignmentId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("assignment_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("created_by");
            entity.Property(e => e.LocResult).HasColumnName("loc_result");
            entity.Property(e => e.ManualReason)
                .HasColumnType("text")
                .HasColumnName("manual_reason");
            entity.Property(e => e.ManuallyEdited).HasColumnName("manually_edited");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.StudentId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("student_id");
            entity.Property(e => e.SubmittedAt)
                .HasColumnType("datetime")
                .HasColumnName("submitted_at");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("updated_by");
            entity.Property(e => e.ZipCode).HasColumnName("zip_code");

            entity.HasOne(d => d.Assignment).WithMany(p => p.Submissions)
                .HasForeignKey(d => d.AssignmentId)
                .HasConstraintName("FK_Sub_Assignment");

            entity.HasOne(d => d.Student).WithMany(p => p.Submissions)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK_Sub_Student");
        });

        modelBuilder.Entity<TestCase>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TestCase__3213E83FCCFB249E");

            entity.ToTable("TestCase");

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("id");
            entity.Property(e => e.AssignmentId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("assignment_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
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
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Assignment).WithMany(p => p.TestCases)
                .HasForeignKey(d => d.AssignmentId)
                .HasConstraintName("FK_TestCase_Assignment");
        });

        modelBuilder.Entity<TestCaseResult>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TestCase__3213E83F06B66727");

            entity.ToTable("TestCaseResult");

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("id");
            entity.Property(e => e.ActualOutput)
                .HasColumnType("text")
                .HasColumnName("actual_output");
            entity.Property(e => e.IsPassed).HasColumnName("is_passed");
            entity.Property(e => e.SubmissionId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("submission_id");
            entity.Property(e => e.TestCaseId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("test_case_id");

            entity.HasOne(d => d.Submission).WithMany(p => p.TestCaseResults)
                .HasForeignKey(d => d.SubmissionId)
                .HasConstraintName("FK_TCR_Submission");

            entity.HasOne(d => d.TestCase).WithMany(p => p.TestCaseResults)
                .HasForeignKey(d => d.TestCaseId)
                .HasConstraintName("FK_TCR_TestCase");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__User__3213E83F8E1A45AA");

            entity.ToTable("User");

            entity.HasIndex(e => e.Email, "UQ__User__AB6E61644F0C51E0").IsUnique();

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("created_by");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.RoleId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("role_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_User_Role");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
