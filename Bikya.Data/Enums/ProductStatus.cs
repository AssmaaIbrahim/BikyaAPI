using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.Data.Enums
{
    public enum ProductStatus
    {
        Available,   
        InProcess,   
        Trading,     // exchanged
        Traded,      // traded (for backward compatibility)
        Sold         // sold
    }
}
