﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BackSide2.DAO.Entities
{
    public class Person : BaseEntity
    {
        [Key] public new long Id { get; set; }
        [DefaultValue(null)]
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public Role Role { get; set; }
        [DefaultValue(null)]
        public string FirstName { get; set; }
        [DefaultValue(null)]
        public string Surname { get; set; }
        [DefaultValue(null)]
        public bool? Gender { get; set; }
        [DefaultValue(null)]
        public uint? Language { get; set; }
    }

    public enum Role
    {
        Admin = 1,
        User = 2
    }
}