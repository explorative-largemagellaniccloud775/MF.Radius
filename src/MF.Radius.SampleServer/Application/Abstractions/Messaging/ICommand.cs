namespace MF.Radius.SampleServer.Application.Abstractions.Messaging;

/// <summary>
/// Represents a command request with an expected result.
/// </summary>
/// <typeparam name="TResult">
/// The type of result expected from the command execution.
/// </typeparam>
public interface ICommand<out TResult>
{
    
}
