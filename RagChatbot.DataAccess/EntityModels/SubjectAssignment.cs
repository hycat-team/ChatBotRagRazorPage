namespace RagChatbot.DataAccess.EntityModels
{
    public class SubjectAssignment
    {
        public int SubjectId { get; set; }
        public Subject Subject { get; set; } = null!;

        public int LecturerId { get; set; }
        public AppUser Lecturer { get; set; } = null!;
    }
}
