using System;
using System.Collections.Generic;
using System.Text;

namespace ExploreCsharpEight
{
    #region Nullable_PersonDefinition
    #nullable enable
    internal class Person
    {
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }

        public Person(string first, string last) =>
            (FirstName, LastName) = (first, last);

        public Person(string first, string middle, string last) =>
            (FirstName, MiddleName, LastName) = (first, middle, last);

        public override string ToString() => $"{FirstName} {MiddleName} {LastName}";
    }
    #nullable restore
    #endregion

    class NullableReferences
    {

        #region Nullable_GetLengthMethod
        #nullable enable
        private static int GetLengthOfMiddleName(Person p)
        {
                string middleName = p.MiddleName;
                return middleName.Length;
        }
        #nullable restore
        #endregion

        internal int NullableTestBed()
        {
            #region Nullable_Usage
            #nullable enable
            Person miguel = new Person("Miguel", "de Icaza");
            var length = GetLengthOfMiddleName(miguel);
            Console.WriteLine(length);
            #nullable restore
            //Was this tested on a person who doesn't have a middle name?
            #endregion
            return 0;
        }
    }
}
