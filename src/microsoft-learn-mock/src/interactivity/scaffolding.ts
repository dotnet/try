export const scaffoldingMethod = `using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Program
{
  class Program
  {
    static void Main(string[] args)
    {
      #region controller
____
      #endregion
    }
  }
}`;

export const scaffoldingClass = `using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Program
{
  class Program
  {
    #region controller
____
    #endregion
  }
}`;

export function scaffoldCode(code: string, scaffoldingType: string): string {
    switch (scaffoldingType) {
        case 'try-dotnet-class':
            code = scaffoldingClass.replace('____', () => code);
            break;
        case 'try-dotnet-method':
            code = scaffoldingMethod.replace('____', () => code);
            break;
        case 'try-dotnet':
            break;
        default:
            code = scaffoldingMethod.replace('____', () => code);
            break;
    }
    return code;
}

export const scaffoldContainsRegion = ['try-dotnet-class', 'try-dotnet-method'];
