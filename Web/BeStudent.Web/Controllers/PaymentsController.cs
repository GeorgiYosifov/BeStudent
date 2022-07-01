﻿namespace BeStudent.Web.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;

    using BeStudent.Services.Data;
    using BeStudent.Web.ViewModels.Payment;
    using BeStudent.Web.ViewModels.Semester;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using PayPal.Core;
    using PayPal.v1.Payments;

    public class PaymentsController : BaseController
    {
        private readonly IConfiguration configuration;
        private readonly IPaymentsService paymentsService;
        private readonly IGradesService gradesService;

        public PaymentsController(
            IConfiguration configuration,
            IPaymentsService paymentsService,
            IGradesService gradesService)
        {
            this.configuration = configuration;
            this.paymentsService = paymentsService;
            this.gradesService = gradesService;
        }

        [Authorize(Roles = "User")]
        public async Task<IActionResult> ShowNewSemester()
        {
            var now = DateTime.Now;
            var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await this.paymentsService.GetUser<PaymentUserViewModel>(userId);
            var nextSemester = 0;

            if (user.SemesterNumber == 0)
            {
                nextSemester = user.SemesterNumber + 1;
            }
            else
            {
                var semesterId = user.StudentSemesters.LastOrDefault().SemesterId;

                var model = await this.gradesService.GetAll<SemesterForGradesViewModel>(semesterId);

                var studentSubjects = model.Subjects
                    .Where(s => s.StudentSubjects.FirstOrDefault(x => x.StudentId == userId) != null)
                    .ToList();
                model.Subjects = studentSubjects;

                var examGrades = new List<double>();
                var homeworksGrades = new List<double>();
                foreach (var subject in model.Subjects)
                {
                    var innerGradeForExams = 0.0;
                    foreach (var exam in subject.Exams)
                    {
                        var grade = exam.Grades
                            .FirstOrDefault(g => g.StudentId == userId && g.ExamId == exam.Id);
                        if (grade == null)
                        {
                            this.TempData["message"] = "You must have grade on each exam!";
                            return this.RedirectToAction("Index", "Home");
                        }

                        innerGradeForExams += grade.Mark;
                    }

                    examGrades.Add(innerGradeForExams / subject.Exams.Count());

                    var innerGradeForHomeworks = 0.0;
                    foreach (var homework in subject.Homeworks)
                    {
                        var grade = homework.Grades
                            .FirstOrDefault(h => h.StudentId == userId && h.HomeworkId == homework.Id);
                        if (grade == null)
                        {
                            innerGradeForHomeworks += 2;
                        }
                        else
                        {
                            innerGradeForHomeworks += grade.Mark;
                        }
                    }

                    homeworksGrades.Add(innerGradeForHomeworks / subject.Homeworks.Count());
                }

                var gradeToPass = examGrades.Average();
                if (homeworksGrades.Average() >= 4)
                {
                    gradeToPass++;
                }

                if (gradeToPass < 3)
                {
                    this.TempData["message"] = "Your average grade is lower than 3! You should have more than 3 to pass this semester.";
                    return this.RedirectToAction("Index", "Home");
                }

                nextSemester = user.SemesterNumber + 1;
            }

            var semester = await this.paymentsService
                .GetSemester<PaymentSemesterViewModel>(user.CourseName, nextSemester, now.Year);
            if (semester == null)
            {
                this.TempData["message"] = "Your course dont have more semesters than these up to now.";
                return this.RedirectToAction("Index", "Home");
            }

            var price = 0.0m;
            foreach (var subject in semester.Subjects)
            {
                price += subject.Price;
            }

            var paymentAttemptId = await this.paymentsService.CreatePaymentAttemptAsync(userId, semester.Id, price);

            var viewModel = new PaymentCourseViewModel
            {
                Name = user.CourseName,
                Price = price,
                PaymentAttemptId = paymentAttemptId,
                Semester = semester,
            };

            return this.View(viewModel);
        }

        [Authorize(Roles = "User")]
        public async Task<IActionResult> Pay([FromQuery] string attemptId)
        {
            var attempt = await this.paymentsService.GetPaymentAttempt(attemptId);

            var clientId = this.configuration.GetSection("PayPal").GetSection("clientId").Value;
            var secret = this.configuration.GetSection("PayPal").GetSection("secret").Value;

            var environment = new SandboxEnvironment(clientId, secret);
            var client = new PayPalHttpClient(environment);

            var portocol = this.HttpContext.Request.Scheme;
            var host = this.HttpContext.Request.Host;

            var returnUrl = $"{portocol}://{host}/Payments/Execute/{attempt.Id}";
            var cancelUrl = $"{portocol}://{host}/Homes/Index";

            var payment = new PayPal.v1.Payments.Payment()
            {
                Intent = "sale",
                Transactions = new List<Transaction>()
                {
                    new Transaction()
                    {
                        Amount = new Amount()
                        {
                            Total = attempt.Price.ToString("G", CultureInfo.InvariantCulture),
                            Currency = "USD",
                        },
                    },
                },
                RedirectUrls = new RedirectUrls()
                {
                    ReturnUrl = returnUrl,
                    CancelUrl = cancelUrl,
                },
                Payer = new Payer()
                {
                    PaymentMethod = "paypal",
                },
            };

            PaymentCreateRequest request = new PaymentCreateRequest();
            request.RequestBody(payment);

            System.Net.HttpStatusCode statusCode;

            try
            {
                BraintreeHttp.HttpResponse response = await client.Execute(request);
                statusCode = response.StatusCode;
                Payment result = response.Result<Payment>();

                string redirectUrl = null;
                foreach (LinkDescriptionObject link in result.Links)
                {
                    if (link.Rel.Equals("approval_url"))
                    {
                        redirectUrl = link.Href;
                    }
                }

                if (redirectUrl == null)
                {
                    return this.Json("Failed to find an approval_url in the response!");
                }
                else
                {
                    return this.Redirect(redirectUrl);
                }
            }
            catch (BraintreeHttp.HttpException ex)
            {
                statusCode = ex.StatusCode;
                var debugId = ex.Headers.GetValues("PayPal-Debug-Id").FirstOrDefault();
                return this.Json("Request failed!  HTTP response code was " + statusCode + ", debug ID was " + debugId);
            }
        }

        [Authorize(Roles = "User")]
        [HttpGet("Payments/Execute/{attemptId}")]
        public async Task<IActionResult> Execute(string attemptId, [FromQuery] string payerID, string paymentId)
        {
            var clientId = this.configuration.GetSection("PayPal").GetSection("clientId").Value;
            var secret = this.configuration.GetSection("PayPal").GetSection("secret").Value;

            var environment = new SandboxEnvironment(clientId, secret);
            var client = new PayPalHttpClient(environment);

            PaymentExecuteRequest request = new PaymentExecuteRequest(paymentId);
            request.RequestBody(new PaymentExecution()
            {
                PayerId = payerID,
            });

            var response = await client.Execute<PaymentExecuteRequest>(request);
            var statusCode = response.StatusCode;

            if (statusCode.ToString() == "OK")
            {
                var attempt = await this.paymentsService.GetPaymentAttempt(attemptId);

                var userId = attempt.StudentId;
                var semesterId = attempt.SemesterId;
                var semester = await this.paymentsService
                    .GetSemester<PaymentSemesterViewModel>(semesterId);

                await this.paymentsService.RegisterUserToSemesterAsync(userId, semesterId);
                foreach (var subject in semester.Subjects)
                {
                    await this.paymentsService.RegisterUserToSubjectAsync(userId, subject.Id);
                }

                await this.paymentsService.CreatePaymentAsync(userId, semesterId);

                this.TempData["message"] = $"Payment status code {statusCode}";
                return this.RedirectToAction("Index", "Home");
            }
            else
            {
                // Delete paymentAttmept
                this.TempData["message"] = $"Payment status code {statusCode}";
                return this.RedirectToAction("Index", "Home");
            }
        }
    }
}
