using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabAssistantOPP_LAO.DTO.DTOs
{
    public class LabAssignmentDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tiêu đề không được để trống.")]
        [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Mô tả không được để trống.")]
        public string Description { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "LOC phải lớn hơn 0.")]
        public int? LocTotal { get; set; }

        [Required(ErrorMessage = "Giáo viên tạo đề bài không được để trống.")]
        public int TeacherId { get; set; }

        [Required(ErrorMessage = "Trạng thái không được để trống.")]
        public string Status { get; set; }

		public List<int>? ClassIds { get; set; }

		public IFormFile? File { get; set; }
	}

    public class CreateLabAssignmentDto
    {

        [Required(ErrorMessage = "Tiêu đề không được để trống.")]
        [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Mô tả không được để trống.")]
        public string Description { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "LOC phải lớn hơn 0.")]
        public int? LocTotal { get; set; }

        [Required(ErrorMessage = "Giáo viên tạo đề bài không được để trống.")]
        public int TeacherId { get; set; }

        [Required(ErrorMessage = "Trạng thái không được để trống.")]
        public string Status { get; set; }

		public List<int>? ClassIds { get; set; }

		public IFormFile? File { get; set; }
	}
}
