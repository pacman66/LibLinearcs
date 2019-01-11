
#pragma warning disable 1574
using System;

///
/// <summary>
/// <c>ContractAssertions</c> is a simple set of utilities to provide Contract-Like assertions in c# dotnet core 
/// </summary>
public class ContractAssertions {

    #pragma warning disable 1574
    /// <summary>
    /// For Preconditions: If <c>predicate</c> is false then throw an exception of type <c>TException</c> with provide constructor parameters <c>args</c>
    /// </summary>
    /// <typeparam name="TException">A type that inherits from the Exception base class.</typeparam>
    /// <param name="predicate">A boolean value</param>
    /// <param name="args">Arguments to be provided to <c>TException</c> constructor.</param>
    /// <exception cref="TException">Thrown when  <c>predicate</c> is false.</exception>
    /// <exception cref="System.Reflection.TargetInvocationException">The constructor being called throws as exception.</exception>
    /// <exception cref="MethodAccessException">The caller does not have permission to call this constructor.</exception>
    /// <exception cref="MissingMethodException">No matching public constructor was found.</exception>    
    public static void Requires<TException>( bool predicate, params Object[] args )            
    where TException : Exception, new()
    {
        if ( !predicate ) throw (TException)Activator.CreateInstance(typeof(TException), args);
    }

    /// <summary>
    /// For Post conditions: If <c>predicate</c> is false then throw an exception of type <c>TException</c> with provide constructor parameters <c>args</c>
    /// </summary>
    /// <typeparam name="TException">A type that inherits from the Exception base class.</typeparam>
    /// <param name="predicate">A boolean value</param>
    /// <param name="args">Arguments to be provided to <c>TException</c> constructor.</param>
    /// <exception cref="TException">Thrown when  <c>predicate</c> is false.</exception>
    /// <exception cref="System.Reflection.TargetInvocationException">The constructor being called throws as exception.</exception>
    /// <exception cref="MethodAccessException">The caller does not have permission to call this constructor.</exception>
    /// <exception cref="MissingMethodException">No matching public constructor was found.</exception>    
    public static void Ensures<TException>( bool predicate, params Object[] args )            
    where TException : Exception, new()
    {
        if ( !predicate ) throw (TException)Activator.CreateInstance(typeof(TException), args);
    }


    
    //  Warning 1574 -  cref for TException
    #pragma warning restore 1574

}

