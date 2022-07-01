﻿namespace BeStudent.Web.ViewModels.Exam
{
    using System;
    using System.Collections.Generic;

    using BeStudent.Data.Models;
    using BeStudent.Services.Mapping;
    using BeStudent.Web.ViewModels.File;
    using BeStudent.Web.ViewModels.SendFile;
    using Ganss.XSS;

    public class ExamViewModel : IMapFrom<Exam>
    {
        public int Id { get; set; }

        public string SubjectName { get; set; }

        public string Title { get; set; }

        public string Type { get; set; }

        public string Description { get; set; }

        public string SanitizedDescription => new HtmlSanitizer().Sanitize(this.Description);

        public DateTime? Open { get; set; }

        public DateTime? Close { get; set; }

        public IEnumerable<FileViewModel> Files { get; set; }

        public IEnumerable<SendFileViewModel> SendFiles { get; set; }

        public IEnumerable<OnlineTestStartViewModel> OnlineTests { get; set; }
    }
}
