namespace TradingJournal.ViewModels;

public sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : CommandBase
{
    public override bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

    public override void Execute(object? parameter) => execute();
}

public sealed class RelayCommand<T>(Action<T?> execute, Predicate<T?>? canExecute = null) : CommandBase
{
    public override bool CanExecute(object? parameter) =>
        canExecute?.Invoke(ConvertParameter(parameter)) ?? true;

    public override void Execute(object? parameter) => execute(ConvertParameter(parameter));

    private static T? ConvertParameter(object? parameter)
    {
        if (parameter is null)
        {
            return default;
        }

        if (parameter is T typedValue)
        {
            return typedValue;
        }

        throw new InvalidOperationException(
            $"Expected a command parameter of type {typeof(T).Name} but received {parameter.GetType().Name}.");
    }
}
