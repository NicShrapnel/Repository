using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityFramework_Repository.Exceptions
{
    public class Repository_ContextDisposeException : Exception
    {
        public Repository_ContextDisposeException(string message) : base(message) { }
    }

}
