
using System;
public class ContractAssertions {
            public static void Requires<TException>( bool Predicate, string _Message )            
            where TException : Exception, new()
            {
            if ( !Predicate )
         {      Exception e = (TException)Activator.CreateInstance(typeof(TException), new object[] { _Message });;
                throw new TException();
            }
        }

}