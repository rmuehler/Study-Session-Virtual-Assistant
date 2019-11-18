using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VirtualAssistant
{
    public class UserProfile
    {

        public int Id { get; set; }
        public string Name { get; set; }

        public string Email { get; set; }

        public int Phonenumber { get; set; }

        public string Date { get; set; }
        public User self { get; set; }

    }
}