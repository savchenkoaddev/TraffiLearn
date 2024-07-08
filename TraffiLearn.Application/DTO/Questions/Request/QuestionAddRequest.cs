﻿using System.ComponentModel.DataAnnotations;

namespace TraffiLearn.Application.DTO.Questions.Request
{
    public sealed class QuestionAddRequest
    {
        [Required]
        [StringLength(500)]
        public string? Text { get; set; }

        [Required]
        public List<string>? PossibleAnswears { get; set; }

        [Required]
        public List<string>? CorrectAnswears { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int? TicketNumber { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int? QuestionNumber { get; set; }
    }
}