using System;
using System.Collections.Generic;
using System.Text;

namespace RavenDB.Samples.Verity.App.Models
{
    public class User
    {
        public string Id { get; set; } = null!;
        public string CompanyId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Surname { get; set; } = null!;
        public string Email { get; set; } = null!;
    }
}
