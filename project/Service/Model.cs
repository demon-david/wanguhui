using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Service
{
    public class User
    {
        public Guid Id { get; set; }

        public Int32 Score { get; set; }

        public Boolean isMatching { get; set; }

        public Boolean isFighting { get; set; }
    }
}
