namespace Ukubola.Persistence
{
    /// <summary>Resolve actor references only after every entity has its saved state.</summary>
    public interface IEntityRestoreComplete { void FinishRestore(); }
}
