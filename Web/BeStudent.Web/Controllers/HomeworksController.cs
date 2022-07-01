﻿namespace BeStudent.Web.Controllers
{
    using System;
    using System.Security.Claims;
    using System.Threading.Tasks;

    using BeStudent.Services.Data;
    using BeStudent.Web.ViewModels.File;
    using BeStudent.Web.ViewModels.Grade;
    using BeStudent.Web.ViewModels.Homework;
    using BeStudent.Web.ViewModels.SendFile;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    public class HomeworksController : BaseController
    {
        private readonly IThemesService themesService;
        private readonly IHomeworksService homeworksService;

        public HomeworksController(
            IThemesService themesService,
            IHomeworksService homeworksService)
        {
            this.themesService = themesService;
            this.homeworksService = homeworksService;
        }

        [Authorize(Roles = "User")]
        [HttpGet("Subjects/{subjectName}/Homeworks/{homeworkId}/Send")]
        public IActionResult Send(string subjectName, [FromQuery] DateTime deadline)
        {
            var now = DateTime.Now;
            if (deadline < now)
            {
                this.TempData["message"] = "The time to send file for this homework has expired!";
                return this.RedirectToAction("Themes", "Subjects", new { subjectName });
            }

            return this.View();
        }

        [Authorize(Roles = "User")]
        [HttpPost("Subjects/{subjectName}/Homeworks/{homeworkId}/Send")]
        public async Task<IActionResult> Send(string subjectName, int homeworkId, HomeworkSendInputModel input)
        {
            if (!this.ModelState.IsValid)
            {
                return this.View(input);
            }

            var fileUri = string.Empty;
            if (input.File != null)
            {
                fileUri = await this.themesService
                    .UploadFileToCloudinary(input.File.FileName, input.File.OpenReadStream());
            }

            var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
            await this.homeworksService.SendAsync(userId, homeworkId, fileUri, input.FileDescription);

            return this.RedirectToAction("Themes", "Subjects", new { subjectName });
        }

        [Authorize(Roles = "Lector")]
        [HttpGet("Subjects/{subjectName}/Homeworks/{homeworkId}/Sended")]
        public async Task<IActionResult> Sended(string subjectName, int homeworkId)
        {
            var viewModel = new SendFilesListViewModel
            {
                HomeworkId = homeworkId,
                SubjectName = subjectName,
                SendFiles = await this.homeworksService.GetAllSendedFiles<SendFileViewModel>(homeworkId),
            };

            return this.View(viewModel);
        }

        [Authorize(Roles = "Lector")]
        [HttpGet("Subjects/{subjectName}/Homeworks/Create")]
        public IActionResult Create()
        {
            return this.View();
        }

        [Authorize(Roles = "Lector")]
        [HttpPost("Subjects/{subjectName}/Homeworks/Create")]
        public async Task<IActionResult> Create(string subjectName, HomeworkCreateInputModel input)
        {
            if (!this.ModelState.IsValid)
            {
                return this.View(input);
            }

            var fileUri = string.Empty;
            if (input.File != null)
            {
                fileUri = await this.themesService
                    .UploadFileToCloudinary(input.File.FileName, input.File.OpenReadStream());
                if (input.FileDescription == null)
                {
                    input.FileDescription = "document";
                }
            }

            await this.homeworksService.CreateAsync(subjectName, input.Title, input.Description, fileUri, input.FileDescription, input.Deadline);

            return this.RedirectToAction("Themes", "Subjects", new { subjectName });
        }

        [Authorize(Roles = "Lector")]
        [HttpGet("Subjects/{subjectName}/Homeworks/{homeworkId}/AddNewFile")]
        public IActionResult AddNewFile(string subjectName, int homeworkId)
        {
            var model = new AddNewFileInputModel
            {
                SubjectName = subjectName,
                HomeworkId = homeworkId,
            };

            return this.View(model);
        }

        [Authorize(Roles = "Lector")]
        [HttpGet("Subjects/{subjectName}/Homeworks/{homeworkId}/AddNewFile")]
        public async Task<IActionResult> AddNewFile(string subjectName, int homeworkId, AddNewFileInputModel input)
        {
            if (!this.ModelState.IsValid)
            {
                return this.View(input);
            }

            var fileUri = string.Empty;
            if (input.File != null)
            {
                fileUri = await this.themesService
                    .UploadFileToCloudinary(input.File.FileName, input.File.OpenReadStream());
                if (input.FileDescription == null)
                {
                    input.FileDescription = "document";
                }
            }

            await this.homeworksService.AddNewFileAsync(homeworkId, fileUri, input.FileDescription);

            return this.RedirectToAction("Themes", "Subjects", new { subjectName });
        }

        [Authorize(Roles = "Lector")]
        [HttpGet("Subjects/{subjectName}/Homeworks/{homeworkId}/SetGrade")]
        public IActionResult SetGrade(string subjectName, int homeworkId, [FromQuery] string studentId, int sendFileId)
        {
            var gradeModel = new GradeSetInputModel
            {
                StudentId = studentId,
                HomeworkId = homeworkId,
                SendFileId = sendFileId,
                SubjectName = subjectName,
            };

            return this.View(gradeModel);
        }

        [Authorize(Roles = "Lector")]
        [HttpPost("Subjects/{subjectName}/Homeworks/{homeworkId}/SetGrade")]
        public async Task<IActionResult> SetGrade(string subjectName, int homeworkId, string studentId, int sendFileId, GradeSetInputModel input)
        {
            if (!this.ModelState.IsValid)
            {
                return this.View(input);
            }

            await this.homeworksService.SetGradeAsync(input.Mark, input.Description, homeworkId, studentId, sendFileId);

            return this.RedirectToAction("Sended", "Homeworks", new { subjectName, homeworkId });
        }
    }
}
