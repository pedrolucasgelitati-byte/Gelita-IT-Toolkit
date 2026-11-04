namespace GelitaITToolkit.Models
{
    internal enum OperationOutcome { NotStarted, Completed, Warning, Failed, Canceled }

    internal sealed class OperationState
    {
        public bool HasWork { get; private set; }
        public bool HasWarning { get; private set; }
        public bool HasError { get; private set; }
        public bool Canceled { get; private set; }
        public void Started() => HasWork = true;
        public void Warn() => HasWarning = true;
        public void Fail() => HasError = true;
        public void Cancel() => Canceled = true;
        public OperationOutcome Outcome => HasError ? OperationOutcome.Failed
            : Canceled ? OperationOutcome.Canceled : HasWarning ? OperationOutcome.Warning
            : HasWork ? OperationOutcome.Completed : OperationOutcome.NotStarted;
    }
}
