﻿namespace BeStudent.Data.Models
{
    using System;
    using System.Collections.Generic;

    using BeStudent.Data.Common.Models;

    public class Chat : BaseDeletableModel<string>
    {
        public Chat()
        {
            this.Id = Guid.NewGuid().ToString();
            this.Messages = new HashSet<Message>();
            this.Users = new HashSet<ApplicationUser>();
        }

        public int SemesterId { get; set; }

        public virtual Semester Semester { get; set; }

        public virtual ICollection<Message> Messages { get; set; }

        public virtual ICollection<ApplicationUser> Users { get; set; }
    }
}
