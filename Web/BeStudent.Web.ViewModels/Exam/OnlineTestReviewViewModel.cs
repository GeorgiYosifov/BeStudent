namespace BeStudent.Web.ViewModels.Exam
{
    using System.Collections.Generic;

    using BeStudent.Data.Models;
    using BeStudent.Services.Mapping;
    using BeStudent.Web.ViewModels.Student;

    public class OnlineTestReviewViewModel : IMapFrom<OnlineTest>
    {
        public int Id { get; set; }

        public StudentViewModel Student { get; set; }

        public IEnumerable<StudentViewModel> Students { get; set; }

        public IEnumerable<QuestionViewModel> Questions { get; set; }

        public IList<SetPointsInputModel> SetPoints { get; set; }

        public bool HasOpenAnswers { get; set; }
    }
}
