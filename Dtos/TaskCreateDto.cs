using System.ComponentModel.DataAnnotations;

namespace TaskManager.Dtos
{
    public class TaskCreateDto
    {
        [Required]
        public string Title { get; set; }

        public string? Description { get; set; }

        public bool IsCompleted { get; set; }
    }
}
