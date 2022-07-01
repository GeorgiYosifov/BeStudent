﻿namespace BeStudent.Services.Data
{
    using System.Linq;
    using System.Threading.Tasks;

    using BeStudent.Data.Common.Repositories;
    using BeStudent.Data.Models;
    using BeStudent.Services.Mapping;
    using Microsoft.EntityFrameworkCore;

    public class SemestersService : ISemestersService
    {
        private readonly IDeletableEntityRepository<Semester> semesterRepository;
        private readonly IDeletableEntityRepository<ApplicationUser> userRepository;
        private readonly IDeletableEntityRepository<StudentSubject> userSubjectRepository;
        private readonly IDeletableEntityRepository<StudentSemester> userSemesterRepository;
        private readonly IDeletableEntityRepository<Chat> chatRepository;

        public SemestersService(
            IDeletableEntityRepository<Semester> semesterRepository,
            IDeletableEntityRepository<ApplicationUser> userRepository,
            IDeletableEntityRepository<StudentSubject> userSubjectRepository,
            IDeletableEntityRepository<StudentSemester> userSemesterRepository,
            IDeletableEntityRepository<Chat> chatRepository)
        {
            this.semesterRepository = semesterRepository;
            this.userRepository = userRepository;
            this.userSubjectRepository = userSubjectRepository;
            this.userSemesterRepository = userSemesterRepository;
            this.chatRepository = chatRepository;
        }

        public async Task<bool> AddLectorAsync(int subjectId, int semesterId, string email)
        {
            var lector = this.userRepository
                .All()
                .FirstOrDefault(u => u.Email == email && u.Role == "Lector");

            if (lector == null)
            {
                return false;
            }

            var userSubject = new StudentSubject
            {
                StudentId = lector.Id,
                SubjectId = subjectId,
            };
            await this.userSubjectRepository.AddAsync(userSubject);
            await this.userSubjectRepository.SaveChangesAsync();

            var userSemester = new StudentSemester
            {
                StudentId = lector.Id,
                SemesterId = semesterId,
            };
            await this.userSemesterRepository.AddAsync(userSemester);
            await this.userSemesterRepository.SaveChangesAsync();

            return true;
        }

        public async Task CreateAsync(int number, int year, string courseName, int courseId)
        {
            var semester = new Semester
            {
                Number = number,
                Year = year,
                CourseName = courseName,
                CourseId = courseId,
            };

            await this.semesterRepository.AddAsync(semester);
            await this.semesterRepository.SaveChangesAsync();

            var chat = new Chat
            {
                SemesterId = semester.Id,
            };

            await this.chatRepository.AddAsync(chat);
            await this.chatRepository.SaveChangesAsync();
        }

        public async Task<T> GetDetails<T>(int id)
        {
            return await this.semesterRepository
                .All()
                .Where(s => s.Id == id)
                .To<T>()
                .FirstOrDefaultAsync();
        }
    }
}
